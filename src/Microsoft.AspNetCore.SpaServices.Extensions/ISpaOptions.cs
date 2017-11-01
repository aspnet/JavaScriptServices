// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SpaServices
{
    /// <summary>
    /// Describes options for hosting a Single Page Application (SPA).
    /// </summary>
    public interface ISpaOptions
    {
        /// <summary>
        /// Gets or sets the URL, relative to <see cref="UrlPrefix"/>,
        /// of the default page that hosts your SPA user interface.
        /// The typical value is <c>"index.html"</c>.
        /// </summary>
        string DefaultPage { get; set; }

        /// <summary>
        /// Gets the path, relative to the application working directory,
        /// of the directory that contains the SPA source files during
        /// development. The directory may not exist in published applications.
        /// </summary>
        string SourcePath { get; }

        /// <summary>
        /// Gets the URL path, relative to your application's <c>PathBase</c>, from which
        /// the SPA files are served.
        ///
        /// For example, if your SPA files are located in <c>wwwroot/dist</c>, then
        /// the value should usually be <c>"/dist"</c>, because that is the URL prefix
        /// from which browsers can request those files.
        /// 
        /// The value must begin with a <code>'/'</code> character.
        /// </summary>
        string UrlPrefix { get; }
    }
}
