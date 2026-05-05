import type { MessageDto } from './chat-api'
import type { MarketplaceTask, TaskApplication } from './marketplace'

export function buildIdePromptFromContext(params: {
  task?: MarketplaceTask | null
  application?: TaskApplication | null
  messages?: MessageDto[]
}) {
  const { task, application, messages = [] } = params
  const fileMessages = messages.filter((message) => !!message.fileUrl || !!message.fileName)
  const recentMessages = messages
    .filter((message) => message.content?.trim())
    .slice(-6)
    .map((message) => `${message.senderName}: ${message.content.trim()}`)

  const parts = [
    'Нужно подготовить рабочий проект для фриланс-заказа.',
    task
      ? `Заказ: ${task.title}\nОписание: ${task.description}\nКатегория: ${task.category}\nБюджет: ${task.budget} ${task.currency}`
      : null,
    application
      ? `Контекст отклика фрилансера:\n${application.proposal}\nПредложенный бюджет: ${application.proposedBudget}`
      : null,
    recentMessages.length > 0
      ? `Ключевые сообщения из рабочего чата:\n${recentMessages.join('\n')}`
      : null,
    fileMessages.length > 0
      ? `В чате приложены файлы проекта: ${fileMessages.map((message) => message.fileName ?? 'file').join(', ')}. Учти эти файлы как входной контекст и продолжай работу по существующему проекту, а не генерируй всё с нуля.`
      : 'Если файлов проекта нет, сначала создай исходную структуру и дальше развивай проект по описанию.',
  ]

  return parts.filter(Boolean).join('\n\n')
}

export function buildIdePrefillQuery(prompt: string, meta?: { taskId?: string | null; chatId?: string | null }) {
  const query = new URLSearchParams()
  query.set('prefill', prompt)
  if (meta?.taskId) query.set('taskId', meta.taskId)
  if (meta?.chatId) query.set('chatId', meta.chatId)
  return `/ide?${query.toString()}`
}

export function getApplicationRedirectKey(applicationId: string) {
  return `libr4.acceptedApplicationRedirect.${applicationId}`
}
