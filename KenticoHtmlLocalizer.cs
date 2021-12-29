using CMS.Base;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Localization;
using CMS.SiteProvider;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace XperienceCommunity.HtmlLocalizer
{
    public class KenticoHtmlLocalizer<T> : IHtmlLocalizer<T>
    {
        private readonly IStringLocalizer<T> _localizer;
        private readonly IProgressiveCache _progressiveCache;
        private readonly ISiteService _siteService;
        private readonly ISiteInfoProvider _siteInfoProvider;

        private string _culture;
        private string _defaultCulture;
        private Dictionary<string, string> _kenticoResourceStrings { get; set; }
        /// <summary>
        /// Creates a new <see cref="HtmlLocalizer"/>.
        /// </summary>
        /// <param name="localizer">The <see cref="IStringLocalizer"/> to read strings from.</param>
        public KenticoHtmlLocalizer(IStringLocalizer<T> localizer,
            ISiteInfoProvider siteInfoProvider,
            IProgressiveCache progressiveCache,
            ISiteService siteService)
        {
            if (localizer == null)
            {
                throw new ArgumentNullException(nameof(localizer));
            }

            _localizer = localizer;
            _progressiveCache = progressiveCache;
            _siteService = siteService;
            _siteInfoProvider = siteInfoProvider;
            InitializeDictionary();
        }

        private void InitializeDictionary()
        {
            _culture = System.Globalization.CultureInfo.CurrentCulture.Name;
            int siteID = _siteService.CurrentSite.SiteID;
            var site = _progressiveCache.Load(cs =>
            {
                if (cs.Cached)
                {
                    cs.CacheDependency = CacheHelper.GetCacheDependency($"{SiteInfo.OBJECT_TYPE}|byid|{siteID}");
                }

                return _siteInfoProvider.Get(_siteService.CurrentSite.SiteID);
            }, new CacheSettings(1440, "StringLocalizerSite", siteID));
            _defaultCulture = site.DefaultVisitorCulture;

            // Now load up dictionary
            _kenticoResourceStrings = _progressiveCache.Load(cs =>
            {
                if (cs.Cached)
                {
                    cs.CacheDependency = CacheHelper.GetCacheDependency(new string[]
                    {
                        $"{ResourceStringInfo.OBJECT_TYPE}|all",
                        $"{ResourceTranslationInfo.OBJECT_TYPE}|all",
                    });
                }

                var results = ConnectionHelper.ExecuteQuery(
                    @"select StringKey, TranslationText from (
                        select ROW_NUMBER() over (partition by StringKey order by case when CultureCode = '" + _culture + @"' then 0 else 1 end) as priority, StringKey, TranslationText from CMS_ResourceString
                        left join CMS_ResourceTranslation on StringID = TranslationStringID
                        left join CMS_Culture on TranslationCultureID = CultureID
                        where CultureCode in ('" + _culture + @"', '" + _defaultCulture + @"')
                        ) combined where priority = 1"
                    , null, QueryTypeEnum.SQLQuery);
                return results.Tables[0].Rows.Cast<DataRow>()
                    .Select(x => new Tuple<string, string>(ValidationHelper.GetString(x["StringKey"], "").ToLower(), ValidationHelper.GetString(x["TranslationText"], "")))
                    .GroupBy(x => x.Item1)
                    .ToDictionary(key => key.Key, value => value.FirstOrDefault().Item2);
            }, new CacheSettings(1440, "LocalizedStringDictionary", _culture, _defaultCulture));
        }

        /// <inheritdoc />
        public virtual LocalizedHtmlString this[string name]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }
                var result = _localizer[name];
                if (result.ResourceNotFound)
                {
                    result = LocalizeWithKentico(name);
                }

                return ToHtmlString(result);
            }
        }



        /// <inheritdoc />
        public virtual LocalizedHtmlString this[string name, params object[] arguments]
        {
            get
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }
                var result = _localizer[name];
                if (result.ResourceNotFound)
                {
                    result = LocalizeWithKentico(name);
                }

                return ToHtmlString(result, arguments);
            }
        }

        /// <inheritdoc />
        public virtual LocalizedString GetString(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            var result = _localizer[name];
            if (result.ResourceNotFound)
            {
                result = LocalizeWithKentico(name);
            }

            return result;
        }

        /// <inheritdoc />
        public virtual LocalizedString GetString(string name, params object[] arguments)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            var result = _localizer[name, arguments];
            if (result.ResourceNotFound)
            {
                result = LocalizeWithKentico(name, arguments);
            }
            return result;
        }

        /// <inheritdoc />
        public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            var strings = new List<LocalizedString>();
            strings.AddRange(_localizer.GetAllStrings(includeParentCultures));
            // add custom strings
            strings.AddRange(_kenticoResourceStrings.Select(x => new LocalizedString(x.Key, x.Value, true)));
            return strings;
        }


        /// <summary>
        /// Creates a new <see cref="LocalizedHtmlString"/> for a <see cref="LocalizedString"/>.
        /// </summary>
        /// <param name="result">The <see cref="LocalizedString"/>.</param>
        protected virtual LocalizedHtmlString ToHtmlString(LocalizedString result) =>
            new LocalizedHtmlString(result.Name, result.Value, result.ResourceNotFound);

        protected virtual LocalizedHtmlString ToHtmlString(LocalizedString result, object[] arguments) =>
            new LocalizedHtmlString(result.Name, result.Value, result.ResourceNotFound, arguments);


        private LocalizedString LocalizeWithKentico(string name, params object[] arguments)
        {
            string value = string.Empty;
            string key = name.ToLower().Replace("{$", "").Replace("$}", "").Trim();
            if (_kenticoResourceStrings.ContainsKey(key.ToLower()))
            {
                value = _kenticoResourceStrings[key.ToLower()];
            }
            if (string.IsNullOrWhiteSpace(value))
            {
                // Fall back, if the string has {$ $} or is complex, try to run with this.
                value = _progressiveCache.Load(cs =>
                {
                    if (cs.Cached)
                    {
                        cs.CacheDependency = CacheHelper.GetCacheDependency(new string[]
                        {
                            $"{ResourceStringInfo.OBJECT_TYPE}|all",
                            $"{ResourceTranslationInfo.OBJECT_TYPE}|all",
                        });
                    }
                    return ResHelper.LocalizeString(name, _culture);
                }, new CacheSettings(30, "ResHelperLocalization", name, _culture, _defaultCulture));
            }
            if (arguments.Length > 0 && !string.IsNullOrWhiteSpace(value))
            {
                value = string.Format(value, arguments);
            }
            return new LocalizedString(name, value, !string.IsNullOrWhiteSpace(value));
        }
    }
}
