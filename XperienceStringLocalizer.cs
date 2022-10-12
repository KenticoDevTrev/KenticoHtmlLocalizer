using CMS.Base;
using CMS.Helpers;
using CMS.SiteProvider;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace XperienceCommunity.Localizer.Internal
{
    internal class XperienceStringLocalizer<T> : XperienceStringLocalizerBase, IStringLocalizer<T>
    {
        private readonly IStringLocalizer<T> _localizer;

        /// <summary>
        /// Creates a new <see cref="HtmlLocalizer"/>.
        /// </summary>
        /// <param name="localizer">The <see cref="IStringLocalizer"/> to read strings from.</param>
        public XperienceStringLocalizer(IStringLocalizer<T> localizer,
            ISiteInfoProvider siteInfoProvider,
            IProgressiveCache progressiveCache,
            ISiteService siteService) : base(siteInfoProvider, progressiveCache, siteService)
        {
            if (localizer == null)
            {
                throw new ArgumentNullException(nameof(localizer));
            }

            _localizer = localizer;
        }

        public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            var strings = new List<LocalizedString>();
            strings.AddRange(_localizer.GetAllStrings(includeParentCultures));
            // add custom strings
            strings.AddRange(XperienceResourceStrings.Select(x => new LocalizedString(x.Key, x.Value, true)));
            return strings;
        }

        LocalizedString IStringLocalizer.this[string name]
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
                    result = LocalizeWithKentico(name, result.SearchedLocation);
                }

                return result;
            }
        }

        LocalizedString IStringLocalizer.this[string name, params object[] arguments]
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
                    result = LocalizeWithKentico(name, result.SearchedLocation, arguments);
                }

                return result;
            }
        }

    }

    public class XperienceStringLocalizer : XperienceStringLocalizerBase, IStringLocalizer
    {
        private readonly IStringLocalizer _localizer;

        /// <summary>
        /// Creates a new <see cref="HtmlLocalizer"/>.
        /// </summary>
        /// <param name="localizer">The <see cref="IStringLocalizer"/> to read strings from.</param>
        public XperienceStringLocalizer(IStringLocalizer localizer,
            ISiteInfoProvider siteInfoProvider,
            IProgressiveCache progressiveCache,
            ISiteService siteService) : base(siteInfoProvider, progressiveCache, siteService)
        {
            if (localizer == null)
            {
                throw new ArgumentNullException(nameof(localizer));
            }

            _localizer = localizer;
        }


        /// <inheritdoc />
        public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            var strings = new List<LocalizedString>();
            strings.AddRange(_localizer.GetAllStrings(includeParentCultures));
            // add custom strings
            strings.AddRange(XperienceResourceStrings.Select(x => new LocalizedString(x.Key, x.Value, true)));
            return strings;
        }

        LocalizedString IStringLocalizer.this[string name]
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
                    result = LocalizeWithKentico(name, result.SearchedLocation);
                }

                return result;
            }
        }

        LocalizedString IStringLocalizer.this[string name, params object[] arguments]
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
                    result = LocalizeWithKentico(name, result.SearchedLocation, arguments);
                }

                return result;
            }
        }

    }
}
