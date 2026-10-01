const KEY = 'finflow_signup_source'
const MAX_AGE_MS = 30 * 24 * 60 * 60 * 1000

/**
 * Remembers where a visitor came from (e.g. `?ref=blog:owner-drawings` from a blog post's "Try FinFlow free"),
 * so sign-ups can be credited to the post even if they look around before registering. Kept for 30 days.
 */
export function captureSignupSource(search: string) {
  const ref = new URLSearchParams(search).get('ref')?.trim()
  if (!ref) return
  try {
    localStorage.setItem(KEY, JSON.stringify({ ref: ref.slice(0, 100), at: Date.now() }))
  } catch {
    // Storage blocked (private mode): the source is simply not recorded.
  }
}

export function readSignupSource(): string | undefined {
  try {
    const saved = JSON.parse(localStorage.getItem(KEY) ?? 'null') as { ref?: string; at?: number } | null
    if (saved?.ref && saved.at && Date.now() - saved.at < MAX_AGE_MS) return saved.ref
  } catch {
    // Unreadable: ignore.
  }
  return undefined
}

export function clearSignupSource() {
  try {
    localStorage.removeItem(KEY)
  } catch {
    // ignore
  }
}
