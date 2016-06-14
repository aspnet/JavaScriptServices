using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SpaServices.Prerendering
{
    [HtmlTargetElement(Attributes = PrerenderModuleAttributeName)]
    public class PrerenderTagHelper : TagHelper
    {
        private const string PrerenderModelAttributeName = "asp-prerender-model";
        private const string PrerenderModuleAttributeName = "asp-prerender-module";
        private const string PrerenderExportAttributeName = "asp-prerender-export";
        private const string PrerenderWebpackConfigAttributeName = "asp-prerender-webpack-config";

        private readonly IPrerenderer _prerenderer;

        public PrerenderTagHelper(IServiceProvider serviceProvider)
        {
            _prerenderer = (IPrerenderer)serviceProvider.GetService(typeof(IPrerenderer)) ?? new Prerenderer(serviceProvider);
        }

        [HtmlAttributeName(PrerenderModuleAttributeName)]
        public string ModuleName { get; set; }

        [HtmlAttributeName(PrerenderExportAttributeName)]
        public string ExportName { get; set; }

        [HtmlAttributeName(PrerenderWebpackConfigAttributeName)]
        public string WebpackConfigPath { get; set; }

        [HtmlAttributeName(PrerenderModelAttributeName)]
        public object Model { get; set; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var result = await _prerenderer.RenderToString(
                ViewContext.HttpContext,
                new JavaScriptModuleExport(ModuleName)
                {
                    ExportName = ExportName,
                    WebpackConfig = WebpackConfigPath
                },
                Model);

            output.Content.SetHtmlContent(result.Html);

            if (result.Globals != null)
            {
                var stringBuilder = new StringBuilder();
                foreach (var property in result.Globals.Properties())
                {
                    stringBuilder.Append($"window.{property.Name} = {property.Value.ToString(Formatting.None)};");
                }
                if (stringBuilder.Length > 0)
                {
                    output.PostElement.SetHtmlContent($"<script>{ stringBuilder.ToString() }</script>");
                }
            }
        }
    }
}