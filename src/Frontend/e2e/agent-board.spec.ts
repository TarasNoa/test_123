import { expect, test } from '@playwright/test';

const mockRuns = [
  {
    runId: '11111111-1111-1111-1111-111111111111',
    title: 'Calorie Vision',
    status: 'Planning',
    stage: 'plan',
    agentCount: 1,
    lastActivityAtUtc: new Date().toISOString(),
    pinned: false,
    archived: false,
  },
  {
    runId: '22222222-2222-2222-2222-222222222222',
    title: 'Banking API',
    status: 'Verifying',
    stage: 'verify',
    agentCount: 2,
    lastActivityAtUtc: new Date().toISOString(),
    pinned: false,
    archived: false,
  },
  {
    runId: '33333333-3333-3333-3333-333333333333',
    title: 'Shop Frontend',
    status: 'WaitingForCi',
    stage: 'ship',
    agentCount: 1,
    lastActivityAtUtc: new Date().toISOString(),
    pinned: false,
    archived: false,
    prUrl: 'https://github.com/org/repo/pull/7',
    prNumber: 7,
    ciStatus: 'pending',
    ciLogsUrl: 'https://github.com/org/repo/actions/runs/123',
  },
];

test.beforeEach(async ({ page }) => {
  await page.route('**/api/v1/ide/agent-fleet/**', async (route) => {
    const url = route.request().url();
    if (url.includes('/events/stream')) {
      await route.fulfill({
        status: 200,
        contentType: 'text/event-stream',
        body: `data: ${JSON.stringify({ type: 'snapshot', items: mockRuns })}\n\n`,
      });
      return;
    }
    await route.continue();
  });

  await page.route('**/api/v1/ide/agent-fleet', async (route) => {
    if (route.request().method() !== 'GET') {
      await route.continue();
      return;
    }
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(mockRuns),
    });
  });
});

test('board renders 3 runs in different columns', async ({ page }) => {
  await page.addInitScript(() => localStorage.setItem('accessToken', 'e2e-test-token'));
  await page.goto('/ide/agent-board', { waitUntil: 'load' });

  await expect(page.getByTestId('agent-board')).toBeVisible({ timeout: 45_000 });
  await expect(page.getByTestId('agent-board-title')).toHaveText('Agent Board');

  const planningCol = page.getByTestId('fleet-column-Planning');
  const verifyingCol = page.getByTestId('fleet-column-Verifying');
  const waitingCiCol = page.getByTestId('fleet-column-WaitingForCi');

  await expect(planningCol.getByTestId('fleet-card-11111111-1111-1111-1111-111111111111')).toBeVisible();
  await expect(verifyingCol.getByTestId('fleet-card-22222222-2222-2222-2222-222222222222')).toBeVisible();
  await expect(waitingCiCol.getByTestId('fleet-card-33333333-3333-3333-3333-333333333333')).toBeVisible();

  await expect(planningCol.getByText('Calorie Vision')).toBeVisible();
  await expect(verifyingCol.getByText('Banking API')).toBeVisible();
  await expect(waitingCiCol.getByText('Shop Frontend')).toBeVisible();
});

test('ci badge opens log drawer', async ({ page }) => {
  await page.addInitScript(() => localStorage.setItem('accessToken', 'e2e-test-token'));
  await page.goto('/ide/agent-board', { waitUntil: 'load' });

  await page.getByTestId('fleet-ci-badge-33333333-3333-3333-3333-333333333333').click();
  await expect(page.getByTestId('ci-log-drawer')).toBeVisible();
  await expect(page.getByTestId('ci-drawer-status')).toContainText('pending');
  await expect(page.getByTestId('ci-drawer-open-logs')).toHaveAttribute(
    'href',
    'https://github.com/org/repo/actions/runs/123',
  );
});
