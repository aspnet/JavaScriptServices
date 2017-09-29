// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.SpaServices.Prerendering;
using System;
using System.Linq;
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
        public Task Build(ISpaBuilder spaBuilder)
        {
            // Locate the AngularCliMiddleware within the provided ISpaBuilder
            var angularCliMiddleware = spaBuilder
                .Properties.Keys.OfType<AngularCliMiddleware>().FirstOrDefault();
            if (angularCliMiddleware == null)
            {
                throw new Exception(
                    $"Cannot use {nameof (AngularCliBuilder)} unless you are also using {nameof(AngularCliMiddleware)}.");
            }

            return angularCliMiddleware.StartAngularCliBuilderAsync(_cliAppName);
        }
    }
}
