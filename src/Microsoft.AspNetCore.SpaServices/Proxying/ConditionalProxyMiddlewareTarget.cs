// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.SpaServices.Proxy
{
    internal class ConditionalProxyMiddlewareTarget
    {
        public ConditionalProxyMiddlewareTarget(string scheme, string host, string port)
        {
            Scheme = scheme;
            Host = host;
            Port = port;
        }

        public string Scheme { get; }
        public string Host { get; }
        public string Port { get; }
    }
}