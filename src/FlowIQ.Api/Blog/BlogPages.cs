using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using FlowIQ.Application.Blog;
using FlowIQ.Domain.Blog;

namespace FlowIQ.Api.Blog;

/// <summary>
/// The public blog as plain server-rendered HTML, so search engines and WhatsApp/LinkedIn/X link previews see
/// the real title, text and image. Links are relative: in production the web site passes /blog/* through to the API,
/// so the pages live on the main domain next to the app.
/// </summary>
public class BlogPages(string siteUrl)
{
    public const string SiteName = "FinFlow";
    public const string BlogTitle = "The FinFlow Blog";
    public const string BlogDescription =
        "Practical cash-flow advice for African small businesses: forecasting, getting paid on time, and keeping business and personal money apart.";

    private static readonly CultureInfo DateCulture = CultureInfo.GetCultureInfo("en-GB");

    private readonly string _site = siteUrl.TrimEnd('/');

    public string Absolute(string path) => _site + path;

    // ---------------------------------------------------------------- pages

    public string Index(BlogIndexResult r)
    {
        var heading = r.Category?.Label ?? (r.Tag is null ? BlogTitle : $"Posts tagged “{r.Tag}”");
        var path = r.Category is not null ? $"/blog/category/{r.Category.Key}" : r.Tag is not null ? $"/blog/tag/{Uri.EscapeDataString(r.Tag)}" : "/blog";
        var canonical = r.Page > 1 ? $"{path}?page={r.Page}" : path;
        var title = r.Category is null && r.Tag is null ? $"{BlogTitle}: cash-flow advice for African SMEs" : $"{heading} | {BlogTitle}";

        var body = new StringBuilder();
        body.Append($$"""
            <section class="hero">
              <div class="wrap">
                <p class="eyebrow">{{(r.Category is null && r.Tag is null ? "Blog" : "<a href=\"/blog\">Blog</a>")}}</p>
                <h1>{{E(heading)}}</h1>
                <p class="lead">{{E(r.Category is null && r.Tag is null ? BlogDescription : CategoryBlurb(r.Category?.Key) ?? "Every post on this topic.")}}</p>
              </div>
            </section>
            <div class="wrap">
              <nav class="pills" aria-label="Categories">
                {{Pill("/blog", "All posts", r.Category is null && r.Tag is null)}}
                {{string.Concat(BlogCategories.All.Select(c => Pill($"/blog/category/{c.Key}", c.Label, r.Category?.Key == c.Key)))}}
              </nav>
            """);

        if (r.Posts.Count == 0)
        {
            body.Append("""<p class="empty">No posts here yet. Check back soon.</p>""");
        }
        else
        {
            body.Append("""<div class="grid">""");
            foreach (var p in r.Posts) body.Append(Card(p));
            body.Append("</div>");
        }

        if (r.PageCount > 1)
        {
            body.Append("""<nav class="pager" aria-label="Pages">""");
            if (r.Page > 1) body.Append($"""<a href="{PageLink(path, r.Page - 1)}" rel="prev">← Newer posts</a>""");
            body.Append($"""<span>Page {r.Page} of {r.PageCount}</span>""");
            if (r.Page < r.PageCount) body.Append($"""<a href="{path}?page={r.Page + 1}" rel="next">Older posts →</a>""");
            body.Append("</nav>");
        }

        if (r.Tags.Count > 0)
        {
            body.Append("""<section class="tags-cloud"><h2>Topics</h2><div class="tags">""");
            foreach (var (tag, count) in r.Tags.Take(30))
            {
                var active = tag == r.Tag ? " active" : "";
                body.Append($"""<a class="tag{active}" href="/blog/tag/{Uri.EscapeDataString(tag)}">#{E(tag)} <span>{count}</span></a>""");
            }
            body.Append("</div></section>");
        }

        body.Append(Cta("/register?ref=blog", "Running a business? See your cash coming.",
            "FinFlow forecasts your next 90 days, chases unpaid invoices for you, and keeps personal withdrawals out of your business numbers."));
        body.Append("</div>");

        var jsonLd = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Blog",
            ["name"] = BlogTitle,
            ["description"] = BlogDescription,
            ["url"] = Absolute("/blog"),
            ["publisher"] = Publisher(),
        };

