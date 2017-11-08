// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SpaServices
{
    /// <summary>
    /// Describes options for hosting a Single Page Application (SPA).
    /// </summary>
    public class SpaOptions
    {
        private string _urlPrefix;
        private string _defaultPageUrl = "index.html";

        /// <summary>
        /// Gets or sets the URL, relative to <see cref="UrlPrefix"/>,
        /// of the default page that hosts your SPA user interface.
        /// The default value is <c>"index.html"</c>.
        /// </summary>
        public string DefaultPage
        {
            get => _defaultPageUrl;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException($"The value for {nameof(DefaultPage)} cannot be null or empty.");
                }

                _defaultPageUrl = value;
            }
        }

        /// <summary>
        /// Gets or sets the path, relative to the application working directory,
        /// of the directory that contains the SPA source files during
        /// development. The directory may not exist in published applications.
        /// </summary>
        public string SourcePath { get; set; }

        /// <summary>
        /// Gets or sets the URL path, relative to your application's <c>PathBase</c>, from
        /// which the SPA files are served.
        ///
        /// For example, if your SPA files are located in <c>wwwroot/dist</c>, then
        /// the value should usually be <c>"/dist"</c>, because that is the URL prefix
        /// from which browsers can request those files.
        /// 
        /// The value must begin with a <code>'/'</code> character.
        /// </summary>
        public string UrlPrefix
        {
            get => _urlPrefix;
            set
            {
                if (value == null || !value.StartsWith("/", StringComparison.Ordinal))
                {
                    throw new ArgumentException($"The value for {nameof(UrlPrefix)} must start with '/'");
                }

                _urlPrefix = value;
            }
        }

        // Currently there isn't a use case for constructing this in application code, but if that changes,
        // this internal constructor will be removed.
        internal SpaOptions()
        {
        }
    }
}
