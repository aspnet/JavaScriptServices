using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Extensions;

namespace Microsoft.AspNetCore.SpaServices.Prerendering
{
    public class Prerenderer : IPrerenderer
    {
        private static Lazy<StringAsTempFile> _nodeScript = new Lazy<StringAsTempFile>(() =>
        {
            var script = EmbeddedResourceReader.Read(typeof(Prerenderer), "/Content/Node/prerenderer.js");
            return new StringAsTempFile(script);
        });

        private readonly string _applicationBasePath;
        private readonly INodeServices _nodeServices;
        private readonly PrerenderOptions _options;

        public Prerenderer(IServiceProvider serviceProvider)
        {
            _applicationBasePath = ((IHostingEnvironment)serviceProvider.GetService(typeof(IHostingEnvironment))).ContentRootPath;
            this._options = (PrerenderOptions)serviceProvider.GetService(typeof(PrerenderOptions)) ?? new PrerenderOptions();
            this._options.FileName = this._options.FileName ?? _nodeScript.Value.FileName;
            this._nodeServices = (INodeServices)serviceProvider.GetService(typeof(INodeServices)) ?? NodeServices.Configuration.CreateNodeServices(new NodeServicesOptions
            {
                HostingModel = NodeHostingModel.Http,
                ProjectPath = _applicationBasePath
            });
        }

        public Task<RenderToStringResult> RenderToString(HttpContext context, JavaScriptModuleExport module, object model = null)
            => RenderToString(_applicationBasePath, context, module, model);

        public Task<RenderToStringResult> RenderToString(string basePath, HttpContext context, JavaScriptModuleExport module, object model = null)
            => _nodeServices.Invoke<RenderToStringResult>(new NodeInvocationInfo
            {
                ExportedFunctionName = _options.ExportedFunctionName,
                ModuleName = _options.FileName,
                Args = new object[]
                {
                    basePath,
                    module,
                    UriHelper.GetEncodedUrl(context.Request),
                    context.Request.Path + context.Request.QueryString.Value,
                    context.Request.Cookies,
                    _options.PayloadProvider(context),
                    model
                }
            });
    }
}