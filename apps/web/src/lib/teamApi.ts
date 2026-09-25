import { api } from './api'
import type { ApiResponse, AuthResponse, InvitationResponse, TeamMemberResponse } from './types'

export async function fetchTeamMembers() {
  const res = await api.get<ApiResponse<TeamMemberResponse[]>>('/api/team/members')
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load team members')
  return res.data.data
}

export async function fetchPendingInvitations() {
  const res = await api.get<ApiResponse<InvitationResponse[]>>('/api/team/invitations')
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to load invitations')
  return res.data.data
}

export async function createInvitation(email: string, role: string) {
  const res = await api.post<ApiResponse<InvitationResponse>>('/api/team/invitations', { email, role })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to create invitation')
  return res.data.data
}

export async function revokeInvitation(id: string) {
  await api.post(`/api/team/invitations/${id}/revoke`)
}

export async function updateMemberRole(userId: string, role: string) {
  const res = await api.patch<ApiResponse<TeamMemberResponse>>(`/api/team/members/${userId}/role`, { role })
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to update role')
  return res.data.data
}

export async function removeMember(userId: string) {
  await api.delete(`/api/team/members/${userId}`)
}

export async function acceptInvitation(input: { token: string; firstName: string; lastName: string; password: string }) {
  const res = await api.post<ApiResponse<AuthResponse>>('/api/team/accept-invitation', input)
  if (!res.data.data) throw new Error(res.data.message ?? 'Failed to accept invitation')
  return res.data.data
}
