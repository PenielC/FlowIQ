using FlowIQ.Api.Blog;
using FlowIQ.Application.Blog;
using FlowIQ.Application.Common.Interfaces;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowIQ.Api.Controllers;

/// <summary>
/// The public blog, as HTML pages (not JSON). The web site's host passes /blog/* and /sitemap.xml through to here,
/// so readers and search engines see them on the main domain.
/// </summary>
[ApiController]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
public class BlogController(ISender sender, IAppUrlProvider urls) : ControllerBase
{
    private BlogPages Pages => new(urls.WebBaseUrl);

    [HttpGet("/blog")]
    public Task<IActionResult> Index([FromQuery] int page = 1, CancellationToken cancellationToken = default) =>
        IndexPage(null, null, page, cancellationToken);

    [HttpGet("/blog/category/{key}")]
    public Task<IActionResult> Category(string key, [FromQuery] int page = 1, CancellationToken cancellationToken = default) =>
        IndexPage(key, null, page, cancellationToken);

    [HttpGet("/blog/tag/{tag}")]
    public Task<IActionResult> Tag(string tag, [FromQuery] int page = 1, CancellationToken cancellationToken = default) =>
        IndexPage(null, tag, page, cancellationToken);

    [HttpGet("/blog/{slug}")]
    public async Task<IActionResult> Post(string slug, CancellationToken cancellationToken)
    {
        BlogPostResult post;
        try
        {
            post = await sender.Send(new GetBlogPostQuery(slug), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return Html(Pages.NotFound(), StatusCodes.Status404NotFound);
        }

        if (HttpMethods.IsGet(Request.Method) && !IsBot())
        {
            await sender.Send(new RecordBlogViewCommand(post.Post.Id), cancellationToken);
        }

        return Html(Pages.Post(post));
    }

    /// <summary>"Try FinFlow free" on a post: counted, then on to sign-up carrying where the reader came from.</summary>
    [HttpGet("/blog/{slug}/start")]
    public async Task<IActionResult> Start(string slug, CancellationToken cancellationToken)
    {
        var source = IsBot() ? "blog" : await sender.Send(new RecordBlogCtaClickCommand(slug), cancellationToken);
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Robots-Tag"] = "noindex";
        return Redirect($"/register?ref={Uri.EscapeDataString(source)}");
    }

    [HttpGet("/blog/images/{id:guid}")]
    public async Task<IActionResult> Image(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var image = await sender.Send(new GetBlogImageQuery(id), cancellationToken);
            // An image never changes under its id, so it can be cached for good.
            Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return File(image.Data, image.ContentType);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("/blog/rss.xml")]
    public async Task<IActionResult> Rss(CancellationToken cancellationToken)
    {
        var posts = await sender.Send(new GetBlogFeedQuery(), cancellationToken);
        Response.Headers.CacheControl = "public, max-age=600";
        return Content(Pages.Rss(posts), "application/rss+xml; charset=utf-8");
    }

    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> Sitemap(CancellationToken cancellationToken)
    {
        var posts = await sender.Send(new GetBlogFeedQuery(), cancellationToken);
        Response.Headers.CacheControl = "public, max-age=600";
        return Content(Pages.Sitemap(posts), "application/xml; charset=utf-8");
    }

    private async Task<IActionResult> IndexPage(string? category, string? tag, int page, CancellationToken cancellationToken)
    {
        try
        {
            return Html(Pages.Index(await sender.Send(new GetBlogIndexQuery(category, tag, page), cancellationToken)));
        }
        catch (KeyNotFoundException)
        {
            return Html(Pages.NotFound(), StatusCodes.Status404NotFound);
        }
    }

    private ContentResult Html(string html, int status = StatusCodes.Status200OK)
    {
        Response.Headers.CacheControl = status == StatusCodes.Status200OK ? "public, max-age=60" : "no-cache";
        return new ContentResult { Content = html, ContentType = "text/html; charset=utf-8", StatusCode = status };
    }

    private static readonly string[] BotMarkers =
        ["bot", "crawl", "spider", "slurp", "preview", "facebookexternalhit", "whatsapp", "telegram", "skype", "headless", "curl", "wget", "python", "monitor"];

    /// <summary>Search engines, link-preview fetchers and scripts: not counted as readers.</summary>
    private bool IsBot()
    {
        var agent = Request.Headers.UserAgent.ToString();
        return agent.Length == 0 || BotMarkers.Any(m => agent.Contains(m, StringComparison.OrdinalIgnoreCase));
    }
}
