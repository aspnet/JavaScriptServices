// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.NodeServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System.Threading;
using Microsoft.AspNetCore.SpaServices.Proxy;

namespace Microsoft.AspNetCore.SpaServices.AngularCli
{
    internal class AngularCliMiddleware
    {
        private const string _middlewareResourceName = "/Content/Node/angular-cli-middleware.js";

        private readonly INodeServices _nodeServices;
        private readonly string _middlewareScriptPath;

        public AngularCliMiddleware(ISpaBuilder spaBuilder, string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new ArgumentException("Cannot be null or empty", nameof(sourcePath));
            }

            // Prepare to make calls into Node
            var appBuilder = spaBuilder.AppBuilder;
            _nodeServices = CreateNodeServicesInstance(appBuilder, sourcePath);
            _middlewareScriptPath = GetAngularCliMiddlewareScriptPath(appBuilder);

            // Start Angular CLI and attach to middleware pipeline
            var angularCliServerInfoTask = StartAngularCliServerAsync();
            spaBuilder.AddStartupTask(angularCliServerInfoTask);

            // Proxy the corresponding requests through ASP.NET and into the Node listener
            // Anything under /<publicpath> (e.g., /dist) is proxied as a normal HTTP request
            // with a typical timeout (100s is the default from HttpClient).
            UseProxyToLocalAngularCliMiddleware(appBuilder, spaBuilder.PublicPath,
                angularCliServerInfoTask, TimeSpan.FromSeconds(100));

            // Advertise the availability of this feature to other SPA middleware
            spaBuilder.Properties.Add(this, null);
        }

        public Task StartAngularCliBuilderAsync(string cliAppName)
        {
            return _nodeServices.InvokeExportAsync<AngularCliServerInfo>(
                _middlewareScriptPath,
                "startAngularCliBuilder",
                cliAppName);
        }

        private static INodeServices CreateNodeServicesInstance(
            IApplicationBuilder appBuilder, string sourcePath)
        {
            // Unlike other consumers of NodeServices, AngularCliMiddleware dosen't share Node instances, nor does it
            // use your DI configuration. It's important for AngularCliMiddleware to have its own private Node instance
            // because it must *not* restart when files change (it's designed to watch for changes and rebuild).
            var nodeServicesOptions = new NodeServicesOptions(appBuilder.ApplicationServices)
            {
                WatchFileExtensions = new string[] { }, // Don't watch anything
                ProjectPath = Path.Combine(Directory.GetCurrentDirectory(), sourcePath),
            };

            return NodeServicesFactory.CreateNodeServices(nodeServicesOptions);
        }

        private static string GetAngularCliMiddlewareScriptPath(IApplicationBuilder appBuilder)
        {
            var script = EmbeddedResourceReader.Read(typeof(AngularCliMiddleware), _middlewareResourceName);
            var nodeScript = new StringAsTempFile(script, GetStoppingToken(appBuilder));
            return nodeScript.FileName;
        }

        private static CancellationToken GetStoppingToken(IApplicationBuilder appBuilder)
        {
            var applicationLifetime = appBuilder
                .ApplicationServices
                .GetService(typeof(IApplicationLifetime));
            return ((IApplicationLifetime)applicationLifetime).ApplicationStopping;
        }

        private async Task<AngularCliServerInfo> StartAngularCliServerAsync()
        {
            // Tell Node to start the server hosting the Angular CLI
            var angularCliServerInfo = await _nodeServices.InvokeExportAsync<AngularCliServerInfo>(
                _middlewareScriptPath,
                "startAngularCliServer");

            // Even after the Angular CLI claims to be listening for requests, there's a short
            // period where it will give an error if you make a request too quickly. Give it
            // a moment to finish starting up.
            await Task.Delay(500);

            return angularCliServerInfo;
        }

        private static void UseProxyToLocalAngularCliMiddleware(
            IApplicationBuilder appBuilder, string publicPath,
            Task<AngularCliServerInfo> serverInfoTask, TimeSpan requestTimeout)
        {
            // This is hardcoded to use http://localhost because:
            // - the requests are always from the local machine (we're not accepting remote
            //   requests that go directly to the Angular CLI middleware server)
            // - given that, there's no reason to use https, and we couldn't even if we
            //   wanted to, because in general the Angular CLI server has no certificate
            var proxyOptionsTask = serverInfoTask.ContinueWith(
                task => new ConditionalProxyMiddlewareTarget(
                    "http", "localhost", task.Result.Port.ToString()));
            appBuilder.UseMiddleware<ConditionalProxyMiddleware>(publicPath, requestTimeout, proxyOptionsTask);
        }

#pragma warning disable CS0649
        class AngularCliServerInfo
        {
            public int Port { get; set; }
        }
    }
#pragma warning restore CS0649
}
