using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;
using SportsStore.Models.ViewModels;

namespace SportsStore.Infrastructure
{
    // 🏷️ TAG HELPER - Custom HTML tag generator
    // Tạo các link phân trang tự động từ model data
    // 🔗 KẾ THỪA: PageLinkTagHelper kế thừa từ class TagHelper (ASP.NET Core)
    [HtmlTargetElement("div", Attributes = "page-model")]
    public class PageLinkTagHelper : TagHelper
    {
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly ILogger<PageLinkTagHelper> _logger;

        public PageLinkTagHelper(IUrlHelperFactory helperFactory, ILogger<PageLinkTagHelper> logger)
        {
            _urlHelperFactory = helperFactory;
            _logger = logger;
        }

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext? ViewContext { get; set; }

        public PagingInfo? PageModel { get; set; }
        public string? PageAction { get; set; }

        [HtmlAttributeName(DictionaryAttributePrefix = "page-url-")]
        public Dictionary<string, object> PageUrlValues { get; set; } = new();

        public bool PageClassesEnabled { get; set; } = false;
        public string PageClass { get; set; } = string.Empty;
        public string PageClassNormal { get; set; } = string.Empty;
        public string PageClassSelected { get; set; } = string.Empty;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (ViewContext == null || PageModel == null || PageAction == null)
            {
                _logger.LogWarning("PageLinkTagHelper: ViewContext, PageModel hoặc PageAction bị null.");
                return;
            }

            _logger.LogInformation("Rendering pagination with {TotalPages} total pages", PageModel.TotalPages);

            IUrlHelper urlHelper = _urlHelperFactory.GetUrlHelper(ViewContext);
            TagBuilder result = new TagBuilder("div");

            for (int i = 1; i <= PageModel.TotalPages; i++)
            {
                TagBuilder tag = new TagBuilder("a");
                PageUrlValues["page"] = i;
                string url = urlHelper.Action(PageAction, PageUrlValues)!;
                tag.Attributes["href"] = url;

                _logger.LogDebug("Generated link for page {PageNumber}: {Url}", i, url);

                if (PageClassesEnabled)
                {
                    tag.AddCssClass(PageClass);
                    tag.AddCssClass(i == PageModel.CurrentPage
                        ? PageClassSelected
                        : PageClassNormal);
                }

                tag.InnerHtml.Append(i.ToString());
                result.InnerHtml.AppendHtml(tag);
            }

            output.Content.AppendHtml(result.InnerHtml);
        }
    }
}
