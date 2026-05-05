'use client'

import * as React from 'react'
import { Button } from '@/components/ui/button'
import { Check, Copy } from 'lucide-react'
import { cn } from '@/lib/utils'

interface CodeBlockProps {
  code: string
  language?: string
  filename?: string
  className?: string
  showLineNumbers?: boolean
}

export function CodeBlock({
  code,
  language = 'text',
  filename,
  className,
  showLineNumbers = true,
}: CodeBlockProps) {
  const [copied, setCopied] = React.useState(false)

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(code)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch {
      // ignore
    }
  }

  const lines = code.split('\n')
  const maxLineNumberWidth = lines.length.toString().length

  return (
    <div className={cn(
      'rounded-lg border bg-muted/50 overflow-hidden',
      className
    )}>
      {/* Header */}
      <div className="flex items-center justify-between border-b bg-muted/80 px-3 py-2">
        <div className="flex items-center gap-2">
          {filename ? (
            <span className="text-xs font-medium">{filename}</span>
          ) : (
            <span className="text-xs text-muted-foreground uppercase tracking-wider">
              {language}
            </span>
          )}
        </div>
        <Button
          variant="ghost"
          size="icon"
          className="h-6 w-6"
          onClick={handleCopy}
        >
          {copied ? (
            <Check className="h-3.5 w-3.5 text-green-500" />
          ) : (
            <Copy className="h-3.5 w-3.5" />
          )}
        </Button>
      </div>

      {/* Code */}
      <div className="overflow-x-auto">
        <pre className="text-xs leading-relaxed">
          <code className="block">
            {lines.map((line, index) => (
              <div key={index} className="flex">
                {showLineNumbers && (
                  <span
                    className="select-none text-muted-foreground/50 pr-3 pl-3 text-right min-w-[3rem]"
                    style={{ minWidth: `${maxLineNumberWidth + 2}ch` }}
                  >
                    {index + 1}
                  </span>
                )}
                <span className="pr-4 whitespace-pre">
                  {line || ' '}
                </span>
              </div>
            ))}
          </code>
        </pre>
      </div>
    </div>
  )
}

// Компонент для inline code
export function InlineCode({ children, className }: { children: React.ReactNode; className?: string }) {
  return (
    <code className={cn(
      'rounded bg-muted px-1.5 py-0.5 text-xs font-mono text-foreground',
      className
    )}>
      {children}
    </code>
  )
}

// Утилита для парсинга markdown-style code blocks из текста
export function parseCodeBlocks(text: string): Array<
  | { type: 'text'; content: string }
  | { type: 'code'; code: string; language: string }
> {
  const result: Array<
    | { type: 'text'; content: string }
    | { type: 'code'; code: string; language: string }
  > = []

  const codeBlockRegex = /```(\w+)?\n([\s\S]*?)```/g
  let lastIndex = 0
  let match

  while ((match = codeBlockRegex.exec(text)) !== null) {
    // Добавляем текст до code block
    if (match.index > lastIndex) {
      result.push({
        type: 'text',
        content: text.slice(lastIndex, match.index).trim(),
      })
    }

    // Добавляем code block
    result.push({
      type: 'code',
      language: match[1] || 'text',
      code: match[2].trim(),
    })

    lastIndex = match.index + match[0].length
  }

  // Добавляем оставшийся текст
  if (lastIndex < text.length) {
    const remaining = text.slice(lastIndex).trim()
    if (remaining) {
      result.push({ type: 'text', content: remaining })
    }
  }

  // Если ничего не найдено, возвращаем весь текст
  if (result.length === 0 && text.trim()) {
    result.push({ type: 'text', content: text.trim() })
  }

  return result
}

// Компонент для рендеринга сообщения с code blocks
export function MessageContent({ content, className }: { content: string; className?: string }) {
  const parsed = parseCodeBlocks(content)

  if (parsed.length === 1 && parsed[0].type === 'text') {
    return <p className={cn('whitespace-pre-wrap', className)}>{parsed[0].content}</p>
  }

  return (
    <div className={cn('space-y-3', className)}>
      {parsed.map((block, index) =>
        block.type === 'text' ? (
          <p key={index} className="whitespace-pre-wrap text-xs">
            {block.content}
          </p>
        ) : (
          <CodeBlock
            key={index}
            code={block.code}
            language={block.language}
            showLineNumbers={block.code.split('\n').length > 1}
          />
        )
      )}
    </div>
  )
}
