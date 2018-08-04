// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.NodeServices.Npm;
using Microsoft.AspNetCore.NodeServices.Util;
using Microsoft.AspNetCore.SpaServices.Extensions.Util;
using Microsoft.AspNetCore.SpaServices.Util;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SpaServices.NpmCommandDevelopmentServer
{
    /// <summary>
    /// Base middleware for dev servers based on running npn command
    /// </summary>
    public static class NpmCommandDevelopmentServerMiddleware
    {
        private const string LogCategoryName = "Microsoft.AspNetCore.SpaServices";

        /// <summary>
        /// This method uses npm to start dev server
        /// </summary>
        /// <param name="spaBuilder">The <see cref="ISpaBuilder"/>.</param>
        /// <param name="npmScriptName">The name of the script in your package.json file that launches the dev server.</param>
        /// <param name="argumentsFactory">The factory to create arguments for npm script</param>
        /// <param name="envVarsFactory">The factory to create environment variables for npm script</param>
        /// <param name="startCheckRegex">The regex to identify that script is run successfully</param>
        /// <param name="serverStartedEventHandler">The handler for server started event</param>
        public static void Attach(
            ISpaBuilder spaBuilder,
            string npmScriptName,
            Func<PreStartNpmServerInfo, string> argumentsFactory,
            Func<PreStartNpmServerInfo, IDictionary<string, string>> envVarsFactory,
            Regex startCheckRegex,
            Func<PreStartNpmServerInfo, Match, Task> serverStartedEventHandler)
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

            // Start npm server and attach to middleware pipeline
            var appBuilder = spaBuilder.ApplicationBuilder;
            var logger = LoggerFinder.GetOrCreateLogger(appBuilder, LogCategoryName);
            var portTask = StartNpmAppServerAsync(sourcePath, npmScriptName, argumentsFactory, envVarsFactory, startCheckRegex, serverStartedEventHandler, logger);

            // Everything we proxy is hardcoded to target http://localhost because:
            // - the requests are always from the local machine (we're not accepting remote
            //   requests that go directly to the npm server)
            // - given that, there's no reason to use https, and we couldn't even if we
            //   wanted to, because in general the npm server has no certificate
            var targetUriTask = portTask.ContinueWith(
                task => new UriBuilder("http", "localhost", task.Result).Uri);

            SpaProxyingExtensions.UseProxyToSpaDevelopmentServer(spaBuilder, () =>
            {
                // On each request, we create a separate startup task with its own timeout. That way, even if
                // the first request times out, subsequent requests could still work.
                var timeout = spaBuilder.Options.StartupTimeout;
                return targetUriTask.WithTimeout(timeout,
                    $"The npm server did not start listening for requests " +
                    $"within the timeout period of {timeout.Seconds} seconds. " +
                    $"Check the log output for error information.");
            });
        }

        private static async Task<int> StartNpmAppServerAsync(
            string sourcePath,
            string npmScriptName,
            Func<PreStartNpmServerInfo, string> argumentsFactory,
            Func<PreStartNpmServerInfo, IDictionary<string, string>> envVarsFactory,
            Regex startCheckRegex,
            Func<PreStartNpmServerInfo, Match, Task> serverStartedEventHandler,
            ILogger logger)
        {
            var preferences = new PreStartNpmServerInfo
            {
                Port = TcpPortFinder.FindAvailablePort()
            };

            logger.LogInformation($"Starting npm server on port {preferences.Port}...");

            var npmScriptRunner = new NpmScriptRunner(
                sourcePath, npmScriptName, argumentsFactory(preferences), envVarsFactory(preferences));
            npmScriptRunner.AttachToLogger(logger);

            Match serverStartedLine;
            using (var stdErrReader = new EventedStreamStringReader(npmScriptRunner.StdErr))
            {
                try
                {
                    serverStartedLine = await npmScriptRunner.StdOut.WaitForMatch(startCheckRegex);
                }
                catch (EndOfStreamException ex)
                {
                    throw new InvalidOperationException(
                        $"The NPM script '{npmScriptName}' exited without indicating that the " +
                        $"server was listening for requests. The error output was: " +
                        $"{stdErrReader.ReadAsString()}", ex);
                }
            }

            if (serverStartedEventHandler != null)
            {
                await serverStartedEventHandler(preferences, serverStartedLine);
            }

            return preferences.Port;
        }
    }
}
