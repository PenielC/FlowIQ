import Svg, { Polyline } from 'react-native-svg'

const points = [8, 14, 10, 18, 22, 19, 26, 24, 30, 28, 34, 40]

export function Sparkline({ width = 96, height = 36, color = '#6EE7B7' }: { width?: number; height?: number; color?: string }) {
  const max = Math.max(...points)
  const min = Math.min(...points)
  const step = width / (points.length - 1)
  const coords = points
    .map((p, i) => {
      const x = i * step
      const y = height - ((p - min) / (max - min)) * height
      return `${x},${y}`
    })
    .join(' ')

  return (
    <Svg width={width} height={height}>
      <Polyline points={coords} fill="none" stroke={color} strokeWidth={2} strokeLinecap="round" strokeLinejoin="round" />
    </Svg>
  )
}
