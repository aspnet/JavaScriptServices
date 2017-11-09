// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SpaServices.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring an application to serve static files for a
    /// Single Page Application (SPA).
    /// </summary>
    public static class SpaStaticFilesExtensions
    {
        /// <summary>
        /// Registers an <see cref="ISpaStaticFiles"/> service that can provide static
        /// files to be served for a Single Page Application (SPA).
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configuration">If specified, this callback will be invoked to set additional configuration options.</param>
        public static void AddSpaStaticFiles(
            this IServiceCollection services,
            Action<SpaStaticFilesOptions> configuration = null)
        {
            services.AddSingleton<ISpaStaticFiles>(serviceProvider =>
            {
                // Use the options configured in DI (or blank if none was configured)
                var optionsProvider = serviceProvider.GetService<IOptions<SpaStaticFilesOptions>>();
                var options = optionsProvider.Value;

                // Allow the developer to perform further configuration
                configuration?.Invoke(options);

                if (string.IsNullOrEmpty(options.RootPath))
                {
                    throw new InvalidOperationException($"No {nameof(SpaStaticFilesOptions.RootPath)} " +
                        $"was set on the {nameof(SpaStaticFilesOptions)}.");
                }

                return new DefaultSpaStaticFiles(serviceProvider, options);
            });
        }

        /// <summary>
        /// Configures the application to serve static files for a Single Page Application (SPA).
        /// The files will be located using the registered <see cref="ISpaStaticFiles"/> service.
        /// </summary>
        /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/>.</param>
        public static void UseSpaStaticFiles(this IApplicationBuilder applicationBuilder)
        {
            UseSpaStaticFiles(applicationBuilder,
                overrideFileProvider: null,
                allowFallbackOnServingWebRootFiles: false);
        }

        internal static void UseSpaStaticFiles(
            this IApplicationBuilder app,
            IFileProvider overrideFileProvider,
            bool allowFallbackOnServingWebRootFiles)
        {
            IFileProvider fileProvider;

            if (overrideFileProvider != null)
            {
                fileProvider = overrideFileProvider;
            }
            else
            {
                var spaStaticFilesService = app.ApplicationServices.GetService<ISpaStaticFiles>();
                if (spaStaticFilesService != null)
                {
                    if (spaStaticFilesService.TryGetFileProvider(out var foundFileProvider))
                    {
                        fileProvider = foundFileProvider;
                    }
                    else
                    {
                        // If an ISpaStaticFiles was configured but it says no IFileProvider
                        // is available, then UseSpaStaticFiles is a no-op. This is typically
                        // the case in development when serving files through a SPA development
                        // server (e.g., Angular CLI or create-react-app).
                        return;
                    }
                }
                else if (!allowFallbackOnServingWebRootFiles)
                {
                    throw new InvalidOperationException($"To use {nameof(UseSpaStaticFiles)}, you must " +
                        $"first register an {nameof(ISpaStaticFiles)} in the service provider, typically " +
                        $"by calling services.{nameof(AddSpaStaticFiles)}.");
                }
                else
                {
                    // Fall back on serving wwwroot
                    fileProvider = null;
                }
            }

            app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });
        }
    }
}
