using Microsoft.AspNet.Http;
using Microsoft.AspNet.NodeServices;
using Microsoft.AspNet.SpaServices.Prerendering;
using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.SpaServices
{
    public class PrerenderOptions
    {
        public Func<HttpContext, object[]> PayloadProvider { get; set; } = c => new object[0];

        public string FileName { get; set; }

        public string ExportedFunctionName { get; set; } = "renderToString";
    }
}
