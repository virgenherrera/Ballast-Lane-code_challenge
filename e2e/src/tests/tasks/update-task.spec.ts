import { expect, test } from '@playwright/test';

test.describe('Update Task', () => {
  test('UpdateTask_ChangeStatusFromUI_ReflectsInListImmediately', async ({ page, request }) => {
    // Arrange: seed a task via direct API call
    const createResponse = await request.post('/api/tasks', {
      data: {
        title: 'E2E status change test',
        dueDate: new Date(Date.now() + 86400000).toISOString(),
      },
    });
    const task = await createResponse.json();

    // Navigate to task list
    await page.goto('/tasks');
    await page.waitForSelector(`[data-testid="status-select-${task.id}"]`);

    // Act: change status via dropdown
    const [patchRequest] = await Promise.all([
      page.waitForRequest(
        (req) => req.method() === 'PATCH' && req.url().includes(`/api/tasks/${task.id}`),
      ),
      page.selectOption(`[data-testid="status-select-${task.id}"]`, 'Completed'),
    ]);

    // Assert 1: Network — PATCH fired with correct body
    const body = JSON.parse(patchRequest.postData() ?? '{}');
    expect(body).toEqual({ status: 'Completed' });

    // Assert 2: DOM — list reflects new status without reload
    const selectValue = await page
      .locator(`[data-testid="status-select-${task.id}"]`)
      .inputValue();
    expect(selectValue).toBe('Completed');
  });
});
