
# XperienceCommunity.HtmlLocalizer
Adds fallback support of Kentico Xperience's Localization keys/translations to the default IHtmlLocalizer.
In the past, most translation keys / translations were controlled through Kentico Xperience's Localization -> Resource strings.  This allowed users to create keys and translate them, with fall back to the default language.

This package restores that functionality, while allowing normal `.resx` resource file translations.

# Installation and Requirements
Kentico Xperience 13 (.net 5.0) required (minimum hotfix 5).
To install...
1. Install the `XperienceCommunity.HtmlLocalizer` package into your MVC site
2. call `services.AddKenticoHtmlLocalizer<SharedResources>();` on your service collection, where `SharedResources` is just an empty class in your assembly (used to find resx files)

# Usage
Use IHtmlLocalizer as you would normally.  Now you also can put in Kentico Localization macros in your strings as well.  Here's some examples:

``` cshtml
@inject IHtmlLocalizer<SharedResources> HtmlLocalizer;

<p>@HtmlLocalizer["myresource.key"]</p>
<p>@HtmlLocalizer.GetString("{$ general.greeting $}") Bob</p>
```
# Contributions, bug fixes and License

Feel free to Fork and submit pull requests to contribute.

You can submit bugs through the issue list and i will get to them as soon as i can, unless you want to fix it yourself and submit a pull request!

Check the License.txt for License information
