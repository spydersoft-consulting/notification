import { test, expect, request as apiRequest, APIRequestContext } from '@playwright/test';
import { userAId, userAToken, userBId, userBToken, machineToken, readOnlyToken } from '../playwright.config';
import { NotificationDto } from './types';

const BASE = process.env.NOTIFICATION_BASE_URL ?? 'http://localhost:5300';
const SOURCE = 'notifications-spec';

function contextFor(_request: APIRequestContext, token: string) {
  return apiRequest.newContext({ baseURL: BASE, extraHTTPHeaders: { Authorization: `Bearer ${token}` } });
}

async function cleanup(request: APIRequestContext, userId: string) {
  const ctx = await contextFor(request, machineToken);
  try {
    await ctx.delete(`/api/test/notifications?userId=${encodeURIComponent(userId)}`);
  } finally {
    await ctx.dispose();
  }
}

async function createNotification(ctx: APIRequestContext, userId: string, overrides: Partial<{ type: string; priority: string }> = {}) {
  return ctx.post('/api/v1/notifications', {
    data: {
      userId,
      source: SOURCE,
      type: overrides.type ?? 'ping',
      subject: 'Test subject',
      body: 'Test body',
      priority: overrides.priority ?? 'Normal',
    },
  });
}

test('CreateNotification_MachineToken_Returns201', async ({ request }) => {
  const ctx = await contextFor(request, machineToken);
  try {
    const response = await createNotification(ctx, userAId);
    expect(response.status()).toBe(201);
    const body = (await response.json()) as NotificationDto;
    expect(body.status).toBe('Created');
  } finally {
    await ctx.dispose();
  }
});

test('CreateNotification_NoWriteScope_Returns403', async ({ request }) => {
  // A read-only token (no notification:write) cannot create, regardless of target user.
  const ctx = await contextFor(request, readOnlyToken);
  try {
    const response = await createNotification(ctx, userAId);
    expect(response.status()).toBe(403);
  } finally {
    await ctx.dispose();
  }
});

test('List_ReturnsOwnNotificationsOnly', async ({ request }) => {
  const machine = await contextFor(request, machineToken);
  const userA = await contextFor(request, userAToken);
  try {
    await createNotification(machine, userAId);
    await createNotification(machine, userBId);

    const response = await userA.get(`/api/v1/notifications?source=${SOURCE}`);
    expect(response.status()).toBe(200);
    const list = (await response.json()) as NotificationDto[];
    expect(list.length).toBeGreaterThan(0);
    for (const n of list) {
      expect(n.userId).toBe(userAId);
    }
  } finally {
    await machine.dispose();
    await userA.dispose();
  }
});

test('MarkRead_UpdatesIsReadAndReadAt_IsIdempotent', async ({ request }) => {
  const machine = await contextFor(request, machineToken);
  const userA = await contextFor(request, userAToken);
  try {
    const created = await (await createNotification(machine, userAId)).json() as NotificationDto;

    const first = await userA.post(`/api/v1/notifications/${created.id}/read`);
    expect(first.status()).toBe(200);
    const firstDto = (await first.json()) as NotificationDto;
    expect(firstDto.isRead).toBe(true);
    expect(firstDto.readAt).not.toBeNull();

    const second = await userA.post(`/api/v1/notifications/${created.id}/read`);
    expect(second.status()).toBe(200);
  } finally {
    await machine.dispose();
    await userA.dispose();
  }
});

test('MarkAllRead_UpdatesCount_ThenUnreadCountIsZero', async ({ request }) => {
  const machine = await contextFor(request, machineToken);
  const userA = await contextFor(request, userAToken);
  try {
    await createNotification(machine, userAId, { type: 'mark-all-read-test' });
    await createNotification(machine, userAId, { type: 'mark-all-read-test' });
    await createNotification(machine, userAId, { type: 'mark-all-read-test' });

    const markAll = await userA.post('/api/v1/notifications/read-all');
    expect(markAll.status()).toBe(200);
    const markAllBody = await markAll.json();
    expect(markAllBody.updatedCount).toBeGreaterThanOrEqual(3);

    const unread = await userA.get('/api/v1/notifications/unread-count');
    const unreadBody = await unread.json();
    expect(unreadBody.count).toBe(0);
  } finally {
    await machine.dispose();
    await userA.dispose();
  }
});

test('UnreadCount_ReflectsState', async ({ request }) => {
  const machine = await contextFor(request, machineToken);
  const userB = await contextFor(request, userBToken);
  try {
    await cleanup(request, userBId);

    const first = await (await createNotification(machine, userBId, { type: 'unread-count-test' })).json() as NotificationDto;
    await createNotification(machine, userBId, { type: 'unread-count-test' });
    await userB.post(`/api/v1/notifications/${first.id}/read`);

    const response = await userB.get('/api/v1/notifications/unread-count');
    const body = await response.json();
    expect(body.count).toBe(1);
  } finally {
    await machine.dispose();
    await userB.dispose();
  }
});

test('Dispatch_EventuallyReachesTerminalStatus', async ({ request }) => {
  const machine = await contextFor(request, machineToken);
  const userA = await contextFor(request, userAToken);
  try {
    const created = await (await createNotification(machine, userAId, { type: 'dispatch-test' })).json() as NotificationDto;

    let status = created.status;
    for (let attempt = 0; attempt < 20 && (status === 'Created' || status === 'Dispatching'); attempt++) {
      await new Promise((resolve) => setTimeout(resolve, 200));
      const response = await userA.get(`/api/v1/notifications/${created.id}`);
      const dto = (await response.json()) as NotificationDto;
      status = dto.status;
    }

    expect(['Dispatched', 'PartiallyFailed']).toContain(status);
  } finally {
    await machine.dispose();
    await userA.dispose();
  }
});
