using CMS.Base;
using CMS.Helpers;
using CMS.SiteProvider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using XperienceCommunity.Localizer.Internal;

namespace XperienceCommunity.Localizer
{
    public static class XperienceLocalizerExtension
    {
        /// <summary>
        /// Adds Kentico Localization to the base StringLocalizerFactory, thus adding fallback to all IStringLocalizer / IHtmlLocalizer generated
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddXperienceLocalizer(this IServiceCollection services)
        {
            services.Decorate<IStringLocalizerFactory>((inner, provider) =>
            {
                var siteInfoProvider = provider.GetService<ISiteInfoProvider>();
                var progressiveCache = provider.GetService<IProgressiveCache>();
                var siteService = provider.GetService<ISiteService>();
                return new XperienceStringLocalizerFactory(inner, siteInfoProvider, progressiveCache, siteService);
            });
            return services;
        }
    }
}
