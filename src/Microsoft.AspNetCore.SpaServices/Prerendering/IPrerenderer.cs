using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SpaServices.Prerendering
{
    public interface IPrerenderer
    {
        Task<RenderToStringResult> RenderToString(HttpContext context, JavaScriptModuleExport module, object model = null);
        Task<RenderToStringResult> RenderToString(string basePath, HttpContext context, JavaScriptModuleExport module, object model = null);
    }
}
