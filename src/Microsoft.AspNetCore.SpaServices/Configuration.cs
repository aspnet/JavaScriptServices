using Microsoft.AspNetCore.SpaServices.Prerendering;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.AspNetCore.SpaServices
{
    public static class Configuration
    {
        public static void AddPrerender(this IServiceCollection serviceCollection) => AddPrerender(serviceCollection, _ => new PrerenderOptions());

        public static void AddPrerender(this IServiceCollection services, Func<IServiceProvider, PrerenderOptions> optionsFactory)
        {
            services.AddTransient(typeof(PrerenderOptions), optionsFactory);
            services.AddSingleton(typeof(IPrerenderer), serviceProvider => new Prerenderer(serviceProvider));
        }
    }
}
