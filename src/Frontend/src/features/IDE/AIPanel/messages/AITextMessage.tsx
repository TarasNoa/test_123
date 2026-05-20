import { Show, type Component } from 'solid-js';
import type { ChatMessage } from '../../IDEStore';

export const AITextMessage: Component<{ msg: ChatMessage }> = (props) => {
  const renderMarkdown = (text: string) => {
    const parts = text.split(/(```[\s\S]*?```|\*\*.*?\*\*|\*.*?\*|\n)/g);
    return parts.filter(Boolean).map((part, i) => {
      if (part.startsWith('```')) {
        const lines = part.slice(3, -3).split('\n');
        const lang = lines[0].trim();
        const code = lines.slice(1).join('\n');
        return (
          <div class="relative my-2 rounded-lg bg-surface border border-surface-3 overflow-hidden">
            <div class="flex items-center justify-between px-3 py-1 bg-surface-2 border-b border-surface-3">
              <span class="text-[10px] text-muted-foreground">{lang || 'text'}</span>
              <button
                onClick={() => navigator.clipboard.writeText(code)}
                class="text-[10px] text-muted-foreground hover:text-foreground px-1"
              >
                Copy
              </button>
            </div>
            <pre class="p-3 text-[11px] font-mono overflow-x-auto"><code>{code}</code></pre>
          </div>
        );
      }
      if (part.startsWith('**') && part.endsWith('**')) {
        return <strong class="text-foreground">{part.slice(2, -2)}</strong>;
      }
      if (part.startsWith('*') && part.endsWith('*') && !part.startsWith('**')) {
        return <em>{part.slice(1, -1)}</em>;
      }
      if (part === '\n') return <br />;
      return <span>{part}</span>;
    });
  };

  return (
    <div class="max-w-[95%] text-xs text-foreground leading-relaxed">
      {renderMarkdown(props.msg.text || '')}
      <Show when={props.msg.isStreaming}>
        <span class="inline-block w-2 h-4 bg-secondary ml-0.5 animate-pulse" />
      </Show>
    </div>
  );
};
