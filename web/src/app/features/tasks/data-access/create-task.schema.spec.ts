import { TITLE_MAX_LENGTH } from '../models/task.constants';
import { createTaskSchema } from './create-task.schema';

describe('createTaskSchema', () => {
  it('createTaskSchema_WithEmptyTitle_Fails', () => {
    const result = createTaskSchema.safeParse({ title: '' });

    expect(result.success).toBe(false);
  });

  it('createTaskSchema_WithWhitespaceOnlyTitle_Fails', () => {
    const result = createTaskSchema.safeParse({ title: '    ' });

    expect(result.success).toBe(false);
  });

  it('createTaskSchema_WithNbspOnlyTitle_Fails', () => {
    const nbspOnlyTitle = '   ';
    const result = createTaskSchema.safeParse({ title: nbspOnlyTitle });

    expect(result.success).toBe(false);
  });

  it('createTaskSchema_WithTitleExceeding200Chars_Fails', () => {
    const result = createTaskSchema.safeParse({ title: 'a'.repeat(TITLE_MAX_LENGTH + 1) });

    expect(result.success).toBe(false);
  });

  it('createTaskSchema_WithTitleExactly200Chars_Passes', () => {
    const result = createTaskSchema.safeParse({ title: 'a'.repeat(TITLE_MAX_LENGTH) });

    expect(result.success).toBe(true);
  });

  it('createTaskSchema_WithPastDueDate_Fails', () => {
    const result = createTaskSchema.safeParse({
      title: 'Buy groceries',
      dueDate: '2020-01-01T00:00:00Z',
    });

    expect(result.success).toBe(false);
  });

  it('createTaskSchema_WithValidPayload_Passes', () => {
    const futureDate = new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString();

    const result = createTaskSchema.safeParse({
      title: 'Buy groceries',
      description: 'Milk, eggs, bread',
      dueDate: futureDate,
    });

    expect(result.success).toBe(true);
  });
});
