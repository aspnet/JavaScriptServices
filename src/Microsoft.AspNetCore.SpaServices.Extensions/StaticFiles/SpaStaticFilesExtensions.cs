﻿// Copyright (c) .NET Foundation. All rights reserved.
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
        /// Registers an <see cref="ISpaStaticFileProvider"/> service that can provide static
        /// files to be served for a Single Page Application (SPA).
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configuration">If specified, this callback will be invoked to set additional configuration options.</param>
        public static void AddSpaStaticFiles(
            this IServiceCollection services,
            Action<SpaStaticFilesOptions> configuration = null)
        {
            services.AddSingleton<ISpaStaticFileProvider>(serviceProvider =>
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

                return new DefaultSpaStaticFileProvider(serviceProvider, options);
            });
        }

        /// <summary>
        /// Configures the application to serve static files for a Single Page Application (SPA).
        /// The files will be located using the registered <see cref="ISpaStaticFileProvider"/> service.
        /// </summary>
        /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/>.</param>
        public static void UseSpaStaticFiles(this IApplicationBuilder applicationBuilder)
        {
            if (applicationBuilder == null)
            {
                throw new ArgumentNullException(nameof(applicationBuilder));
            }

            UseSpaStaticFilesInternal(applicationBuilder,
                overrideFileProvider: null,
                allowFallbackOnServingWebRootFiles: false);
        }

        internal static void UseSpaStaticFilesInternal(
            this IApplicationBuilder app,
            IFileProvider overrideFileProvider,
            bool allowFallbackOnServingWebRootFiles)
        {
            var shouldServeStaticFiles = ShouldServeStaticFiles(
                app,
                overrideFileProvider,
                allowFallbackOnServingWebRootFiles,
                out var fileProviderOrDefault);

            if (shouldServeStaticFiles)
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = fileProviderOrDefault
                });
            }
        }

        private static bool ShouldServeStaticFiles(
            IApplicationBuilder app,
            IFileProvider overrideFileProvider,
            bool allowFallbackOnServingWebRootFiles,
            out IFileProvider fileProviderOrDefault)
        {
            if (overrideFileProvider != null)
            {
                // If the file provider was explicitly supplied, that takes precedence over any other
                // configured file provider. This is most useful if the application hosts multiple SPAs
                // (via multiple calls to UseSpa()), so each needs to serve its own separate static files
                // instead of using AddSpaStaticFiles/UseSpaStaticFiles.
                fileProviderOrDefault = overrideFileProvider;
                return true;
            }

            var spaStaticFilesService = app.ApplicationServices.GetService<ISpaStaticFileProvider>();
            if (spaStaticFilesService != null)
            {
                // If an ISpaStaticFileProvider was configured but it says no IFileProvider is available
                // (i.e., it supplies 'null'), this implies we should not serve any static files. This
                // is typically the case in development when SPA static files are being served from a
                // SPA development server (e.g., Angular CLI or create-react-app), in which case no
                // directory of prebuilt files will exist on disk.
                fileProviderOrDefault = spaStaticFilesService.FileProvider;
                return fileProviderOrDefault != null;
            }
            else if (!allowFallbackOnServingWebRootFiles)
            {
                throw new InvalidOperationException($"To use {nameof(UseSpaStaticFiles)}, you must " +
                    $"first register an {nameof(ISpaStaticFileProvider)} in the service provider, typically " +
                    $"by calling services.{nameof(AddSpaStaticFiles)}.");
            }
            else
            {
                // Fall back on serving wwwroot
                fileProviderOrDefault = null;
                return true;
            }
        }
    }
}
