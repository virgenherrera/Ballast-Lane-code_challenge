import type { Locator, Page } from '@playwright/test';
import { SELECTORS } from '../constants/selectors.constants.js';

export class TaskListPage {
  readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  async goto(): Promise<void> {
    await this.page.goto('/tasks');
  }

  get taskList(): Locator {
    return this.page.locator(SELECTORS.taskList);
  }

  get taskListItems(): Locator {
    return this.page.getByTestId('task-list-item');
  }

  get statusFilter(): Locator {
    return this.page.locator(SELECTORS.statusFilter);
  }

  get emptyState(): Locator {
    return this.page.getByTestId('empty-state');
  }

  get pagePrev(): Locator {
    return this.page.getByTestId('page-prev');
  }

  get pageNext(): Locator {
    return this.page.getByTestId('page-next');
  }

  statusSelectFor(id: string): Locator {
    return this.page.locator(SELECTORS.statusSelectById(id));
  }

  deleteButtonFor(id: string): Locator {
    return this.page.locator(SELECTORS.deleteBtnById(id));
  }

  async selectStatusFilter(status: string): Promise<void> {
    await this.page.selectOption(SELECTORS.statusFilter, status);
  }

  async selectTaskStatus(id: string, status: string): Promise<void> {
    await this.page.selectOption(SELECTORS.statusSelectById(id), status);
  }

  async waitForTaskList(): Promise<void> {
    await this.page.waitForSelector(SELECTORS.taskList);
  }

  async waitForDeleteButton(id: string): Promise<void> {
    await this.page.waitForSelector(SELECTORS.deleteBtnById(id));
  }

  async waitForStatusSelect(id: string): Promise<void> {
    await this.page.waitForSelector(SELECTORS.statusSelectById(id));
  }
}
