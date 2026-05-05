'use client'
import { useMemo, useState } from 'react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { FileIcon, FolderIcon } from 'lucide-react'
import type { GeneratedFile } from '@/lib/app-generation-api'

interface TreeNode {
  name: string
  path: string
  isFile: boolean
  children?: TreeNode[]
  file?: GeneratedFile
}

function buildTree(files: GeneratedFile[]): TreeNode {
  const root: TreeNode = { name: '', path: '', isFile: false, children: [] }
  for (const f of files) {
    const parts = f.relativePath.replace(/\\/g, '/').split('/')
    let cursor = root
    for (let i = 0; i < parts.length; i++) {
      const isLast = i === parts.length - 1
      const part = parts[i]
      cursor.children ??= []
      let next = cursor.children.find((c) => c.name === part)
      if (!next) {
        next = {
          name: part,
          path: parts.slice(0, i + 1).join('/'),
          isFile: isLast,
          children: isLast ? undefined : [],
          file: isLast ? f : undefined,
        }
        cursor.children.push(next)
      }
      cursor = next
    }
  }
  // Sort: folders first, then files, alphabetically.
  const sortRec = (n: TreeNode) => {
    if (!n.children) return
    n.children.sort((a, b) => {
      if (a.isFile !== b.isFile) return a.isFile ? 1 : -1
      return a.name.localeCompare(b.name)
    })
    n.children.forEach(sortRec)
  }
  sortRec(root)
  return root
}

function TreeView({
  node,
  depth = 0,
  selected,
  onSelect,
}: {
  node: TreeNode
  depth?: number
  selected?: string
  onSelect: (file: GeneratedFile) => void
}) {
  if (!node.children) return null
  return (
    <ul className="space-y-0.5">
      {node.children.map((child) => (
        <TreeNodeView
          key={child.path}
          node={child}
          depth={depth}
          selected={selected}
          onSelect={onSelect}
        />
      ))}
    </ul>
  )
}

function TreeNodeView({
  node,
  depth,
  selected,
  onSelect,
}: {
  node: TreeNode
  depth: number
  selected?: string
  onSelect: (file: GeneratedFile) => void
}) {
  const [open, setOpen] = useState(depth < 1)
  const indent = { paddingLeft: `${depth * 12}px` }
  if (node.isFile) {
    const isActive = selected === node.path
    return (
      <li>
        <button
          type="button"
          onClick={() => node.file && onSelect(node.file)}
          className={`flex w-full items-center gap-1 rounded px-1.5 py-0.5 text-left text-sm hover:bg-accent ${
            isActive ? 'bg-accent font-medium' : ''
          }`}
          style={indent}
        >
          <FileIcon className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
          <span className="truncate">{node.name}</span>
        </button>
      </li>
    )
  }
  return (
    <li>
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className="flex w-full items-center gap-1 rounded px-1.5 py-0.5 text-left text-sm hover:bg-accent"
        style={indent}
      >
        <FolderIcon className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
        <span className="truncate">{node.name}</span>
      </button>
      {open && node.children && (
        <ul className="space-y-0.5">
          {node.children.map((child) => (
            <TreeNodeView
              key={child.path}
              node={child}
              depth={depth + 1}
              selected={selected}
              onSelect={onSelect}
            />
          ))}
        </ul>
      )}
    </li>
  )
}

export function FileTree({ files }: { files: GeneratedFile[] }) {
  const [selected, setSelected] = useState<GeneratedFile | null>(null)
  const tree = useMemo(() => buildTree(files), [files])

  if (!files || files.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Файлы</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground">Файлов ещё нет.</p>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Файлы ({files.length})</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="grid gap-4 md:grid-cols-[260px_1fr]">
          <div className="max-h-[520px] overflow-auto rounded border bg-muted/30 p-2">
            <TreeView
              node={tree}
              selected={selected?.relativePath}
              onSelect={setSelected}
            />
          </div>
          <div className="min-h-[260px] overflow-auto rounded border bg-background">
            {selected ? (
              <>
                <div className="flex items-center justify-between border-b bg-muted/40 px-3 py-2">
                  <code className="text-xs">{selected.relativePath}</code>
                  {selected.language && (
                    <span className="text-xs text-muted-foreground">{selected.language}</span>
                  )}
                </div>
                <pre className="max-h-[460px] overflow-auto p-3 text-xs leading-relaxed">
                  <code>{selected.content}</code>
                </pre>
              </>
            ) : (
              <div className="p-6 text-sm text-muted-foreground">
                Выберите файл слева, чтобы посмотреть содержимое.
              </div>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  )
}
