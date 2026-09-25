import { Building2, Copy, Trash2, UserPlus, X } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { fetchCompanyLogo, removeCompanyLogo, updateCompanyCurrency, uploadCompanyLogo } from '../../lib/companiesApi'
import { useAuth } from '../../lib/AuthContext'
import { currencies } from '../../lib/currencies'
import { fetchPendingInvitations, fetchTeamMembers, removeMember, revokeInvitation, updateMemberRole } from '../../lib/teamApi'
import { teamRoles, type InvitationResponse, type TeamMemberResponse } from '../../lib/types'
import { InviteMemberModal } from './InviteMemberModal'

const MAX_LOGO_SIZE_BYTES = 2 * 1024 * 1024
const ALLOWED_LOGO_TYPES = ['image/png', 'image/jpeg', 'image/webp']

const ROLE_COLORS: Record<string, string> = {
  Owner: 'bg-purple-100 text-purple-700',
  Admin: 'bg-blue-100 text-blue-700',
  Member: 'bg-slate-100 text-slate-600',
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
}

export function SettingsPage() {
  const { user, refreshUser } = useAuth()
  const canManage = user?.role === 'Owner' || user?.role === 'Admin'
  const isOwner = user?.role === 'Owner'

  const [isSavingCurrency, setIsSavingCurrency] = useState(false)
  const [currencyError, setCurrencyError] = useState<string | null>(null)

  const [members, setMembers] = useState<TeamMemberResponse[]>([])
  const [invitations, setInvitations] = useState<InvitationResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [reloadToken, setReloadToken] = useState(0)
  const [showInviteModal, setShowInviteModal] = useState(false)
  const [copiedId, setCopiedId] = useState<string | null>(null)

  const [logoDataUrl, setLogoDataUrl] = useState<string | null>(null)
  const [logoError, setLogoError] = useState<string | null>(null)
  const [isUploadingLogo, setIsUploadingLogo] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)

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
    fetchCompanyLogo()
      .then((logo) => !cancelled && setLogoDataUrl(logo.dataUrl))
      .catch(() => {})
    return () => {
      cancelled = true
    }
  }, [])

  async function handleLogoSelected(file: File) {
    setLogoError(null)
    if (!ALLOWED_LOGO_TYPES.includes(file.type)) {
      setLogoError('Logo must be a PNG, JPEG, or WebP image.')
      return
    }
    if (file.size > MAX_LOGO_SIZE_BYTES) {
      setLogoError('Logo must be 2MB or smaller.')
      return
    }

    setIsUploadingLogo(true)
    try {
      await uploadCompanyLogo(file)
      const logo = await fetchCompanyLogo()
      setLogoDataUrl(logo.dataUrl)
    } catch (err) {
      setLogoError(err instanceof Error ? err.message : 'Failed to upload logo')
    } finally {
      setIsUploadingLogo(false)
    }
  }

  async function handleRemoveLogo() {
    setIsUploadingLogo(true)
    try {
      await removeCompanyLogo()
      setLogoDataUrl(null)
    } catch (err) {
      setLogoError(err instanceof Error ? err.message : 'Failed to remove logo')
    } finally {
      setIsUploadingLogo(false)
    }
  }

  async function handleCurrencyChange(currency: string) {
    setCurrencyError(null)
    setIsSavingCurrency(true)
    try {
      await updateCompanyCurrency(currency)
      await refreshUser()
    } catch (err) {
      setCurrencyError(err instanceof Error ? err.message : 'Failed to update currency')
    } finally {
      setIsSavingCurrency(false)
    }
  }

  function refresh() {
    setIsLoading(true)
    setReloadToken((t) => t + 1)
  }

  async function handleRoleChange(userId: string, role: string) {
    await updateMemberRole(userId, role)
    refresh()
  }

  async function handleRemove(userId: string) {
    await removeMember(userId)
    refresh()
  }

  async function handleRevoke(invitationId: string) {
    await revokeInvitation(invitationId)
    refresh()
  }

  function copyInviteLink(invitation: InvitationResponse) {
    const link = `${window.location.origin}/accept-invitation?token=${encodeURIComponent(invitation.token)}`
    navigator.clipboard.writeText(link).then(() => {
      setCopiedId(invitation.id)
      setTimeout(() => setCopiedId(null), 2000)
    })
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Settings</h1>
        <p className="text-sm text-slate-500">Manage your team.</p>
      </div>

      <div className="rounded-2xl border border-slate-200 bg-white shadow-sm">
        <div className="border-b border-slate-100 p-5">
          <h2 className="text-base font-semibold text-slate-900">Company Profile</h2>
          <p className="mt-0.5 text-sm text-slate-500">This logo appears on your invoices.</p>
        </div>
        <div className="flex items-center gap-4 p-5">
          <div className="flex h-16 w-16 shrink-0 items-center justify-center overflow-hidden rounded-xl border border-slate-200 bg-slate-50">
            {logoDataUrl ? (
              <img src={logoDataUrl} alt="Company logo" className="h-full w-full object-contain" />
            ) : (
              <Building2 size={22} className="text-slate-300" />
            )}
          </div>

          {isOwner ? (
            <div className="flex flex-col gap-2">
              <div className="flex items-center gap-3">
                <button
                  type="button"
                  onClick={() => fileInputRef.current?.click()}
                  disabled={isUploadingLogo}
                  className="rounded-lg border border-slate-200 px-3 py-1.5 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-60"
                >
                  {isUploadingLogo ? 'Working…' : logoDataUrl ? 'Change Logo' : 'Upload Logo'}
                </button>
                {logoDataUrl && (
                  <button
                    type="button"
                    onClick={handleRemoveLogo}
                    disabled={isUploadingLogo}
                    className="text-sm font-medium text-slate-400 hover:text-red-500 disabled:opacity-60"
                  >
                    Remove
                  </button>
                )}
              </div>
              <input
                ref={fileInputRef}
                type="file"
                accept="image/png,image/jpeg,image/webp"
                className="hidden"
                onChange={(e) => {
                  const file = e.target.files?.[0]
                  if (file) handleLogoSelected(file)
                  e.target.value = ''
                }}
              />
              {logoError && <p className="text-xs text-red-500">{logoError}</p>}
              <p className="text-xs text-slate-400">PNG, JPEG, or WebP. Up to 2MB.</p>
            </div>
          ) : (
            <p className="text-sm text-slate-400">Only the account owner can change the company logo.</p>
          )}
        </div>

        <div className="flex items-center justify-between border-t border-slate-100 p-5">
          <div>
            <p className="text-sm font-medium text-slate-900">Currency</p>
            <p className="text-xs text-slate-400">Used to format every amount shown across the app.</p>
          </div>
          {isOwner ? (
            <div className="flex flex-col items-end gap-1">
              <select
                value={user?.companyCurrency ?? 'USD'}
                onChange={(e) => handleCurrencyChange(e.target.value)}
                disabled={isSavingCurrency}
                className="rounded-lg border border-slate-200 px-3 py-1.5 text-sm focus:border-brand-primary focus:outline-none disabled:opacity-60"
              >
                {currencies.map((c) => (
                  <option key={c.code} value={c.code}>
                    {c.code} — {c.name}
                  </option>
                ))}
              </select>
              {currencyError && <p className="text-xs text-red-500">{currencyError}</p>}
            </div>
          ) : (
            <span className="text-sm font-medium text-slate-500">{user?.companyCurrency ?? 'USD'}</span>
          )}
        </div>
      </div>

      <div className="rounded-2xl border border-slate-200 bg-white shadow-sm">
        <div className="flex items-center justify-between border-b border-slate-100 p-5">
          <h2 className="text-base font-semibold text-slate-900">Team Members</h2>
          {canManage && (
            <button
              type="button"
              onClick={() => setShowInviteModal(true)}
              className="flex items-center gap-2 rounded-lg bg-brand-primary px-3 py-1.5 text-sm font-semibold text-white hover:bg-emerald-700"
            >
              <UserPlus size={14} />
              Invite
            </button>
          )}
        </div>

        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-slate-100 text-left text-xs text-slate-500">
              <th className="px-5 py-3 font-medium">Name</th>
              <th className="px-5 py-3 font-medium">Email</th>
              <th className="px-5 py-3 font-medium">Role</th>
              <th className="px-5 py-3 font-medium">Joined</th>
              <th className="px-5 py-3 font-medium"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
            {isLoading ? (
              <tr>
                <td colSpan={5} className="px-5 py-8 text-center text-slate-400">
                  Loading…
                </td>
              </tr>
            ) : (
              members.map((member) => (
                <tr key={member.id}>
                  <td className="px-5 py-3 font-medium text-slate-900">
                    {member.firstName} {member.lastName}
                  </td>
                  <td className="px-5 py-3 text-slate-500">{member.email}</td>
                  <td className="px-5 py-3">
                    {isOwner && member.id !== user?.id ? (
                      <select
                        value={member.role}
                        onChange={(e) => handleRoleChange(member.id, e.target.value)}
                        className={`rounded-full border-0 px-2 py-0.5 text-xs font-medium ${ROLE_COLORS[member.role] ?? ROLE_COLORS.Member}`}
                      >
                        {teamRoles.map((r) => (
                          <option key={r} value={r}>
                            {r}
                          </option>
                        ))}
                      </select>
                    ) : (
                      <span className={`inline-block rounded-full px-2 py-0.5 text-xs font-medium ${ROLE_COLORS[member.role] ?? ROLE_COLORS.Member}`}>
                        {member.role}
                      </span>
                    )}
                  </td>
                  <td className="px-5 py-3 text-slate-500">{formatDate(member.joinedAtUtc)}</td>
                  <td className="px-5 py-3 text-right">
                    {isOwner && member.id !== user?.id && (
                      <button
                        type="button"
                        onClick={() => handleRemove(member.id)}
                        className="text-slate-400 hover:text-red-500"
                        aria-label="Remove member"
                      >
                        <Trash2 size={14} />
                      </button>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {invitations.length > 0 && (
        <div className="rounded-2xl border border-slate-200 bg-white shadow-sm">
          <div className="border-b border-slate-100 p-5">
            <h2 className="text-base font-semibold text-slate-900">Pending Invitations</h2>
          </div>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-slate-100 text-left text-xs text-slate-500">
                <th className="px-5 py-3 font-medium">Email</th>
                <th className="px-5 py-3 font-medium">Role</th>
                <th className="px-5 py-3 font-medium">Expires</th>
                <th className="px-5 py-3 font-medium"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {invitations.map((invitation) => (
                <tr key={invitation.id}>
                  <td className="px-5 py-3 font-medium text-slate-900">{invitation.email}</td>
                  <td className="px-5 py-3">
                    <span className={`inline-block rounded-full px-2 py-0.5 text-xs font-medium ${ROLE_COLORS[invitation.role] ?? ROLE_COLORS.Member}`}>
                      {invitation.role}
                    </span>
                  </td>
                  <td className="px-5 py-3 text-slate-500">{formatDate(invitation.expiresAtUtc)}</td>
                  <td className="px-5 py-3 text-right">
                    <div className="flex items-center justify-end gap-3">
                      <button
                        type="button"
                        onClick={() => copyInviteLink(invitation)}
                        className="flex items-center gap-1 text-xs font-medium text-brand-primary hover:underline"
                      >
                        <Copy size={13} />
                        {copiedId === invitation.id ? 'Copied!' : 'Copy link'}
                      </button>
                      {canManage && (
                        <button
                          type="button"
                          onClick={() => handleRevoke(invitation.id)}
                          className="text-slate-400 hover:text-red-500"
                          aria-label="Revoke invitation"
                        >
                          <X size={14} />
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {showInviteModal && (
        <InviteMemberModal
          onClose={() => setShowInviteModal(false)}
          onCreated={() => {
            setShowInviteModal(false)
            refresh()
          }}
        />
      )}
    </div>
  )
}
