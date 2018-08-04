using Microsoft.AspNetCore.SpaServices;
using Microsoft.AspNetCore.SpaServices.NpmCommandDevelopmentServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace VueMiddlewareExample.VueDevelopmentServer
{
    public static class VueDevelopmentServerMiddlewareExtensions
    {
        private static TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(5); // This is a development-time only feature, so a very long timeout is fine

        /// <summary>
        /// Handles requests by passing them through to an instance of the create-react-app server.
        /// This means you can always serve up-to-date CLI-built resources without having
        /// to run the create-react-app server manually.
        ///
        /// This feature should only be used in development. For production deployments, be
        /// sure not to enable the create-react-app server.
        /// </summary>
        /// <param name="spaBuilder">The <see cref="ISpaBuilder"/>.</param>
        /// <param name="npmScript">The name of the script in your package.json file that launches the create-react-app server.</param>
        public static void UseVueDevelopmentServer(
            this ISpaBuilder spaBuilder,
            string npmScript)
        {
            if (spaBuilder == null)
            {
                throw new ArgumentNullException(nameof(spaBuilder));
            }

            var spaOptions = spaBuilder.Options;

            if (string.IsNullOrEmpty(spaOptions.SourcePath))
            {
                throw new InvalidOperationException($"To use {nameof(UseVueDevelopmentServer)}, you must supply a non-empty value for the {nameof(SpaOptions.SourcePath)} property of {nameof(SpaOptions)} when calling {nameof(SpaApplicationBuilderExtensions.UseSpa)}.");
            }

            var startRegex = new Regex("INFO  Starting development server...", RegexOptions.None, RegexMatchTimeout);
            NpmCommandDevelopmentServerMiddleware.Attach(spaBuilder, npmScript, p => null, CreateEnvVars, startRegex, null);
        }

        private static Dictionary<string, string> CreateEnvVars(PreStartNpmServerInfo info)
        {
            return new Dictionary<string, string>
            {
                { "PORT", info.Port.ToString() },
                { "BROWSER", "none" }, // We don't want create-react-app to open its own extra browser window pointing to the internal dev server port
            };
        }
    }
}
