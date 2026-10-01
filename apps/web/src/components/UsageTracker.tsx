import { useEffect } from 'react'
import { useLocation } from 'react-router-dom'
import { featureForPath, trackPage } from '../lib/usageApi'

/** Counts which part of FinFlow each page visit belongs to (no content, just the module). Renders nothing. */
export function UsageTracker() {
  const { pathname } = useLocation()
  useEffect(() => {
    const feature = featureForPath(pathname)
    if (feature) trackPage(feature)
  }, [pathname])
  return null
}
