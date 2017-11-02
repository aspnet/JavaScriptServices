// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.AspNetCore.SpaServices;
using Microsoft.AspNetCore.SpaServices.Prerendering;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Extension methods for configuring prerendering of a Single Page Application.
    /// </summary>
    public static class SpaPrerenderingExtensions
    {
        /// <summary>
        /// Enables server-side prerendering middleware for a Single Page Application.
        /// </summary>
        /// <param name="appBuilder">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="entryPoint">The path, relative to your application root, of the JavaScript file containing prerendering logic.</param>
        /// <param name="buildOnDemand">Optional. If specified, executes the supplied <see cref="ISpaPrerendererBuilder"/> before looking for the <paramref name="entryPoint"/> file. This is only intended to be used during development.</param>
        public static void UseSpaPrerendering(
            this IApplicationBuilder appBuilder,
            string entryPoint,
            ISpaPrerendererBuilder buildOnDemand = null)
        {
            if (string.IsNullOrEmpty(entryPoint))
            {
                throw new ArgumentException("Cannot be null or empty", nameof(entryPoint));
            }

            var defaultPageMiddleware = SpaDefaultPageMiddleware.FindInPipeline(appBuilder);
            if (defaultPageMiddleware == null)
            {
                throw new Exception($"{nameof(UseSpaPrerendering)} should be called inside the 'configure' callback of a call to {nameof(SpaApplicationBuilderExtensions.UseSpa)}.");
            }

            var urlPrefix = defaultPageMiddleware.UrlPrefix;
            if (urlPrefix == null || urlPrefix.Length < 2)
            {
                throw new ArgumentException(
                    "If you are using server-side prerendering, the SPA's public path must be " +
                    "set to a non-empty and non-root value. This makes it possible to identify " +
                    "requests for the SPA's internal static resources, so the prerenderer knows " +
                    "not to return prerendered HTML for those requests.",
                    nameof(urlPrefix));
            }

            // We only want to start one build-on-demand task, but it can't commence until
            // a request comes in (because we need to wait for all middleware to be configured)
            var lazyBuildOnDemandTask = new Lazy<Task>(() => buildOnDemand?.Build(appBuilder));

            // Get all the necessary context info that will be used for each prerendering call
            var serviceProvider = appBuilder.ApplicationServices;
            var nodeServices = GetNodeServices(serviceProvider);
            var applicationStoppingToken = serviceProvider.GetRequiredService<IApplicationLifetime>()
                .ApplicationStopping;
            var applicationBasePath = serviceProvider.GetRequiredService<IHostingEnvironment>()
                .ContentRootPath;
            var moduleExport = new JavaScriptModuleExport(entryPoint);
            var urlPrefixAsPathString = new PathString(urlPrefix);

            // Add the actual middleware that intercepts requests for the SPA default file
            // and invokes the prerendering code
            appBuilder.Use(async (context, next) =>
            {
                // Don't interfere with requests that are within the SPA's urlPrefix, because
                // these requests are meant to serve its internal resources (.js, .css, etc.)
                if (context.Request.Path.StartsWithSegments(urlPrefixAsPathString))
                {
                    await next();
                    return;
                }

                // If we're building on demand, do that first
                var buildOnDemandTask = lazyBuildOnDemandTask.Value;
                if (buildOnDemandTask != null && !buildOnDemandTask.IsCompleted)
                {
                    await buildOnDemandTask;
                }

                // As a workaround for @angular/cli not emitting the index.html in 'server'
                // builds, pass through a URL that can be used for obtaining it. Longer term,
                // remove this.
                var customData = new
                {
                    templateUrl = GetDefaultFileAbsoluteUrl(context, defaultPageMiddleware.DefaultPageUrl)
                };

                // TODO: Add an optional "supplyCustomData" callback param so people using
                //       UsePrerendering() can, for example, pass through cookies into the .ts code

                var renderResult = await Prerenderer.RenderToString(
                    applicationBasePath,
                    nodeServices,
                    applicationStoppingToken,
                    moduleExport,
                    context,
                    customDataParameter: customData,
                    timeoutMilliseconds: 0);

                await ApplyRenderResult(context, renderResult);
            });
        }

        private static async Task ApplyRenderResult(HttpContext context, RenderToStringResult renderResult)
        {
            if (!string.IsNullOrEmpty(renderResult.RedirectUrl))
            {
                context.Response.Redirect(renderResult.RedirectUrl);
            }
            else
            {
                // The Globals property exists for back-compatibility but is meaningless
                // for prerendering that returns complete HTML pages
                if (renderResult.Globals != null)
                {
                    throw new Exception($"{nameof(renderResult.Globals)} is not supported when prerendering via {nameof(UseSpaPrerendering)}(). Instead, your prerendering logic should return a complete HTML page, in which you embed any information you wish to return to the client.");
                }

                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(renderResult.Html);
            }
        }

        private static string GetDefaultFileAbsoluteUrl(HttpContext context, string defaultPageUrl)
        {
            var req = context.Request;
            var defaultFileAbsoluteUrl = UriHelper.BuildAbsolute(
                req.Scheme, req.Host, req.PathBase, defaultPageUrl);
            return defaultFileAbsoluteUrl;
        }

        private static INodeServices GetNodeServices(IServiceProvider serviceProvider)
        {
            // Use the registered instance, or create a new private instance if none is registered
            var instance = (INodeServices)serviceProvider.GetService(typeof(INodeServices));
            return instance ?? NodeServicesFactory.CreateNodeServices(
                new NodeServicesOptions(serviceProvider));
        }
    }
}
