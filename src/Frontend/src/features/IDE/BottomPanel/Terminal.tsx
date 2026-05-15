import { onMount, onCleanup, type Component } from 'solid-js';
import { store } from '../IDEStore';
import { Terminal as XTerm } from '@xterm/xterm';
import { FitAddon } from '@xterm/addon-fit';
import '@xterm/xterm/css/xterm.css';

export const Terminal: Component = () => {
  let containerRef!: HTMLDivElement;
  let term: XTerm;
  let fitAddon: FitAddon;
  let ws: WebSocket;

  onMount(() => {
    term = new XTerm({
      fontFamily: "'JetBrains Mono', 'Fira Code', monospace",
      fontSize: 13,
      theme: {
        background: '#0F131A',
        foreground: '#F5F7FA',
        cursor: '#35E0D0',
        selectionBackground: 'rgba(53,224,208,0.15)',
        black: '#0F131A',
        brightBlack: '#1D2430',
        red: '#EF4444',
        brightRed: '#F87171',
        green: '#10B981',
        brightGreen: '#34D399',
        yellow: '#F59E0B',
        brightYellow: '#FBBF24',
        blue: '#3B82F6',
        brightBlue: '#60A5FA',
        magenta: '#A855F7',
        brightMagenta: '#C084FC',
        cyan: '#06B6D4',
        brightCyan: '#22D3EE',
        white: '#F5F7FA',
        brightWhite: '#FFFFFF',
      },
      cursorBlink: true,
      cursorStyle: 'block',
      allowProposedApi: true,
    });

    fitAddon = new FitAddon();
    term.loadAddon(fitAddon);
    term.open(containerRef);
    fitAddon.fit();

    term.writeln('\x1b[1;36m$ Welcome to Libr4 Terminal\x1b[0m');
    term.writeln('');

    const sessionId = store.sessionId || 'default';
    const token = localStorage.getItem('accessToken') || '';
    const baseWs = (import.meta.env.VITE_WS_BASE_URL as string) || 'ws://localhost:5000';
    const wsUrl = `${baseWs.replace(/\/$/, '')}/ws/terminal/${sessionId}?access_token=${token}`;

    try {
      ws = new WebSocket(wsUrl);

      ws.onopen = () => {
        term.writeln('\x1b[32m[Connected to terminal server]\x1b[0m');
      };

      ws.onmessage = (event) => {
        term.write(event.data);
      };

      ws.onclose = () => {
        term.writeln('\x1b[31m[Disconnected]\x1b[0m');
      };

      ws.onerror = () => {
        term.writeln('\x1b[31m[Connection error]\x1b[0m');
      };

      term.onData((data) => {
        if (ws.readyState === WebSocket.OPEN) {
          ws.send(data);
        }
      });
    } catch {
      term.writeln('\x1b[31mFailed to connect to terminal server\x1b[0m');
    }

    const onResize = () => {
      try { fitAddon.fit(); } catch {}
    };
    window.addEventListener('resize', onResize);

    onCleanup(() => {
      window.removeEventListener('resize', onResize);
      ws?.close();
      term?.dispose();
    });
  });

  return <div ref={(el) => { containerRef = el; }} class="w-full h-full" />;
};
