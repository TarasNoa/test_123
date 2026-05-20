import { onCleanup, createEffect, Show, type Component } from 'solid-js';
import { store, setStore, setTabDirty, markFileDirty } from '../IDEStore';

// @ts-ignore
import EditorWorker from 'monaco-editor/esm/vs/editor/editor.worker?worker';
// @ts-ignore
import JsonWorker from 'monaco-editor/esm/vs/language/json/json.worker?worker';
// @ts-ignore
import CssWorker from 'monaco-editor/esm/vs/language/css/css.worker?worker';
// @ts-ignore
import HtmlWorker from 'monaco-editor/esm/vs/language/html/html.worker?worker';
// @ts-ignore
import TsWorker from 'monaco-editor/esm/vs/language/typescript/ts.worker?worker';

if (typeof window !== 'undefined') {
  (window as any).MonacoEnvironment = {
    getWorker(_moduleId: string, label: string) {
      switch (label) {
        case 'json': return new JsonWorker();
        case 'css': return new CssWorker();
        case 'html': return new HtmlWorker();
        case 'typescript':
        case 'javascript': return new TsWorker();
        default: return new EditorWorker();
      }
    },
  };
}

const langMap: Record<string, string> = {
  typescript: 'typescript',
  javascript: 'javascript',
  json: 'json',
  css: 'css',
  html: 'html',
  markdown: 'markdown',
  python: 'python',
  rust: 'rust',
  csharp: 'csharp',
  fsharp: 'fsharp',
  text: 'plaintext',
};

const defaultContent: Record<string, string> = {
  'package.json': '{\n  "name": "libr4-frontend",\n  "version": "1.0.0",\n  "type": "module"\n}',
  'tsconfig.json': '{\n  "compilerOptions": {\n    "target": "ESNext",\n    "module": "ESNext"\n  }\n}',
};

export const MonacoEditor: Component = () => {
  let containerRef!: HTMLDivElement;
  let editor: any;
  let diffEditor: any;
  let saveTimeout: ReturnType<typeof setTimeout>;
  let monacoInstance: any;
  let currentModel: any;
  let isInit = false;

  const activeTab = () => store.openTabs.find((t) => t.id === store.activeTabId);
  const isDiffMode = () => store.diffTabId === store.activeTabId && !!activeTab()?.originalContent && !!activeTab()?.proposedContent;

  const initEditor = () => {
    if (isInit || !containerRef) return;
    isInit = true;

    import('monaco-editor')
      .then((monaco) => {
        monacoInstance = monaco;

        monaco.editor.defineTheme('libr4-dark', {
          base: 'vs-dark',
          inherit: true,
          rules: [],
          colors: {
            'editor.background': '#0F131A',
            'editor.foreground': '#F5F7FA',
            'editorLineNumber.foreground': '#98A2B6',
            'editor.selectionBackground': '#2E1E3A',
            'editor.lineHighlightBackground': '#1A1F2A',
            'editorCursor.foreground': '#9B7CFF',
            'editorIndentGuide.background': '#1D2430',
          },
        });

        syncEditor();
      })
      .catch((err) => {
        console.error('Monaco load failed:', err);
      });
  };

  const createNormalEditor = () => {
    if (editor) return;
    if (diffEditor) { diffEditor.dispose(); diffEditor = null; }
    editor = monacoInstance.editor.create(containerRef, {
      theme: 'libr4-dark',
      fontSize: 13,
      fontFamily: "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
      fontLigatures: true,
      lineNumbers: 'on',
      minimap: { enabled: true, side: 'right', scale: 1 },
      wordWrap: 'off',
      scrollBeyondLastLine: false,
      smoothScrolling: true,
      cursorBlinking: 'smooth',
      cursorSmoothCaretAnimation: 'on',
      bracketPairColorization: { enabled: true },
      guides: { indentation: true, bracketPairs: true },
      padding: { top: 8 },
      renderWhitespace: 'selection',
      suggest: { showIcons: true },
      automaticLayout: true,
      readOnly: false,
    });

    editor.onDidChangeCursorPosition((e: any) => {
      setStore('cursorPosition', { line: e.position.lineNumber, column: e.position.column });
    });

    editor.onDidChangeModelContent(() => {
      const tab = activeTab();
      if (!tab) return;
      setTabDirty(tab.id, true);
      clearTimeout(saveTimeout);
      saveTimeout = setTimeout(() => saveFile(tab.path, editor.getValue()), 1000);
    });
  };

  const createDiffEditor = () => {
    if (diffEditor) return;
    if (editor) { editor.dispose(); editor = null; }
    diffEditor = monacoInstance.editor.createDiffEditor(containerRef, {
      theme: 'libr4-dark',
      fontSize: 13,
      fontFamily: "'JetBrains Mono', 'Fira Code', ui-monospace, monospace",
      automaticLayout: true,
      readOnly: true,
      renderSideBySide: false,
    });
  };

  const syncEditor = () => {
    if (!monacoInstance) return;
    const tab = activeTab();
    if (!tab) return;

    if (isDiffMode()) {
      createDiffEditor();
      const original = monacoInstance.editor.createModel(tab.originalContent || '', langMap[tab.language] || 'plaintext');
      const modified = monacoInstance.editor.createModel(tab.proposedContent || '', langMap[tab.language] || 'plaintext');
      diffEditor.setModel({ original, modified });
    } else {
      createNormalEditor();
      if (currentModel) currentModel.dispose();
      const content = tab.content || defaultContent[tab.name] || '';
      currentModel = monacoInstance.editor.createModel(content, langMap[tab.language] || 'plaintext');
      editor.setModel(currentModel);
      editor.updateOptions({ readOnly: tab.isAgentEditing });
    }
  };

  createEffect(() => {
    const tabId = store.activeTabId;
    if (tabId) {
      if (!isInit) initEditor();
      else syncEditor();
    }
  });

  onCleanup(() => {
    currentModel?.dispose();
    editor?.dispose();
    diffEditor?.dispose();
    clearTimeout(saveTimeout);
  });

  const saveFile = async (path: string, content: string) => {
    const foundTab = store.openTabs.find((t) => t.path === path);
    if (!foundTab || !store.sessionId) return;
    try {
      const token = localStorage.getItem('accessToken');
      const res = await fetch(`${import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000'}/api/v1/ide/files/save`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
        body: JSON.stringify({ path, content, sessionId: store.sessionId }),
      });
      if (res.ok) {
        setTabDirty(foundTab.id, false);
        markFileDirty(path, false);
      }
    } catch {
      // silently fail
    }
  };

  return (
    <div class="flex-1 relative w-full h-full">
      <div
        ref={(el) => { containerRef = el; }}
        class="absolute inset-0"
        style={{ display: store.activeTabId ? 'block' : 'none' }}
      />
      <Show when={!store.activeTabId}>
        <div class="absolute inset-0 flex flex-col items-center justify-center text-muted-foreground">
          <svg class="w-8 h-8 mb-3 opacity-30" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
            <path stroke-linecap="round" stroke-linejoin="round" d="M17.25 6.75L22.5 12l-5.25 5.25m-10.5 0L1.5 12l5.25-5.25m7.5-3l-4.5 16.5" />
          </svg>
          <p class="text-sm">Select a file to start editing</p>
          <p class="text-xs mt-1 opacity-50">Or ask the AI to generate code for you</p>
        </div>
      </Show>
    </div>
  );
};
