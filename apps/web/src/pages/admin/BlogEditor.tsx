import { Bold, Heading2, ImagePlus, Italic, Link2, List, ListOrdered, Quote, X } from 'lucide-react'
import { useRef, useState, type FormEvent } from 'react'
import {
  blogImageUrl,
  previewBlogBody,
  saveBlogPost,
  slugify,
  uploadBlogImage,
  type BlogCategory,
  type BlogPost,
} from '../../lib/blogApi'

const field = 'w-full rounded-lg border border-slate-200 px-3 py-2 text-sm focus:border-brand-primary focus:outline-none disabled:bg-slate-50 disabled:text-slate-500'
const label = 'mb-1 block text-sm font-medium text-slate-700'
const IMAGE_TYPES = 'image/png,image/jpeg,image/webp,image/gif'

/** Write or edit a blog post: details on the left, Markdown text with a live preview on the right. */
export function BlogEditor({
  post,
  categories,
  onClose,
  onSaved,
}: {
  post: BlogPost | null
  categories: BlogCategory[]
  onClose: () => void
  onSaved: (post: BlogPost) => void
}) {
  const published = !!post?.publishedAtUtc
  const [title, setTitle] = useState(post?.title ?? '')
  const [slug, setSlug] = useState(post?.slug ?? '')
  const [slugEdited, setSlugEdited] = useState(!!post)
  const [summary, setSummary] = useState(post?.summary ?? '')
  const [body, setBody] = useState(post?.body ?? '')
  const [category, setCategory] = useState(post?.category ?? categories[0]?.key ?? 'guides')
  const [tags, setTags] = useState(post?.tags.join(', ') ?? '')
  const [authorName, setAuthorName] = useState(post?.authorName ?? '')
  const [coverImageId, setCoverImageId] = useState<string | null>(post?.coverImageId ?? null)
  const [coverImageAlt, setCoverImageAlt] = useState(post?.coverImageAlt ?? '')
  const [tab, setTab] = useState<'write' | 'preview'>('write')
  const [previewHtml, setPreviewHtml] = useState('')
  const [busy, setBusy] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const bodyRef = useRef<HTMLTextAreaElement>(null)
  const inlineImageRef = useRef<HTMLInputElement>(null)

  const address = slugEdited ? slugify(slug) : slugify(title)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy('save')
    setError(null)
    try {
      const saved = await saveBlogPost(post?.id ?? null, {
        title,
        slug: address || null,
        summary,
        body,
        category,
        tags: tags.split(',').map((t) => t.trim()).filter(Boolean),
        authorName: authorName.trim() || null,
        coverImageId,
        coverImageAlt: coverImageAlt.trim() || null,
      })
      onSaved(saved)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save')
    } finally {
      setBusy(null)
    }
  }

  async function showPreview() {
    setTab('preview')
    setBusy('preview')
    try {
      setPreviewHtml(await previewBlogBody(body))
    } catch (err) {
      setPreviewHtml('')
      setError(err instanceof Error ? err.message : 'Failed to preview')
    } finally {
      setBusy(null)
    }
  }

  async function upload(file: File | undefined, onDone: (id: string) => void) {
    if (!file) return
    setBusy('upload')
    setError(null)
    try {
      onDone((await uploadBlogImage(file)).id)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to upload image')
    } finally {
      setBusy(null)
    }
  }

  /** Wraps the selection (or inserts a placeholder) with Markdown, keeping the cursor in a useful place. */
  function format(before: string, after = '', placeholder = '') {
    const el = bodyRef.current
    if (!el) return
    const { selectionStart: start, selectionEnd: end } = el
    const selected = body.slice(start, end) || placeholder
    const next = body.slice(0, start) + before + selected + after + body.slice(end)
    setBody(next)
    requestAnimationFrame(() => {
      el.focus()
      el.setSelectionRange(start + before.length, start + before.length + selected.length)
    })
  }

  /** Puts a prefix at the start of each selected line (headings, lists, quotes). */
  function prefixLines(prefix: (i: number) => string) {
    const el = bodyRef.current
    if (!el) return
    const lineStart = body.lastIndexOf('\n', el.selectionStart - 1) + 1
    const end = el.selectionEnd
    const block = body.slice(lineStart, end) || 'Text'
    const replaced = block
      .split('\n')
      .map((line, i) => prefix(i) + line)
      .join('\n')
    setBody(body.slice(0, lineStart) + replaced + body.slice(Math.max(end, lineStart)))
    requestAnimationFrame(() => el.focus())
  }

  const tools = [
    { icon: Heading2, name: 'Heading' },
    { icon: Bold, name: 'Bold' },
    { icon: Italic, name: 'Italic' },
    { icon: Link2, name: 'Link' },
    { icon: List, name: 'Bulleted list' },
    { icon: ListOrdered, name: 'Numbered list' },
    { icon: Quote, name: 'Quote' },
    { icon: ImagePlus, name: 'Insert image' },
  ]

  function runTool(name: string) {
    switch (name) {
      case 'Heading': return prefixLines(() => '## ')
      case 'Bold': return format('**', '**', 'bold text')
      case 'Italic': return format('_', '_', 'italic text')
      case 'Link': return format('[', '](https://)', 'link text')
      case 'Bulleted list': return prefixLines(() => '- ')
      case 'Numbered list': return prefixLines((i) => `${i + 1}. `)
      case 'Quote': return prefixLines(() => '> ')
      case 'Insert image': return inlineImageRef.current?.click()
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-stretch justify-center bg-slate-900/40 p-0 sm:p-4">
      <form onSubmit={submit} className="flex w-full max-w-7xl flex-col overflow-hidden bg-white shadow-xl sm:rounded-2xl" aria-label="Edit blog post">
        <div className="flex items-center justify-between border-b border-slate-100 px-5 py-3">
          <h2 className="text-lg font-bold text-slate-900">{post ? 'Edit post' : 'New post'}</h2>
          <div className="flex items-center gap-2">
            {error && <p className="max-w-md truncate text-sm text-red-600" role="alert" title={error}>{error}</p>}
            <button
              type="submit"
              disabled={busy !== null}
              className="rounded-lg bg-brand-primary px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700 disabled:opacity-60"
            >
              {busy === 'save' ? 'Saving…' : post ? 'Save changes' : 'Save as draft'}
            </button>
            <button type="button" onClick={onClose} aria-label="Close" className="rounded-md p-1.5 text-slate-400 hover:text-slate-600">
              <X size={20} />
            </button>
          </div>
        </div>

        <div className="grid min-h-0 flex-1 grid-cols-1 overflow-y-auto lg:grid-cols-[360px_1fr] lg:overflow-hidden">
          {/* ---- details */}
          <div className="flex flex-col gap-3 border-b border-slate-100 p-5 lg:overflow-y-auto lg:border-b-0 lg:border-r">
            <div>
              <label htmlFor="bpTitle" className={label}>Title</label>
              <input id="bpTitle" required maxLength={140} value={title} onChange={(e) => setTitle(e.target.value)} className={field} />
            </div>
            <div>
              <label htmlFor="bpSlug" className={label}>Address</label>
              <div className="flex items-center rounded-lg border border-slate-200 text-sm focus-within:border-brand-primary">
                <span className="pl-3 text-slate-400">/blog/</span>
                <input
                  id="bpSlug"
                  value={address}
                  disabled={published}
                  onChange={(e) => {
                    setSlug(e.target.value)
                    setSlugEdited(true)
                  }}
                  className="min-w-0 flex-1 rounded-r-lg py-2 pr-3 focus:outline-none disabled:bg-slate-50 disabled:text-slate-500"
                />
              </div>
              <p className="mt-1 text-xs text-slate-500">
                {published ? 'Fixed while published, so shared links keep working.' : 'Made from the title unless you change it.'}
              </p>
            </div>
            <div>
              <label htmlFor="bpSummary" className={label}>Summary</label>
              <textarea id="bpSummary" required rows={3} maxLength={300} value={summary} onChange={(e) => setSummary(e.target.value)} className={field} />
              <p className="mt-1 text-xs text-slate-500">Shown on the blog, in Google and in link previews. {summary.length}/300</p>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label htmlFor="bpCategory" className={label}>Category</label>
                <select id="bpCategory" value={category} onChange={(e) => setCategory(e.target.value)} className={field}>
                  {categories.map((c) => (
                    <option key={c.key} value={c.key}>{c.label}</option>
                  ))}
                </select>
              </div>
              <div>
                <label htmlFor="bpAuthor" className={label}>Author</label>
                <input id="bpAuthor" maxLength={80} value={authorName} onChange={(e) => setAuthorName(e.target.value)} className={field} placeholder="The FinFlow team" />
              </div>
            </div>
            <div>
              <label htmlFor="bpTags" className={label}>Tags</label>
              <input id="bpTags" value={tags} onChange={(e) => setTags(e.target.value)} className={field} placeholder="invoicing, late payments" />
              <p className="mt-1 text-xs text-slate-500">Up to 5, separated by commas.</p>
            </div>
            <div>
              <span className={label}>Cover image</span>
              {coverImageId ? (
                <div className="flex flex-col gap-2">
                  <img src={blogImageUrl(coverImageId)} alt="" className="aspect-[1200/630] w-full rounded-lg border border-slate-200 object-cover" data-testid="cover-preview" />
                  <div className="flex gap-2 text-xs">
                    <label className="cursor-pointer font-medium text-brand-primary hover:underline">
                      Replace
                      <input type="file" accept={IMAGE_TYPES} className="hidden" onChange={(e) => upload(e.target.files?.[0], setCoverImageId)} />
                    </label>
                    <button type="button" onClick={() => setCoverImageId(null)} className="font-medium text-slate-500 hover:text-red-600">
                      Remove
                    </button>
                  </div>
                  <input
                    aria-label="Cover image description"
                    maxLength={200}
                    value={coverImageAlt}
                    onChange={(e) => setCoverImageAlt(e.target.value)}
                    className={field}
                    placeholder="Describe the image (for screen readers and Google)"
                  />
                </div>
              ) : (
                <label className="flex cursor-pointer flex-col items-center justify-center gap-1 rounded-lg border border-dashed border-slate-300 px-3 py-6 text-center text-sm text-slate-500 hover:border-brand-primary hover:text-slate-700">
                  <ImagePlus size={20} />
                  {busy === 'upload' ? 'Uploading…' : 'Upload a cover (1200 × 630 works best, up to 2 MB)'}
                  <input type="file" accept={IMAGE_TYPES} className="hidden" data-testid="cover-input" onChange={(e) => upload(e.target.files?.[0], setCoverImageId)} />
                </label>
              )}
            </div>
          </div>

          {/* ---- text */}
          <div className="flex min-h-[520px] flex-col p-5 lg:min-h-0">
            <div className="mb-2 flex flex-wrap items-center justify-between gap-2">
              <div className="flex rounded-lg bg-slate-100 p-0.5 text-sm">
                <button type="button" onClick={() => setTab('write')} className={`rounded-md px-3 py-1 font-medium ${tab === 'write' ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-500'}`}>
                  Write
                </button>
                <button type="button" onClick={showPreview} className={`rounded-md px-3 py-1 font-medium ${tab === 'preview' ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-500'}`}>
                  Preview
                </button>
              </div>
              {tab === 'write' && (
                <div className="flex items-center gap-0.5">
                  {tools.map(({ icon: Icon, name }) => (
                    <button key={name} type="button" onClick={() => runTool(name)} title={name} aria-label={name} className="rounded-md p-1.5 text-slate-500 hover:bg-slate-100 hover:text-slate-800">
                      <Icon size={16} />
                    </button>
                  ))}
                  <input
                    ref={inlineImageRef}
                    type="file"
                    accept={IMAGE_TYPES}
                    className="hidden"
                    onChange={(e) => {
                      upload(e.target.files?.[0], (id) => format('![', `](${blogImageUrl(id)})`, 'describe the image'))
                      e.target.value = ''
                    }}
                  />
                </div>
              )}
            </div>
            {tab === 'write' ? (
              <>
                <textarea
                  ref={bodyRef}
                  required
                  aria-label="Post text"
                  value={body}
                  onChange={(e) => setBody(e.target.value)}
                  className="min-h-0 flex-1 resize-none rounded-lg border border-slate-200 p-4 font-mono text-sm leading-relaxed focus:border-brand-primary focus:outline-none"
                  placeholder={'Write in Markdown.\n\n## A heading\n\nA paragraph with **bold** text and a [link](https://example.com).\n\n- A list item'}
                />
                <p className="mt-1 text-xs text-slate-500">
                  Markdown: ## heading, **bold**, _italic_, [link](https://…), - list, &gt; quote, | tables |. About {Math.max(1, Math.round(body.split(/\s+/).filter(Boolean).length / 220))} min read.
                </p>
              </>
            ) : (
              <div className="min-h-0 flex-1 overflow-y-auto rounded-lg border border-slate-200 p-6">
                {busy === 'preview' ? (
                  <p className="text-sm text-slate-400">Loading preview…</p>
                ) : (
                  <article className="blog-prose mx-auto max-w-[680px]" data-testid="blog-preview">
                    <h1>{title || 'Untitled'}</h1>
                    <p className="lead">{summary}</p>
                    {coverImageId && <img src={blogImageUrl(coverImageId)} alt={coverImageAlt} />}
                    {/* Rendered by the API with raw HTML disabled, exactly as on the live blog. */}
                    <div dangerouslySetInnerHTML={{ __html: previewHtml }} />
                  </article>
                )}
              </div>
            )}
          </div>
        </div>
      </form>
    </div>
  )
}
