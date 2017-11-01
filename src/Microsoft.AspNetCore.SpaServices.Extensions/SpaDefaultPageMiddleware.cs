// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;

namespace Microsoft.AspNetCore.SpaServices
{
    internal class SpaDefaultPageMiddleware
    {
        public static void Attach(IApplicationBuilder app, ISpaOptions spaOptions)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (spaOptions == null)
            {
                throw new ArgumentNullException(nameof(spaOptions));
            }

            var defaultPageUrl = ConstructDefaultPageUrl(spaOptions.UrlPrefix, spaOptions.DefaultPage);

            // Rewrite all requests to the default page
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
                var message = "The SPA default page middleware could not return the default page " +
                    $"'{defaultPageUrl}' because it was not found on disk, and no other middleware " +
                    "handled the request.\n";

                // Try to clarify the common scenario where someone runs an application in
                // Production environment without first publishing the whole application
                // or at least building the SPA.
                var hostEnvironment = (IHostingEnvironment)context.RequestServices.GetService(typeof(IHostingEnvironment));
                if (hostEnvironment != null && hostEnvironment.IsProduction())
                {
                    message += "Your application is running in Production mode, so make sure it has " +
                        "been published, or that you have built your SPA manually. Alternatively you " +
                        "may wish to switch to the Development environment.\n";
                }

                throw new InvalidOperationException(message);
            });
        }

        private static string ConstructDefaultPageUrl(string urlPrefix, string defaultPage)
        {
            if (string.IsNullOrEmpty(defaultPage))
            {
                defaultPage = DefaultSpaOptions.DefaultDefaultPageValue;
            }

            return new PathString(urlPrefix).Add(new PathString("/" + defaultPage));
        }
    }
}
