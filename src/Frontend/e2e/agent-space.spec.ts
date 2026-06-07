import { expect, test } from '@playwright/test';

const spaceId = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

const mockSpaceDetail = {
  space: {
    spaceId,
    name: 'Calorie Pipeline Space',
    repositoryUrl: null,
    baseBranch: 'main',
    ownerId: 'user-1',
    sharedMemoryScope: `project:${spaceId}`,
    mcpProfile: null,
    createdAtUtc: new Date().toISOString(),
    rootPath: '/tmp/spaces/calorie',
    integrationBranch: 'space/integration',
  },
  members: [
    {
      memberId: 'exp001',
      spaceId,
      role: 'Explorer',
      runId: '11111111-1111-1111-1111-111111111111',
      worktreePath: '/tmp/spaces/calorie/worktrees/exp001',
      branchName: 'agent/explorer/exp001',
      status: 'Completed',
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: new Date().toISOString(),
      lastError: null,
    },
    {
      memberId: 'impl01',
      spaceId,
      role: 'Implementer',
      runId: '22222222-2222-2222-2222-222222222222',
      worktreePath: '/tmp/spaces/calorie/worktrees/impl01',
      branchName: 'agent/implementer/impl01',
      status: 'Running',
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: new Date().toISOString(),
      lastError: null,
    },
    {
      memberId: 'ver001',
      spaceId,
      role: 'Verifier',
      runId: '33333333-3333-3333-3333-333333333333',
      worktreePath: '/tmp/spaces/calorie/main',
      branchName: 'space/integration',
      status: 'Running',
      createdAtUtc: new Date().toISOString(),
      updatedAtUtc: new Date().toISOString(),
      lastError: null,
    },
  ],
  recentContext: [
    {
      eventId: 'evt1',
      spaceId,
      kind: 'space_context_ready',
      title: 'Context ready',
      payload: 'API plan published',
      authorMemberId: 'exp001',
      timestampUtc: new Date().toISOString(),
    },
    {
      eventId: 'evt2',
      spaceId,
      kind: 'verifier_started',
      title: 'Verifier on integration branch',
      payload: 'space/integration',
      authorMemberId: 'ver001',
      timestampUtc: new Date().toISOString(),
    },
  ],
};

test.beforeEach(async ({ page }) => {
  await page.route(`**/api/v1/ide/spaces/${spaceId}`, async (route) => {
    if (route.request().method() !== 'GET') {
      await route.continue();
      return;
    }
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(mockSpaceDetail),
    });
  });

  await page.route(`**/api/v1/ide/spaces/${spaceId}/orchestrate`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        spaceId,
        stage: 'verifier_spawned',
        contextReady: true,
        explorer: mockSpaceDetail.members[0],
        implementer: mockSpaceDetail.members[1],
        verifier: mockSpaceDetail.members[2],
      }),
    });
  });
});

test('space detail shows implementer and verifier on integration branch', async ({ page }) => {
  await page.goto(`/ide/spaces/${spaceId}`, { waitUntil: 'load' });

  await expect(page.getByTestId('space-detail')).toBeVisible({ timeout: 45_000 });
  await expect(page.getByTestId('space-detail-title')).toContainText('Calorie Pipeline Space');

  await expect(page.getByTestId('space-member-exp001')).toBeVisible();
  await expect(page.getByTestId('space-member-impl01')).toBeVisible();
  await expect(page.getByTestId('space-member-ver001')).toBeVisible();

  await expect(page.getByTestId('space-member-ver001')).toContainText('space/integration');
  await expect(page.getByText('Context ready')).toBeVisible();
  await expect(page.getByText('Verifier on integration branch')).toBeVisible();
});

test('space detail run pipeline triggers orchestrate', async ({ page }) => {
  await page.goto(`/ide/spaces/${spaceId}`, { waitUntil: 'load' });
  await expect(page.getByTestId('space-detail')).toBeVisible({ timeout: 45_000 });

  await page.getByTestId('space-orchestrate-btn').click();
  await expect(page.getByText('Pipeline stage: verifier_spawned')).toBeVisible({ timeout: 10_000 });
});
