import { colors } from '@/constants/colors'
import { useAuth } from '@/lib/AuthContext'
import { createBillingPortalSession, createCheckoutSession, fetchSubscriptionPlans, fetchSubscriptionStatus } from '@/lib/subscriptionsApi'
import { fetchPendingInvitations, fetchTeamMembers, removeMember, revokeInvitation } from '@/lib/teamApi'
import type { InvitationResponse, SubscriptionPlanResponse, SubscriptionStatusResponse, TeamMemberResponse } from '@/lib/types'
import { InviteMemberModal } from '@/screens/InviteMemberModal'
import { CreditCard, LogOut, Share2, Trash2, UserPlus, X } from 'lucide-react-native'
import { useEffect, useState } from 'react'
import { ActivityIndicator, Alert, Linking, Pressable, ScrollView, Share, StyleSheet, Text, View } from 'react-native'
import { SafeAreaView } from 'react-native-safe-area-context'

const STATUS_LABELS: Record<string, string> = {
  Incomplete: 'Incomplete',
  Trialing: 'Trialing',
  Active: 'Active',
  PastDue: 'Past Due',
  Canceled: 'Canceled',
  Unpaid: 'Unpaid',
}

const ROLE_COLORS: Record<string, string> = {
  Owner: colors.purple,
  Admin: colors.blue,
  Member: colors.textSecondary,
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
}

