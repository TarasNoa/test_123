import { expect, test } from '@playwright/test';

const runId = 'dddddddd-dddd-dddd-dddd-dddddddddddd';

const mockFiles = {
  files: [
    { relativePath: 'src/App.tsx', language: 'typescript', contentLength: 120, content: 'export {}' },
    { relativePath: 'src/main.ts', language: 'typescript', contentLength: 80, content: 'import "./App"' },
  ],
};

let reviewStatus = {
  runId,
  status: 'Pending',
  requireHumanReview: true,
  totalFiles: 2,
  decidedFiles: 0,
  approvedFiles: 0,
  rejectedFiles: 0,
  repairRequestedFiles: 0,
  files: [] as unknown[],
  pendingPaths: ['src/App.tsx', 'src/main.ts'],
};

test.beforeEach(async ({ page }) => {
  reviewStatus = {
    runId,
    status: 'Pending',
    requireHumanReview: true,
    totalFiles: 2,
    decidedFiles: 0,
    approvedFiles: 0,
    rejectedFiles: 0,
    repairRequestedFiles: 0,
    files: [],
    pendingPaths: ['src/App.tsx', 'src/main.ts'],
  };

  await page.route(`**/api/v1/ide/agent-fleet/${runId}/summary`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        entry: {
          runId,
          title: 'Review E2E Run',
          status: 'Completed',
          stage: 'verify',
          agentCount: 1,
          lastActivityAtUtc: new Date().toISOString(),
          pinned: false,
          archived: false,
          startedAtUtc: new Date().toISOString(),
          costUsd: 0,
        },
        subagentCount: 0,
        delegationCount: 0,
        evidenceCount: 0,
      }),
    });
  });

  await page.route(`**/api/v1/ide/app-generation/${runId}/generated-files`, async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(mockFiles) });
  });

  await page.route(`**/api/v1/ide/app-generation/${runId}/diffs`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        runId,
        total: 1,
        items: [{
          path: 'src/App.tsx',
          language: 'typescript',
          changeKind: 'Modify',
          stepNumber: 2,
          toolName: 'write_file',
          hunkCount: 1,
          lastChangedUtc: new Date().toISOString(),
          provenanceId: 'rollout:1',
        }],
      }),
    });
  });

  await page.route(`**/api/v1/ide/app-generation/${runId}/diffs/evidence`, async (route) => {
    const url = new URL(route.request().url());
    if (url.searchParams.get('path')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          runId,
          path: url.searchParams.get('path'),
          correlatedStepNumber: 2,
          items: [],
          overlays: [],
        }),
      });
      return;
    }
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ runId, paths: [] }),
    });
  });

  await page.route(`**/api/v1/ide/app-generation/${runId}/diffs/detail**`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        path: 'src/App.tsx',
        language: 'typescript',
        changeKind: 'Modify',
        unifiedDiff: '--- a/App.tsx\n+++ b/App.tsx\n-export {}\n+export const App = () => null',
      }),
    });
  });

  await page.route(`**/api/v1/ide/app-generation/${runId}/verify/artifacts/console-errors.json`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([{ level: 'error', message: 'fail at src/App.tsx:1:1' }]),
    });
  });

  await page.route(`**/api/v1/ide/app-generation/${runId}/review`, async (route) => {
    if (route.request().method() === 'GET') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(reviewStatus) });
      return;
    }
    if (route.request().method() === 'POST') {
      const body = route.request().postDataJSON() as { decision: string; paths: string[] };
      reviewStatus = {
        ...reviewStatus,
        status: 'Approved',
        decidedFiles: body.paths.length,
        approvedFiles: body.paths.length,
        pendingPaths: [],
        files: body.paths.map((p) => ({
          path: p,
          decision: body.decision,
          notes: null,
          reviewerId: null,
          decidedAtUtc: new Date().toISOString(),
        })),
      };
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(reviewStatus) });
    }
  });

  for (const suffix of ['permission-mode', 'rollout', 'usage', 'dashboard/build']) {
    await page.route(`**/api/v1/ide/**/${runId}/${suffix}**`, async (route) => {
      await route.fulfill({ status: 404, body: '{}' });
    });
  }
  await page.route(`**/api/v1/ide/agent-fleet/${runId}/timeline`, async (route) => {
    await route.fulfill({ status: 200, body: JSON.stringify({ events: [] }) });
  });
});

test('review page batch approve updates status', async ({ page }) => {
  await page.goto(`/ide/runs/${runId}/review`, { waitUntil: 'load' });

  await expect(page.getByTestId('diff-panel')).toBeVisible({ timeout: 45_000 });
  await expect(page.getByTestId('review-status')).toContainText('Pending');

  await page.getByTestId('review-approve-all').click();

  await expect(page.getByTestId('review-status')).toContainText('Approved', { timeout: 10_000 });
  await expect(page.getByTestId('review-open-pr')).toBeVisible();
});

test('review page open PR after approve with existing pr url', async ({ page }) => {
  await page.route(`**/api/v1/ide/agent-fleet/${runId}/summary`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        entry: {
          runId,
          title: 'Review E2E Run',
          status: 'PrReady',
          stage: 'ship',
          agentCount: 1,
          lastActivityAtUtc: new Date().toISOString(),
          pinned: false,
          archived: false,
          prUrl: 'https://github.com/org/repo/pull/99',
          prNumber: 99,
        },
        subagentCount: 0,
        delegationCount: 0,
        evidenceCount: 0,
      }),
    });
  });

  reviewStatus = {
    ...reviewStatus,
    status: 'Approved',
    approvedFiles: 2,
    decidedFiles: 2,
    pendingPaths: [],
  };

  await page.goto(`/ide/runs/${runId}/review`, { waitUntil: 'load' });
  await expect(page.getByTestId('review-open-pr')).toContainText('Open PR #99');
});
