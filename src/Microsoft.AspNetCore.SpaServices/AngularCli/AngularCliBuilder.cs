// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SpaServices.Prerendering;
using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SpaServices.AngularCli
{
    /// <summary>
    /// Provides an implementation of <see cref="ISpaPrerendererBuilder"/> that can build
    /// an Angular application by invoking the Angular CLI.
    /// </summary>
    public class AngularCliBuilder : ISpaPrerendererBuilder
    {
        private readonly string _cliAppName;

        /// <summary>
        /// Constructs an instance of <see cref="AngularCliBuilder"/>.
        /// </summary>
        /// <param name="cliAppName">The name of the application to be built. This must match an entry in your <c>.angular-cli.json</c> file.</param>
        public AngularCliBuilder(string cliAppName)
        {
            _cliAppName = cliAppName;
        }

        /// <inheritdoc />
        public Task Build(IApplicationBuilder app)
        {
            // Locate the AngularCliMiddleware within the provided IApplicationBuilder
            if (app.Properties.TryGetValue(
                AngularCliMiddleware.AngularCliMiddlewareKey,
                out var angularCliMiddleware))
            {
                return ((AngularCliMiddleware)angularCliMiddleware)
                    .StartAngularCliBuilderAsync(_cliAppName);
            }
            else
            {
                throw new Exception(
                    $"Cannot use {nameof(AngularCliBuilder)} unless you are also using" +
                    $" {nameof(AngularCliMiddlewareExtensions.UseAngularCliServer)}.");
            }
        }
    }
}
