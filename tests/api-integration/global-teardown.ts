import { request } from '@playwright/test';
import { userAId, userBId, machineToken } from './playwright.config';

const BASE = process.env.NOTIFICATION_BASE_URL ?? 'http://localhost:5300';

export default async function globalTeardown() {
  const ctx = await request.newContext({ baseURL: BASE, extraHTTPHeaders: { Authorization: `Bearer ${machineToken}` } });
  try {
    await ctx.delete(`/api/test/notifications?userId=${encodeURIComponent(userAId)}`);
    await ctx.delete(`/api/test/notifications?userId=${encodeURIComponent(userBId)}`);
  } finally {
    await ctx.dispose();
  }
}
