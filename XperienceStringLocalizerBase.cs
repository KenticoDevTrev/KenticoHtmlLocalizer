﻿using CMS.Base;
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

        static string CurrentCulture
        {
            get
            {
                return System.Globalization.CultureInfo.CurrentCulture.Name;
            }
        }

        internal Dictionary<string, string> XperienceResourceStrings
        {
            get
            {
                return GetDictionary(CurrentCulture);
            }
        }

        internal string SiteVisitorDefaultCulture
        {
            get
            {
                int siteID = _siteService.CurrentSite?.SiteID ?? -1;
                var site = _progressiveCache.Load(cs =>
                {
                    if (cs.Cached)
                    {
                        cs.CacheDependency = CacheHelper.GetCacheDependency($"{SiteInfo.OBJECT_TYPE}|byid|{siteID}");
                    }

                    return siteID > 0 ? _siteInfoProvider.Get(siteID) : _siteInfoProvider.Get().First();
                }, new CacheSettings(1440, "StringLocalizerSite", siteID));
                return site.DefaultVisitorCulture;
            }
        }

        internal string CMSDefaultCulture
        {
            get
            {
                int siteID = _siteService.CurrentSite?.SiteID ?? -1;
                return _progressiveCache.Load(cs =>
                {
                    if (cs.Cached)
                    {
                        cs.CacheDependency = CacheHelper.GetCacheDependency($"{SettingsKeyInfo.OBJECT_TYPE}|byname|CMSDefaultCultureCode");
                    }
                    
                    var site= siteID > 0 ? _siteInfoProvider.Get(siteID) : _siteInfoProvider.Get().First();
                    var defaultCulture = SettingsKeyInfoProvider.GetValue("CMSDefaultCultureCode", site.SiteID);
                    return string.IsNullOrWhiteSpace(defaultCulture) ? "en-US" : defaultCulture;

                }, new CacheSettings(1440, "StringLocalizerSettingsCulture", siteID));
            }
        }

        private Dictionary<string, string> GetDictionary(string cultureName)
        {

            // Now load up dictionary
            return _progressiveCache.Load(cs =>
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
                        select ROW_NUMBER() over (partition by StringKey order by case when CultureCode = '" + cultureName + @"' then 0 else case when CultureCode = '" + SiteVisitorDefaultCulture + @"' then 1 else 2 end end) as priority, StringKey, TranslationText from CMS_ResourceString
                        left join CMS_ResourceTranslation on StringID = TranslationStringID
                        left join CMS_Culture on TranslationCultureID = CultureID
                        where CultureCode in ('" + cultureName + @"', '" + SiteVisitorDefaultCulture + @"', '"+CMSDefaultCulture+@"')
                        ) combined where priority = 1"
                    , null, QueryTypeEnum.SQLQuery);
                return results.Tables[0].Rows.Cast<DataRow>()
                    .Select(x => new Tuple<string, string>(ValidationHelper.GetString(x["StringKey"], "").ToLower(), ValidationHelper.GetString(x["TranslationText"], "")))
                    .GroupBy(x => x.Item1)
                    .ToDictionary(key => key.Key, value => value.First().Item2);
            }, new CacheSettings(1440, "LocalizedStringDictionary", cultureName, SiteVisitorDefaultCulture, CMSDefaultCulture));
        }

        internal LocalizedString LocalizeWithKentico(string name, params object[] arguments)
        {
            string value = string.Empty;
            var dictionary = GetDictionary(CurrentCulture);
            string key = name.ToLower().Replace("{$", "").Replace("$}", "").Trim();

            if (dictionary.ContainsKey(key.ToLower()))
            {
                value = dictionary[key.ToLower()];
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
                    return ResHelper.LocalizeString(name, CurrentCulture);
                }, new CacheSettings(30, "ResHelperLocalization", name, CurrentCulture, SiteVisitorDefaultCulture));
            }
            if (arguments.Length > 0 && !string.IsNullOrWhiteSpace(value) && value.IndexOf($"{{{arguments.Length-1}}}") != -1)
            {
                value = string.Format(value, arguments);
            }
            return new LocalizedString(name, value, string.IsNullOrWhiteSpace(value) || name.Equals(value, StringComparison.OrdinalIgnoreCase));
        }
    }
}
