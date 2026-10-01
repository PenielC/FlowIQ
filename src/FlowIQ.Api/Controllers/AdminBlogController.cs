using System.IdentityModel.Tokens.Jwt;
using FlowIQ.Api.Blog;
using FlowIQ.Application.Blog;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Contracts.Common;
using FlowIQ.Domain.Blog;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlowIQ.Api.Controllers;

public record SaveBlogPostRequest(
    string Title,
    string? Slug,
    string Summary,
    string Body,
    string Category,
    IReadOnlyList<string>? Tags,
    string? AuthorName,
    Guid? CoverImageId,
    string? CoverImageAlt);

public record SetBlogPostPublishedRequest(bool Published);

public record BlogPreviewRequest(string Body);

public record BlogPreviewResponse(string Html);

public record BlogAdminListResponse(IReadOnlyList<BlogPostAdminResult> Posts, IReadOnlyList<BlogCategory> Categories);

public record BlogImageUploadResponse(Guid Id, string Url);

/// <summary>Platform admins write and publish blog posts here.</summary>
[ApiController]
[Route("api/admin/blog")]
[Authorize]
public class AdminBlogController(ISender sender, IPlatformAdminChecker platformAdminChecker) : ControllerBase
{
    [HttpGet("posts")]
    public async Task<ActionResult<ApiResponse<BlogAdminListResponse>>> List(CancellationToken cancellationToken)
    {
        if (EnsureAdmin() is { } forbid) return forbid;
        var posts = await sender.Send(new ListBlogPostsQuery(), cancellationToken);
        return Ok(ApiResponse<BlogAdminListResponse>.Ok(new BlogAdminListResponse(posts, BlogCategories.All)));
    }

    /// <summary>Creates a draft; nobody sees it until it's published.</summary>
    [HttpPost("posts")]
    public Task<ActionResult<ApiResponse<BlogPostAdminResult>>> Create(SaveBlogPostRequest request, CancellationToken cancellationToken) =>
        Save(null, request, cancellationToken);

    [HttpPut("posts/{id:guid}")]
    public Task<ActionResult<ApiResponse<BlogPostAdminResult>>> Update(Guid id, SaveBlogPostRequest request, CancellationToken cancellationToken) =>
        Save(id, request, cancellationToken);

    [HttpPost("posts/{id:guid}/published")]
    public async Task<ActionResult<ApiResponse<BlogPostAdminResult>>> SetPublished(Guid id, SetBlogPostPublishedRequest request, CancellationToken cancellationToken)
    {
        if (EnsureAdmin() is { } forbid) return forbid;
        return Ok(ApiResponse<BlogPostAdminResult>.Ok(await sender.Send(new SetBlogPostPublishedCommand(id, request.Published), cancellationToken)));
    }

    [HttpDelete("posts/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (EnsureAdmin() is { } forbid) return forbid;
        await sender.Send(new DeleteBlogPostCommand(id), cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { }));
    }

    /// <summary>The post's text as it will appear on the blog (same renderer as the live page).</summary>
    [HttpPost("preview")]
    public ActionResult<ApiResponse<BlogPreviewResponse>> Preview(BlogPreviewRequest request)
    {
        if (EnsureAdmin() is { } forbid) return forbid;
        return Ok(ApiResponse<BlogPreviewResponse>.Ok(new BlogPreviewResponse(BlogMarkdown.ToHtml(request.Body ?? string.Empty))));
    }

    /// <summary>Uploads a cover or in-post image (PNG, JPEG, WebP or GIF, up to 2 MB).</summary>
    [HttpPost("images")]
    [RequestSizeLimit(BlogImage.MaxBytes + 64 * 1024)]
    public async Task<ActionResult<ApiResponse<BlogImageUploadResponse>>> UploadImage(IFormFile file, CancellationToken cancellationToken)
    {
        if (EnsureAdmin() is { } forbid) return forbid;
        if (file is null || file.Length == 0) return BadRequest(ApiResponse<BlogImageUploadResponse>.Fail("Choose an image to upload."));
        if (file.Length > BlogImage.MaxBytes) return BadRequest(ApiResponse<BlogImageUploadResponse>.Fail("Images must be 2 MB or smaller."));

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        var id = await sender.Send(new UploadBlogImageCommand(stream.ToArray(), file.ContentType, file.FileName), cancellationToken);
        return Ok(ApiResponse<BlogImageUploadResponse>.Ok(new BlogImageUploadResponse(id, $"/blog/images/{id}")));
    }

    private async Task<ActionResult<ApiResponse<BlogPostAdminResult>>> Save(Guid? id, SaveBlogPostRequest r, CancellationToken cancellationToken)
    {
        if (EnsureAdmin() is { } forbid) return forbid;
        var result = await sender.Send(
            new SaveBlogPostCommand(id, r.Title, r.Slug, r.Summary, r.Body, r.Category, r.Tags, r.AuthorName, r.CoverImageId, r.CoverImageAlt),
            cancellationToken);
        return Ok(ApiResponse<BlogPostAdminResult>.Ok(result));
    }

    private ActionResult? EnsureAdmin()
    {
        var email = User.FindFirst(JwtRegisteredClaimNames.Email)!.Value;
        return platformAdminChecker.IsPlatformAdmin(email) ? null : Forbid();
    }
}
