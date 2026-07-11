import { expect, test } from '@playwright/test';

test.describe('Create Task', () => {
  test('CreateTask_FromUI_AppearsInTaskList', async ({ page }) => {
    const uniqueTitle = `E2E Task ${Date.now()}`;

    await page.goto('/tasks/create');
    await page.locator('#title').fill(uniqueTitle);

    await page.getByRole('button', { name: 'Create Task' }).click();

    await expect(page.getByText('Task created successfully.')).toBeVisible();

    await page.goto('/tasks');
    await expect(page.getByTestId('task-list-stub')).toBeVisible();
    await expect(page.getByText(uniqueTitle)).toBeVisible();
  });

  test('CreateTask_WithEmptyTitle_ShowsValidationErrorInUI', async ({ page }) => {
    await page.goto('/tasks/create');

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
    await page.locator('#title').click();
    await page.locator('#title').blur();

    await expect(page.getByText('Title is required')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Create Task' })).toBeDisabled();
    expect(requestFired).toBe(false);
  });

  test('CreateTask_WithPastDueDate_ShowsValidationErrorInUI', async ({ page }) => {
    await page.goto('/tasks/create');

    await page.locator('#title').fill('Task with past due date');

    const pastDate = new Date(Date.now() - 24 * 60 * 60 * 1000);
    const pastDateValue = pastDate.toISOString().slice(0, 16);
    await page.locator('#dueDate').fill(pastDateValue);
    await page.locator('#dueDate').blur();

    await expect(page.getByText('Due date must be in the future')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Create Task' })).toBeDisabled();
  });
});
