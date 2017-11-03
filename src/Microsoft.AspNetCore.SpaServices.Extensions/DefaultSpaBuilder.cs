// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.SpaServices
{
    internal class DefaultSpaBuilder : ISpaBuilder
    {
        public IApplicationBuilder ApplicationBuilder { get; }

        public SpaOptions Options { get; }

        public DefaultSpaBuilder(IApplicationBuilder applicationBuilder, string sourcePath, string urlPrefix)
        {
            ApplicationBuilder = applicationBuilder 
                ?? throw new System.ArgumentNullException(nameof(applicationBuilder));

            Options = new SpaOptions(sourcePath, urlPrefix);
        }
    }
}
