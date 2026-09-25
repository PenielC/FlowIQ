import { colors } from '@/constants/colors'
import { Eye, EyeOff, Lock } from 'lucide-react-native'
import { useState } from 'react'
import { Pressable, StyleSheet, TextInput, View } from 'react-native'

export function PasswordField({
  value,
  onChangeText,
  placeholder,
}: {
  value: string
  onChangeText: (value: string) => void
  placeholder?: string
}) {
  const [visible, setVisible] = useState(false)

  return (
    <View style={styles.wrap}>
      <Lock size={16} color={colors.textMuted} style={styles.icon} />
      <TextInput
        style={styles.input}
        value={value}
        onChangeText={onChangeText}
        secureTextEntry={!visible}
        placeholder={placeholder}
        placeholderTextColor={colors.textMuted}
        autoCapitalize="none"
      />
      <Pressable onPress={() => setVisible((v) => !v)} hitSlop={8} style={styles.toggle}>
        {visible ? <EyeOff size={16} color={colors.textMuted} /> : <Eye size={16} color={colors.textMuted} />}
      </Pressable>
    </View>
  )
}

const styles = StyleSheet.create({
  wrap: { position: 'relative', justifyContent: 'center' },
  icon: { position: 'absolute', left: 14, zIndex: 1 },
  toggle: { position: 'absolute', right: 14 },
  input: {
    backgroundColor: colors.surfaceLight,
    borderWidth: 1,
    borderColor: colors.borderLight,
    borderRadius: 10,
    paddingLeft: 40,
    paddingRight: 40,
    paddingVertical: 12,
    color: colors.textOnLight,
    fontSize: 15,
  },
})
