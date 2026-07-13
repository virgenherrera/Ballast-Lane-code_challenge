import { expect, test } from '../../fixtures/tasks.fixture.js';

test.describe('List Tasks Pagination', () => {
  test('ListTasks_NavigatePages_UpdatesListAndPagingControls', async ({
    authenticatedPage: page,
    authenticatedRequest,
    taskListPage,
  }) => {
    // Arrange: create 25 tasks via API (> DefaultPerPage of 20)
    const suffix = Date.now();
    const seedCount = 25;

    for (let i = 0; i < seedCount; i += 1) {
      // eslint-disable-next-line no-await-in-loop
      await authenticatedRequest.post('/api/tasks', {
        data: {
          title: `E2E pagination ${suffix} #${i}`,
          dueDate: new Date(Date.now() + 86400000).toISOString(),
        },
      });
    }

    // Act: navigate to /tasks
    await taskListPage.goto();
    await taskListPage.waitForTaskList();

    // Assert page 1: task-list-item count == 20; page-prev disabled; page-next enabled
    await expect(taskListPage.taskListItems).toHaveCount(20);
    await expect(taskListPage.pagePrev).toBeDisabled();
    await expect(taskListPage.pageNext).toBeEnabled();

    // Act: click page-next
    await Promise.all([
      page.waitForResponse(
        (res) => res.request().method() === 'GET' && res.url().includes('/api/tasks?'),
      ),
      taskListPage.pageNext.click(),
    ]);

    // Assert page 2: at least one item is present; page-prev enabled; URL page=2.
    // Exact count/page-next state are NOT asserted here because parallel test
    // workers share the same SeedOwnerId and may create additional tasks
    // concurrently, making the total (and therefore page 2's exact size and
    // whether a page 3 exists) non-deterministic.
    await expect(taskListPage.taskListItems.first()).toBeVisible();
    await expect(taskListPage.pagePrev).toBeEnabled();
    expect(page.url()).toMatch(/[?&]page=2\b/);

    // Act: click page-prev
    await Promise.all([
      page.waitForResponse(
        (res) => res.request().method() === 'GET' && res.url().includes('/api/tasks?'),
      ),
      taskListPage.pagePrev.click(),
    ]);

    // Assert page 1 again: task-list-item count == 20; page-prev disabled
    await expect(taskListPage.taskListItems).toHaveCount(20);
    await expect(taskListPage.pagePrev).toBeDisabled();
  });
});
