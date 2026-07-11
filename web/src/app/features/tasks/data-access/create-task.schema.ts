import { z } from 'zod';

import { DESCRIPTION_MAX_LENGTH, TITLE_MAX_LENGTH } from '../models/task.constants';

export const createTaskSchema = z.object({
  title: z
    .string()
    .transform((value) => value.trim())
    .refine((value) => value.length > 0, { message: 'title required' })
    .refine((value) => value.length <= TITLE_MAX_LENGTH, {
      message: `title must not exceed ${TITLE_MAX_LENGTH} characters`,
    }),
  description: z.string().max(DESCRIPTION_MAX_LENGTH).nullish(),
  dueDate: z
    .string()
    .datetime()
    .nullish()
    .refine((value) => !value || new Date(value) > new Date(), {
      message: 'must be future',
    }),
});

export type CreateTaskSchema = z.infer<typeof createTaskSchema>;
