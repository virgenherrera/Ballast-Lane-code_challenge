import { test as base, type APIRequestContext, type Page } from '@playwright/test';

interface AuthFixtures {
  authenticatedRequest: APIRequestContext;
  authenticatedPage: Page;
}

interface AuthWorkerFixtures {
  authToken: string;
}

/**
 * Auth fixture that registers a unique test user per worker, logs in,
 * and provides:
 * - `authenticatedRequest`: APIRequestContext with Bearer token header
 * - `authenticatedPage`: Page with the token seeded into localStorage['access_token'],
 *   exercising the real Angular `authInterceptor` (reads via `AuthService.getToken()`)
 *   instead of injecting the Authorization header at the network layer.
 *
 * The token is obtained once per worker (not per test) for performance.
 */
export const test = base.extend<AuthFixtures, AuthWorkerFixtures>({
  authToken: [
    async ({ playwright }, use, workerInfo) => {
      const apiPort = process.env['API_PORT'] ?? '3000';
      const apiBaseURL = `http://localhost:${apiPort}`;

      const email = `e2e-worker-${workerInfo.workerIndex}-${Date.now()}@test.local`;
      const password = 'Test1234!';
      const name = `E2E Worker ${workerInfo.workerIndex}`;

      const requestContext = await playwright.request.newContext({
        baseURL: apiBaseURL,
      });

      // Register
      const registerResponse = await requestContext.post('/api/auth/register', {
        data: { email, name, password },
      });

      if (!registerResponse.ok()) {
        const body = await registerResponse.text();
        throw new Error(
          `Auth fixture: registration failed (${registerResponse.status()}): ${body}`,
        );
      }

      // Login
      const loginResponse = await requestContext.post('/api/auth/login', {
        data: { email, password },
      });

      if (!loginResponse.ok()) {
        const body = await loginResponse.text();
        throw new Error(`Auth fixture: login failed (${loginResponse.status()}): ${body}`);
      }

      const loginBody: { accessToken: string } = await loginResponse.json();
      const token = loginBody.accessToken;

      await use(token);

      await requestContext.dispose();
    },
    { scope: 'worker' },
  ],

  authenticatedRequest: async ({ playwright, authToken }, use) => {
    const webPort = process.env['WEB_PORT'] ?? '4200';
    const baseURL = `http://localhost:${webPort}`;

    const requestContext = await playwright.request.newContext({
      baseURL,
      extraHTTPHeaders: {
        Authorization: `Bearer ${authToken}`,
      },
    });

    await use(requestContext);

    await requestContext.dispose();
  },

  authenticatedPage: async ({ page, authToken, baseURL }, use) => {
    // Navigate first — localStorage is origin-bound, so the origin must be
    // established before it can be written to.
    await page.goto(baseURL ?? '/');

    // Seed the token where AuthService.getToken() reads it from. Angular's
    // authInterceptor then attaches `Authorization: Bearer <token>` for real,
    // exercising the actual frontend auth flow instead of a route-level hack.
    await page.evaluate((token) => {
      localStorage.setItem('access_token', token);
    }, authToken);

    await use(page);
  },
});

export { expect } from '@playwright/test';
