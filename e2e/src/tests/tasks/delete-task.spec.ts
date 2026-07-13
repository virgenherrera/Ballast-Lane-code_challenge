import { expect, test } from '../../fixtures/tasks.fixture.js';

test.describe('Delete Task', () => {
  test('DeleteTask_FromUI_RemovesFromListAndConfirms', async ({
    authenticatedPage: page,
    authenticatedRequest,
    taskListPage,
  }) => {
    // Arrange: seed a task via direct API call
    const createResponse = await authenticatedRequest.post('/api/tasks', {
      data: {
        title: `E2E delete test ${Date.now()}`,
        dueDate: new Date(Date.now() + 86400000).toISOString(),
      },
    });
    const task = await createResponse.json();

    // Navigate to task list and wait for task to appear
    await taskListPage.goto();
    await taskListPage.waitForDeleteButton(task.id);

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
      taskListPage.deleteButtonFor(task.id).click(),
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

  test('DeleteTask_CancelConfirmation_TaskRemainsInList', async ({
    authenticatedPage: page,
    authenticatedRequest,
    taskListPage,
  }) => {
    // Arrange: seed a task
    const createResponse = await authenticatedRequest.post('/api/tasks', {
      data: {
        title: `E2E cancel delete ${Date.now()}`,
        dueDate: new Date(Date.now() + 86400000).toISOString(),
      },
    });
    const task = await createResponse.json();

    await taskListPage.goto();
    await taskListPage.waitForDeleteButton(task.id);

    // Act: dismiss the confirmation dialog, then click delete
    page.on('dialog', (dialog) => dialog.dismiss());
    await taskListPage.deleteButtonFor(task.id).click();

    // Assert: task still visible
    await expect(page.getByText(task.title)).toBeVisible();
  });
});
