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
        /// <param name="sourcePath">The disk path, relative to the current directory, of the directory containing the SPA source files. When Angular CLI executes, this will be its working directory.</param>
        public static void UseAngularCliServer(
            this IApplicationBuilder app,
            string sourcePath)
        {
            var defaultPageMiddleware = SpaDefaultPageMiddleware.FindInPipeline(app);
            if (defaultPageMiddleware == null)
            {
                throw new Exception($"{nameof(UseAngularCliServer)} should be called inside the 'configue' callback of a call to {nameof(SpaApplicationBuilderExtensions.UseSpa)}.");
            }

            new AngularCliMiddleware(app, sourcePath, defaultPageMiddleware);
        }
    }
}
