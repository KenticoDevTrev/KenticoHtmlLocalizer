using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace XperienceCommunity.HtmlLocalizer
{
    public static class KenticoHtmlLocalizerExtension
    {

        /// <summary>
        /// Adds fallback to the HtmlLocalizer that if it can't find it in a resx it will use Kentico's Localization keys / translations
        /// </summary>
        /// <typeparam name="Type">Type of a class in the assembly where the resx is found.</typeparam>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddKenticoHtmlLocalizer<Type>(this IServiceCollection services)
        {
            return services.AddScoped<IHtmlLocalizer<Type>, KenticoHtmlLocalizer<Type>>();
        }
    }
}
