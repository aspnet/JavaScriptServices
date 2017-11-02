// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SpaServices;
using System;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Provides extension methods used for configuring an application to
    /// host a client-side Single Page Application (SPA).
    /// </summary>
    public static class SpaApplicationBuilderExtensions
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
        /// <param name="configure">
        /// Optional. If specified, this callback will be invoked so that additional middleware
        /// can be registered within the context of this SPA.
        /// </param>
        public static void UseSpa(
            this IApplicationBuilder app,
            string urlPrefix,
            string defaultPage = null,
            Action configure = null)
        {
            new SpaDefaultPageMiddleware(app, urlPrefix, defaultPage, configure);
        }
    }
}
