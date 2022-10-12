

# XperienceCommunity.Localizer
Adds fallback support of Kentico Xperience's Localization keys/translations to the default IHtmlLocalizer/IStringLocalizer/IViewLocalizer)

In the past, most translation keys / translations were controlled through Kentico Xperience's Localization -> Resource strings.  This allowed users to create keys and translate them, with fall back to the default language.

This package restores that functionality, while allowing normal `.resx` resource file translations.

# Installation and Requirements
Kentico Xperience 13 (.net 5.0) required (minimum hotfix 5).
To install...
1. Install the `XperienceCommunity.Localizer` package into your MVC site
2. add this line of code in your startup
```
	services
		.AddLocalization()
        .AddXperienceLocalizer() // MUST call after AddLocalization() !
```

# Usage
Use `IHtmlLocalizer<>` / `IStringLocalizer<>` as you would normally.  Now you also can put in Kentico Xperience Localization macros in your strings as well.  Here's some examples:

``` cshtml
@inject IHtmlLocalizer<SharedResources> HtmlLocalizer;

<p>@HtmlLocalizer["myresource.key"]</p>
<p>@HtmlLocalizer.GetString("{$ general.greeting $}") Bob</p>
```

## Resx help
.Net core can be confusing for resource file placement...very confusing.  So let me lay out what i have:

**In Startup**
``` csharp
services.AddLocalization()
	.AddXperienceLocalizer() // Call after AddLocalization, adds Kentico Resource String support
	.AddControllersWithViews()
	.AddViewLocalization() // honestly couldn't get View Localization to ever work...
	.AddDataAnnotationsLocalization(options =>
	    {
	        options.DataAnnotationLocalizerProvider = (type, factory) =>
	        {
		        // This will use your ~/Resources/SharedResources.resx, with kentico fall back
	            return factory.Create(typeof(SharedResources));
	        };
	    });
```
**In your Project**
1. Create a folder `Resources` under the root of your project.
2. Create a class `SharedResources.cs` (namespace didn't seem to matter, but i put no namespace myself)
3. Create a `SharedResources.resx` for your default translations, `SharedResources.en.resx` for your language translations, and `SharedResources.en-US.resx` for your culture specific language translations.

Logic will prefer the `.language-Region.resx` first, then the `.language.resx` second, then `.resx` third, and lastly it will use Xperience's Localization Resource Strings (matching language-Region first followed by site default).

**Other Resource Files**
You can also add resource files specifically for certain areas, for example if you have a View Component `/Components/MyThing/MyThingViewComponent` , you can place a `MyThingViewComponent.resx` in the same folder as your class, and call `IHtmlLocalizer<MyThingViewComponent> componentLocalizer` and it should use that, and still fall back to Xperience's Localization Resource Strings.


# Contributions, bug fixes and License
Thanks to Sean Wright for some more help on this one.

Feel free to Fork and submit pull requests to contribute.

You can submit bugs through the issue list and i will get to them as soon as i can, unless you want to fix it yourself and submit a pull request!

Check the License.txt for License information
