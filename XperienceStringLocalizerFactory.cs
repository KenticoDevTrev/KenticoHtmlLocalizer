using CMS.Base;
using CMS.Helpers;
using CMS.SiteProvider;
using Microsoft.Extensions.Localization;
using System;

namespace XperienceCommunity.Localizer.Internal
{
    internal class XperienceStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly IStringLocalizerFactory _baseStringLocalizerFactory;
        private readonly ISiteInfoProvider _siteInfoProvider;
        private readonly IProgressiveCache _progressiveCache;
        private readonly ISiteService _siteService;

        public XperienceStringLocalizerFactory(IStringLocalizerFactory baseStringLocalizerFactory,
            ISiteInfoProvider siteInfoProvider,
            IProgressiveCache progressiveCache,
            ISiteService siteService)
        {
            _baseStringLocalizerFactory = baseStringLocalizerFactory;
            _siteInfoProvider = siteInfoProvider;
            _progressiveCache = progressiveCache;
            _siteService = siteService;
        }

        public IStringLocalizer Create(Type resourceSource)
        {
            var baseLocalizer = _baseStringLocalizerFactory.Create(resourceSource);
            return _progressiveCache.Load(cs =>
            {
                return new XperienceStringLocalizer(baseLocalizer, _siteInfoProvider, _progressiveCache, _siteService);
            }, new CacheSettings(1440, "GetStringLocalizer", baseLocalizer.GetType().FullName));
        }

        public IStringLocalizer Create(string baseName, string location)
        {
            var baseLocalizer = _baseStringLocalizerFactory.Create(baseName, location);
            return _progressiveCache.Load(cs =>
            {
                return new XperienceStringLocalizer(baseLocalizer, _siteInfoProvider, _progressiveCache, _siteService);
            }, new CacheSettings(1440, "GetStringLocalizer", baseLocalizer.GetType().FullName));
        }
    }
}
