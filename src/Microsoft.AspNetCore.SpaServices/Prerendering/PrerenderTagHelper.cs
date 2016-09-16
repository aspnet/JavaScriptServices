using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SpaServices.Prerendering
{
    [HtmlTargetElement(Attributes = PrerenderModuleAttributeName)]
    public class PrerenderTagHelper : TagHelper
    {
        private const string PrerenderModuleAttributeName = "asp-prerender-module";
        private const string PrerenderExportAttributeName = "asp-prerender-export";
        private const string PrerenderWebpackConfigAttributeName = "asp-prerender-webpack-config";
        private const string PrerenderDataAttributeName = "asp-prerender-data";
        private const string PrerenderTimeoutAttributeName = "asp-prerender-timeout";
        private static INodeServices _fallbackNodeServices; // Used only if no INodeServices was registered with DI

        private readonly string _applicationBasePath;
        private readonly INodeServices _nodeServices;

        public PrerenderTagHelper(IServiceProvider serviceProvider)
        {
            var hostEnv = (IHostingEnvironment) serviceProvider.GetService(typeof(IHostingEnvironment));
            _nodeServices = (INodeServices) serviceProvider.GetService(typeof(INodeServices)) ?? _fallbackNodeServices;
            _applicationBasePath = hostEnv.ContentRootPath;

            // Consider removing the following. Having it means you can get away with not putting app.AddNodeServices()
            // in your startup file, but then again it might be confusing that you don't need to.
            if (_nodeServices == null)
            {
                _nodeServices = _fallbackNodeServices = NodeServicesFactory.CreateNodeServices(
                    new NodeServicesOptions(serviceProvider));
            }
        }

        [HtmlAttributeName(PrerenderModuleAttributeName)]
        public string ModuleName { get; set; }

        [HtmlAttributeName(PrerenderExportAttributeName)]
        public string ExportName { get; set; }

        [HtmlAttributeName(PrerenderWebpackConfigAttributeName)]
        public string WebpackConfigPath { get; set; }

        [HtmlAttributeName(PrerenderDataAttributeName)]
        public object CustomDataParameter { get; set; }

        [HtmlAttributeName(PrerenderTimeoutAttributeName)]
        public int TimeoutMillisecondsParameter { get; set; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            // We want to pass the original, unencoded incoming URL data through to Node, so that
            // server-side code has the same view of the URL as client-side code (on the client,
            // location.pathname returns an unencoded string).
            // The following logic handles special characters in URL paths in the same way that
            // Node and client-side JS does. For example, the path "/a=b%20c" gets passed through
            // unchanged (whereas other .NET APIs do change it - Path.Value will return it as
            // "/a=b c" and Path.ToString() will return it as "/a%3db%20c")
            var requestFeature = ViewContext.HttpContext.Features.Get<IHttpRequestFeature>();
            var unencodedPathAndQuery = requestFeature.RawTarget;

            var request = ViewContext.HttpContext.Request;
            var unencodedAbsoluteUrl = $"{request.Scheme}://{request.Host}{unencodedPathAndQuery}";

            var result = await Prerenderer.RenderToString(
                _applicationBasePath,
                _nodeServices,
                new JavaScriptModuleExport(ModuleName)
                {
                    ExportName = ExportName,
                    WebpackConfig = WebpackConfigPath
                },
                unencodedAbsoluteUrl,
                unencodedPathAndQuery,
                CustomDataParameter,
                TimeoutMillisecondsParameter);

            if (!string.IsNullOrEmpty(result.RedirectUrl))
            {
                // It's a redirection
                ViewContext.HttpContext.Response.Redirect(result.RedirectUrl);
                return;
            }

            // It's some HTML to inject
            output.Content.SetHtmlContent(result.Html);

            // Also attach any specified globals to the 'window' object. This is useful for transferring
            // general state between server and client.
            if (result.Globals != null)
            {
                var stringBuilder = new StringBuilder();
                foreach (var property in result.Globals.Properties())
                {
                    stringBuilder.AppendFormat("window.{0} = {1};",
                        property.Name,
                        property.Value.ToString(Formatting.None));
                }
                if (stringBuilder.Length > 0)
                {
                    output.PostElement.SetHtmlContent($"<script>{stringBuilder}</script>");
                }
            }
        }
    }
}