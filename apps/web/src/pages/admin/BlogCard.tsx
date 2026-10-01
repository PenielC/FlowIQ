import { ExternalLink, Newspaper, Pencil, Plus, Trash2 } from 'lucide-react'
import { useCallback, useEffect, useState } from 'react'
import { formatDate } from '../../lib/categoryDisplay'
import { deleteBlogPost, fetchBlogPosts, setBlogPostPublished, type BlogCategory, type BlogPost } from '../../lib/blogApi'
import { BlogEditor } from './BlogEditor'

/** Platform admin: write and publish blog posts, and see which ones bring readers and sign-ups. */
export function BlogCard() {
  const [posts, setPosts] = useState<BlogPost[]>([])
  const [categories, setCategories] = useState<BlogCategory[]>([])
  const [editing, setEditing] = useState<BlogPost | 'new' | null>(null)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(() => {
    fetchBlogPosts()
      .then((r) => {
        setPosts(r.posts)
        setCategories(r.categories)
      })
      .catch((err) => setError(err instanceof Error ? err.message : 'Failed to load posts'))
  }, [])

  useEffect(load, [load])

  async function togglePublished(p: BlogPost) {
    if (p.publishedAtUtc && !window.confirm(`Unpublish "${p.title}"? It will disappear from the blog and its links will stop working.`)) return
    setError(null)
    try {
      await setBlogPostPublished(p.id, !p.publishedAtUtc)
      load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update')
    }
  }

  async function remove(p: BlogPost) {
    if (!window.confirm(`Delete "${p.title}" for good? This can't be undone.`)) return
    await deleteBlogPost(p.id).catch(() => {})
    load()
  }

  const categoryLabel = (key: string) => categories.find((c) => c.key === key)?.label ?? key
  const totals = posts.reduce((t, p) => ({ views: t.views + p.views, clicks: t.clicks + p.ctaClicks, signups: t.signups + p.signups }), { views: 0, clicks: 0, signups: 0 })

  return (
    <div className="rounded-2xl border border-slate-200 bg-white shadow-sm" data-testid="blog-card">
      <div className="flex items-start justify-between gap-4 border-b border-slate-100 p-5">
        <div>
          <h2 className="flex items-center gap-2 text-base font-semibold text-slate-900">
            <Newspaper size={17} className="text-brand-primary" /> Blog
          </h2>
          <p className="mt-1 text-sm text-slate-500">
            Posts appear at{' '}
            <a href="/blog" target="_blank" rel="noopener noreferrer" className="font-medium text-brand-primary hover:underline">
              /blog
            </a>{' '}
            once published. Reads, &ldquo;Try FinFlow free&rdquo; clicks and the sign-ups they led to are counted per post.
          </p>
        </div>
        <button
          type="button"
          onClick={() => setEditing('new')}
          className="flex shrink-0 items-center gap-1.5 rounded-lg bg-brand-primary px-3 py-2 text-sm font-semibold text-white hover:bg-emerald-700"
        >
          <Plus size={15} /> New post
        </button>
      </div>
      {error && <p className="px-5 pt-3 text-sm text-red-600">{error}</p>}
      {posts.length === 0 ? (
        <p className="px-5 py-6 text-sm text-slate-400">No posts yet.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-100 text-left text-xs text-slate-500">
                <th className="w-full px-5 py-2 font-medium">Post</th>
                <th className="whitespace-nowrap px-3 py-2 text-right font-medium">Reads</th>
                <th className="whitespace-nowrap px-3 py-2 text-right font-medium">CTA clicks</th>
                <th className="whitespace-nowrap px-3 py-2 text-right font-medium">Sign-ups</th>
                <th className="px-5 py-2" />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {posts.map((p) => (
                <tr key={p.id} data-testid="blog-row">
                  <td className="max-w-0 min-w-64 px-5 py-3">
                    <p className="truncate font-semibold text-slate-900">{p.title}</p>
                    <p className="truncate text-xs text-slate-500">
                      <span
                        className={`mr-1.5 rounded-full px-2 py-0.5 text-[11px] font-medium ${p.publishedAtUtc ? 'bg-emerald-50 text-emerald-700' : 'bg-slate-100 text-slate-600'}`}
                      >
                        {p.publishedAtUtc ? `Published ${formatDate(p.publishedAtUtc)}` : 'Draft'}
                      </span>
                      {categoryLabel(p.category)} · {p.readingMinutes} min · /blog/{p.slug}
                    </p>
                  </td>
                  <td className="px-3 py-3 text-right tabular-nums">{p.views}</td>
                  <td className="px-3 py-3 text-right tabular-nums">{p.ctaClicks}</td>
                  <td className="px-3 py-3 text-right font-semibold tabular-nums text-slate-900">{p.signups}</td>
                  <td className="px-5 py-3">
                    <div className="flex items-center justify-end gap-1">
                      <button type="button" onClick={() => togglePublished(p)} className="rounded-md px-2.5 py-1 text-xs font-medium text-slate-700 hover:bg-slate-100">
                        {p.publishedAtUtc ? 'Unpublish' : 'Publish'}
                      </button>
                      {p.publishedAtUtc && (
                        <a
                          href={`/blog/${p.slug}`}
                          target="_blank"
                          rel="noopener noreferrer"
                          aria-label={`View ${p.title}`}
                          className="rounded-md p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
                        >
                          <ExternalLink size={14} />
                        </a>
                      )}
                      <button type="button" onClick={() => setEditing(p)} aria-label={`Edit ${p.title}`} className="rounded-md p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-700">
                        <Pencil size={14} />
                      </button>
                      <button type="button" onClick={() => remove(p)} aria-label={`Delete ${p.title}`} className="rounded-md p-1.5 text-slate-400 hover:bg-red-50 hover:text-red-600">
                        <Trash2 size={14} />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
            <tfoot>
              <tr className="border-t border-slate-200 text-xs text-slate-500">
                <td className="px-5 py-2">All posts</td>
                <td className="px-3 py-2 text-right tabular-nums">{totals.views}</td>
                <td className="px-3 py-2 text-right tabular-nums">{totals.clicks}</td>
                <td className="px-3 py-2 text-right font-semibold tabular-nums text-slate-700">{totals.signups}</td>
                <td />
              </tr>
            </tfoot>
          </table>
        </div>
      )}
      {editing && categories.length > 0 && (
        <BlogEditor
          post={editing === 'new' ? null : editing}
          categories={categories}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null)
            load()
          }}
        />
      )}
    </div>
  )
}
