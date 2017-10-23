// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.NodeServices;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.SpaServices.Extensions.Proxy;

namespace Microsoft.AspNetCore.SpaServices.AngularCli
{
    internal class AngularCliMiddleware
    {
        private const string _middlewareResourceName = "/Content/Node/angular-cli-middleware.js";

        internal readonly static string AngularCliMiddlewareKey = Guid.NewGuid().ToString();

        private readonly INodeServices _nodeServices;
        private readonly string _middlewareScriptPath;
        private readonly HttpClient _neverTimeOutHttpClient =
            ConditionalProxy.CreateHttpClientForProxy(Timeout.InfiniteTimeSpan);

        public AngularCliMiddleware(
            IApplicationBuilder appBuilder,
            string sourcePath,
            SpaDefaultPageMiddleware defaultPageMiddleware)
        {
            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new ArgumentException("Cannot be null or empty", nameof(sourcePath));
            }

            // Prepare to make calls into Node
            _nodeServices = CreateNodeServicesInstance(appBuilder, sourcePath);
            _middlewareScriptPath = GetAngularCliMiddlewareScriptPath(appBuilder);

            // Start Angular CLI and attach to middleware pipeline
            var angularCliServerInfoTask = StartAngularCliServerAsync();

            // Everything we proxy is hardcoded to target http://localhost because:
            // - the requests are always from the local machine (we're not accepting remote
            //   requests that go directly to the Angular CLI middleware server)
            // - given that, there's no reason to use https, and we couldn't even if we
            //   wanted to, because in general the Angular CLI server has no certificate
            var proxyOptionsTask = angularCliServerInfoTask.ContinueWith(
                task => new ConditionalProxyMiddlewareTarget(
                    "http", "localhost", task.Result.Port.ToString()));

            var applicationStoppingToken = GetStoppingToken(appBuilder);

            // Proxy all requests into the Angular CLI server
            appBuilder.Use(async (context, next) =>
            {
                var didProxyRequest = await ConditionalProxy.PerformProxyRequest(
                    context, _neverTimeOutHttpClient, proxyOptionsTask, applicationStoppingToken);

                // Since we are proxying everything, this is the end of the middleware pipeline.
                // We won't call next().
                if (!didProxyRequest)
                {
                    context.Response.StatusCode = 404;
                }
            });

            // Advertise the availability of this feature to other SPA middleware
            appBuilder.Properties.Add(AngularCliMiddlewareKey, this);
        }

        internal Task StartAngularCliBuilderAsync(string cliAppName)
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

            if (!Directory.Exists(nodeServicesOptions.ProjectPath))
            {
                throw new DirectoryNotFoundException($"Directory not found: {nodeServicesOptions.ProjectPath}");
            }

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

#pragma warning disable CS0649
        class AngularCliServerInfo
        {
            public int Port { get; set; }
        }
    }
#pragma warning restore CS0649
}
