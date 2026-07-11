import { expect } from '@playwright/test';
import { test } from '../fixtures/health.fixture.js';

test.describe('Health Semaphore', () => {
  test('should render the TaskFlow heading', async ({ healthPage }) => {
    await healthPage.goto();
    await expect(healthPage.heading).toHaveText('TaskFlow');
  });

  test('should display health status indicator', async ({ healthPage }) => {
    await healthPage.goto();
    await expect(healthPage.semaphore).toBeVisible({ timeout: 5000 });
    const status = await healthPage.semaphore.getAttribute('data-status');
    expect(['loading', 'ok', 'degraded', 'error']).toContain(status);
  });

  test('should show status details text', async ({ healthPage }) => {
    await healthPage.goto();
    await expect(healthPage.statusDetails).toBeVisible({ timeout: 5000 });
    const text = await healthPage.statusDetails.textContent();
    expect(text).toMatch(/API is healthy|API is up|API is unreachable|Checking/);
  });
});
