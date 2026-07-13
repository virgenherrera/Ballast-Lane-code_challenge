import { expect, test } from '../../fixtures/tasks.fixture.js';

test.describe('View Task Detail', () => {
  test('ViewTaskDetail_FromList_ShowsFullTaskInfo', async ({
    authenticatedPage: page,
    authenticatedRequest,
    taskDetailPage,
    taskListPage,
  }) => {
    // Arrange: create 1 task via API with title + description + dueDate
    const uniqueTitle = `E2E detail view ${Date.now()}`;
    const dueDate = new Date(Date.now() + 86400000).toISOString();

    const createResponse = await authenticatedRequest.post('/api/tasks', {
      data: {
        title: uniqueTitle,
        description: 'E2E detail description',
        dueDate,
      },
    });
    const task = await createResponse.json();

    // Act: navigate to /tasks, click the task row/link to navigate to detail
    await taskListPage.goto();
    await taskListPage.waitForTaskList();
    await expect(page.getByText(uniqueTitle)).toBeVisible();

    await page.getByText(uniqueTitle).click();

    // Assert: URL is /tasks/{id}
    await expect(page).toHaveURL(new RegExp(`/tasks/${task.id}$`));

    // Assert: detail container visible
    await expect(taskDetailPage.container).toBeVisible();

    // Assert: title, description, status render as raw text
    await expect(taskDetailPage.fieldValue(task.title, { exact: true })).toBeVisible();
    await expect(taskDetailPage.fieldValue('E2E detail description')).toBeVisible();
    await expect(taskDetailPage.fieldValue(task.status, { exact: true })).toBeVisible();

    // Due Date / Created / Updated are rendered via formatDate(), which
    // produces a locale-dependent string — assert the labeled field has
    // non-empty content rather than matching the raw ISO value.
    await expect(taskDetailPage.fieldByLabel('Due Date')).not.toHaveText('');
    await expect(taskDetailPage.fieldByLabel('Created')).not.toHaveText('');
    await expect(taskDetailPage.fieldByLabel('Updated')).not.toHaveText('');

    // Assert: Edit and Delete actions are available
    await expect(taskDetailPage.editLink).toBeVisible();
    await expect(taskDetailPage.deleteButton).toBeVisible();
  });
});
