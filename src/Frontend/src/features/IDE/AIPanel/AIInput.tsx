import { createSignal, Show, type Component } from 'solid-js';
import { store, setStore, addMessage } from '../IDEStore';
import { config } from '../../../lib/config';

export const AIInput: Component = () => {
  const [rows, setRows] = createSignal(1);

  const send = async () => {
    const text = store.inputText.trim();
    if (!text) return;

    addMessage({
      type: 'user',
      id: crypto.randomUUID(),
      text,
      timestamp: new Date(),
      attachedFiles: store.contextFiles,
      selectedCode: store.contextSelectedCode
        ? { code: store.contextSelectedCode, language: 'typescript', lines: '' }
        : undefined,
    });

    setStore('inputText', '');
    setRows(1);

    try {
      const token = localStorage.getItem('accessToken');
      const res = await fetch(`${config.apiBaseUrl}/api/v1/ai/chat/message`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', ...(token ? { Authorization: `Bearer ${token}` } : {}) },
        body: JSON.stringify({
          sessionId: store.sessionId,
          message: text,
          autonomyLevel: store.autonomyLevel,
          provider: store.selectedModel,
          context: {
            currentFile: store.activeTabId,
            selectedCode: store.contextSelectedCode,
            attachedFiles: store.contextFiles,
            openTabs: store.openTabs.map((t) => t.path),
          },
        }),
      });
      if (!res.ok) {
        const err = await res.text();
        throw new Error(err || `HTTP ${res.status}`);
      }

      const data = await res.json();
      if (data.response) {
        addMessage({
          type: 'ai',
          id: data.messageId || crypto.randomUUID(),
          text: data.response,
          timestamp: new Date(),
          isStreaming: false,
        });
      }
    } catch (err: any) {
      const rawMessage: string = err?.message || 'Unknown error';
      const firstLine = rawMessage
        .split('\n')[0]
        .split(' at ')[0]
        .trim();

      let userMessage: string;
      if (rawMessage.includes('401') || rawMessage.includes('Unauthorized')) {
        userMessage = '⚠️ AI provider не настроен. Добавьте API ключ в docker-compose.yml.';
      } else if (rawMessage.includes('Failed to fetch') || rawMessage.includes('NetworkError')) {
        userMessage = '⚠️ Нет соединения с бэкендом. Проверьте что IDE API запущен.';
      } else if (rawMessage.includes('404')) {
        userMessage = '⚠️ Endpoint не найден. Проверьте маршруты в Gateway.';
      } else {
        userMessage = `⚠️ Ошибка: ${firstLine}`;
      }

      addMessage({
        type: 'ai',
        id: crypto.randomUUID(),
        text: userMessage,
        timestamp: new Date(),
        isStreaming: false,
      });
    }
  };

  const onKeyDown = (e: KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      send();
    }
    if (e.key === 'Escape') {
      setStore('inputText', '');
      setRows(1);
    }
  };

  const onInput = (value: string) => {
    setStore('inputText', value);
    const lineCount = value.split('\n').length;
    setRows(Math.min(Math.max(lineCount, 1), 5));
  };

  const isActive = () => Object.values(store.activeAgents).some((a) => a.status === 'running');

  return (
    <div class="shrink-0 px-3 py-2 border-t border-surface-3">
      <div class="relative">
        <textarea
          value={store.inputText}
          onInput={(e) => onInput(e.currentTarget.value)}
          onKeyDown={onKeyDown}
          rows={rows()}
          placeholder="Describe a task, ask a question, or @mention a file..."
          class="w-full resize-none bg-surface-2 border border-surface-3 rounded-xl px-3 py-2 pr-16 text-xs text-foreground outline-none focus:border-secondary/30 placeholder:text-muted-foreground/40 transition-colors"
        />
        <button
          onClick={isActive() ? () => {} : send}
          class={[
            'absolute right-2 bottom-2 p-1.5 rounded-lg transition-colors',
            isActive()
              ? 'bg-error/10 text-error hover:bg-error/20'
              : store.inputText.trim()
                ? 'bg-secondary/10 text-secondary hover:bg-secondary/20'
                : 'text-muted-foreground',
          ].join(' ')}
        >
          {isActive() ? (
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M5.25 7.5A2.25 2.25 0 017.5 5.25h9a2.25 2.25 0 012.25 2.25v9a2.25 2.25 0 01-2.25 2.25h-9a2.25 2.25 0 01-2.25-2.25v-9z" />
            </svg>
          ) : (
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M6 12L3.269 3.126A59.768 59.768 0 0121.485 12 59.77 59.77 0 013.27 20.876L5.999 12zm0 0h7.5" />
            </svg>
          )}
        </button>
      </div>
      <div class="flex items-center justify-between mt-1 px-1">
        <div class="flex gap-2">
          <button class="text-muted-foreground/50 hover:text-muted-foreground transition-colors">
            <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
              <path stroke-linecap="round" stroke-linejoin="round" d="M18.375 12.739l-7.693 7.693a4.5 4.5 0 01-6.364-6.364l10.94-10.94A3 3 0 1119.5 7.372L8.552 18.32m.009-.01l-.01.01m5.699-9.941l-7.81 7.81a1.5 1.5 0 002.122 2.122l7.81-7.81" />
            </svg>
          </button>
          <button class="text-muted-foreground/50 hover:text-muted-foreground transition-colors">
            <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
              <path stroke-linecap="round" stroke-linejoin="round" d="M6.827 6.175A2.31 2.31 0 015.186 7.23c-.38.054-.757.112-1.134.175C2.999 7.58 2.25 8.507 2.25 9.574V18a2.25 2.25 0 002.236 2.228M6.827 6.175A2.244 2.244 0 017.5 5.38V4.5a3 3 0 013-3h3a3 3 0 013 3v.88a2.244 2.244 0 01.673 1.795c0 .39-.1.768-.28 1.1m0 0a2.251 2.251 0 01-1.79 1.1M9.75 12l2.25 2.25m0 0l2.25-2.25M12 14.25V18" />
            </svg>
          </button>
        </div>
        <span class="text-[9px] text-muted-foreground/40">Enter to send, Shift+Enter for new line</span>
      </div>
    </div>
  );
};
