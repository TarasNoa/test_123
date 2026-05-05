'use client'

import * as React from 'react'
import { useParams, useRouter } from 'next/navigation'
import dynamic from 'next/dynamic'
import Link from 'next/link'
import { Button } from '@/components/ui/button'
import { FileTree } from '@/components/ide/file-tree'
import { AgentChat } from '@/components/ide/agent-chat'
import { EditorTabs } from '@/components/ide/editor-tabs'
import { TerminalPanel } from '@/components/ide/Terminal'
import { StatusBar } from '@/components/ide/status-bar'
import { StreamingIndicator } from '@/components/ide/streaming-indicator'
import { AgentGallery } from '@/components/ide/agent-gallery'
import { ArtifactFileList } from '@/components/ide/artifact-file-list'
import { ToolVisualization } from '@/components/ide/tool-visualization'
import { CommandPalette } from '@/components/ui/command-palette'
import {
  getReport,
  startRun,
  pauseRun,
  resumeRun,
  cancelRun,
  listRuns,
  isRunActive,
  type AppGenerationReport,
  type GeneratedFile,
} from '@/lib/app-generation-api'
import {
  ArrowLeft,
  PanelLeftClose,
  PanelLeftOpen,
  PanelRightClose,
  PanelRightOpen,
} from 'lucide-react'
import { cn } from '@/lib/utils'

const MonacoEditor = dynamic(
  () => import('@monaco-editor/react').then((m) => m.default),
  {
    ssr: false,
    loading: () => (
      <div className="flex h-full items-center justify-center text-sm text-muted-foreground">
        Загрузка редактора...
      </div>
    ),
  }
)

function getLanguageFromPath(path: string): string {
  const ext = path.split('.').pop()?.toLowerCase()
  const map: Record<string, string> = {
    cs: 'csharp', fs: 'fsharp', ts: 'typescript', tsx: 'typescript',
    js: 'javascript', jsx: 'javascript', py: 'python', rs: 'rust',
    go: 'go', json: 'json', xml: 'xml', yaml: 'yaml', yml: 'yaml',
    md: 'markdown', html: 'html', css: 'css', scss: 'scss',
    sql: 'sql', sh: 'shell', bash: 'shell', ps1: 'powershell',
    dockerfile: 'dockerfile', toml: 'ini', sln: 'plaintext',
    csproj: 'xml', fsproj: 'xml', props: 'xml',
  }
  return map[ext ?? ''] ?? 'plaintext'
}

