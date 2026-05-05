import { api } from './api'

export interface BatchTranslateResponse {
  items: string[]
  targetLanguage: string
  sourceLanguage?: string | null
}

export const translationApi = {
  translateBatch: (items: string[], targetLanguage: string, sourceLanguage?: string, model?: string) =>
    api<BatchTranslateResponse>('/ai/translate/batch', {
      method: 'POST',
      body: JSON.stringify({ items, targetLanguage, sourceLanguage, model }),
    }),
}

const PROFICIENCY_RANK: Record<string, number> = {
  Native: 4,
  Fluent: 3,
  Conversational: 2,
  Basic: 1,
}

export function detectPreferredLanguage(
  languages: Array<{ code: string; proficiency: string }> | undefined,
  browserLanguage?: string
) {
  const bestLanguage = [...(languages ?? [])]
    .sort((a, b) => (PROFICIENCY_RANK[b.proficiency] ?? 0) - (PROFICIENCY_RANK[a.proficiency] ?? 0))[0]

  if (bestLanguage?.code) {
    return normalizeLanguageCode(bestLanguage.code)
  }

  if (browserLanguage) {
    return normalizeLanguageCode(browserLanguage)
  }

  return 'en'
}

export function normalizeLanguageCode(language: string) {
  return language.toLowerCase().split('-')[0]
}

export function getLanguageLabel(language: string) {
  const labels: Record<string, string> = {
    ru: 'Русский',
    en: 'English',
    uk: 'Українська',
    es: 'Español',
    fr: 'Français',
    de: 'Deutsch',
    zh: '中文',
    ja: '日本語',
  }

  return labels[normalizeLanguageCode(language)] ?? language.toUpperCase()
}
