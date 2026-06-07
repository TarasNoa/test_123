import { createEffect, onCleanup, type Component } from 'solid-js';
import { langMap, parseUnifiedDiff } from './reviewUtils';

// @ts-ignore
import EditorWorker from 'monaco-editor/esm/vs/editor/editor.worker?worker';
// @ts-ignore
import TsWorker from 'monaco-editor/esm/vs/language/typescript/ts.worker?worker';

if (typeof window !== 'undefined' && !(window as any).MonacoEnvironment) {
  (window as any).MonacoEnvironment = {
    getWorker(_moduleId: string, label: string) {
      if (label === 'typescript' || label === 'javascript') return new TsWorker();
      return new EditorWorker();
    },
  };
}

export const ReviewDiffViewer: Component<{
  path: string;
  language: string;
  content: string;
  unifiedDiff?: string | null;
}> = (props) => {
  let containerRef!: HTMLDivElement;
  let diffEditor: any;
  let monacoInstance: any;
  let originalModel: any;
  let modifiedModel: any;

  const disposeModels = () => {
    originalModel?.dispose();
    modifiedModel?.dispose();
    originalModel = null;
    modifiedModel = null;
  };

  createEffect(() => {
    const path = props.path;
    const language = props.language;
    const content = props.content;
    const unifiedDiff = props.unifiedDiff;

    if (!containerRef) return;

    import('monaco-editor').then((monaco) => {
      monacoInstance = monaco;
      if (!monacoInstance.editor.getEditors?.()?.length) {
        monaco.editor.defineTheme('libr4-review-dark', {
          base: 'vs-dark',
          inherit: true,
          rules: [],
          colors: {
            'editor.background': '#0F131A',
            'editor.foreground': '#F5F7FA',
          },
        });
      }

      if (!diffEditor) {
        diffEditor = monacoInstance.editor.createDiffEditor(containerRef, {
          theme: 'libr4-review-dark',
          fontSize: 11,
          fontFamily: "'JetBrains Mono', ui-monospace, monospace",
          automaticLayout: true,
          readOnly: true,
          renderSideBySide: true,
          minimap: { enabled: false },
          scrollBeyondLastLine: false,
        });
      }

      disposeModels();
      const parsed = parseUnifiedDiff(unifiedDiff, content);
      const lang = langMap[language] ?? 'plaintext';
      originalModel = monacoInstance.editor.createModel(parsed.original, lang);
      modifiedModel = monacoInstance.editor.createModel(parsed.modified || content, lang);
      diffEditor.setModel({ original: originalModel, modified: modifiedModel });
    }).catch(console.error);

    void path;
  });

  onCleanup(() => {
    disposeModels();
    diffEditor?.dispose();
  });

  return <div ref={containerRef} class="h-full min-h-[240px] w-full" />;
};

export default ReviewDiffViewer;
