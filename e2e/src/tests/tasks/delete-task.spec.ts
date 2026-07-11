import { expect, test } from '@playwright/test';

test.describe('Delete Task', () => {
  test('DeleteTask_FromUI_RemovesFromListAndConfirms', async ({ page, request }) => {
    // Arrange: seed a task via direct API call
    const createResponse = await request.post('/api/tasks', {
      data: {
        title: `E2E delete test ${Date.now()}`,
        dueDate: new Date(Date.now() + 86400000).toISOString(),
      },
    });
    const task = await createResponse.json();

    // Navigate to task list and wait for task to appear
    await page.goto('/tasks');
    await page.waitForSelector(`[data-testid="delete-btn-${task.id}"]`);

    // Capture console errors
    const consoleErrors: string[] = [];
    page.on('console', (msg) => {
      if (msg.type() === 'error') {
        consoleErrors.push(msg.text());
      }
    });

    // Act: accept the confirmation dialog, then click delete
    page.on('dialog', (dialog) => dialog.accept());

    // Wait for the DELETE request to complete
    const [deleteResponse] = await Promise.all([
      page.waitForResponse(
        (res) => res.request().method() === 'DELETE' && res.url().includes(`/api/tasks/${task.id}`),
      ),
      page.click(`[data-testid="delete-btn-${task.id}"]`),
    ]);

    // Assert 1: DELETE returned 204
    expect(deleteResponse.status()).toBe(204);

    // Assert 2: task is removed from DOM
    await expect(page.getByText(task.title)).not.toBeVisible();

    // Assert 3: no full page reload occurred (URL unchanged)
    expect(page.url()).toContain('/tasks');

    // Assert 4: no console errors
    expect(consoleErrors).toEqual([]);
  });

  test('DeleteTask_CancelConfirmation_TaskRemainsInList', async ({ page, request }) => {
    // Arrange: seed a task
    const createResponse = await request.post('/api/tasks', {
      data: {
        title: `E2E cancel delete ${Date.now()}`,
        dueDate: new Date(Date.now() + 86400000).toISOString(),
      },
    });
    const task = await createResponse.json();

    await page.goto('/tasks');
    await page.waitForSelector(`[data-testid="delete-btn-${task.id}"]`);

    // Act: dismiss the confirmation dialog, then click delete
    page.on('dialog', (dialog) => dialog.dismiss());
    await page.click(`[data-testid="delete-btn-${task.id}"]`);

    // Assert: task still visible
    await expect(page.getByText(task.title)).toBeVisible();
  });
});
