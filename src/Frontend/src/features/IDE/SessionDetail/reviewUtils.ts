import type { DiffPathOverlay } from '../services/runSession';

export const langMap: Record<string, string> = {
  typescript: 'typescript',
  javascript: 'javascript',
  json: 'json',
  css: 'css',
  html: 'html',
  markdown: 'markdown',
  python: 'python',
  rust: 'rust',
  csharp: 'csharp',
  xml: 'xml',
  yaml: 'yaml',
  text: 'plaintext',
};

export type ConsoleErrorEntry = {
  level: string;
  message: string;
  paths: string[];
};

export function overlayFor(overlays: DiffPathOverlay[] | undefined, filePath: string): DiffPathOverlay | null {
  if (!overlays?.length) return null;
  return overlays.find((o) =>
    pathMatches(o.path, filePath)) ?? null;
}

export function pathMatches(candidate: string, target: string): boolean {
  const a = normalizePath(candidate);
  const b = normalizePath(target);
  return a === b
    || a.endsWith(`/${b}`)
    || b.endsWith(`/${a}`)
    || a.split('/').pop() === b.split('/').pop();
}

export function normalizePath(path: string): string {
  return path.replace(/\\/g, '/').replace(/^\/+/, '');
}

const stackPathRe = /(?:[\w.-]+\/)+[\w.-]+\.(?:tsx?|jsx?|vue|py|cs|java|go|rs|php|rb|swift|kt|css|scss|html|json|yaml|yml|md)(?::\d+(?::\d+)?)?/gi;

export function extractPathsFromText(text: string): string[] {
  const found = new Set<string>();
  for (const match of text.matchAll(stackPathRe)) {
    const raw = match[0].split(':')[0];
    if (raw) found.add(normalizePath(raw));
  }
  return [...found];
}

export function parseConsoleErrors(json: unknown): ConsoleErrorEntry[] {
  const items = Array.isArray(json) ? json : json && typeof json === 'object' ? [json] : [];
  return items.map((item) => {
    const rec = item as Record<string, unknown>;
    const message = String(rec.message ?? rec.text ?? rec.msg ?? JSON.stringify(item));
    const level = String(rec.level ?? rec.type ?? 'error');
    const file = rec.file ? normalizePath(String(rec.file)) : null;
    const paths = [
      ...(file ? [file] : []),
      ...extractPathsFromText(message),
      ...(rec.stack ? extractPathsFromText(String(rec.stack)) : []),
    ];
    return { level, message, paths: [...new Set(paths)] };
  });
}

export function parseUnifiedDiff(patch: string | null | undefined, fallbackCurrent: string): {
  original: string;
  modified: string;
} {
  if (!patch?.trim()) return { original: '', modified: fallbackCurrent };

  const before: string[] = [];
  const after: string[] = [];
  for (const line of patch.split('\n')) {
    if (line.startsWith('---') || line.startsWith('+++') || line.startsWith('@@')) continue;
    if (line.startsWith('-')) before.push(line.slice(1));
    else if (line.startsWith('+')) after.push(line.slice(1));
    else if (line.startsWith(' ')) {
      before.push(line.slice(1));
      after.push(line.slice(1));
    }
  }

  if (before.length === 0 && after.length === 0) {
    return { original: '', modified: fallbackCurrent };
  }

  return {
    original: before.join('\n'),
    modified: after.length > 0 ? after.join('\n') : fallbackCurrent,
  };
}

export type FilmstripItem = {
  id: string;
  kind: string;
  fileName: string;
  url: string;
  thumbnailUrl?: string | null;
  stepNumber?: number | null;
  lastModifiedUtc: string;
  source: string;
};

export function toFilmstripItems(items: DiffEvidenceItem[]): FilmstripItem[] {
  return items
    .filter((i) =>
      i.kind.toLowerCase().includes('screenshot')
      || i.fileName.endsWith('.png')
      || i.fileName.endsWith('.jpg')
      || i.fileName.endsWith('.webm')
      || i.kind.toLowerCase().includes('video'))
    .map((i) => ({
      id: `${i.source}:${i.fileName}`,
      kind: i.kind,
      fileName: i.fileName,
      url: mediaUrl(i.downloadUrl),
      thumbnailUrl: i.thumbnailUrl ? mediaUrl(i.thumbnailUrl) : null,
      stepNumber: i.stepNumber,
      lastModifiedUtc: i.lastModifiedUtc,
      source: i.source,
    }))
    .sort((a, b) => a.lastModifiedUtc.localeCompare(b.lastModifiedUtc));
}

export function mediaUrl(url: string): string {
  return url.startsWith('http') ? url : `${window.location.origin}${url}`;
}
