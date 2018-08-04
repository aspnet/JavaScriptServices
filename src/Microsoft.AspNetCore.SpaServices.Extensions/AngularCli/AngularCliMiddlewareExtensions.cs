// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SpaServices.NpmCommandDevelopmentServer;

namespace Microsoft.AspNetCore.SpaServices.AngularCli
{
    /// <summary>
    /// Extension methods for enabling Angular CLI middleware support.
    /// </summary>
    public static class AngularCliMiddlewareExtensions
    {
        private static TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(5); // This is a development-time only feature, so a very long timeout is fine

        /// <summary>
        /// Handles requests by passing them through to an instance of the Angular CLI server.
        /// This means you can always serve up-to-date CLI-built resources without having
        /// to run the Angular CLI server manually.
        ///
        /// This feature should only be used in development. For production deployments, be
        /// sure not to enable the Angular CLI server.
        /// </summary>
        /// <param name="spaBuilder">The <see cref="ISpaBuilder"/>.</param>
        /// <param name="npmScript">The name of the script in your package.json file that launches the Angular CLI process.</param>
        public static void UseAngularCliServer(
            this ISpaBuilder spaBuilder,
            string npmScript)
        {
            if (spaBuilder == null)
            {
                throw new ArgumentNullException(nameof(spaBuilder));
            }

            var spaOptions = spaBuilder.Options;

            if (string.IsNullOrEmpty(spaOptions.SourcePath))
            {
                throw new InvalidOperationException($"To use {nameof(UseAngularCliServer)}, you must supply a non-empty value for the {nameof(SpaOptions.SourcePath)} property of {nameof(SpaOptions)} when calling {nameof(SpaApplicationBuilderExtensions.UseSpa)}.");
            }

            var startRegex = new Regex("open your browser on (http\\S+)", RegexOptions.None, RegexMatchTimeout);
            NpmCommandDevelopmentServerMiddleware.Attach(spaBuilder, npmScript, ArgumentsFactory, p => null, startRegex, ServerStartedEventHandler);
        }

        private static string ArgumentsFactory(PreStartNpmServerInfo parameters)
        {
            return $"--port {parameters.Port}";
        }

        private static async Task ServerStartedEventHandler(PreStartNpmServerInfo parameters, Match serverStartedMatch)
        {
            // To determine when it's actually ready, try making HEAD requests to '/'. If it
            // produces any HTTP response (even if it's 404) then it's ready. If it rejects the
            // connection then it's not ready. We keep trying forever because this is dev-mode
            // only, and only a single startup attempt will be made, and there's a further level
            // of timeouts enforced on a per-request basis.
            var timeoutMilliseconds = 1000;
            using (var client = new HttpClient())
            {
                while (true)
                {
                    try
                    {
                        var uri = new Uri(serverStartedMatch.Groups[1].Value);

                        // If we get any HTTP response, the CLI server is ready
                        await client.SendAsync(
                            new HttpRequestMessage(HttpMethod.Head, uri),
                            new CancellationTokenSource(timeoutMilliseconds).Token);
                        return;
                    }
                    catch (Exception)
                    {
                        await Task.Delay(500);

                        // Depending on the host's networking configuration, the requests can take a while
                        // to go through, most likely due to the time spent resolving 'localhost'.
                        // Each time we have a failure, allow a bit longer next time (up to a maximum).
                        // This only influences the time until we regard the dev server as 'ready', so it
                        // doesn't affect the runtime perf (even in dev mode) once the first connection is made.
                        // Resolves https://github.com/aspnet/JavaScriptServices/issues/1611
                        if (timeoutMilliseconds < 10000)
                        {
                            timeoutMilliseconds += 3000;
                        }
                    }
                }
            }
        }
    }
}
