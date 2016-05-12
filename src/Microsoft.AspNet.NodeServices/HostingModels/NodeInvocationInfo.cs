using System.Collections.Generic;

namespace Microsoft.AspNet.NodeServices
{
    public class NodeInvocationInfo
    {
        public NodeInvocationInfo() { }
        public string ModuleName { get; set; }
        public string ExportedFunctionName { get; set; }
        public IList<object> Payload { get; set; } = new List<object>();
        public IList<object> Args { get; set; } = new List<object>();
    }
}
