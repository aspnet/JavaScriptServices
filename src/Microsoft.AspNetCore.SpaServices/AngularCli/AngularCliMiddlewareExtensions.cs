// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SpaServices.AngularCli
{
    /// <summary>
    /// Extension methods for enabling Angular CLI middleware support.
    /// </summary>
    public static class AngularCliMiddlewareExtensions
    {
        /// <summary>
        /// Enables Angular CLI middleware support. This hosts an instance of the Angular CLI in memory in
        /// your application so that you can always serve up-to-date CLI-built resources without having
        /// to run CLI server manually.
        ///
        /// Incoming requests that match Angular CLI-built files will be handled by returning the CLI server
        /// output directly.
        ///
        /// This feature should only be used in development. For production deployments, be sure not to
        /// enable Angular CLI middleware.
        /// </summary>
        /// <param name="spaBuilder">The <see cref="ISpaBuilder"/>.</param>
        /// <param name="sourcePath">The path, relative to the application root, of the directory containing the SPA source files.</param>
        public static void UseAngularCliMiddleware(
            this ISpaBuilder spaBuilder,
            string sourcePath)
        {
            new AngularCliMiddleware(spaBuilder, sourcePath);
        }
    }
}
