namespace Microsoft.AspNet.SpaServices.Prerendering
{
	public class JavaScriptModuleExport
	{
		public string moduleName { get; private set; }
		public string exportName { get; set; }
		public string webpackConfig { get; set; }

		public JavaScriptModuleExport(string moduleName)
		{
			this.moduleName = moduleName;
		}
	}
}
