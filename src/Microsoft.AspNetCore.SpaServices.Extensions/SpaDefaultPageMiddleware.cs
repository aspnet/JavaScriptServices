using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;

namespace Microsoft.AspNetCore.SpaServices
{
    internal class SpaDefaultPageMiddleware
    {
        private static readonly string _propertiesKey = Guid.NewGuid().ToString();

        public static SpaDefaultPageMiddleware FindInPipeline(IApplicationBuilder app)
        {
            return app.Properties.TryGetValue(_propertiesKey, out var instance)
                ? (SpaDefaultPageMiddleware)instance
                : null;
        }

        public string UrlPrefix { get; }
        public string DefaultPageUrl { get; }

        public SpaDefaultPageMiddleware(IApplicationBuilder app, string urlPrefix,
            string defaultPage, Action configure)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            UrlPrefix = urlPrefix ?? throw new ArgumentNullException(nameof(urlPrefix));
            DefaultPageUrl = ConstructDefaultPageUrl(urlPrefix, defaultPage);

            // Attach to pipeline, but invoke 'configure' to give the developer a chance
            // to insert extra middleware before the 'default page' pipeline entries
            RegisterSoleInstanceInPipeline(app);
            configure?.Invoke();
            AttachMiddlewareToPipeline(app);
        }

        private void RegisterSoleInstanceInPipeline(IApplicationBuilder app)
        {
            if (app.Properties.ContainsKey(_propertiesKey))
            {
                throw new Exception($"Only one usage of {nameof(SpaApplicationBuilderExtensions.UseSpa)} is allowed in any single branch of the middleware pipeline. This is because one instance would handle all requests.");
            }

            app.Properties[_propertiesKey] = this;
        }

        private void AttachMiddlewareToPipeline(IApplicationBuilder app)
        {
            // Rewrite all requests to the default page
            app.Use((context, next) =>
            {
                context.Request.Path = DefaultPageUrl;
                return next();
            });

            // Serve it as file from disk
            app.UseStaticFiles();

            // If the default file didn't get served as a static file (because it
            // was not present on disk), the SPA is definitely not going to work.
            app.Use((context, next) =>
            {
                var message = $"The SPA default page middleware could not return the default page '{DefaultPageUrl}' because it was not found on disk, and no other middleware handled the request.\n";

                // Try to clarify the common scenario where someone runs an application in
                // Production environment without first publishing the whole application
                // or at least building the SPA.
                var hostEnvironment = (IHostingEnvironment)context.RequestServices.GetService(typeof(IHostingEnvironment));
                if (hostEnvironment != null && hostEnvironment.IsProduction())
                {
                    message += "Your application is running in Production mode, so make sure it has been published, or that you have built your SPA manually. Alternatively you may wish to switch to the Development environment.\n";
                }

                throw new Exception(message);
            });
        }

        private static string ConstructDefaultPageUrl(string urlPrefix, string defaultPage)
        {
            if (string.IsNullOrEmpty(defaultPage))
            {
                defaultPage = "index.html";
            }

            return new PathString(urlPrefix).Add(new PathString("/" + defaultPage));
        }
    }
}
