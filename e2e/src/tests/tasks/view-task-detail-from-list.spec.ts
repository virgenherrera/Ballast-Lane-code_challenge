import { expect, test } from '@playwright/test';

test.describe('View Task Detail', () => {
  test('ViewTaskDetail_FromList_ShowsFullTaskInfo', async ({ page, request }) => {
    // Arrange: create 1 task via API with title + description + dueDate
    const uniqueTitle = `E2E detail view ${Date.now()}`;
    const dueDate = new Date(Date.now() + 86400000).toISOString();

    const createResponse = await request.post('/api/tasks', {
      data: {
        title: uniqueTitle,
        description: 'E2E detail description',
        dueDate,
      },
    });
    const task = await createResponse.json();

    // Act: navigate to /tasks, click the task row/link to navigate to detail
    await page.goto('/tasks');
    await page.waitForSelector('[data-testid="task-list"]');
    await expect(page.getByText(uniqueTitle)).toBeVisible();

    await page.getByText(uniqueTitle).click();

    // Assert: URL is /tasks/{id}
    await expect(page).toHaveURL(new RegExp(`/tasks/${task.id}$`));

    // Assert: detail container visible
    await expect(page.getByTestId('task-detail')).toBeVisible();

    // Assert: all 8 fields rendered
    await expect(page.getByText(task.title, { exact: true })).toBeVisible();
    await expect(page.getByText('E2E detail description')).toBeVisible();
    await expect(page.getByText(task.status, { exact: true })).toBeVisible();
    await expect(page.getByText(task.ownerId, { exact: true })).toBeVisible();
    await expect(page.getByText(task.id, { exact: true })).toBeVisible();
    await expect(page.getByText(task.dueDate, { exact: true })).toBeVisible();

    // Created At / Updated At are asserted via their <dt>/<dd> pair rather
    // than a bare getByText: for a freshly created (never updated) task the
    // two timestamps are identical, so a global text search matches both
    // <dd> elements and trips Playwright's strict-mode uniqueness check.
    const dl = page.locator('dl');
    await expect(dl.locator('dt', { hasText: 'Created At' }).locator('+ dd')).toHaveText(
      task.createdAt,
    );
    await expect(dl.locator('dt', { hasText: 'Updated At' }).locator('+ dd')).toHaveText(
      task.updatedAt,
    );
  });
});
