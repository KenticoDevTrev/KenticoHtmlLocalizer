using CMS.Base;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Localization;
using CMS.SiteProvider;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace XperienceCommunity.Localizer.Internal
{
    public class XperienceStringLocalizerBase
    {
        internal readonly IProgressiveCache _progressiveCache;
        internal readonly ISiteService _siteService;
        internal readonly ISiteInfoProvider _siteInfoProvider;

        public XperienceStringLocalizerBase(ISiteInfoProvider siteInfoProvider,
            IProgressiveCache progressiveCache,
            ISiteService siteService)
        {
            _progressiveCache = progressiveCache;
            _siteService = siteService;
            _siteInfoProvider = siteInfoProvider;
        }

        internal string _culture
        {
            get
            {
                return System.Globalization.CultureInfo.CurrentCulture.Name;
            }
        }

        internal string _defaultCulture
        {
            get
            {
                int siteID = _siteService.CurrentSite?.SiteID ?? 1;
                var site = _progressiveCache.Load(cs =>
                {
                    if (cs.Cached)
                    {
                        cs.CacheDependency = CacheHelper.GetCacheDependency($"{SiteInfo.OBJECT_TYPE}|byid|{siteID}");
                    }

                    return _siteInfoProvider.Get(siteID);
                }, new CacheSettings(1440, "StringLocalizerSite", siteID));
                return site.DefaultVisitorCulture;
            }
        }
        private Dictionary<string, Dictionary<string, string>> _xperienceResourceStrings { get; set; } = new Dictionary<string, Dictionary<string, string>>();

        internal Dictionary<string, string> XperienceResourceStrings { get
            {
                if(!_xperienceResourceStrings.ContainsKey(_culture))
                {
                    InitializeDictionary(_culture);
                }
                return _xperienceResourceStrings[_culture];
            }
        }

        private void InitializeDictionary(string cultureName)
        {

            // Now load up dictionary
            _xperienceResourceStrings.Add(cultureName, _progressiveCache.Load(cs =>
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
            }, new CacheSettings(1440, "LocalizedStringDictionary", _culture, _defaultCulture)));
        }

        internal LocalizedString LocalizeWithKentico(string name, params object[] arguments)
        {
            string value = string.Empty;
            string key = name.ToLower().Replace("{$", "").Replace("$}", "").Trim();
            if (XperienceResourceStrings.ContainsKey(key.ToLower()))
            {
                value = XperienceResourceStrings[key.ToLower()];
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
