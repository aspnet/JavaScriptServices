// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides extension methods used for configuring an application to
    /// host a client-side Single Page Application (SPA).
    /// </summary>
    public static class SpaDefaultPageExtensions
    {
        /// <summary>
        /// Handles all requests from this point in the middleware chain by returning
        /// the default page for the Single Page Application (SPA).
        /// 
        /// This middleware should be placed late in the chain, so that other middleware
        /// for serving static files, MVC actions, etc., takes precedence.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="urlPrefix">
        /// The URL path, relative to your application's <c>PathBase</c>, from which the
        /// SPA files are served.
        ///
        /// For example, if your SPA files are located in <c>wwwroot/dist</c>, then
        /// the value should usually be <c>"dist"</c>, because that is the URL prefix
        /// from which browsers can request those files.
        /// </param>
        /// <param name="defaultPage">
        /// Optional. If specified, configures the path (relative to <paramref name="urlPrefix"/>)
        /// of the default page that hosts your SPA user interface.
        /// If not specified, the default value is <c>"index.html"</c>.
        /// </param>
        public static void UseSpaDefaultPage(
            this IApplicationBuilder app,
            string urlPrefix,
            string defaultPage = null)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (urlPrefix == null)
            {
                throw new ArgumentNullException(nameof(urlPrefix));
            }

            // Rewrite all requests to the default page
            var defaultPageUrl = GetDefaultPageUrl(urlPrefix, defaultPage);
            app.Use((context, next) =>
            {
                context.Request.Path = defaultPageUrl;
                return next();
            });

            // Serve it as file from disk
            app.UseStaticFiles();

            // If the default file didn't get served as a static file (because it
            // was not present on disk), the SPA is definitely not going to work.
            app.Use((context, next) =>
            {
                var message = $"The {nameof(UseSpaDefaultPage)}() middleware could not return the default page '{defaultPageUrl}' because it was not found on disk, and no other middleware handled the request.\n";

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
        }

        internal static string GetDefaultPageUrl(string urlPrefix, string defaultPage)
        {
            if (string.IsNullOrEmpty(defaultPage))
            {
                defaultPage = "index.html";
            }

            return new PathString(urlPrefix).Add(new PathString("/" + defaultPage));
        }
    }
}
