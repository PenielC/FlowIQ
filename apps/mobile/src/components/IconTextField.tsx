import { colors } from '@/constants/colors'
import type { LucideIcon } from 'lucide-react-native'
import { StyleSheet, TextInput, View, type TextInputProps } from 'react-native'

export function IconTextField({
  icon: Icon,
  ...inputProps
}: { icon: LucideIcon } & TextInputProps) {
  return (
    <View style={styles.wrap}>
      <Icon size={16} color={colors.textMuted} style={styles.icon} />
      <TextInput style={styles.input} placeholderTextColor={colors.textMuted} {...inputProps} />
    </View>
  )
}

const styles = StyleSheet.create({
  wrap: { position: 'relative', justifyContent: 'center' },
  icon: { position: 'absolute', left: 14, zIndex: 1 },
  input: {
    backgroundColor: colors.surfaceLight,
    borderWidth: 1,
    borderColor: colors.borderLight,
    borderRadius: 10,
    paddingLeft: 40,
    paddingRight: 14,
    paddingVertical: 12,
    color: colors.textOnLight,
    fontSize: 15,
  },
})
