import type { Locator, Page } from '@playwright/test';
import { SELECTORS } from '../constants/selectors.constants.js';

export class HealthPage {
  readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  async goto(): Promise<void> {
    await this.page.goto('/');
  }

  get semaphore(): Locator {
    return this.page.locator(SELECTORS.semaphore);
  }

  get heading(): Locator {
    return this.page.locator(SELECTORS.heading);
  }

  get statusDetails(): Locator {
    return this.page.locator(SELECTORS.statusDetails);
  }
}
