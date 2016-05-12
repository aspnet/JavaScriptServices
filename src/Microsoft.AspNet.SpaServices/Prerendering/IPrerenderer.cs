using System.Threading.Tasks;

namespace Microsoft.AspNet.SpaServices.Prerendering
{
    public interface IPrerenderer
    {
        Task<RenderToStringResult> RenderToString(JavaScriptModuleExport module);
        Task<RenderToStringResult> RenderToString(string basePath, JavaScriptModuleExport module);
    }
}
