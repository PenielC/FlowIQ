import lockupLight from '../assets/flowiq-lockup-transparent.png'
import lockupDark from '../assets/flowiq-lockup-dark.png'
import icon from '../assets/flowiq-icon-transparent.png'

/**
 * `variant="light"` (default) is the white-text wordmark for dark backgrounds
 * (sidebar, auth hero panel, landing nav/footer). `variant="dark"` recolors
 * "Flow" to navy for use on light backgrounds — the transparent PNG's "Flow"
 * text is baked-in white, so it disappears on a white/light panel otherwise.
 *
 * `src` overrides the image entirely (still respecting `variant` for `alt`/sizing
 * conventions) — used where a different source lockup is needed without changing
 * the app-wide default.
 */
export function Logo({
  className = '',
  variant = 'light',
  src,
}: {
  className?: string
  variant?: 'light' | 'dark'
  src?: string
}) {
  return (
    <img
      src={src ?? (variant === 'dark' ? lockupDark : lockupLight)}
      alt="FlowIQ"
      className={`h-8 w-auto shrink-0 self-start ${className}`}
    />
  )
}

export function LogoIcon({ className = '' }: { className?: string }) {
  return <img src={icon} alt="FlowIQ" className={`h-8 w-auto shrink-0 self-start ${className}`} />
}
