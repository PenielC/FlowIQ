import { colors } from '@/constants/colors'
import { TabList, TabSlot, TabTrigger, Tabs } from 'expo-router/ui'
import { FileText, Home, ListChecks, MoreHorizontal, Plus } from 'lucide-react-native'
import { Pressable, StyleSheet, View } from 'react-native'
import { SafeAreaView } from 'react-native-safe-area-context'
import { TabButton } from './TabButton'

export function AppTabBar() {
  return (
    <Tabs style={styles.root}>
      <TabSlot style={styles.slot} />

      <SafeAreaView edges={['bottom']} style={styles.barWrap}>
        <View style={styles.bar}>
          <TabTrigger name="index" asChild>
            <TabButton icon={Home} label="Home" />
          </TabTrigger>
          <TabTrigger name="transactions" asChild>
            <TabButton icon={ListChecks} label="Transactions" />
          </TabTrigger>

          <View style={styles.fabSlot}>
            <TabTrigger name="transactions" asChild>
              <Pressable style={styles.fab}>
                <Plus size={22} color="#fff" />
              </Pressable>
            </TabTrigger>
          </View>

          <TabTrigger name="invoices" asChild>
            <TabButton icon={FileText} label="Invoices" />
          </TabTrigger>
          <TabTrigger name="more" asChild>
            <TabButton icon={MoreHorizontal} label="More" />
          </TabTrigger>
        </View>
      </SafeAreaView>

      <TabList style={styles.hidden}>
        <TabTrigger name="index" href="/" />
        <TabTrigger name="transactions" href="/transactions" />
        <TabTrigger name="invoices" href="/invoices" />
        <TabTrigger name="more" href="/more" />
      </TabList>
    </Tabs>
  )
}

const styles = StyleSheet.create({
  root: { flex: 1, backgroundColor: colors.navy },
  slot: { flex: 1 },
  hidden: { display: 'none' },
  barWrap: { backgroundColor: colors.navyLight, borderTopWidth: 1, borderTopColor: colors.border },
  bar: {
    flexDirection: 'row',
    alignItems: 'center',
    height: 60,
    paddingHorizontal: 8,
  },
  fabSlot: { width: 56, alignItems: 'center' },
  fab: {
    width: 48,
    height: 48,
    borderRadius: 24,
    backgroundColor: colors.primary,
    alignItems: 'center',
    justifyContent: 'center',
    marginTop: -24,
    shadowColor: colors.primary,
    shadowOpacity: 0.5,
    shadowRadius: 10,
    shadowOffset: { width: 0, height: 4 },
    elevation: 6,
  },
})
