using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SpaServices.Prerendering
{
	public class RenderToStringResult
	{
		public string Html { get; set; }
		public JObject Globals { get; set; }
	}
}