export default function IdeProjectPage() {
  const params = useParams()
  const router = useRouter()
  const projectId = params.projectId as string

  const [report, setReport] = React.useState<AppGenerationReport | null>(null)
  const [currentRunId, setCurrentRunId] = React.useState<string>(projectId)
  const [openFiles, setOpenFiles] = React.useState<GeneratedFile[]>([])
  const [activeFile, setActiveFile] = React.useState<string>('')
  const [sidebarOpen, setSidebarOpen] = React.useState(true)
  const [chatOpen, setChatOpen] = React.useState(true)
  const [commandPaletteOpen, setCommandPaletteOpen] = React.useState(false)
  const pollRef = React.useRef<NodeJS.Timeout>()

  const isRunning = isRunActive(report?.status)

  React.useEffect(() => {
    loadReport(currentRunId)
    return () => {
      if (pollRef.current) clearInterval(pollRef.current)
    }
  }, [currentRunId])

  // Command palette keyboard shortcut
  React.useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault()
        setCommandPaletteOpen(true)
      }
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [])

  React.useEffect(() => {
    if (isRunning) {
      pollRef.current = setInterval(() => loadReport(currentRunId), 3000)
    } else {
      if (pollRef.current) clearInterval(pollRef.current)
    }
    return () => {
      if (pollRef.current) clearInterval(pollRef.current)
    }
  }, [isRunning, currentRunId])

  async function loadReport(runId: string) {
    try {
      const r = await getReport(runId)
      if (r) {
        setReport(r)
        if (r.files && r.files.length > 0 && openFiles.length === 0) {
          setOpenFiles([r.files[0]])
          setActiveFile(r.files[0].relativePath)
        }
      }
    } catch {
      // Run may not exist yet
    }
  }

  function handleFileSelect(file: GeneratedFile) {
    if (!openFiles.find((f) => f.relativePath === file.relativePath)) {
      setOpenFiles((prev) => [...prev, file])
    }
    setActiveFile(file.relativePath)
  }

  function handleTabClose(path: string) {
    setOpenFiles((prev) => prev.filter((f) => f.relativePath !== path))
    if (activeFile === path) {
      const remaining = openFiles.filter((f) => f.relativePath !== path)
      setActiveFile(remaining.length > 0 ? remaining[remaining.length - 1].relativePath : '')
    }
  }

  function getNewestRunId(runs: { id: string }[]) {
    return runs[0]?.id ?? null
  }

  async function handleStartGeneration(prompt: string, maxIterations: number) {
    await startRun({ userRequest: prompt, maxIterations })
    const runs = await listRuns()
    const newestRunId = getNewestRunId(runs)
    if (newestRunId) {
      setCurrentRunId(newestRunId)
      setOpenFiles([])
      setActiveFile('')
      if (newestRunId !== projectId) {
        router.replace(`/ide/${newestRunId}`)
      }
      await loadReport(newestRunId)
    }
  }

  async function handleFollowUp(prompt: string) {
    await startRun({
      userRequest: prompt,
      maxIterations: 20,
      resumeFromRunId: currentRunId,
    })
    const runs = await listRuns()
    const newestRunId = getNewestRunId(runs)
    if (newestRunId) {
      setCurrentRunId(newestRunId)
      setOpenFiles([])
      setActiveFile('')
      router.replace(`/ide/${newestRunId}`)
      await loadReport(newestRunId)
    }
  }

  async function handlePause() {
    await pauseRun(currentRunId)
    await loadReport(currentRunId)
  }
  async function handleResume() {
    await resumeRun(currentRunId)
    await loadReport(currentRunId)
  }
  async function handleCancel() {
    await cancelRun(currentRunId)
    await loadReport(currentRunId)
  }

  const currentFile = openFiles.find((f) => f.relativePath === activeFile)
  const files = report?.files ?? []

  return (
    <div className="flex h-screen flex-col bg-background">
      {/* Top bar */}
      <div className="flex h-10 items-center gap-2 border-b bg-muted/30 px-2">
        <Button variant="ghost" size="icon" className="h-7 w-7" asChild>
          <Link href="/ide">
            <ArrowLeft className="h-4 w-4" />
          </Link>
        </Button>
        <div className="h-5 w-px bg-border" />
        <Button
          variant="ghost"
          size="icon"
          className="h-7 w-7"
          onClick={() => setSidebarOpen(!sidebarOpen)}
        >
          {sidebarOpen ? (
            <PanelLeftClose className="h-4 w-4" />
          ) : (
            <PanelLeftOpen className="h-4 w-4" />
          )}
        </Button>
        <span className="text-sm font-medium truncate flex-1">
          {report?.plan?.applicationName ?? 'Libr4 IDE'}
        </span>
        <Button
          variant="ghost"
          size="icon"
          className="h-7 w-7"
          onClick={() => setChatOpen(!chatOpen)}
        >
          {chatOpen ? (
            <PanelRightClose className="h-4 w-4" />
          ) : (
            <PanelRightOpen className="h-4 w-4" />
          )}
        </Button>
      </div>

      {/* Main area */}
      <div className="flex flex-1 overflow-hidden">
        {/* File tree */}
        {sidebarOpen && (
          <div className="w-56 shrink-0 border-r flex flex-col">
            <div className="px-3 py-1.5 text-[10px] font-semibold uppercase tracking-wider text-muted-foreground border-b">
              Explorer
            </div>
            <FileTree
              files={files}
              selectedPath={activeFile}
              onSelect={handleFileSelect}
            />
          </div>
        )}

        {/* Editor + Output */}
        <div className="flex flex-1 flex-col overflow-hidden">
          <EditorTabs
            tabs={openFiles.map((f) => ({
              path: f.relativePath,
              name: f.relativePath.split('/').pop() ?? f.relativePath,
            }))}
            activeTab={activeFile}
            onSelect={setActiveFile}
            onClose={handleTabClose}
          />

          <div className="flex-1 overflow-hidden">
            {currentFile ? (
              <MonacoEditor
                height="100%"
                language={getLanguageFromPath(currentFile.relativePath)}
                value={currentFile.content}
                theme="vs-dark"
                options={{
                  readOnly: isRunning,
                  minimap: { enabled: false },
                  fontSize: 13,
                  lineNumbers: 'on',
                  scrollBeyondLastLine: false,
                  wordWrap: 'on',
                  padding: { top: 8 },
                }}
              />
            ) : (
              <div className="flex h-full items-center justify-center text-muted-foreground">
                <div className="text-center space-y-2">
                  <p className="text-sm">
                    {files.length === 0
                      ? 'Опишите приложение в чате справа'
                      : 'Выберите файл для просмотра'}
                  </p>
                </div>
              </div>
            )}
          </div>

          {/* Terminal panel */}
          <TerminalPanel 
            workspaceId={currentRunId}
            onCommandOutput={(tabId, output) => {
              // Можно логировать или передавать в чат
              console.log(`[${tabId}] Output:`, output)
            }}
          />
        </div>

        {/* Agent chat */}
        {chatOpen && (
          <div className="w-80 shrink-0 border-l">
            <AgentChat
              report={report}
              isRunning={isRunning}
              runId={currentRunId}
              onStartGeneration={handleStartGeneration}
              onFollowUp={handleFollowUp}
              onPause={handlePause}
              onResume={handleResume}
              onCancel={handleCancel}
            />
          </div>
        )}
      </div>

      <StatusBar report={report} activeFile={activeFile || null} />

      {/* Command Palette */}
      <CommandPalette 
        open={commandPaletteOpen} 
        onOpenChange={setCommandPaletteOpen}
        items={[
          { id: '1', label: 'Open File', icon: 'File', shortcut: 'Ctrl+P', action: () => {} },
          { id: '2', label: 'Search', icon: 'Search', shortcut: 'Ctrl+Shift+F', action: () => {} },
          { id: '3', label: 'Terminal', icon: 'Terminal', shortcut: 'Ctrl+`', action: () => {} },
          { id: '4', label: 'Settings', icon: 'Settings', shortcut: 'Ctrl+,', action: () => {} },
        ]}
      />
    </div>
  )
}
