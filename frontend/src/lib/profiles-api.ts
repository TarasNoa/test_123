import { api } from './api'

export interface ProfileLanguageDto {
  code: string
  proficiency: 'Basic' | 'Conversational' | 'Fluent' | 'Native' | string
}

export interface ProfileDto {
  id: string
  userId: string
  headline?: string | null
  bio?: string | null
  location?: string | null
  timeZone?: string | null
  avatarUrl?: string | null
  coverUrl?: string | null
  websiteUrl?: string | null
  availability?: string
  hourlyRate?: number | null
  hourlyRateCurrency?: string | null
  completenessPct: number
  isPublic: boolean
  skills: Array<{ name: string; level: string; yearsOfExperience: number; verified: boolean }>
  languages: ProfileLanguageDto[]
  socials: Array<{ platform: string; url: string }>
}

export const profilesApi = {
  getMyProfile: () => api<ProfileDto>('/profiles/me'),
  getProfile: (userId: string) => api<ProfileDto>(`/profiles/${userId}`),
}
