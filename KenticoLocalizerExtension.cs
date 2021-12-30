using CMS.Base;
using CMS.Helpers;
using CMS.SiteProvider;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace XperienceCommunity.HtmlLocalizer
{
    public static class KenticoLocalizerExtension
    {
        /// <summary>
        /// Adds Kentico Localization to the base StringLocalizerFactory, thus adding fallback to all IStringLocalizer / IHtmlLocalizer generated
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddXperienceLocalization(this IServiceCollection services)
        {
            services.Decorate<IStringLocalizerFactory>((inner, provider) =>
            {
                var siteInfoProvider = provider.GetService<ISiteInfoProvider>();
                var progressiveCache = provider.GetService<IProgressiveCache>();
                var siteService = provider.GetService<ISiteService>();
                return new KenticoStringLocalizerFactory(inner, siteInfoProvider, progressiveCache, siteService);
            });
            return services;
        }
    }
}
