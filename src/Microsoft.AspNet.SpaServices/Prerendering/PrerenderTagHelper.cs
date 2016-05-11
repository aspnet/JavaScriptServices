using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Extensions;
using Microsoft.AspNet.NodeServices;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;

namespace Microsoft.AspNet.SpaServices.Prerendering
{
    [HtmlTargetElement(Attributes = PrerenderModuleAttributeName)]
    public class PrerenderTagHelper : TagHelper
    {
        private readonly PrerenderOptions _prerenderOptions;
        static INodeServices fallbackNodeServices; // Used only if no INodeServices was registered with DI

        const string PrerenderModuleAttributeName = "asp-prerender-module";
        const string PrerenderExportAttributeName = "asp-prerender-export";
        const string PrerenderWebpackConfigAttributeName = "asp-prerender-webpack-config";

        [HtmlAttributeName(PrerenderModuleAttributeName)]
        public string ModuleName { get; set; }

        [HtmlAttributeName(PrerenderExportAttributeName)]
        public string ExportName { get; set; }

        [HtmlAttributeName(PrerenderWebpackConfigAttributeName)]
        public string WebpackConfigPath { get; set; }

        private string applicationBasePath;
        private IHttpContextAccessor contextAccessor;
        private INodeServices nodeServices;

        public PrerenderTagHelper(IServiceProvider serviceProvider, IHttpContextAccessor contextAccessor, PrerenderOptions prerenderOptions)
        {
            var appEnv = (IApplicationEnvironment)serviceProvider.GetService(typeof(IApplicationEnvironment));
            this.contextAccessor = contextAccessor;
            this.nodeServices = (INodeServices)serviceProvider.GetService(typeof(INodeServices)) ?? fallbackNodeServices;
            this.applicationBasePath = appEnv.ApplicationBasePath;

            // Consider removing the following. Having it means you can get away with not putting app.AddNodeServices()
            // in your startup file, but then again it might be confusing that you don't need to.
            if (this.nodeServices == null)
            {
                this.nodeServices = fallbackNodeServices = NodeServices.Configuration.CreateNodeServices(new NodeServicesOptions
                {
                    HostingModel = NodeHostingModel.Http,
                    ProjectPath = this.applicationBasePath
                });
            }

            _prerenderOptions = prerenderOptions ?? new PrerenderOptions();
            _prerenderOptions.BeforeRender = (context, info) =>
            {
                info.Args.Add(this.applicationBasePath);
                info.Args.Add(new JavaScriptModuleExport(this.ModuleName)
                {
                    exportName = this.ExportName,
                    webpackConfig = this.WebpackConfigPath
                });
                info.Args.Add(UriHelper.GetEncodedUrl(this.contextAccessor.HttpContext.Request));
                info.Args.Add(this.contextAccessor.HttpContext.Request.Path + this.contextAccessor.HttpContext.Request.QueryString.Value);
                info.Args.Add(this.contextAccessor.HttpContext.Request.Cookies);
            };
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            var request = this.contextAccessor.HttpContext.Request;
            var invocationInfo = new NodeInvocationInfo();
            this._prerenderOptions.BeforeRender(this.contextAccessor.HttpContext, invocationInfo);
            var result = await this.nodeServices.Invoke<RenderToStringResult>(invocationInfo);
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