export default function MoreScreen() {
  const { user, logout } = useAuth()
  const canManage = user?.role === 'Owner' || user?.role === 'Admin'
  const isOwner = user?.role === 'Owner'

  const [members, setMembers] = useState<TeamMemberResponse[]>([])
  const [invitations, setInvitations] = useState<InvitationResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [reloadToken, setReloadToken] = useState(0)
  const [showInviteModal, setShowInviteModal] = useState(false)

  const [subscriptionStatus, setSubscriptionStatus] = useState<SubscriptionStatusResponse | null>(null)
  const [plans, setPlans] = useState<SubscriptionPlanResponse[]>([])
  const [isBillingLoading, setIsBillingLoading] = useState(true)
  const [billingActionKey, setBillingActionKey] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    Promise.all([fetchTeamMembers(), fetchPendingInvitations()])
      .then(([m, i]) => {
        if (!cancelled) {
          setMembers(m)
          setInvitations(i)
        }
      })
      .catch(() => {})
      .finally(() => !cancelled && setIsLoading(false))
    return () => {
      cancelled = true
    }
  }, [reloadToken])

  useEffect(() => {
    let cancelled = false
    Promise.all([fetchSubscriptionStatus(), fetchSubscriptionPlans()])
      .then(([status, planList]) => {
        if (!cancelled) {
          setSubscriptionStatus(status)
          setPlans(planList)
        }
      })
      .catch(() => {})
      .finally(() => !cancelled && setIsBillingLoading(false))
    return () => {
      cancelled = true
    }
  }, [])

  async function handleSubscribe(planKey: string) {
    setBillingActionKey(planKey)
    try {
      const url = await createCheckoutSession(planKey)
      await Linking.openURL(url)
    } catch {
      Alert.alert('Could not start checkout', 'Please try again.')
    } finally {
      setBillingActionKey(null)
    }
  }

  async function handleManageBilling() {
    setBillingActionKey('portal')
    try {
      const url = await createBillingPortalSession()
      await Linking.openURL(url)
    } catch {
      Alert.alert('Could not open billing portal', 'Please try again.')
    } finally {
      setBillingActionKey(null)
    }
  }

  function refresh() {
    setIsLoading(true)
    setReloadToken((t) => t + 1)
  }

  function handleRemove(userId: string) {
    Alert.alert('Remove team member?', 'They will lose access immediately.', [
      { text: 'Cancel', style: 'cancel' },
      {
        text: 'Remove',
        style: 'destructive',
        onPress: async () => {
          await removeMember(userId)
          refresh()
        },
      },
    ])
  }

  async function handleRevoke(invitationId: string) {
    await revokeInvitation(invitationId)
    refresh()
  }

  function shareInviteLink(invitation: InvitationResponse) {
    const webUrl = process.env.EXPO_PUBLIC_WEB_URL ?? 'http://localhost:5173'
    Share.share({ message: `Join our team on FlowIQ: ${webUrl}/accept-invitation?token=${invitation.token}` })
  }

  return (
    <SafeAreaView style={styles.safe}>
      <ScrollView contentContainerStyle={styles.content}>
        <View style={styles.card}>
          <Text style={styles.name}>
            {user?.firstName} {user?.lastName}
          </Text>
          <Text style={styles.email}>{user?.email}</Text>
          <Text style={styles.company}>
            {user?.companyName} · {user?.role}
          </Text>
        </View>

        <View style={styles.sectionHeader}>
          <Text style={styles.sectionTitle}>Team Members</Text>
          {canManage && (
            <Pressable style={styles.inviteButton} onPress={() => setShowInviteModal(true)}>
              <UserPlus size={13} color={colors.primary} />
              <Text style={styles.inviteButtonText}>Invite</Text>
            </Pressable>
          )}
        </View>

        {isLoading ? (
          <ActivityIndicator color={colors.primary} style={{ marginVertical: 20 }} />
        ) : (
          <View style={styles.listCard}>
            {members.map((member, i) => (
              <View key={member.id} style={[styles.memberRow, i === members.length - 1 && { borderBottomWidth: 0 }]}>
                <View style={styles.memberInfo}>
                  <Text style={styles.memberName}>
                    {member.firstName} {member.lastName}
                  </Text>
                  <Text style={styles.memberEmail}>{member.email}</Text>
                </View>
                <Text style={[styles.roleBadge, { color: ROLE_COLORS[member.role] ?? colors.textSecondary }]}>{member.role}</Text>
                {isOwner && member.id !== user?.id && (
                  <Pressable onPress={() => handleRemove(member.id)} style={styles.iconButton}>
                    <Trash2 size={14} color={colors.negative} />
                  </Pressable>
                )}
              </View>
            ))}
          </View>
        )}

        {invitations.length > 0 && (
          <>
            <View style={styles.sectionHeader}>
              <Text style={styles.sectionTitle}>Pending Invitations</Text>
            </View>
            <View style={styles.listCard}>
              {invitations.map((invitation, i) => (
                <View key={invitation.id} style={[styles.memberRow, i === invitations.length - 1 && { borderBottomWidth: 0 }]}>
                  <View style={styles.memberInfo}>
                    <Text style={styles.memberName}>{invitation.email}</Text>
                    <Text style={styles.memberEmail}>Expires {formatDate(invitation.expiresAtUtc)}</Text>
                  </View>
                  <Pressable onPress={() => shareInviteLink(invitation)} style={styles.iconButton}>
                    <Share2 size={14} color={colors.primary} />
                  </Pressable>
                  {canManage && (
                    <Pressable onPress={() => handleRevoke(invitation.id)} style={styles.iconButton}>
                      <X size={14} color={colors.negative} />
                    </Pressable>
                  )}
                </View>
              ))}
            </View>
          </>
        )}

        <View style={styles.sectionHeader}>
          <Text style={styles.sectionTitle}>Billing</Text>
        </View>
        {isBillingLoading ? (
          <ActivityIndicator color={colors.primary} style={{ marginVertical: 20 }} />
        ) : subscriptionStatus?.hasSubscription ? (
          <View style={styles.listCard}>
            <View style={[styles.memberRow, { borderBottomWidth: 0 }]}>
              <View style={styles.memberInfo}>
                <Text style={styles.memberName}>{subscriptionStatus.planDisplayName} Plan</Text>
                <Text style={styles.memberEmail}>
                  {STATUS_LABELS[subscriptionStatus.status ?? ''] ?? subscriptionStatus.status} · ${subscriptionStatus.monthlyPriceUsd?.toFixed(2)}/mo
                </Text>
              </View>
              {isOwner && (
                <Pressable onPress={handleManageBilling} style={styles.iconButton} disabled={billingActionKey !== null}>
                  <CreditCard size={16} color={colors.primary} />
                </Pressable>
              )}
            </View>
          </View>
        ) : isOwner ? (
          <View style={styles.listCard}>
            {plans.map((plan, i) => (
              <View key={plan.key} style={[styles.memberRow, i === plans.length - 1 && { borderBottomWidth: 0 }]}>
                <View style={styles.memberInfo}>
                  <Text style={styles.memberName}>{plan.displayName}</Text>
                  <Text style={styles.memberEmail}>${plan.monthlyPriceUsd.toFixed(0)}/mo</Text>
                </View>
                <Pressable onPress={() => handleSubscribe(plan.key)} style={styles.iconButton} disabled={billingActionKey !== null}>
                  <Text style={{ color: colors.primary, fontSize: 12, fontWeight: '600' }}>
                    {billingActionKey === plan.key ? 'Opening…' : 'Subscribe'}
                  </Text>
                </Pressable>
              </View>
            ))}
          </View>
        ) : (
          <View style={styles.listCard}>
            <Text style={{ color: colors.textMuted, textAlign: 'center', paddingVertical: 16, fontSize: 13 }}>
              Only the account owner can manage billing.
            </Text>
          </View>
        )}

        <Pressable style={styles.logoutButton} onPress={logout}>
          <LogOut size={16} color={colors.negative} />
          <Text style={styles.logoutText}>Log out</Text>
        </Pressable>
      </ScrollView>

      <InviteMemberModal
        visible={showInviteModal}
        onClose={() => setShowInviteModal(false)}
        onCreated={() => {
          setShowInviteModal(false)
          refresh()
        }}
      />
    </SafeAreaView>
  )
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: colors.navy },
  content: { padding: 20, paddingBottom: 40 },
  card: {
    backgroundColor: colors.navyCard,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    padding: 16,
    marginBottom: 24,
  },
  name: { color: colors.textPrimary, fontSize: 16, fontWeight: '700' },
  email: { color: colors.textSecondary, fontSize: 13, marginTop: 2 },
  company: { color: colors.textMuted, fontSize: 12, marginTop: 6 },
  sectionHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 10 },
  sectionTitle: { color: colors.textPrimary, fontSize: 15, fontWeight: '600' },
  inviteButton: { flexDirection: 'row', alignItems: 'center', gap: 4 },
  inviteButtonText: { color: colors.primary, fontSize: 13, fontWeight: '600' },
  listCard: {
    backgroundColor: colors.navyCard,
    borderRadius: 16,
    borderWidth: 1,
    borderColor: colors.border,
    paddingHorizontal: 14,
    marginBottom: 24,
  },
  memberRow: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    gap: 10,
  },
  memberInfo: { flex: 1 },
  memberName: { color: colors.textPrimary, fontSize: 14, fontWeight: '600' },
  memberEmail: { color: colors.textSecondary, fontSize: 12, marginTop: 2 },
  roleBadge: { fontSize: 12, fontWeight: '600' },
  iconButton: { padding: 4 },
  logoutButton: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    backgroundColor: colors.navyCard,
    borderRadius: 12,
    borderWidth: 1,
    borderColor: colors.border,
    paddingVertical: 12,
    paddingHorizontal: 16,
  },
  logoutText: { color: colors.negative, fontSize: 14, fontWeight: '600' },
})
