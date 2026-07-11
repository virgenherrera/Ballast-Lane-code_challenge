import { test as base } from '@playwright/test';
import { HealthPage } from '../pages/health.page.js';

export const test = base.extend<{ healthPage: HealthPage }>({
  healthPage: async ({ page }, use) => {
    await use(new HealthPage(page));
  },
});
