'use client'

import * as React from 'react'
import { cn } from '@/lib/utils'
import { ScrollArea } from '@/components/ui/scroll-area'
import {
  FileCode,
  FileJson,
  FileText,
  FolderClosed,
  FolderOpen,
  File,
  FileType,
} from 'lucide-react'

interface GeneratedFile {
  relativePath: string
  language?: string
  content: string
}

interface TreeNode {
  name: string
  path: string
  isDir: boolean
  children: TreeNode[]
  file?: GeneratedFile
}

function buildTree(files: GeneratedFile[]): TreeNode[] {
  const root: TreeNode[] = []

  for (const file of files) {
    const parts = file.relativePath.split(/[/\\]/)
    let current = root
    let pathSoFar = ''

    for (let i = 0; i < parts.length; i++) {
      pathSoFar += (i > 0 ? '/' : '') + parts[i]
      const isLast = i === parts.length - 1
      let existing = current.find((n) => n.name === parts[i])

      if (!existing) {
        existing = {
          name: parts[i],
          path: pathSoFar,
          isDir: !isLast,
          children: [],
          file: isLast ? file : undefined,
        }
        current.push(existing)
      }
      current = existing.children
    }
  }

  function sort(nodes: TreeNode[]): TreeNode[] {
    return nodes
      .sort((a, b) => {
        if (a.isDir && !b.isDir) return -1
        if (!a.isDir && b.isDir) return 1
        return a.name.localeCompare(b.name)
      })
      .map((n) => ({ ...n, children: sort(n.children) }))
  }

  return sort(root)
}

function getFileIcon(name: string) {
  const ext = name.split('.').pop()?.toLowerCase()
  switch (ext) {
    case 'cs': case 'ts': case 'tsx': case 'js': case 'jsx': case 'py': case 'rs': case 'go':
      return <FileCode className="h-4 w-4 text-secondary" />
    case 'json': case 'yaml': case 'yml': case 'toml':
      return <FileJson className="h-4 w-4 text-primary" />
    case 'md': case 'txt': case 'log':
      return <FileText className="h-4 w-4 text-muted-foreground" />
    case 'csproj': case 'sln': case 'fsproj':
      return <FileType className="h-4 w-4 text-primary" />
    default:
      return <File className="h-4 w-4 text-muted-foreground" />
  }
}

function TreeItem({
  node,
  depth,
  selectedPath,
  onSelect,
}: {
  node: TreeNode
  depth: number
  selectedPath: string
  onSelect: (file: GeneratedFile) => void
}) {
  const [open, setOpen] = React.useState(true)
  const isSelected = node.path === selectedPath

  if (node.isDir) {
    return (
      <div>
        <button
          onClick={() => setOpen(!open)}
          className="flex w-full items-center gap-1.5 px-2 py-1 text-xs hover:bg-accent/50 transition-colors rounded-sm"
          style={{ paddingLeft: `${depth * 12 + 8}px` }}
        >
          {open ? <FolderOpen className="h-4 w-4 text-primary" /> : <FolderClosed className="h-4 w-4 text-primary" />}
          <span className="truncate">{node.name}</span>
        </button>
        {open && node.children.map((child) => (
          <TreeItem key={child.path} node={child} depth={depth + 1} selectedPath={selectedPath} onSelect={onSelect} />
        ))}
      </div>
    )
  }

  return (
    <button
      onClick={() => node.file && onSelect(node.file)}
      className={cn(
        'flex w-full items-center gap-1.5 px-2 py-1 text-xs transition-colors rounded-sm',
        isSelected ? 'bg-accent text-accent-foreground' : 'hover:bg-accent/50'
      )}
      style={{ paddingLeft: `${depth * 12 + 8}px` }}
    >
      {getFileIcon(node.name)}
      <span className="truncate">{node.name}</span>
    </button>
  )
}

interface FileTreeProps {
  files: GeneratedFile[]
  selectedPath: string
  onSelect: (file: GeneratedFile) => void
}

export function FileTree({ files, selectedPath, onSelect }: FileTreeProps) {
  const tree = React.useMemo(() => buildTree(files), [files])

  return (
    <ScrollArea className="h-full">
      <div className="py-1">
        {tree.length === 0 ? (
          <p className="px-3 py-4 text-xs text-muted-foreground text-center">
            Файлы появятся после генерации
          </p>
        ) : (
          tree.map((node) => (
            <TreeItem key={node.path} node={node} depth={0} selectedPath={selectedPath} onSelect={onSelect} />
          ))
        )}
      </div>
    </ScrollArea>
  )
}
