import { colors } from '@/constants/colors'
import type { LucideIcon } from 'lucide-react-native'
import { forwardRef } from 'react'
import { Pressable, StyleSheet, Text, type PressableProps, type View } from 'react-native'

type TabButtonProps = PressableProps & {
  isFocused?: boolean
  icon: LucideIcon
  label: string
}

export const TabButton = forwardRef<View, TabButtonProps>(({ isFocused, icon: Icon, label, ...props }, ref) => {
  const color = isFocused ? colors.primary : colors.textSecondary
  return (
    <Pressable ref={ref} {...props} style={styles.button}>
      <Icon size={20} color={color} />
      <Text style={[styles.label, { color }]}>{label}</Text>
    </Pressable>
  )
})
TabButton.displayName = 'TabButton'

const styles = StyleSheet.create({
  button: { flex: 1, alignItems: 'center', justifyContent: 'center', gap: 3, paddingVertical: 6 },
  label: { fontSize: 11, fontWeight: '500' },
})
