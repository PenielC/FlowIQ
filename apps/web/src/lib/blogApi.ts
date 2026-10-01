import { api } from './api'
import type { ApiResponse } from './types'

export interface BlogCategory {
  key: string
  label: string
}

export interface BlogPost {
  id: string
  title: string
  slug: string
  summary: string
  body: string
  category: string
  tags: string[]
  authorName: string
  coverImageId: string | null
  coverImageAlt: string | null
  publishedAtUtc: string | null
  createdAtUtc: string
  readingMinutes: number
  views: number
  ctaClicks: number
  /** Businesses that signed up after clicking the post's "Try FinFlow free". */
  signups: number
}

export interface SaveBlogPostInput {
  title: string
  slug: string | null
  summary: string
  body: string
  category: string
  tags: string[]
  authorName: string | null
  coverImageId: string | null
  coverImageAlt: string | null
}

/** Where a blog image is served. Relative: the blog lives on the web site's own domain. */
export const blogImageUrl = (id: string) => `/blog/images/${id}`

/** Mirrors the server's slug rule, to show the address while typing. */
export function slugify(text: string) {
  return text
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 80)
    .replace(/-+$/, '')
}

export async function fetchBlogPosts() {
  const res = await api.get<ApiResponse<{ posts: BlogPost[]; categories: BlogCategory[] }>>('/api/admin/blog/posts')
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load posts')
  return res.data.data
}

export async function saveBlogPost(id: string | null, input: SaveBlogPostInput) {
  const res = id
    ? await api.put<ApiResponse<BlogPost>>(`/api/admin/blog/posts/${id}`, input)
    : await api.post<ApiResponse<BlogPost>>('/api/admin/blog/posts', input)
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to save')
  return res.data.data
}

export async function setBlogPostPublished(id: string, published: boolean) {
  const res = await api.post<ApiResponse<BlogPost>>(`/api/admin/blog/posts/${id}/published`, { published })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to update')
  return res.data.data
}

export async function deleteBlogPost(id: string) {
  await api.delete(`/api/admin/blog/posts/${id}`)
}

export async function previewBlogBody(body: string) {
  const res = await api.post<ApiResponse<{ html: string }>>('/api/admin/blog/preview', { body })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to preview')
  return res.data.data.html
}

export async function uploadBlogImage(file: File) {
  const formData = new FormData()
  formData.append('file', file)
  const res = await api.post<ApiResponse<{ id: string; url: string }>>('/api/admin/blog/images', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to upload image')
  return res.data.data
}
