import { env } from 'node:process';
import { z } from 'zod';

const proxyEnv = z
  .object({
    API_PORT: z
      .string({ error: 'API_PORT env var is required — set it before running ng serve' })
      .regex(/^\d+$/, 'API_PORT must be a numeric string')
      .transform(Number),
  })
  .parse(env);

const target = `http://localhost:${proxyEnv.API_PORT}`;

export default {
  '/api': { target, secure: false },
  '/health': { target, secure: false },
};
