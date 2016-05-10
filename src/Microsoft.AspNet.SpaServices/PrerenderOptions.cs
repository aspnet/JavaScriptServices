using Microsoft.AspNet.Http;
using Microsoft.AspNet.NodeServices;
using Microsoft.AspNet.SpaServices.Prerendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SpaServices
{
    public class PrerenderOptions
    {
        private static Lazy<StringAsTempFile> nodeScript = new Lazy<StringAsTempFile>(() =>
        {
            var script = EmbeddedResourceReader.Read(typeof(Prerenderer), "/Content/Node/prerenderer.js");
            return new StringAsTempFile(script); // Will be cleaned up on process exit
        });

        private readonly IList<Action<HttpContext, NodeInvocationInfo>> _innerBefores;

        public Action<HttpContext, NodeInvocationInfo> BeforeRender
        {
            get
            {
                return (context, info) =>
                {
                    foreach (var before in _innerBefores)
                    {
                        before(context, info);
                    }
                };
            }
            set
            {
                _innerBefores.Add(value);
            }
        }

        public string FileName { get; set; } = nodeScript.Value.FileName;

        public string ExportedFunctionName { get; set; } = "renderToString";

        public PrerenderOptions()
        {
            _innerBefores = new List<Action<HttpContext, NodeInvocationInfo>>();
            _innerBefores.Add((context, info) =>
            {
                info.ExportedFunctionName = this.ExportedFunctionName;
                info.ModuleName = this.FileName;
            });
        }
    }
}
