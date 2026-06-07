#!/usr/bin/env node
/**
 * Minimal mock ACP-compatible JSON-RPC stdio agent for Libr4 integration tests.
 */
import readline from 'node:readline';

const rl = readline.createInterface({ input: process.stdin });

rl.on('line', (line) => {
  if (!line.trim()) return;
  let msg;
  try {
    msg = JSON.parse(line);
  } catch {
    return;
  }

  if (msg.method === 'initialize') {
    process.stdout.write(JSON.stringify({ jsonrpc: '2.0', id: msg.id, result: { ok: true } }) + '\n');
    return;
  }

  if (msg.method === 'session/prompt') {
    process.stdout.write(
      JSON.stringify({ jsonrpc: '2.0', id: msg.id, result: { text: 'mock-acp-done' } }) + '\n',
    );
    process.stdout.write(
      JSON.stringify({
        jsonrpc: '2.0',
        method: 'notifications/message',
        params: { role: 'assistant', content: 'mock-acp-done' },
      }) + '\n',
    );
    return;
  }

  if (msg.id != null) {
    process.stdout.write(JSON.stringify({ jsonrpc: '2.0', id: msg.id, result: { ok: true } }) + '\n');
  }
});
