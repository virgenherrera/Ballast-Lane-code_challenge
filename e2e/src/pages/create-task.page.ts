import type { Locator, Page } from '@playwright/test';
import { SELECTORS } from '../constants/selectors.constants.js';

export class CreateTaskPage {
  readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  async goto(): Promise<void> {
    await this.page.goto('/tasks/create');
  }

  get titleInput(): Locator {
    return this.page.locator(SELECTORS.titleInput);
  }

  get descriptionInput(): Locator {
    return this.page.locator(SELECTORS.descriptionInput);
  }

  get dueDateInput(): Locator {
    return this.page.locator(SELECTORS.dueDateInput);
  }

  get submitButton(): Locator {
    return this.page.getByRole('button', { name: 'Create Task' });
  }

  get successMessage(): Locator {
    return this.page.getByText('Task created successfully.');
  }

  errorMessage(text: string): Locator {
    return this.page.getByText(text);
  }

  async fillTitle(title: string): Promise<void> {
    await this.titleInput.fill(title);
  }

  async fillDescription(description: string): Promise<void> {
    await this.descriptionInput.fill(description);
  }

  async fillDueDate(dueDate: string): Promise<void> {
    await this.dueDateInput.fill(dueDate);
  }

  async submit(): Promise<void> {
    await this.submitButton.click();
  }
}
