﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.Extensions.Proxy;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for proxying requests to a local SPA development server during
    /// development. Not for use in production applications.
    /// </summary>
    public static class SpaProxyingExtensions
    {
        /// <summary>
        /// Configures the application to forward incoming requests to a local Single Page
        /// Application (SPA) development server. This is only intended to be used during
        /// development. Do not enable this middleware in production applications.
        /// </summary>
        /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="baseUri">The target base URI to which requests should be proxied.</param>
        public static void UseProxyToSpaDevelopmentServer(
            this IApplicationBuilder applicationBuilder,
            Uri baseUri)
        {
            UseProxyToSpaDevelopmentServer(
                applicationBuilder,
                Task.FromResult(baseUri));
        }

        /// <summary>
        /// Configures the application to forward incoming requests to a local Single Page
        /// Application (SPA) development server. This is only intended to be used during
        /// development. Do not enable this middleware in production applications.
        /// </summary>
        /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="baseUriTask">A <see cref="Task"/> that resolves with the target base URI to which requests should be proxied.</param>
        public static void UseProxyToSpaDevelopmentServer(
            this IApplicationBuilder applicationBuilder,
            Task<Uri> baseUriTask)
        {
            var applicationStoppingToken = GetStoppingToken(applicationBuilder);

            // It's important not to time out the requests, as some of them might be to
            // server-sent event endpoints or similar, where it's expected that the response
            // takes an unlimited time and never actually completes
            var neverTimeOutHttpClient =
                ConditionalProxy.CreateHttpClientForProxy(Timeout.InfiniteTimeSpan);

            // Proxy all requests into the Angular CLI server
            applicationBuilder.Use(async (context, next) =>
            {
                var didProxyRequest = await ConditionalProxy.PerformProxyRequest(
                    context, neverTimeOutHttpClient, baseUriTask, applicationStoppingToken);

                // Since we are proxying everything, this is the end of the middleware pipeline.
                // We won't call next().
                if (!didProxyRequest)
                {
                    context.Response.StatusCode = 404;
                }
            });
        }

        private static CancellationToken GetStoppingToken(IApplicationBuilder appBuilder)
        {
            var applicationLifetime = appBuilder
                .ApplicationServices
                .GetService(typeof(IApplicationLifetime));
            return ((IApplicationLifetime)applicationLifetime).ApplicationStopping;
        }
    }
}
