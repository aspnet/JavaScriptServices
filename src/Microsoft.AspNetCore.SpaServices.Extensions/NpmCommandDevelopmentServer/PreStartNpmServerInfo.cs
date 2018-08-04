// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SpaServices.NpmCommandDevelopmentServer
{
    /// <summary>
    /// Selected by middleware server parameters for parameter
    /// </summary>
    public class PreStartNpmServerInfo
    {
        /// <summary>
        /// Selected for use port number
        /// </summary>
        public int Port { get; set; }
    }
}