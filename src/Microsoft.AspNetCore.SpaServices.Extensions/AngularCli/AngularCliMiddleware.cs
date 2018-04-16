// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.NodeServices.Npm;
using Microsoft.AspNetCore.NodeServices.Util;
using Microsoft.AspNetCore.SpaServices.Util;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using Microsoft.AspNetCore.SpaServices.Extensions.Util;

namespace Microsoft.AspNetCore.SpaServices.AngularCli
{
    internal static class AngularCliMiddleware
    {
        private const string LogCategoryName = "Microsoft.AspNetCore.SpaServices";
        private static TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(5); // This is a development-time only feature, so a very long timeout is fine

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
            var logger = LoggerFinder.GetOrCreateLogger(appBuilder, LogCategoryName);
            var angularCliServerInfoTask = StartAngularCliServerAsync(sourcePath, npmScriptName, logger);

            // Everything we proxy is hardcoded to target http://localhost because:
            // - the requests are always from the local machine (we're not accepting remote
            //   requests that go directly to the Angular CLI middleware server)
            // - given that, there's no reason to use https, and we couldn't even if we
            //   wanted to, because in general the Angular CLI server has no certificate
            var targetUriTask = angularCliServerInfoTask.ContinueWith(
                task => new UriBuilder("http", "localhost", task.Result.Port).Uri);

            SpaProxyingExtensions.UseProxyToSpaDevelopmentServer(spaBuilder, () =>
            {
                // On each request, we create a separate startup task with its own timeout. That way, even if
                // the first request times out, subsequent requests could still work.
                var timeout = spaBuilder.Options.StartupTimeout;
                return targetUriTask.WithTimeout(timeout,
                    $"The Angular CLI process did not start listening for requests " +
                    $"within the timeout period of {timeout.TotalSeconds} seconds. " +
                    $"Check the log output for error information.");
            });
        }

        private static async Task<AngularCliServerInfo> StartAngularCliServerAsync(
            string sourcePath, string npmScriptName, ILogger logger)
        {
            var portNumber = TcpPortFinder.FindAvailablePort();
            logger.LogInformation($"Starting @angular/cli on port {portNumber}...");

            var npmScriptRunner = new NpmScriptRunner(
                sourcePath, npmScriptName, $"--port {portNumber}", null);
            npmScriptRunner.AttachToLogger(logger);

            Match openBrowserLine;
            using (var stdErrReader = new EventedStreamStringReader(npmScriptRunner.StdErr))
            {
                try
                {
                    openBrowserLine = await npmScriptRunner.StdOut.WaitForMatch(
                        new Regex("open your browser on (http\\S+)", RegexOptions.None, RegexMatchTimeout));
                }
                catch (EndOfStreamException ex)
                {
                    throw new InvalidOperationException(
                        $"The NPM script '{npmScriptName}' exited without indicating that the " +
                        $"Angular CLI was listening for requests. The error output was: " +
                        $"{stdErrReader.ReadAsString()}", ex);
                }
            }

            var uri = new Uri(openBrowserLine.Groups[1].Value);
            var serverInfo = new AngularCliServerInfo { Port = uri.Port };

            // Even after the Angular CLI claims to be listening for requests, there's a short
            // period where it will give an error if you make a request too quickly
            await WaitForAngularCliServerToAcceptRequests(uri);

            return serverInfo;
        }

        private static async Task WaitForAngularCliServerToAcceptRequests(Uri cliServerUri)
        {
            // To determine when it's actually ready, try making HEAD requests to '/'. If it
            // produces any HTTP response (even if it's 404) then it's ready. If it rejects the
            // connection then it's not ready. We keep trying forever because this is dev-mode
            // only, and only a single startup attempt will be made, and there's a further level
            // of timeouts enforced on a per-request basis.
            using (var client = new HttpClient())
            {
                while (true)
                {
                    try
                    {
                        // If we get any HTTP response, the CLI server is ready
                        await client.SendAsync(
                            new HttpRequestMessage(HttpMethod.Head, cliServerUri),
                            new CancellationTokenSource(1000).Token);
                        return;
                    }
                    catch (Exception)
                    {
                        await Task.Delay(1000); // 1 second
                    }
                }
            }
        }

        class AngularCliServerInfo
        {
            public int Port { get; set; }
        }
    }
}
