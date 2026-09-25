import { Image, type ImageSourcePropType, type ImageStyle, type StyleProp } from 'react-native'

const sources = {
  light: require('@/assets/images/flowiq-lockup-light.png'),
  dark: require('@/assets/images/flowiq-lockup-dark.png'),
}

// Matches flowiq-lockup-light.png/-dark.png's actual pixel dimensions (316x100).
const DEFAULT_ASPECT_RATIO = 316 / 100
const DEFAULT_HEIGHT = 42

export function Logo({
  variant = 'light',
  source,
  aspectRatio = DEFAULT_ASPECT_RATIO,
  height = DEFAULT_HEIGHT,
  style,
}: {
  variant?: 'light' | 'dark'
  source?: ImageSourcePropType
  aspectRatio?: number
  height?: number
  style?: StyleProp<ImageStyle>
}) {
  // Compute an explicit pixel width rather than relying on the CSS `aspectRatio` style —
  // react-native-web doesn't reliably size the <img> box from aspectRatio alone (it can
  // fall back to the asset's natural pixel size instead), which native Yoga handles fine.
  // An explicit width avoids that platform-specific ambiguity entirely.
  return (
    <Image
      source={source ?? sources[variant]}
      style={[{ width: height * aspectRatio, height }, style]}
      resizeMode="contain"
    />
  )
}
