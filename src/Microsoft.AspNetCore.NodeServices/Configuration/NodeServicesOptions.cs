using System;
using Microsoft.AspNetCore.NodeServices.HostingModels;
using Microsoft.AspNetCore.NodeServices.Util;

namespace Microsoft.AspNetCore.NodeServices
{
    public class NodeServicesOptions
    {
        public const NodeHostingModel DefaultNodeHostingModel = NodeHostingModel.Http;

        private static readonly string[] DefaultWatchFileExtensions = { ".js", ".jsx", ".ts", ".tsx", ".json", ".html" };

        public NodeServicesOptions()
        {
            HostingModel = DefaultNodeHostingModel;
            WatchFileExtensions = (string[])DefaultWatchFileExtensions.Clone();
        }

        public NodeHostingModel HostingModel { get; set; }
        public Func<INodeInstance> NodeInstanceFactory { get; set; }
        public string ProjectPath { get; set; }
        public string[] WatchFileExtensions { get; set; }
        public INodeInstanceOutputLogger NodeInstanceOutputLogger { get; set; }
    }
}