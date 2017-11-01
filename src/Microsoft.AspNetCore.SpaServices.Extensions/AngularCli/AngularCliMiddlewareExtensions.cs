// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using System;

namespace Microsoft.AspNetCore.SpaServices.AngularCli
{
    /// <summary>
    /// Extension methods for enabling Angular CLI middleware support.
    /// </summary>
    public static class AngularCliMiddlewareExtensions
    {
        /// <summary>
        /// Handles requests by passing them through to an instance of the Angular CLI server.
        /// This means you can always serve up-to-date CLI-built resources without having
        /// to run the Angular CLI server manually.
        ///
        /// This feature should only be used in development. For production deployments, be
        /// sure not to enable the Angular CLI server.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="npmScript">The name of the script in your package.json file that launches the Angular CLI process.</param>
        public static void UseAngularCliServer(
            this IApplicationBuilder app,
            string npmScript)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var spaOptions = DefaultSpaOptions.FindInPipeline(app);
            if (spaOptions == null)
            {
                throw new InvalidOperationException($"{nameof(UseAngularCliServer)} should be called inside the 'configure' callback of a call to {nameof(SpaApplicationBuilderExtensions.UseSpa)}.");
            }

            if (string.IsNullOrEmpty(spaOptions.SourcePath))
            {
                throw new InvalidOperationException($"To use {nameof(UseAngularCliServer)}, you must supply a non-empty value for the {nameof(ISpaOptions.SourcePath)} property of {nameof(ISpaOptions)} when calling {nameof(SpaApplicationBuilderExtensions.UseSpa)}.");
            }

            AngularCliMiddleware.Attach(app, spaOptions.SourcePath, npmScript);
        }
    }
}
