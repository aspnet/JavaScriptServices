using Microsoft.AspNet.Razor.TagHelpers;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SpaServices.Prerendering
{
    [HtmlTargetElement(Attributes = PrerenderModuleAttributeName)]
    public class PrerenderTagHelper : TagHelper
    {
        private readonly IPrerenderer _prerenderer;

        const string PrerenderModuleAttributeName = "asp-prerender-module";
        const string PrerenderExportAttributeName = "asp-prerender-export";
        const string PrerenderWebpackConfigAttributeName = "asp-prerender-webpack-config";

        [HtmlAttributeName(PrerenderModuleAttributeName)]
        public string ModuleName { get; set; }

        [HtmlAttributeName(PrerenderExportAttributeName)]
        public string ExportName { get; set; }

        [HtmlAttributeName(PrerenderWebpackConfigAttributeName)]
        public string WebpackConfigPath { get; set; }

        public PrerenderTagHelper(IServiceProvider serviceProvider)
        {
            this._prerenderer = (IPrerenderer)serviceProvider.GetService(typeof(IPrerenderer)) ?? new Prerenderer(serviceProvider);
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var result = await this._prerenderer.RenderToString(new JavaScriptModuleExport(this.ModuleName)
            {
                exportName = this.ExportName,
                webpackConfig = this.WebpackConfigPath
            });

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
                    output.PostElement.SetHtmlContent($"<script>{ stringBuilder.ToString() }</script>");
                }
            }
        }
    }
}
