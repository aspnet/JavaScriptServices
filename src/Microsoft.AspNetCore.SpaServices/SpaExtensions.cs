// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SpaServices;
using System;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides extension methods used for configuring an application to
    /// host a client-side Single Page Application (SPA).
    /// </summary>
    public static class SpaExtensions
    {
        internal readonly static object IsSpaFallbackRequestTag = new object();

        /// <summary>
        /// Handles all requests from this point in the middleware chain by returning
        /// the default page for the Single Page Application (SPA).
        /// 
        /// This middleware should be placed late in the chain, so that other middleware
        /// for serving static files, MVC actions, etc., takes precedence.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="publicPath">
        /// The URL path, relative to your application's <c>PathBase</c>, from which the
        /// SPA files are served.
        ///
        /// For example, if your SPA files are located in <c>wwwroot/dist</c>, then
        /// the value should usually be <c>"dist"</c>, because that is the URL prefix
        /// from which browsers can request those files.
        /// </param>
        /// <param name="defaultPage">
        /// Optional. If specified, configures the path (relative to <paramref name="publicPath"/>)
        /// of the default page that hosts your SPA user interface.
        /// If not specified, the default value is <c>"index.html"</c>.
        /// </param>
        /// <param name="configure">
        /// Optional. If specified, configures hosting options and further middleware for your SPA.
        /// </param>
        public static void UseSpa(
            this IApplicationBuilder app,
            string publicPath,
            string defaultPage = null,
            Action<ISpaBuilder> configure = null)
        {
            if (string.IsNullOrEmpty(publicPath))
            {
                throw new ArgumentException("Cannot be null or empty", nameof(publicPath));
            }

            if (string.IsNullOrEmpty(defaultPage))
            {
                defaultPage = "index.html";
            }

            var publicPathString = new PathString(publicPath);
            var defaultFilePath = publicPathString.Add(new PathString("/" + defaultPage));

            // Support client-side routing by mapping all requests to the SPA default file
            RewriteAllRequestsToServeDefaultFile(app, publicPathString, defaultFilePath);

            // Allow other SPA configuration. This could add other middleware for
            // serving the default file, such as prerendering or Webpack/AngularCLI middleware.
            configure?.Invoke(new DefaultSpaBuilder(app, publicPath, defaultFilePath));

            // If the default file wasn't served by any other middleware,
            // serve it as a static file from disk
            AddTerminalMiddlewareForDefaultFile(app, defaultFilePath);
        }

        private static void RewriteAllRequestsToServeDefaultFile(IApplicationBuilder app, PathString publicPathString, PathString defaultFilePath)
        {
            app.Use(async (context, next) =>
            {
                // The only requests we don't map to the default file are those
                // for other files within the SPA (e.g., its .js or .css files).
                // Normally this makes no difference in production because those
                // files exist on disk, but it does matter in development if they
                // are being served by some subsequent middleware.
                if (!context.Request.Path.StartsWithSegments(publicPathString))
                {
                    context.Request.Path = defaultFilePath;
                    context.Items[IsSpaFallbackRequestTag] = true;
                }

                await next.Invoke();
            });
        }

        private static void AddTerminalMiddlewareForDefaultFile(
            IApplicationBuilder app, PathString defaultFilePath)
        {
            app.Map(defaultFilePath, _ =>
            {
                app.UseStaticFiles();

                // If the default file didn't get served as a static file (because it
                // was not present on disk), the SPA is definitely not going to work.
                app.Use((context, next) =>
                {
                    var message = $"The {nameof(UseSpa)}() middleware could not return the default page '{defaultFilePath}' because it was not found on disk, and no other middleware handled the request.\n";

                    // Try to clarify the common scenario where someone runs an application in
                    // Production environment without first publishing the whole application
                    // or at least building the SPA.
                    var hostEnvironment = (IHostingEnvironment)context.RequestServices.GetService(typeof(IHostingEnvironment));
                    if (hostEnvironment != null && hostEnvironment.IsProduction())
                    {
                        message += "Your application is running in Production mode, so make sure it has been published, or that you have built your SPA manually. Alternatively you may wish to switch to the Development environment.\n";
                    }

                    throw new Exception(message);
                });
            });
        }
    }
}
