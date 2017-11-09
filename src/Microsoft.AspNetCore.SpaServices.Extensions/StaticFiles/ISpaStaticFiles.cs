// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.SpaServices.StaticFiles
{
    /// <summary>
    /// Represents a service that can provide static files to be served for a Single Page
    /// Application (SPA).
    /// </summary>
    public interface ISpaStaticFiles
    {
        /// <summary>
        /// Gets the file provider, if available, that supplies the static files for the SPA.
        /// </summary>
        /// <param name="fileProvider">The <see cref="IFileProvider"/></param>
        /// <returns>A flag indicating whether a file provider could be supplied.</returns>
        bool TryGetFileProvider(out IFileProvider fileProvider);
    }
}
