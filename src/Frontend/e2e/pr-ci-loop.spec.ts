import { expect, test } from '@playwright/test';

const runId = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee';
const prUrl = 'https://github.com/org/repo/pull/42';

type FleetRun = {
  runId: string;
  title: string;
  status: string;
  stage: string;
  agentCount: number;
  lastActivityAtUtc: string;
  pinned: boolean;
  archived: boolean;
  prUrl?: string;
  prNumber?: number;
  ciStatus?: string;
  ciLogsUrl?: string;
};

let fleetRun: FleetRun = {
  runId,
  title: 'PR CI Loop Run',
  status: 'WaitingForApproval',
  stage: 'review',
  agentCount: 1,
  lastActivityAtUtc: new Date().toISOString(),
  pinned: false,
  archived: false,
};

let reviewStatus = {
  runId,
  status: 'Pending',
  requireHumanReview: true,
  totalFiles: 1,
  decidedFiles: 0,
  approvedFiles: 0,
  rejectedFiles: 0,
  repairRequestedFiles: 0,
  files: [] as unknown[],
  pendingPaths: ['src/App.tsx'],
};

function fleetListBody() {
  return JSON.stringify([fleetRun]);
}

function summaryBody() {
  return JSON.stringify({
    entry: {
      ...fleetRun,
      startedAtUtc: new Date().toISOString(),
      costUsd: 0,
    },
    subagentCount: 0,
    delegationCount: 0,
    evidenceCount: 1,
  });
}

test.beforeEach(async ({ page }) => {
  fleetRun = {
    runId,
    title: 'PR CI Loop Run',
    status: 'WaitingForApproval',
    stage: 'review',
    agentCount: 1,
    lastActivityAtUtc: new Date().toISOString(),
    pinned: false,
    archived: false,
  };

  reviewStatus = {
    runId,
    status: 'Pending',
    requireHumanReview: true,
    totalFiles: 1,
    decidedFiles: 0,
    approvedFiles: 0,
    rejectedFiles: 0,
    repairRequestedFiles: 0,
    files: [],
    pendingPaths: ['src/App.tsx'],
  };

  await page.addInitScript(() => localStorage.setItem('accessToken', 'e2e-test-token'));

  await page.route('**/api/v1/ide/agent-fleet/events/stream**', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'text/event-stream',
      body: `data: ${JSON.stringify({ type: 'snapshot', items: [fleetRun] })}\n\n`,
    });
  });

  await page.route('**/api/v1/ide/agent-fleet', async (route) => {
    if (route.request().method() !== 'GET') {
      await route.continue();
      return;
    }
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: fleetListBody(),
    });
  });

  await page.route(`**/api/v1/ide/agent-fleet/${runId}/summary`, async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: summaryBody() });
  });

  await page.route(`**/api/v1/ide/agent-fleet/${runId}/pull-request`, async (route) => {
    fleetRun = {
      ...fleetRun,
      status: 'WaitingForCi',
      stage: 'ship',
      prUrl,
      prNumber: 42,
      ciStatus: 'pending',
      ciLogsUrl: 'https://github.com/org/repo/actions/runs/9001',
    };
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        success: true,
        skipped: false,
        summary: 'pr created',
        pullRequestNumber: 42,
        pullRequestUrl: prUrl,
        headBranch: `libr4/autogen-${runId.replace(/-/g, '')}`,
      }),
    });
  });

  await page.route(`**/api/v1/ide/app-generation/${runId}/generated-files`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        files: [{ relativePath: 'src/App.tsx', language: 'typescript', contentLength: 10, content: 'export {}' }],
      }),
    });
  });

  await page.route(`**/api/v1/ide/app-generation/${runId}/diffs**`, async (route) => {
    const url = route.request().url();
    if (url.includes('/detail')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ path: 'src/App.tsx', unifiedDiff: '---\n+++' }),
      });
      return;
    }
    if (url.includes('/evidence')) {
      await route.fulfill({ status: 200, body: JSON.stringify({ runId, paths: [] }) });
      return;
    }
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ runId, total: 0, items: [] }),
    });
  });

  await page.route(`**/api/v1/ide/app-generation/${runId}/review`, async (route) => {
    if (route.request().method() === 'GET') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(reviewStatus) });
      return;
    }
    reviewStatus = {
      ...reviewStatus,
      status: 'Approved',
      decidedFiles: 1,
      approvedFiles: 1,
      pendingPaths: [],
    };
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(reviewStatus) });
  });

  for (const suffix of ['permission-mode', 'rollout', 'usage', 'dashboard/build', 'verify/artifacts/console-errors.json']) {
    await page.route(`**/api/v1/ide/**/${runId}/${suffix}**`, async (route) => {
      await route.fulfill({ status: 404, body: '{}' });
    });
  }

  await page.route(`**/api/v1/ide/agent-fleet/${runId}/timeline`, async (route) => {
    await route.fulfill({ status: 200, body: JSON.stringify({ events: [] }) });
  });
});

test('full loop review approve create PR waiting CI then completed on board', async ({ page }) => {
  await page.goto(`/ide/runs/${runId}/review`, { waitUntil: 'load' });
  await expect(page.getByTestId('review-status')).toContainText('Pending', { timeout: 45_000 });

  await page.getByTestId('review-approve-all').click();
  await expect(page.getByTestId('review-status')).toContainText('Approved');
  await page.getByTestId('review-open-pr').click();

  await page.goto('/ide/agent-board', { waitUntil: 'load' });
  await expect(page.getByTestId('agent-board')).toBeVisible({ timeout: 45_000 });

  const card = page.getByTestId(`fleet-card-${runId}`);
  await expect(card).toBeVisible();
  await expect(page.getByTestId('fleet-column-WaitingForCi')).toContainText('PR CI Loop Run');

  await page.getByTestId(`fleet-ci-badge-${runId}`).click();
  await expect(page.getByTestId('ci-log-drawer')).toBeVisible();
  await expect(page.getByTestId('ci-drawer-pr-link')).toContainText('PR #42');

  fleetRun = {
    ...fleetRun,
    status: 'Completed',
    stage: 'ship',
    ciStatus: 'success',
  };

  await page.reload({ waitUntil: 'load' });
  await expect(page.getByTestId('fleet-column-Completed')).toContainText('PR CI Loop Run');
  await expect(page.getByTestId(`fleet-ci-badge-${runId}`)).toContainText('success');
});
