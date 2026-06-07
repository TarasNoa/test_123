import { config } from '../../../lib/config';

export interface InlineCompletionResponse {
  text: string | null;
  suppressed: boolean;
  suppressReason: string | null;
  latencyMs: number;
  modelUsed: string | null;
}

export async function fetchInlineCompletion(params: {
  filePath: string;
  language: string;
  fileContent: string;
  line: number;
  column: number;
  sessionIntent?: string;
  runId?: string;
  suppressWhileAgentRunning?: boolean;
  signal?: AbortSignal;
}): Promise<InlineCompletionResponse | null> {
  try {
    const token = localStorage.getItem('accessToken');
    const res = await fetch(`${config.apiBaseUrl}/api/v1/ide/app-generation/inline-complete`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: JSON.stringify({
        filePath: params.filePath,
        language: params.language,
        fileContent: params.fileContent,
        line: params.line,
        column: params.column,
        sessionIntent: params.sessionIntent,
        runId: params.runId,
        suppressWhileAgentRunning: params.suppressWhileAgentRunning ?? false,
      }),
      signal: params.signal,
    });
    if (!res.ok) return null;
    return (await res.json()) as InlineCompletionResponse;
  } catch {
    return null;
  }
}
