using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.AspNet.SpaServices
{
    public static class Configuration
    {
        public static void AddPrerender(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient(typeof(PrerenderOptions), r => new PrerenderOptions());
        }

        public static void AddPrerender(this IServiceCollection serviceCollection, Func<IServiceProvider, PrerenderOptions> optionsFactory)
        {
            serviceCollection.AddSingleton(typeof(PrerenderOptions), optionsFactory);
        }
    }
}
