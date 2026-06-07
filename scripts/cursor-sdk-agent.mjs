#!/usr/bin/env node
/**
 * Cursor SDK runner for Libr4 AgentBackend adapter.
 * Emits NDJSON lines: status | message | cost | error
 * Requires: npm i @cursor/sdk (or npx) and CURSOR_API_KEY env var.
 */
import { parseArgs } from 'node:util';
import { existsSync } from 'node:fs';
import { resolve } from 'node:path';

const { values } = parseArgs({
  options: {
    cwd: { type: 'string', default: process.cwd() },
    model: { type: 'string', default: 'composer-2.5' },
    prompt: { type: 'string' },
    'api-key-env': { type: 'string', default: 'CURSOR_API_KEY' },
  },
  allowPositionals: true,
});

function emit(type, payload = {}) {
  console.log(JSON.stringify({ type, timestampUtc: new Date().toISOString(), ...payload }));
}

async function main() {
  const prompt = values.prompt ?? '';
  if (!prompt.trim()) {
    emit('error', { error: 'prompt_required' });
    process.exit(1);
  }

  const cwd = resolve(values.cwd ?? process.cwd());
  if (!existsSync(cwd)) {
    emit('error', { error: 'cwd_not_found', cwd });
    process.exit(1);
  }

  const apiKeyEnv = values['api-key-env'] ?? 'CURSOR_API_KEY';
  const apiKey = process.env[apiKeyEnv];
  if (!apiKey) {
    emit('error', { error: 'missing_api_key', env: apiKeyEnv });
    process.exit(2);
  }

  emit('status', { stage: 'spawned' });

  try {
    const { Agent } = await import('@cursor/sdk');
    emit('status', { stage: 'running' });

    const result = await Agent.prompt(prompt, {
      apiKey,
      model: { id: values.model },
      local: { cwd },
    });

    const text = result?.result ?? result?.status ?? 'completed';
    emit('message', { role: 'assistant', content: String(text) });

    const usage = result?.usage;
    if (usage) {
      emit('cost', {
        tokens: usage.totalTokens ?? usage.inputTokens ?? 0,
        costUsd: usage.costUsd ?? 0,
      });
    }

    emit('status', { stage: 'completed' });
    process.exit(0);
  } catch (err) {
    emit('error', { error: err?.message ?? String(err) });
    process.exit(1);
  }
}

main();
