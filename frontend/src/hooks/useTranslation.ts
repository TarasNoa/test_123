'use client'

import { useState, useCallback, useMemo } from 'react'
import { useAuth } from '@/lib/auth'
import { 
  translationApi, 
  detectPreferredLanguage, 
  getLanguageLabel,
  normalizeLanguageCode 
} from '@/lib/translation-api'

interface UseTranslationReturn {
  targetLanguage: string
  targetLanguageLabel: string
  isTranslating: boolean
  translateContent: (content: string) => Promise<string>
  translateMessages: (messages: Array<{ content: string; id: string }>) => Promise<Map<string, string>>
  refreshLanguage: () => void
}

export function useTranslation(): UseTranslationReturn {
  const { user } = useAuth()
  const [isTranslating, setIsTranslating] = useState(false)
  const [cache, setCache] = useState<Map<string, string>>(new Map())

  // Определяем целевой язык: account language → browser language → 'en'
  const targetLanguage = useMemo(() => {
    const browserLang = typeof window !== 'undefined' ? navigator.language : undefined
    return detectPreferredLanguage(user?.languages, browserLang)
  }, [user?.languages])

  const targetLanguageLabel = useMemo(() => {
    return getLanguageLabel(targetLanguage)
  }, [targetLanguage])

  const getCacheKey = useCallback((content: string, lang: string) => {
    return `${normalizeLanguageCode(lang)}:${content}`
  }, [])

  const translateContent = useCallback(async (content: string): Promise<string> => {
    if (!content.trim()) return content
    
    // Если язык целевой - английский или совпадает с исходным (не определили), не переводим
    if (targetLanguage === 'en') return content

    const cacheKey = getCacheKey(content, targetLanguage)
    
    // Проверяем кэш
    if (cache.has(cacheKey)) {
      return cache.get(cacheKey)!
    }

    setIsTranslating(true)
    try {
      const response = await translationApi.translateBatch([content], targetLanguage)
      const translated = response.items[0] ?? content
      
      // Сохраняем в кэш
      setCache(prev => new Map(prev).set(cacheKey, translated))
      
      return translated
    } catch (error) {
      console.error('Translation failed:', error)
      return content // Fallback: возвращаем оригинал при ошибке
    } finally {
      setIsTranslating(false)
    }
  }, [targetLanguage, cache, getCacheKey])

  const translateMessages = useCallback(async (
    messages: Array<{ content: string; id: string }>
  ): Promise<Map<string, string>> => {
    if (messages.length === 0 || targetLanguage === 'en') {
      return new Map(messages.map(m => [m.id, m.content]))
    }

    // Фильтруем уже закэшированные
    const toTranslate: Array<{ content: string; id: string }> = []
    const result = new Map<string, string>()

    for (const msg of messages) {
      const cacheKey = getCacheKey(msg.content, targetLanguage)
      if (cache.has(cacheKey)) {
        result.set(msg.id, cache.get(cacheKey)!)
      } else {
        toTranslate.push(msg)
      }
    }

    if (toTranslate.length === 0) {
      return result
    }

    setIsTranslating(true)
    try {
      const contents = toTranslate.map(m => m.content)
      const response = await translationApi.translateBatch(contents, targetLanguage)
      
      toTranslate.forEach((msg, index) => {
        const translated = response.items[index] ?? msg.content
        const cacheKey = getCacheKey(msg.content, targetLanguage)
        
        setCache(prev => new Map(prev).set(cacheKey, translated))
        result.set(msg.id, translated)
      })

      return result
    } catch (error) {
      console.error('Batch translation failed:', error)
      // Fallback: возвращаем оригиналы
      toTranslate.forEach(msg => result.set(msg.id, msg.content))
      return result
    } finally {
      setIsTranslating(false)
    }
  }, [targetLanguage, cache, getCacheKey])

  const refreshLanguage = useCallback(() => {
    setCache(new Map()) // Очищаем кэш при смене языка
  }, [])

  return {
    targetLanguage,
    targetLanguageLabel,
    isTranslating,
    translateContent,
    translateMessages,
    refreshLanguage,
  }
}

export { getLanguageLabel, normalizeLanguageCode }
