using Microsoft.AspNet.SpaServices.Prerendering;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.AspNet.SpaServices
{
    public static class Configuration
    {
        public static void AddPrerender(this IServiceCollection serviceCollection)
            => AddPrerender(serviceCollection, _ => new PrerenderOptions());

        public static void AddPrerender(this IServiceCollection serviceCollection, Func<IServiceProvider, PrerenderOptions> optionsFactory)
        {
            serviceCollection.AddTransient(typeof(PrerenderOptions), optionsFactory);
            serviceCollection.AddSingleton(typeof(IPrerenderer), serviceProvider => new Prerenderer(serviceProvider));
        }
    }
}
