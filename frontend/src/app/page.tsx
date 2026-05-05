'use client'

import Link from 'next/link'
import { useAuth } from '@/lib/auth'
import { Button } from '@/components/ui/button'
import { Code2, Briefcase, Bot, Shield, Zap, Users, ArrowRight } from 'lucide-react'

const features = [
  {
    icon: Briefcase,
    title: 'Фриланс-биржа',
    desc: 'Находите исполнителей или берите задания. Эскроу-платежи, отзывы, KYC-верификация.',
  },
  {
    icon: Code2,
    title: 'AI IDE',
    desc: 'Встроенная IDE с кодогенерацией через AI-агентов. Monaco editor, quality gates, итерации.',
    accent: true,
  },
  {
    icon: Bot,
    title: 'AI Ассистент',
    desc: 'Чат с LLM: декомпозиция задач, code review, генерация промптов, анализ архитектуры.',
  },
  {
    icon: Shield,
    title: 'Безопасность',
    desc: '2FA, JWT, KYC, security testing через hacker-agent, архитектурные гарантии.',
  },
  {
    icon: Zap,
    title: 'Автоматизация',
    desc: 'Shadow workspace, Docker-изоляция, автоматический build/test цикл, MCP-интеграция.',
  },
  {
    icon: Users,
    title: 'Коллаборация',
    desc: 'Чаты в реальном времени, комнаты совместной работы, видеозвонки, обмен файлами.',
  },
]

export default function LandingPage() {
  const { user } = useAuth()

  return (
    <div className="min-h-screen">
      {/* Nav */}
      <nav className="sticky top-0 z-50 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="container flex h-14 items-center justify-between">
          <Link href="/" className="flex items-center gap-2">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-primary-foreground font-bold text-sm">
              L4
            </div>
            <span className="text-lg font-bold">Libr4</span>
          </Link>
          <div className="flex items-center gap-3">
            {user ? (
              <Button asChild>
                <Link href="/dashboard">
                  В кабинет <ArrowRight className="ml-2 h-4 w-4" />
                </Link>
              </Button>
            ) : (
              <>
                <Button variant="ghost" asChild>
                  <Link href="/login">Войти</Link>
                </Button>
                <Button asChild>
                  <Link href="/register">Начать бесплатно</Link>
                </Button>
              </>
            )}
          </div>
        </div>
      </nav>

      {/* Hero */}
      <section className="container py-24 md:py-32">
        <div className="mx-auto max-w-3xl text-center space-y-6">
          <div className="inline-flex items-center gap-2 rounded-full border bg-muted/50 px-4 py-1.5 text-sm">
            <span className="h-2 w-2 rounded-full bg-primary animate-pulse" />
            AI-powered платформа нового поколения
          </div>
          <h1 className="text-4xl font-bold tracking-tight sm:text-5xl md:text-6xl">
            Фриланс-биржа
            <br />
            <span className="text-primary">с встроенной IDE</span>
          </h1>
          <p className="mx-auto max-w-xl text-lg text-muted-foreground">
            Создавайте задания, находите исполнителей, генерируйте код через AI-агентов —
            всё в одном месте. Эскроу, чаты, code review, автоматический build.
          </p>
          <div className="flex flex-wrap items-center justify-center gap-3">
            {user ? (
              <Button size="lg" asChild>
                <Link href="/dashboard">
                  Перейти в кабинет <ArrowRight className="ml-2 h-4 w-4" />
                </Link>
              </Button>
            ) : (
              <>
                <Button size="lg" asChild>
                  <Link href="/register">
                    Начать бесплатно <ArrowRight className="ml-2 h-4 w-4" />
                  </Link>
                </Button>
                <Button size="lg" variant="outline" asChild>
                  <Link href="/ide">Попробовать IDE</Link>
                </Button>
              </>
            )}
          </div>
        </div>
      </section>

      {/* Features */}
      <section className="border-t bg-muted/30 py-20">
        <div className="container">
          <div className="mx-auto max-w-2xl text-center mb-12">
            <h2 className="text-3xl font-bold tracking-tight">Всё, что нужно для работы</h2>
            <p className="mt-2 text-muted-foreground">
              Объединяем маркетплейс, IDE и AI в единую платформу
            </p>
          </div>
          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {features.map((f) => (
              <div
                key={f.title}
                className="group rounded-xl border bg-card p-6 transition-all hover:shadow-md hover:border-primary/20"
              >
                <div
                  className={`mb-4 inline-flex h-10 w-10 items-center justify-center rounded-lg ${
                    f.accent
                      ? 'bg-secondary text-secondary-foreground'
                      : 'bg-primary/10 text-primary'
                  }`}
                >
                  <f.icon className="h-5 w-5" />
                </div>
                <h3 className="mb-2 font-semibold">{f.title}</h3>
                <p className="text-sm text-muted-foreground">{f.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="py-20">
        <div className="container">
          <div className="mx-auto max-w-2xl rounded-2xl bg-gradient-to-br from-primary/10 via-secondary/10 to-primary/5 border p-8 md:p-12 text-center">
            <h2 className="text-2xl font-bold md:text-3xl">Готовы начать?</h2>
            <p className="mt-2 text-muted-foreground">
              Присоединяйтесь к Libr4 — создавайте, зарабатывайте, автоматизируйте.
            </p>
            <div className="mt-6 flex flex-wrap items-center justify-center gap-3">
              <Button size="lg" asChild>
                <Link href="/register">Создать аккаунт</Link>
              </Button>
              <Button size="lg" variant="outline" asChild>
                <Link href="/tasks">Смотреть задания</Link>
              </Button>
            </div>
          </div>
        </div>
      </section>

      {/* Footer */}
      <footer className="border-t py-8">
        <div className="container flex flex-col items-center gap-4 sm:flex-row sm:justify-between">
          <div className="flex items-center gap-2 text-sm text-muted-foreground">
            <div className="flex h-6 w-6 items-center justify-center rounded bg-primary text-primary-foreground text-xs font-bold">
              L4
            </div>
            Libr4 Platform &copy; {new Date().getFullYear()}
          </div>
          <div className="flex gap-6 text-sm text-muted-foreground">
            <Link href="/tasks" className="hover:text-foreground transition-colors">Задания</Link>
            <Link href="/ide" className="hover:text-foreground transition-colors">IDE</Link>
            <Link href="/login" className="hover:text-foreground transition-colors">Войти</Link>
          </div>
        </div>
      </footer>
    </div>
  )
}
