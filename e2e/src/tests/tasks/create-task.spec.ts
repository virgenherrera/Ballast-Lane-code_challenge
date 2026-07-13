import { expect, test } from '../../fixtures/tasks.fixture.js';

test.describe('Create Task', () => {
  test('CreateTask_FromUI_AppearsInTaskList', async ({
    authenticatedPage: page,
    createTaskPage,
    taskListPage,
  }) => {
    const uniqueTitle = `E2E Task ${Date.now()}`;

    await createTaskPage.goto();
    await createTaskPage.fillTitle(uniqueTitle);

    await createTaskPage.submit();

    await expect(createTaskPage.successMessage).toBeVisible();

    await taskListPage.goto();
    await expect(taskListPage.taskList).toBeVisible();
    await expect(page.getByText(uniqueTitle)).toBeVisible();
  });

  test('CreateTask_WithEmptyTitle_ShowsValidationErrorInUI', async ({
    authenticatedPage: page,
    createTaskPage,
  }) => {
    await createTaskPage.goto();

    let requestFired = false;
    page.on('request', (request) => {
      if (request.method() === 'POST' && request.url().includes('/api/tasks')) {
        requestFired = true;
      }
    });

    // The title field starts empty and required, so the submit button is
    // disabled from the outset (form.invalid). Focus + blur the field to
    // mark it touched and surface the inline error — a real user hits the
    // same disabled-button wall, which is an even stronger client-side
    // guarantee than a rejected click.
    await createTaskPage.titleInput.click();
    await createTaskPage.titleInput.blur();

    await expect(createTaskPage.errorMessage('Title is required')).toBeVisible();
    await expect(createTaskPage.submitButton).toBeDisabled();
    expect(requestFired).toBe(false);
  });

  test('CreateTask_WithPastDueDate_ShowsValidationErrorInUI', async ({ createTaskPage }) => {
    await createTaskPage.goto();

    await createTaskPage.fillTitle('Task with past due date');

    const pastDate = new Date(Date.now() - 24 * 60 * 60 * 1000);
    const pastDateValue = pastDate.toISOString().slice(0, 16);
    await createTaskPage.fillDueDate(pastDateValue);
    await createTaskPage.dueDateInput.blur();

    await expect(createTaskPage.errorMessage('Due date must be in the future')).toBeVisible();
    await expect(createTaskPage.submitButton).toBeDisabled();
  });
});
