﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.AspNetCore.SpaServices.Prerendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/>.</param>
        /// <param name="entryPoint">The path, relative to your application root, of the JavaScript file containing prerendering logic.</param>
        /// <param name="buildOnDemand">Optional. If specified, executes the supplied <see cref="ISpaPrerendererBuilder"/> before looking for the <paramref name="entryPoint"/> file. This is only intended to be used during development.</param>
        /// <param name="excludeUrls">Optional. If specified, requests within these URL paths will bypass the prerenderer.</param>
        /// <param name="supplyData">Optional. If specified, this callback will be invoked during prerendering, allowing you to pass additional data to the prerendering entrypoint code.</param>
        public static void UseSpaPrerendering(
            this IApplicationBuilder applicationBuilder,
            string entryPoint,
            ISpaPrerendererBuilder buildOnDemand = null,
            string[] excludeUrls = null,
            Action<HttpContext, IDictionary<string, object>> supplyData = null)
        {
            if (string.IsNullOrEmpty(entryPoint))
            {
                throw new ArgumentException("Cannot be null or empty", nameof(entryPoint));
            }

            // If we're building on demand, start that process now
            var buildOnDemandTask = buildOnDemand?.Build(applicationBuilder);

            // Get all the necessary context info that will be used for each prerendering call
            var serviceProvider = applicationBuilder.ApplicationServices;
            var nodeServices = GetNodeServices(serviceProvider);
            var applicationStoppingToken = serviceProvider.GetRequiredService<IApplicationLifetime>()
                .ApplicationStopping;
            var applicationBasePath = serviceProvider.GetRequiredService<IHostingEnvironment>()
                .ContentRootPath;
            var moduleExport = new JavaScriptModuleExport(entryPoint);
            var excludePathStrings = (excludeUrls ?? Array.Empty<string>())
                .Select(url => new PathString(url))
                .ToArray();

            // Capture the non-prerendered responses, which in production will typically only
            // be returning the default SPA index.html page (because other resources will be
            // served statically from disk). We will use this as a template in which to inject
            // the prerendered output.
            applicationBuilder.Use(async (context, next) =>
            {
                // If this URL is excluded, skip prerendering
                foreach (var excludePathString in excludePathStrings)
                {
                    if (context.Request.Path.StartsWithSegments(excludePathString))
                    {
                        await next();
                        return;
                    }
                }

                // If we're building on demand, do that first
                if (buildOnDemandTask != null)
                {
                    await buildOnDemandTask;
                }

                // It's no good if we try to return a 304. We need to capture the actual
                // HTML content so it can be passed as a template to the prerenderer.
                RemoveConditionalRequestHeaders(context.Request);

                using (var outputBuffer = new MemoryStream())
                {
                    var originalResponseStream = context.Response.Body;
                    context.Response.Body = outputBuffer;

                    try
                    {
                        await next();
                        outputBuffer.Seek(0, SeekOrigin.Begin);
                    }
                    finally
                    {
                        context.Response.Body = originalResponseStream;
                    }

                    // If it's not a success response, we're not going to have any template HTML
                    // to pass to the prerenderer.
                    if (context.Response.StatusCode < 200 || context.Response.StatusCode >= 300)
                    {
                        var message = $"Prerendering failed because no HTML template could be obtained. Check that your SPA is compiling without errors. The {nameof(SpaApplicationBuilderExtensions.UseSpa)}() middleware returned a response with status code {context.Response.StatusCode}";
                        if (outputBuffer.Length > 0)
                        {
                            message += " and the following content: " + Encoding.UTF8.GetString(outputBuffer.GetBuffer());
                        }

                        throw new InvalidOperationException(message);
                    }

                    // Most prerendering logic will want to know about the original, unprerendered
                    // HTML that the client would be getting otherwise. Typically this is used as
                    // a template from which the fully prerendered page can be generated.
                    var customData = new Dictionary<string, object>
                    {
                        { "originalHtml", Encoding.UTF8.GetString(outputBuffer.GetBuffer()) }
                    };

                    supplyData?.Invoke(context, customData);

                    var (unencodedAbsoluteUrl, unencodedPathAndQuery) = GetUnencodedUrlAndPathQuery(context);
                    var renderResult = await Prerenderer.RenderToString(
                        applicationBasePath,
                        nodeServices,
                        applicationStoppingToken,
                        moduleExport,
                        unencodedAbsoluteUrl,
                        unencodedPathAndQuery,
                        customDataParameter: customData,
                        timeoutMilliseconds: 0,
                        requestPathBase: context.Request.PathBase.ToString());

                    await ApplyRenderResult(context, renderResult);
                }
            });
        }

        private static void RemoveConditionalRequestHeaders(HttpRequest request)
        {
            request.Headers.Remove(HeaderNames.IfMatch);
            request.Headers.Remove(HeaderNames.IfModifiedSince);
            request.Headers.Remove(HeaderNames.IfNoneMatch);
            request.Headers.Remove(HeaderNames.IfUnmodifiedSince);
            request.Headers.Remove(HeaderNames.IfRange);
        }

        private static (string, string) GetUnencodedUrlAndPathQuery(HttpContext httpContext)
        {
            // This is a duplicate of code from Prerenderer.cs in the SpaServices package.
            // Once the SpaServices.Extension package implementation gets merged back into
            // SpaServices, this duplicate can be removed. To remove this, change the code
            // above that calls Prerenderer.RenderToString to use the internal overload
            // that takes an HttpContext instead of a url/path+query pair.
            var requestFeature = httpContext.Features.Get<IHttpRequestFeature>();
            var unencodedPathAndQuery = requestFeature.RawTarget;
            var request = httpContext.Request;
            var unencodedAbsoluteUrl = $"{request.Scheme}://{request.Host}{unencodedPathAndQuery}";
            return (unencodedAbsoluteUrl, unencodedPathAndQuery);
        }

        private static async Task ApplyRenderResult(HttpContext context, RenderToStringResult renderResult)
        {
            context.Response.Clear();

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
