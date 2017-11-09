// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SpaServices.StaticFiles
{
    /// <summary>
    /// Represents options for serving static files for a Single Page Application (SPA).
    /// </summary>
    public class SpaStaticFilesOptions
    {
        /// <summary>
        /// Gets or sets the path of the directory, relative to the application root, in which
        /// the physical files are located.
        /// </summary>
        public string RootPath { get; set; }
    }
}
