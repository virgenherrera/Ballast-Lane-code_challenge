import type { Locator, Page } from '@playwright/test';
import { SELECTORS } from '../constants/selectors.constants.js';

export class TaskDetailPage {
  readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  async goto(id: string): Promise<void> {
    await this.page.goto(`/tasks/${id}`);
  }

  get container(): Locator {
    return this.page.locator(SELECTORS.taskDetail);
  }

  fieldValue(text: string, options?: { exact?: boolean }): Locator {
    return this.page.getByText(text, options);
  }

  fieldByLabel(label: string): Locator {
    return this.page.locator(`span:text-is("${label}") + p`);
  }

  get editLink(): Locator {
    return this.page.getByRole('link', { name: 'Edit' });
  }

  get deleteButton(): Locator {
    return this.page.getByRole('button', { name: 'Delete' });
  }
}
