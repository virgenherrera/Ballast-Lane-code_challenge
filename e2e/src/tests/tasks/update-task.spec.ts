import { expect, test } from '../../fixtures/tasks.fixture.js';

test.describe('Update Task', () => {
  test('UpdateTask_ChangeStatusFromUI_ReflectsInListImmediately', async ({
    authenticatedPage: page,
    authenticatedRequest,
    taskListPage,
  }) => {
    // Arrange: seed a task via direct API call
    const createResponse = await authenticatedRequest.post('/api/tasks', {
      data: {
        title: 'E2E status change test',
        dueDate: new Date(Date.now() + 86400000).toISOString(),
      },
    });
    const task = await createResponse.json();

    // Navigate to task list
    await taskListPage.goto();
    await taskListPage.waitForStatusSelect(task.id);

    // Act: change status via dropdown
    const [patchRequest] = await Promise.all([
      page.waitForRequest(
        (req) => req.method() === 'PATCH' && req.url().includes(`/api/tasks/${task.id}`),
      ),
      taskListPage.selectTaskStatus(task.id, 'Completed'),
    ]);

    // Assert 1: Network — PATCH fired with correct body
    const body = JSON.parse(patchRequest.postData() ?? '{}');
    expect(body).toEqual({ status: 'Completed' });

    // Assert 2: DOM — list reflects new status without reload
    const selectValue = await taskListPage.statusSelectFor(task.id).inputValue();
    expect(selectValue).toBe('Completed');
  });
});
