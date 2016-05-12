using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Extensions;
using Microsoft.AspNet.NodeServices;
using Microsoft.Extensions.PlatformAbstractions;
using System;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SpaServices.Prerendering
{
    public class Prerenderer : IPrerenderer
    {
        private static Lazy<StringAsTempFile> _nodeScript = new Lazy<StringAsTempFile>(() =>
        {
            var script = EmbeddedResourceReader.Read(typeof(Prerenderer), "/Content/Node/prerenderer.js");
            return new StringAsTempFile(script);
        });

        private readonly INodeServices _nodeServices;
        private readonly IApplicationEnvironment _appEnv;
        private readonly PrerenderOptions _options;
        private readonly IHttpContextAccessor _contextAccessor;

        public Prerenderer(IServiceProvider serviceProvider)
        {
            this._contextAccessor = (IHttpContextAccessor)serviceProvider.GetService(typeof(IHttpContextAccessor));
            this._appEnv = (IApplicationEnvironment)serviceProvider.GetService(typeof(IApplicationEnvironment));
            this._options = (PrerenderOptions)serviceProvider.GetService(typeof(PrerenderOptions)) ?? new PrerenderOptions();
            this._options.FileName = this._options.FileName ?? _nodeScript.Value.FileName;
            this._nodeServices = (INodeServices)serviceProvider.GetService(typeof(INodeServices)) ?? NodeServices.Configuration.CreateNodeServices(new NodeServicesOptions
            {
                HostingModel = NodeHostingModel.Http,
                ProjectPath = _appEnv.ApplicationBasePath
            });
        }

        public Task<RenderToStringResult> RenderToString(JavaScriptModuleExport module)
            => RenderToString(_appEnv.ApplicationBasePath, module);

        public Task<RenderToStringResult> RenderToString(string basePath, JavaScriptModuleExport module)
            => _nodeServices.Invoke<RenderToStringResult>(new NodeInvocationInfo
            {
                ExportedFunctionName = _options.ExportedFunctionName,
                ModuleName = _options.FileName,
                Args = new object[]
                {
                    basePath,
                    module,
                    UriHelper.GetEncodedUrl(_contextAccessor.HttpContext.Request),
                    _contextAccessor.HttpContext.Request.Path + _contextAccessor.HttpContext.Request.QueryString.Value,
                    _contextAccessor.HttpContext.Request.Cookies,
                    _options.PayloadProvider(_contextAccessor.HttpContext)
                },
                Payload = _options.PayloadProvider(_contextAccessor.HttpContext)
            });
    }
}
