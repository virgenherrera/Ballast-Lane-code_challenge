import { defineConfig, devices } from '@playwright/test';
import { z } from 'zod';

const env = z
  .object({
    WEB_PORT: z
      .string({ error: 'WEB_PORT is required' })
      .regex(/^\d+$/)
      .transform(Number),
    API_PORT: z
      .string({ error: 'API_PORT is required' })
      .regex(/^\d+$/)
      .transform(Number),
    CI: z
      .string()
      .optional()
      .transform(Boolean),
  })
  .parse(process.env);

const baseURL = `http://localhost:${env.WEB_PORT}`;

export default defineConfig({
  testDir: './src/tests',
  outputDir: './results',
  fullyParallel: true,
  forbidOnly: env.CI,
  retries: env.CI ? 2 : 0,
  workers: env.CI ? 1 : undefined,
  reporter: [['html', { outputFolder: './report', open: 'on-failure' }]],
  use: {
    baseURL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    viewport: { width: 1280, height: 720 },
  },
  projects: [
    {
      name: 'e2e',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
});
