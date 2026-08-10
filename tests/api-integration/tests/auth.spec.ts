import { test, expect } from '@playwright/test';

const BASE = process.env.NOTIFICATION_BASE_URL ?? 'http://localhost:5300';

test('Notifications_GET_NoToken_Returns401', async ({ playwright }) => {
  const anon = await playwright.request.newContext({ baseURL: BASE, extraHTTPHeaders: {} });
  try {
    const response = await anon.get('/api/v1/notifications');
    expect(response.status()).toBe(401);
  } finally {
    await anon.dispose();
  }
});

test('Notifications_POST_NoToken_Returns401', async ({ playwright }) => {
  const anon = await playwright.request.newContext({ baseURL: BASE, extraHTTPHeaders: {} });
  try {
    const response = await anon.post('/api/v1/notifications', {
      data: { userId: 'x', source: 'x', type: 'x', subject: 'x', body: 'x' },
    });
    expect(response.status()).toBe(401);
  } finally {
    await anon.dispose();
  }
});

test('Devices_GET_NoToken_Returns401', async ({ playwright }) => {
  const anon = await playwright.request.newContext({ baseURL: BASE, extraHTTPHeaders: {} });
  try {
    const response = await anon.get('/api/v1/devices');
    expect(response.status()).toBe(401);
  } finally {
    await anon.dispose();
  }
});

test('HealthCheck_NoToken_Returns200', async ({ playwright }) => {
  const anon = await playwright.request.newContext({ baseURL: BASE, extraHTTPHeaders: {} });
  try {
    const response = await anon.get('/livez');
    expect(response.status()).toBe(200);
  } finally {
    await anon.dispose();
  }
});

test('HubInternalPush_NoToken_Returns401', async ({ playwright }) => {
  const hubBase = process.env.NOTIFICATION_HUB_BASE_URL ?? 'http://localhost:5301';
  const anon = await playwright.request.newContext({ baseURL: hubBase, extraHTTPHeaders: {} });
  try {
    const response = await anon.post('/internal/push', {
      data: { userId: 'x', notification: { id: '00000000-0000-0000-0000-000000000000', source: 'x', type: 'x', subject: 'x', body: 'x', priority: 'Normal', createdAt: new Date().toISOString() } },
    });
    expect(response.status()).toBe(401);
  } finally {
    await anon.dispose();
  }
});

test('HubInternalPush_WrongToken_Returns401', async ({ playwright }) => {
  const hubBase = process.env.NOTIFICATION_HUB_BASE_URL ?? 'http://localhost:5301';
  const anon = await playwright.request.newContext({
    baseURL: hubBase,
    extraHTTPHeaders: { Authorization: 'Bearer wrong-token' },
  });
  try {
    const response = await anon.post('/internal/push', {
      data: { userId: 'x', notification: { id: '00000000-0000-0000-0000-000000000000', source: 'x', type: 'x', subject: 'x', body: 'x', priority: 'Normal', createdAt: new Date().toISOString() } },
    });
    expect(response.status()).toBe(401);
  } finally {
    await anon.dispose();
  }
});
