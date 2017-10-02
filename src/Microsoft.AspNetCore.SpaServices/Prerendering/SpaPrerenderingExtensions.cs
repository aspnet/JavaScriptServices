// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.AspNetCore.SpaServices;
using Microsoft.AspNetCore.SpaServices.Prerendering;
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
        /// Adds middleware for server-side prerendering of a Single Page Application.
        /// </summary>
        /// <param name="spaBuilder">The <see cref="ISpaBuilder"/>.</param>
        /// <param name="entryPoint">The path, relative to your application root, of the JavaScript file containing prerendering logic.</param>
        /// <param name="buildOnDemand">Optional. If specified, executes the supplied <see cref="ISpaPrerendererBuilder"/> before looking for the <paramref name="entryPoint"/> file. This is only intended to be used during development.</param>
        public static void UsePrerendering(
            this ISpaBuilder spaBuilder,
            string entryPoint,
            ISpaPrerendererBuilder buildOnDemand = null)
        {
            if (string.IsNullOrEmpty(entryPoint))
            {
                throw new ArgumentException("Cannot be null or empty", nameof(entryPoint));
            }

            // We only want to start one build-on-demand task, but it can't commence until
            // a request comes in (because we need to wait for all middleware to be configured)
            var lazyBuildOnDemandTask = new Lazy<Task>(() => buildOnDemand?.Build(spaBuilder));

            // Get all the necessary context info that will be used for each prerendering call
            var appBuilder = spaBuilder.AppBuilder;
            var serviceProvider = appBuilder.ApplicationServices;
            var nodeServices = GetNodeServices(serviceProvider);
            var applicationStoppingToken = GetRequiredService<IApplicationLifetime>(serviceProvider)
                .ApplicationStopping;
            var applicationBasePath = GetRequiredService<IHostingEnvironment>(serviceProvider)
                .ContentRootPath;
            var moduleExport = new JavaScriptModuleExport(entryPoint);

            // Add the actual middleware that intercepts requests for the SPA default file
            // and invokes the prerendering code
            appBuilder.Use(async (context, next) =>
            {
                // Don't interfere with requests that aren't meant to render the SPA default file
                if (!context.Items.ContainsKey(SpaExtensions.IsSpaFallbackRequestTag))
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

                // If we're waiting for other SPA initialization tasks, do that first.
                await spaBuilder.StartupTasks;

                // As a workaround for @angular/cli not emitting the index.html in 'server'
                // builds, pass through a URL that can be used for obtaining it. Longer term,
                // remove this.
                var customData = new
                {
                    templateUrl = GetDefaultFileAbsoluteUrl(spaBuilder, context)
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

        private static T GetRequiredService<T>(IServiceProvider serviceProvider) where T: class
        {
            return (T)serviceProvider.GetService(typeof(T))
                ?? throw new Exception($"Could not resolve service of type {typeof(T).FullName} in service provider.");
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
                    throw new Exception($"{nameof(renderResult.Globals)} is not supported when prerendering via {nameof(UsePrerendering)}(). Instead, your prerendering logic should return a complete HTML page, in which you embed any information you wish to return to the client.");
                }

                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(renderResult.Html);
            }
        }

        private static string GetDefaultFileAbsoluteUrl(ISpaBuilder spaBuilder, HttpContext context)
        {
            var req = context.Request;
            var defaultFileAbsoluteUrl = UriHelper.BuildAbsolute(
                req.Scheme, req.Host, req.PathBase, spaBuilder.DefaultFilePath);
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
