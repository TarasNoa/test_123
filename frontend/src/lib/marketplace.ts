import type { AuthUser } from './auth'
import type { RunSummary } from './app-generation-api'

export interface MarketplaceTask {
  id: string
  title: string
  description: string
  category: string
  status: string
  clientId: string
  budget: number
  currency: string
  deadline: string | null
  createdAt: string
  applicationCount: number
}

export interface TaskApplication {
  id: string
  taskId: string
  freelancerId: string
  proposal: string
  proposedBudget: number
  status: string
  submittedAt: string
  respondedAt?: string | null
}

export interface SkillMetric {
  key: string
  label: string
  value: number
  hint: string
}

export interface RecommendedTask extends MarketplaceTask {
  matchScore: number
  reasons: string[]
}

export const marketplaceCategoryLabels: Record<string, string> = {
  Development: 'Фронтенд / Бэкенд',
  Design: 'Дизайн',
  Marketing: 'Маркетинг',
  Writing: 'Контент',
  DataEntry: 'Операционные задачи',
  Translation: 'Локализация',
  Other: 'Другое',
}

export const taskStatusMeta: Record<string, { label: string; tone: 'default' | 'secondary' | 'destructive' | 'outline' }> = {
  Draft: { label: 'Черновик', tone: 'outline' },
  Open: { label: 'Открыт', tone: 'default' },
  InProgress: { label: 'В работе', tone: 'secondary' },
  Completed: { label: 'Завершён', tone: 'outline' },
  Cancelled: { label: 'Отменён', tone: 'destructive' },
}

export const applicationStatusMeta: Record<string, { label: string; tone: 'default' | 'secondary' | 'destructive' | 'outline' }> = {
  Pending: { label: 'На рассмотрении', tone: 'secondary' },
  Accepted: { label: 'Одобрен', tone: 'default' },
  Rejected: { label: 'Отклонён', tone: 'destructive' },
  Withdrawn: { label: 'Отозван', tone: 'outline' },
}

export function getCategoryLabel(category: string) {
  return marketplaceCategoryLabels[category] ?? category
}

export function clampScore(value: number) {
  return Math.max(1, Math.min(10, Math.round(value)))
}

export function buildSkillMetrics(
  user: AuthUser | null,
  applications: TaskApplication[],
  runs: RunSummary[]
): SkillMetric[] {
  const accepted = applications.filter((app) => app.status === 'Accepted').length
  const completedRuns = runs.filter((run) => run.status === 'Completed').length
  const totalRuns = runs.length

  const roleBlob = `${user?.roles.join(' ') ?? ''} ${user?.displayName ?? ''}`.toLowerCase()
  const hasFrontendRole = /front|ui|react|next|design/.test(roleBlob)
  const hasBackendRole = /back|api|server|python|c#|dotnet|java|go/.test(roleBlob)
  const hasAiRole = /ai|ml|data|agent/.test(roleBlob)

  return [
    {
      key: 'frontend',
      label: 'Фронтенд',
      value: clampScore((hasFrontendRole ? 5 : 2) + accepted * 0.6 + completedRuns * 0.3),
      hint: 'UI, landing pages, SPA и дизайн-система',
    },
    {
      key: 'backend',
      label: 'Бэкенд',
      value: clampScore((hasBackendRole ? 6 : 3) + accepted * 0.7 + totalRuns * 0.2),
      hint: 'API, data layer, auth, интеграции',
    },
    {
      key: 'ai',
      label: 'AI / IDE',
      value: clampScore((hasAiRole ? 6 : 3) + completedRuns * 0.8 + totalRuns * 0.3),
      hint: 'AI-пайплайны, генерация, автоматизация',
    },
    {
      key: 'delivery',
      label: 'Delivery',
      value: clampScore(3 + accepted * 1.1 + completedRuns * 0.4),
      hint: 'Коммуникация, дедлайны, доведение до релиза',
    },
  ]
}

export function buildRecommendedTasks(tasks: MarketplaceTask[], user: AuthUser | null): RecommendedTask[] {
  const roleBlob = `${user?.roles.join(' ') ?? ''} ${user?.displayName ?? ''}`.toLowerCase()
  return tasks
    .filter((task) => task.status === 'Open')
    .map((task) => {
      let score = 40
      const reasons: string[] = []
      const taskBlob = `${task.title} ${task.description} ${task.category}`.toLowerCase()

      if (/front|ui|react|next|landing|design/.test(taskBlob) && /front|ui|design/.test(roleBlob)) {
        score += 30
        reasons.push('Подходит под ваш frontend/design стек')
      }

      if (/api|backend|django|python|c#|server|auth|postgres/.test(taskBlob) && /back|api|python|c#|server/.test(roleBlob)) {
        score += 30
        reasons.push('Совпадает с вашим backend/API профилем')
      }

      if (task.applicationCount < 3) {
        score += 10
        reasons.push('Невысокая конкуренция на отклик')
      }

      if (task.budget >= 500) {
        score += 8
        reasons.push('Хороший бюджет относительно среднего чека')
      }

      if (task.deadline) {
        const days = Math.ceil((new Date(task.deadline).getTime() - Date.now()) / (1000 * 60 * 60 * 24))
        if (days > 5) {
          score += 6
          reasons.push('Комфортный дедлайн')
        }
      }

      if (reasons.length === 0) {
        reasons.push('Подходит по общему профилю и текущей активности')
      }

      return {
        ...task,
        matchScore: Math.min(99, score),
        reasons: reasons.slice(0, 3),
      }
    })
    .sort((a, b) => b.matchScore - a.matchScore)
    .slice(0, 3)
}

export function getTaskSummary(task: MarketplaceTask) {
  return [
    `Проект: ${task.title}`,
    `Категория: ${getCategoryLabel(task.category)}`,
    `Бюджет: ${task.budget} ${task.currency}`,
    task.deadline ? `Дедлайн: ${new Date(task.deadline).toLocaleDateString('ru-RU')}` : null,
  ]
    .filter(Boolean)
    .join('\n')
}
