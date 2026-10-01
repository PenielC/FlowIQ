using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace FlowIQ.Api.Blog;

/// <summary>
/// Turns a post's Markdown into HTML. Raw HTML in the Markdown is shown as text, links may only be web, mail or
/// in-site addresses, links to other sites open in a new tab, and images load lazily.
/// </summary>
public static class BlogMarkdown
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    public static string ToHtml(string markdown)
    {
        var document = Markdown.Parse(markdown ?? string.Empty, Pipeline);

        foreach (var link in document.Descendants<LinkInline>())
        {
            var url = link.Url?.Trim() ?? string.Empty;
            if (!IsSafe(url))
            {
                link.Url = "#";
                continue;
            }

            if (link.IsImage)
            {
                link.GetAttributes().AddPropertyIfNotExist("loading", "lazy");
            }
            else if (url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                link.GetAttributes().AddPropertyIfNotExist("target", "_blank");
                link.GetAttributes().AddPropertyIfNotExist("rel", "noopener");
            }
        }

        foreach (var autolink in document.Descendants<AutolinkInline>())
        {
            if (!IsSafe(autolink.Url)) autolink.Url = "#";
        }

        // Tables scroll sideways on phones instead of breaking the page.
        foreach (var table in document.Descendants<Markdig.Extensions.Tables.Table>())
        {
            table.GetAttributes().AddClass("table");
        }

        return document.ToHtml(Pipeline);
    }

    private static readonly string[] SafeSchemes = ["http", "https", "mailto", "tel"];

    /// <summary>Relative addresses, or one of <see cref="SafeSchemes"/> (so never javascript: or data:).</summary>
    private static bool IsSafe(string url)
    {
        if (url.StartsWith("//")) return false;
        var colon = url.IndexOf(':');
        var firstSeparator = url.IndexOfAny(['/', '?', '#']);
        if (colon < 0 || (firstSeparator >= 0 && firstSeparator < colon)) return true;
        return SafeSchemes.Contains(url[..colon].Trim().ToLowerInvariant());
    }
}
