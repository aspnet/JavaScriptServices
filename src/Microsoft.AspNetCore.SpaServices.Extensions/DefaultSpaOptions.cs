// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using System;

namespace Microsoft.AspNetCore.SpaServices
{
    internal class DefaultSpaOptions : ISpaOptions
    {
        public const string DefaultDefaultPageValue = "index.html";

        public string DefaultPage { get; set; } = DefaultDefaultPageValue;

        public string SourcePath { get; }

        public string UrlPrefix { get; }

        private static readonly string _propertiesKey = Guid.NewGuid().ToString();

        public DefaultSpaOptions(string sourcePath, string urlPrefix)
        {
            if (urlPrefix == null || !urlPrefix.StartsWith("/", StringComparison.Ordinal))
            {
                throw new ArgumentException("The value must start with '/'", nameof(urlPrefix));
            }

            SourcePath = sourcePath;
            UrlPrefix = urlPrefix;
        }

        internal static ISpaOptions FindInPipeline(IApplicationBuilder app)
        {
            return app.Properties.TryGetValue(_propertiesKey, out var instance)
                ? (ISpaOptions)instance
                : null;
        }

        internal void RegisterSoleInstanceInPipeline(IApplicationBuilder app)
        {
            if (app.Properties.ContainsKey(_propertiesKey))
            {
                throw new InvalidOperationException($"Only one usage of {nameof(SpaApplicationBuilderExtensions.UseSpa)} " +
                    $"is allowed in any single branch of the middleware pipeline. This is because one " +
                    $"instance would handle all requests.");
            }

            app.Properties[_propertiesKey] = this;
        }
    }
}
