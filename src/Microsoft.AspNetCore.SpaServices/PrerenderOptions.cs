using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.SpaServices.Prerendering
{
    public class PrerenderOptions
    {
        public Func<HttpContext, object[]> PayloadProvider { get; set; } = context => new object[0];
        public string FileName { get; set; }
        public string ExportedFunctionName { get; set; } = "renderToString";
    }
}