        return Layout(title, r.Category is null && r.Tag is null ? BlogDescription : $"{heading}: posts from {BlogTitle}.", canonical, null, "website",
            jsonLd, body.ToString(), noIndex: r.Tag is not null);
    }

    public string Post(BlogPostResult r)
    {
        var p = r.Post;
        var path = $"/blog/{p.Slug}";
        var category = BlogCategories.Find(p.Category);
        var cover = p.CoverImageId is { } id ? $"/blog/images/{id}" : null;
        var shareUrl = Uri.EscapeDataString(Absolute(path));
        var shareText = Uri.EscapeDataString(p.Title);

        var body = new StringBuilder();
        body.Append($$"""
            <article class="post">
              <header class="post-head wrap narrow">
                <p class="eyebrow"><a href="/blog">Blog</a>{{(category is null ? "" : $" / <a href=\"/blog/category/{category.Key}\">{E(category.Label)}</a>")}}</p>
                <h1>{{E(p.Title)}}</h1>
                <p class="lead">{{E(p.Summary)}}</p>
                <p class="meta">{{E(p.AuthorName)}} · <time datetime="{{p.PublishedAtUtc:yyyy-MM-dd}}">{{Date(p.PublishedAtUtc)}}</time> · {{p.ReadingMinutes}} min read</p>
              </header>
            """);
        if (cover is not null)
        {
            body.Append($"""<figure class="cover wrap"><img src="{cover}" alt="{E(p.CoverImageAlt ?? "")}" width="1200" height="630"></figure>""");
        }

        body.Append($$"""
              <div class="prose wrap narrow">{{BlogMarkdown.ToHtml(r.Body)}}</div>
              <footer class="post-foot wrap narrow">
                {{(p.Tags.Count == 0 ? "" : $"<div class=\"tags\">{string.Concat(p.Tags.Select(t => $"<a class=\"tag\" href=\"/blog/tag/{Uri.EscapeDataString(t)}\">#{E(t)}</a>"))}</div>")}}
                <div class="share">
                  <span>Share</span>
                  <a href="https://wa.me/?text={{shareText}}%20{{shareUrl}}" target="_blank" rel="noopener">WhatsApp</a>
                  <a href="https://www.linkedin.com/sharing/share-offsite/?url={{shareUrl}}" target="_blank" rel="noopener">LinkedIn</a>
                  <a href="https://x.com/intent/post?url={{shareUrl}}&amp;text={{shareText}}" target="_blank" rel="noopener">X</a>
                  <a href="https://www.facebook.com/sharer/sharer.php?u={{shareUrl}}" target="_blank" rel="noopener">Facebook</a>
                </div>
              </footer>
            </article>
            <div class="wrap narrow">
            """);
        body.Append(Cta($"{path}/start", "Try FinFlow free",
            "See the next 90 days of your cash flow, send invoices that chase themselves, and keep school fees and rent out of your business numbers. Set up takes about 5 minutes."));
        body.Append("</div>");

        if (r.Related.Count > 0)
        {
            body.Append("""<section class="wrap related"><h2>Keep reading</h2><div class="grid">""");
            foreach (var rel in r.Related) body.Append(Card(rel));
            body.Append("</div></section>");
        }

        var jsonLd = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BlogPosting",
            ["headline"] = p.Title,
            ["description"] = p.Summary,
            ["datePublished"] = p.PublishedAtUtc?.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
            ["author"] = new Dictionary<string, object?> { ["@type"] = p.AuthorName == BlogPost.DefaultAuthor ? "Organization" : "Person", ["name"] = p.AuthorName },
            ["publisher"] = Publisher(),
            ["mainEntityOfPage"] = Absolute(path),
            ["image"] = cover is null ? Absolute("/og-image.png") : Absolute(cover),
            ["keywords"] = p.Tags.Count == 0 ? null : string.Join(", ", p.Tags),
            ["articleSection"] = category?.Label,
        };

        return Layout($"{p.Title} | {SiteName}", p.Summary, path, cover, "article", jsonLd, body.ToString(), publishedAtUtc: p.PublishedAtUtc);
    }

    public string NotFound() =>
        Layout($"Page not found | {BlogTitle}", BlogDescription, "/blog", null, "website", null, """
            <section class="hero"><div class="wrap">
              <p class="eyebrow"><a href="/blog">Blog</a></p>
              <h1>We couldn't find that page</h1>
              <p class="lead">The post may have moved or been taken down. Have a look at the latest posts instead.</p>
              <p><a class="btn" href="/blog">Go to the blog</a></p>
            </div></section>
            """, noIndex: true);

    // ---------------------------------------------------------------- feeds

    public string Rss(IReadOnlyList<BlogPostSummary> posts)
    {
        var items = string.Concat(posts.Take(50).Select(p => $"""
                <item>
                  <title>{X(p.Title)}</title>
                  <link>{X(Absolute($"/blog/{p.Slug}"))}</link>
                  <guid isPermaLink="true">{X(Absolute($"/blog/{p.Slug}"))}</guid>
                  <pubDate>{p.PublishedAtUtc?.ToString("r", CultureInfo.InvariantCulture)}</pubDate>
                  <category>{X(BlogCategories.Find(p.Category)?.Label ?? p.Category)}</category>
                  <description>{X(p.Summary)}</description>
                </item>
            """));
        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <rss version="2.0" xmlns:atom="http://www.w3.org/2005/Atom">
              <channel>
                <title>{X(BlogTitle)}</title>
                <link>{X(Absolute("/blog"))}</link>
                <description>{X(BlogDescription)}</description>
                <language>en</language>
                <atom:link href="{X(Absolute("/blog/rss.xml"))}" rel="self" type="application/rss+xml" />
            {items}
              </channel>
            </rss>
            """;
    }

    public string Sitemap(IReadOnlyList<BlogPostSummary> posts)
    {
        var sb = new StringBuilder("""<?xml version="1.0" encoding="UTF-8"?>""" + "\n" + """<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""" + "\n");
        void Url(string path, string priority, DateTime? modified = null) =>
            sb.Append($"  <url><loc>{X(Absolute(path))}</loc>{(modified is { } m ? $"<lastmod>{m:yyyy-MM-dd}</lastmod>" : "")}<priority>{priority}</priority></url>\n");

        Url("/", "1.0");
        Url("/blog", "0.8", posts.FirstOrDefault()?.PublishedAtUtc);
        foreach (var c in BlogCategories.All.Where(c => posts.Any(p => p.Category == c.Key))) Url($"/blog/category/{c.Key}", "0.5");
        foreach (var p in posts) Url($"/blog/{p.Slug}", "0.7", p.PublishedAtUtc);
        Url("/register", "0.5");
        Url("/login", "0.3");
        return sb.Append("</urlset>\n").ToString();
    }

    // ---------------------------------------------------------------- pieces

    private static string Card(BlogPostSummary p)
    {
        var category = BlogCategories.Find(p.Category);
        var image = p.CoverImageId is { } id
            ? $"""<img src="/blog/images/{id}" alt="{E(p.CoverImageAlt ?? "")}" loading="lazy">"""
            : $"""<span class="placeholder">{E(category?.Label ?? "FinFlow")}</span>""";
        return $"""
            <a class="card" href="/blog/{p.Slug}">
              <div class="card-img">{image}</div>
              <div class="card-body">
                <p class="card-cat">{E(category?.Label ?? "")}</p>
                <h2>{E(p.Title)}</h2>
                <p>{E(p.Summary)}</p>
                <p class="meta">{Date(p.PublishedAtUtc)} · {p.ReadingMinutes} min read</p>
              </div>
            </a>
            """;
    }

    private static string Cta(string href, string heading, string text) => $"""
        <aside class="cta">
          <div>
            <h2>{E(heading)}</h2>
            <p>{E(text)}</p>
          </div>
          <a class="btn" href="{href}">Try FinFlow free →</a>
          <p class="small">No credit card needed.</p>
        </aside>
        """;

    private static string Pill(string href, string label, bool active) =>
        $"""<a class="pill{(active ? " active" : "")}" href="{href}"{(active ? " aria-current=\"page\"" : "")}>{E(label)}</a>""";

    private static string PageLink(string path, int page) => page <= 1 ? path : $"{path}?page={page}";

    private static string? CategoryBlurb(string? key) => key switch
    {
        "cash-flow" => "Forecasting, planning and seeing the lean months before they arrive.",
        "getting-paid" => "Invoicing, reminders and getting customers to pay on time without awkward calls.",
        "guides" => "Step-by-step help for running your business finances with FinFlow.",
        "product" => "What's new in FinFlow and how to use it.",
        "stories" => "How African businesses use FinFlow to stay ahead of their cash.",
        _ => null,
    };

    private Dictionary<string, object?> Publisher() => new()
    {
        ["@type"] = "Organization",
        ["name"] = SiteName,
        ["url"] = Absolute("/"),
        ["logo"] = new Dictionary<string, object?> { ["@type"] = "ImageObject", ["url"] = Absolute("/brand/finflow-logo-dark.png") },
    };

    private static string Date(DateTime? utc) => utc?.ToString("d MMMM yyyy", DateCulture) ?? "Draft";

    private static string E(string? text) => WebUtility.HtmlEncode(text ?? string.Empty);

    private static string X(string? text) => System.Security.SecurityElement.Escape(text ?? string.Empty);

    private string Layout(
        string title, string description, string canonicalPath, string? imagePath, string ogType, Dictionary<string, object?>? jsonLd, string content,
        bool noIndex = false, DateTime? publishedAtUtc = null)
    {
        var image = Absolute(imagePath ?? "/og-image.png");
        var ld = jsonLd is null ? "" : $"""<script type="application/ld+json">{JsonSerializer.Serialize(jsonLd.Where(kv => kv.Value is not null).ToDictionary()).Replace("</", "<\\/")}</script>""";
        return $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>{{E(title)}}</title>
              <meta name="description" content="{{E(description)}}">
              <link rel="canonical" href="{{E(Absolute(canonicalPath))}}">
              {{(noIndex ? "<meta name=\"robots\" content=\"noindex, follow\">" : "")}}
              <link rel="icon" type="image/png" href="/favicon.png">
              <link rel="alternate" type="application/rss+xml" title="{{E(BlogTitle)}}" href="/blog/rss.xml">
              <meta property="og:type" content="{{ogType}}">
              <meta property="og:site_name" content="{{SiteName}}">
              <meta property="og:url" content="{{E(Absolute(canonicalPath))}}">
              <meta property="og:title" content="{{E(title)}}">
              <meta property="og:description" content="{{E(description)}}">
              <meta property="og:image" content="{{E(image)}}">
              {{(publishedAtUtc is { } at ? $"<meta property=\"article:published_time\" content=\"{at:yyyy-MM-ddTHH:mm:ssZ}\">" : "")}}
              <meta name="twitter:card" content="summary_large_image">
              <meta name="twitter:title" content="{{E(title)}}">
              <meta name="twitter:description" content="{{E(description)}}">
              <meta name="twitter:image" content="{{E(image)}}">
              <link rel="preconnect" href="https://fonts.googleapis.com">
              <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
              <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap" rel="stylesheet">
              <script async src="https://www.googletagmanager.com/gtag/js?id=G-V5BK8HB4HY"></script>
              <script>window.dataLayer=window.dataLayer||[];function gtag(){dataLayer.push(arguments);}gtag('js',new Date());gtag('config','G-V5BK8HB4HY');</script>
              {{ld}}
              <style>{{Css}}</style>
            </head>
            <body>
              <header class="site-head">
                <div class="wrap bar">
                  <a href="/" class="logo"><img src="/brand/finflow-logo-light.png" alt="FinFlow" width="143" height="40"></a>
                  <nav class="site-nav">
                    <a href="/#features" class="hide-sm">Features</a>
                    <a href="/blog" class="active">Blog</a>
                    <a href="/login" class="hide-sm">Sign in</a>
                    <a href="/register?ref=blog" class="btn btn-sm">Get started free</a>
                  </nav>
                </div>
              </header>
              <main>
            {{content}}
              </main>
              <footer class="site-foot">
                <div class="wrap foot">
                  <div>
                    <img src="/brand/finflow-logo-light.png" alt="FinFlow" width="143" height="40">
                    <p>Smarter Finance. Bigger Dreams.</p>
                  </div>
                  <nav>
                    <a href="/">Home</a>
                    <a href="/blog">Blog</a>
                    <a href="/blog/rss.xml">RSS</a>
                    <a href="/register?ref=blog">Start free</a>
                  </nav>
                </div>
                <p class="copy">© {{DateTime.UtcNow.Year}} FinFlow. All rights reserved.</p>
              </footer>
            </body>
            </html>
            """;
    }

    private const string Css = """
        :root{--navy:#0f172a;--navy-soft:#1e293b;--green:#10b981;--green-dark:#047857;--teal:#14b8a6;--ink:#0f172a;--text:#334155;--muted:#64748b;--line:#e2e8f0;--bg:#f8fafc;--card:#fff}
        *{box-sizing:border-box}
        html{-webkit-text-size-adjust:100%}
        body{margin:0;background:var(--bg);color:var(--text);font:16px/1.65 Inter,ui-sans-serif,system-ui,-apple-system,"Segoe UI",sans-serif;-webkit-font-smoothing:antialiased}
        a{color:var(--green-dark)}
        img{max-width:100%;height:auto}
        .wrap{max-width:1120px;margin:0 auto;padding:0 20px}
        .narrow{max-width:740px}
        .site-head{position:sticky;top:0;z-index:10;background:rgba(15,23,42,.92);backdrop-filter:blur(8px);border-bottom:1px solid rgba(255,255,255,.08)}
        .bar{display:flex;align-items:center;justify-content:space-between;height:68px;gap:12px}
        .logo img{display:block;height:36px;width:auto}
        .site-nav{display:flex;align-items:center;gap:22px}
        .site-nav a{color:#cbd5e1;text-decoration:none;font-size:14px;font-weight:500}
        .site-nav a:hover,.site-nav a.active{color:#fff}
        .btn{display:inline-block;background:linear-gradient(90deg,var(--green),var(--teal));color:#fff !important;text-decoration:none;font-weight:600;border-radius:999px;padding:12px 22px;box-shadow:0 8px 20px -8px rgba(16,185,129,.6);white-space:nowrap}
        .btn:hover{filter:brightness(1.05)}
        .btn-sm{padding:8px 16px;font-size:14px}
        .hero{background:var(--navy);color:#fff;padding:56px 0 64px;background-image:radial-gradient(circle at 85% 10%,rgba(20,184,166,.18),transparent 45%),radial-gradient(circle at 10% 90%,rgba(16,185,129,.14),transparent 40%)}
        .hero h1{font-size:clamp(30px,5vw,46px);line-height:1.15;margin:6px 0 12px;font-weight:800;letter-spacing:-.02em;max-width:820px}
        .hero .lead{color:#cbd5e1;font-size:18px;max-width:680px;margin:0}
        .eyebrow{text-transform:uppercase;letter-spacing:.08em;font-size:12px;font-weight:700;color:var(--green);margin:0}
        .eyebrow a{color:inherit;text-decoration:none}
        .pills{display:flex;gap:8px;overflow-x:auto;padding:24px 0 8px;scrollbar-width:none}
        .pill{flex:none;border:1px solid var(--line);background:#fff;color:var(--text);text-decoration:none;border-radius:999px;padding:7px 15px;font-size:14px;font-weight:500}
        .pill:hover{border-color:var(--green)}
        .pill.active{background:var(--navy);border-color:var(--navy);color:#fff}
        .grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(290px,1fr));gap:24px;padding:20px 0}
        .card{display:flex;flex-direction:column;background:var(--card);border:1px solid var(--line);border-radius:16px;overflow:hidden;text-decoration:none;color:inherit;transition:transform .15s,box-shadow .15s}
        .card:hover{transform:translateY(-2px);box-shadow:0 14px 30px -18px rgba(15,23,42,.35)}
        .card-img{aspect-ratio:1200/630;background:linear-gradient(135deg,#064e3b,#0f766e 55%,#0e7490);display:flex;align-items:center;justify-content:center;overflow:hidden}
        .card-img img{width:100%;height:100%;object-fit:cover}
        .placeholder{color:rgba(255,255,255,.92);font-weight:700;font-size:20px;letter-spacing:-.01em}
        .card-body{padding:18px 20px 20px;display:flex;flex-direction:column;gap:6px;flex:1}
        .card-body h2{font-size:19px;line-height:1.3;margin:0;color:var(--ink);font-weight:700}
        .card-body p{margin:0;font-size:15px;color:var(--muted)}
        .card-cat{color:var(--green-dark) !important;font-size:12px !important;font-weight:700;text-transform:uppercase;letter-spacing:.06em}
        .card .meta{margin-top:auto;padding-top:8px;font-size:13px}
        .pager{display:flex;justify-content:center;align-items:center;gap:20px;padding:8px 0 24px;font-size:15px;color:var(--muted)}
        .pager a{font-weight:600;text-decoration:none}
        .empty{padding:48px 0;color:var(--muted);text-align:center}
        .tags-cloud h2{font-size:15px;color:var(--ink);margin:16px 0 10px}
        .tags{display:flex;flex-wrap:wrap;gap:8px}
        .tag{font-size:13px;color:var(--text);background:#fff;border:1px solid var(--line);border-radius:8px;padding:4px 10px;text-decoration:none}
        .tag span{color:var(--muted)}
        .tag.active,.tag:hover{border-color:var(--green);color:var(--green-dark)}
        .cta{margin:40px 0;background:var(--navy);color:#fff;border-radius:20px;padding:28px;display:grid;gap:14px;background-image:radial-gradient(circle at 100% 0,rgba(20,184,166,.25),transparent 50%)}
        .cta h2{margin:0 0 6px;font-size:23px;line-height:1.25}
        .cta p{margin:0;color:#cbd5e1}
        .cta .btn{justify-self:start}
        .cta .small{font-size:13px;color:#94a3b8}
        .post-head{padding:44px 20px 8px}
        .post-head h1{font-size:clamp(30px,5vw,44px);line-height:1.15;color:var(--ink);margin:10px 0 14px;font-weight:800;letter-spacing:-.02em}
        .post-head .lead{font-size:20px;color:var(--text);margin:0 0 14px}
        .meta{color:var(--muted);font-size:14px;margin:0}
        .cover{margin:24px auto 8px}
        .cover img{display:block;width:100%;border-radius:18px;aspect-ratio:1200/630;object-fit:cover}
        .prose{font-size:18px;color:#1e293b;padding-top:12px}
        .prose h2{font-size:27px;line-height:1.25;color:var(--ink);margin:40px 0 12px;letter-spacing:-.01em}
        .prose h3{font-size:21px;color:var(--ink);margin:30px 0 8px}
        .prose p,.prose ul,.prose ol{margin:0 0 20px}
        .prose li{margin:6px 0}
        .prose img{border-radius:12px;margin:8px 0}
        .prose blockquote{margin:24px 0;padding:4px 20px;border-left:4px solid var(--green);background:#ecfdf5;border-radius:0 12px 12px 0;color:#065f46}
        .prose code{background:#f1f5f9;padding:2px 6px;border-radius:6px;font-size:.88em}
        .prose pre{background:var(--navy);color:#e2e8f0;padding:16px;border-radius:12px;overflow-x:auto}
        .prose pre code{background:none;padding:0}
        .prose hr{border:0;border-top:1px solid var(--line);margin:36px 0}
        .prose .table{display:block;overflow-x:auto;border-collapse:collapse;font-size:16px;margin:0 0 24px}
        .prose th,.prose td{border:1px solid var(--line);padding:8px 12px;text-align:left}
        .prose th{background:#f1f5f9;color:var(--ink)}
        .post-foot{padding-top:12px;display:grid;gap:18px}
        .share{display:flex;flex-wrap:wrap;gap:10px;align-items:center;font-size:14px}
        .share span{color:var(--muted);font-weight:600}
        .share a{border:1px solid var(--line);background:#fff;border-radius:999px;padding:6px 14px;text-decoration:none;color:var(--text)}
        .share a:hover{border-color:var(--green);color:var(--green-dark)}
        .related{padding-bottom:24px}
        .related h2{color:var(--ink);font-size:24px;margin:8px 0 0}
        .site-foot{background:var(--navy);color:#94a3b8;margin-top:40px}
        .foot{display:flex;flex-wrap:wrap;justify-content:space-between;align-items:center;gap:20px;padding:40px 20px}
        .foot img{height:34px;width:auto}
        .foot p{margin:6px 0 0;font-size:14px}
        .foot nav{display:flex;gap:20px;flex-wrap:wrap}
        .foot nav a{color:#cbd5e1;text-decoration:none;font-size:14px}
        .copy{border-top:1px solid rgba(255,255,255,.08);margin:0;padding:18px 20px;text-align:center;font-size:12px;color:#64748b}
        @media (min-width:760px){.cta{grid-template-columns:1fr auto;align-items:center;padding:32px 36px}.cta .small{grid-column:2;justify-self:center;margin-top:-6px}}
        @media (max-width:640px){.hide-sm{display:none}.site-nav{gap:14px}.hero{padding:40px 0 44px}.prose{font-size:17px}.post-head .lead{font-size:18px}}
        """;
}
