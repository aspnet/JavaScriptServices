// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.NodeServices.Npm;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Console;
using System.Net.Sockets;
using System.Net;
using System.IO;
using Microsoft.AspNetCore.NodeServices.Util;

namespace Microsoft.AspNetCore.SpaServices.AngularCli
{
    internal static class AngularCliMiddleware
    {
        private const string LogCategoryName = "Microsoft.AspNetCore.SpaServices";
        private const int TimeoutMilliseconds = 50 * 1000;

        public static void Attach(
            ISpaBuilder spaBuilder,
            string npmScriptName)
        {
            var sourcePath = spaBuilder.Options.SourcePath;
            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new ArgumentException("Cannot be null or empty", nameof(sourcePath));
            }

            if (string.IsNullOrEmpty(npmScriptName))
            {
                throw new ArgumentException("Cannot be null or empty", nameof(npmScriptName));
            }

            // Start Angular CLI and attach to middleware pipeline
            var appBuilder = spaBuilder.ApplicationBuilder;
            var logger = GetOrCreateLogger(appBuilder);
            var angularCliServerInfoTask = StartAngularCliServerAsync(sourcePath, npmScriptName, logger);

            // Everything we proxy is hardcoded to target http://localhost because:
            // - the requests are always from the local machine (we're not accepting remote
            //   requests that go directly to the Angular CLI middleware server)
            // - given that, there's no reason to use https, and we couldn't even if we
            //   wanted to, because in general the Angular CLI server has no certificate
            var targetUriTask = angularCliServerInfoTask.ContinueWith(
                task => new UriBuilder("http", "localhost", task.Result.Port).Uri);

            SpaProxyingExtensions.UseProxyToSpaDevelopmentServer(spaBuilder, targetUriTask);
        }

        internal static ILogger GetOrCreateLogger(IApplicationBuilder appBuilder)
        {
            // If the DI system gives us a logger, use it. Otherwise, set up a default one.
            var loggerFactory = appBuilder.ApplicationServices.GetService<ILoggerFactory>();
            var logger = loggerFactory != null
                ? loggerFactory.CreateLogger(LogCategoryName)
                : new ConsoleLogger(LogCategoryName, null, false);
            return logger;
        }

        private static async Task<AngularCliServerInfo> StartAngularCliServerAsync(
            string sourcePath, string npmScriptName, ILogger logger)
        {
            var portNumber = FindAvailablePort();
            logger.LogInformation($"Starting @angular/cli on port {portNumber}...");

            var npmScriptRunner = new NpmScriptRunner(
                sourcePath, npmScriptName, $"--port {portNumber}");
            npmScriptRunner.AttachToLogger(logger);

            Match openBrowserLine;
            using (var stdErrReader = new EventedStreamStringReader(npmScriptRunner.StdErr))
            {
                try
                {
                    openBrowserLine = await npmScriptRunner.StdOut.WaitForMatch(
                        new Regex("open your browser on (http\\S+)"),
                        TimeoutMilliseconds);
                }
                catch (EndOfStreamException ex)
                {
                    throw new InvalidOperationException(
                        $"The NPM script '{npmScriptName}' exited without indicating that the " +
                        $"Angular CLI was listening for requests. The error output was: " +
                        $"{stdErrReader.ReadAsString()}", ex);
                }
                catch (TaskCanceledException ex)
                {
                    throw new InvalidOperationException(
                        $"The Angular CLI process did not start listening for requests " +
                        $"within the timeout period of {TimeoutMilliseconds / 1000} seconds. " +
                        $"Check the log output for error information.", ex);
                }
            }

            var uri = new Uri(openBrowserLine.Groups[1].Value);
            var serverInfo = new AngularCliServerInfo { Port = uri.Port };

            // Even after the Angular CLI claims to be listening for requests, there's a short
            // period where it will give an error if you make a request too quickly. Give it
            // a moment to finish starting up.
            await Task.Delay(500);

            return serverInfo;
        }

        private static int FindAvailablePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }

#pragma warning disable CS0649
        class AngularCliServerInfo
        {
            public int Port { get; set; }
        }
    }
#pragma warning restore CS0649
}
