import { onMount, onCleanup, type Component } from 'solid-js';
import { store } from '../IDEStore';
import { Terminal as XTerm } from '@xterm/xterm';
import { FitAddon } from '@xterm/addon-fit';

export const Terminal: Component = () => {
  let containerRef!: HTMLDivElement;
  let term: XTerm;
  let fitAddon: FitAddon;
  let ws: WebSocket | null = null;

  onMount(() => {
    term = new XTerm({
      fontFamily: "'JetBrains Mono', 'Fira Code', monospace",
      fontSize: 13,
      theme: {
        background: '#0F131A',
        foreground: '#F5F7FA',
        cursor: '#9B7CFF',
        cursorAccent: '#0F131A',
        selectionBackground: 'rgba(155,124,255,0.2)',
        black: '#0F131A', brightBlack: '#3D4A5C',
        red: '#EF4444', brightRed: '#F87171',
        green: '#10B981', brightGreen: '#34D399',
        yellow: '#F59E0B', brightYellow: '#FBBF24',
        blue: '#3B82F6', brightBlue: '#60A5FA',
        magenta: '#A855F7', brightMagenta: '#C084FC',
        cyan: '#06B6D4', brightCyan: '#22D3EE',
        white: '#F5F7FA', brightWhite: '#FFFFFF',
      },
      cursorBlink: true,
      cursorStyle: 'block',
      allowProposedApi: true,
      disableStdin: false,
      scrollback: 1000,
    });

    fitAddon = new FitAddon();
    term.loadAddon(fitAddon);
    term.open(containerRef);

    setTimeout(() => {
      fitAddon.fit();
      term.focus();
    }, 50);

    term.writeln('\x1b[1;35mWelcome to Libr4 Terminal\x1b[0m');
    term.writeln('');

    const sessionId = store.sessionId || 'default';
    const token = localStorage.getItem('accessToken') || '';
    const baseWs = (import.meta.env.VITE_WS_BASE_URL as string) || 'ws://localhost:5000';
    const wsUrl = `${baseWs.replace(/\/$/, '')}/ws/terminal/${sessionId}?access_token=${encodeURIComponent(token)}`;

    const connect = () => {
      try {
        ws = new WebSocket(wsUrl);

        ws.onopen = () => {
          term.writeln('\x1b[32m[Connected]\x1b[0m');
          term.writeln('');
          const dims = fitAddon.proposeDimensions();
          if (dims && ws?.readyState === WebSocket.OPEN) {
            ws.send(JSON.stringify({ type: 'resize', cols: dims.cols, rows: dims.rows }));
          }
        };

        ws.onmessage = (event) => {
          try {
            const parsed = JSON.parse(event.data);
            if (parsed.type === 'output') {
              term.write(parsed.data);
            } else {
              term.write(event.data);
            }
          } catch {
            term.write(event.data);
          }
        };

        ws.onclose = (e) => {
          term.writeln(`\x1b[33m[Disconnected: ${e.code}]\x1b[0m`);
          setTimeout(() => {
            if (term) {
              term.writeln('\x1b[33m[Reconnecting...]\x1b[0m');
              connect();
            }
          }, 3000);
        };

        ws.onerror = () => {
          term.writeln('\x1b[31m[WebSocket error — is IDE API running?]\x1b[0m');
        };
      } catch (err) {
        term.writeln('\x1b[31m[Failed to connect]\x1b[0m');
      }
    };

    connect();

    term.onData((data) => {
      if (ws?.readyState === WebSocket.OPEN) {
        ws.send(JSON.stringify({ type: 'input', data }));
      } else {
        term.write(data);
      }
    });

    const onResize = () => {
      try {
        fitAddon.fit();
        const dims = fitAddon.proposeDimensions();
        if (dims && ws?.readyState === WebSocket.OPEN) {
          ws.send(JSON.stringify({ type: 'resize', cols: dims.cols, rows: dims.rows }));
        }
      } catch {}
    };
    window.addEventListener('resize', onResize);

    onCleanup(() => {
      window.removeEventListener('resize', onResize);
      ws?.close();
      term?.dispose();
    });
  });

  return (
    <div
      ref={(el) => { containerRef = el; }}
      class="w-full h-full"
      onClick={() => term?.focus()}
      style={{ cursor: 'text' }}
    />
  );
};
