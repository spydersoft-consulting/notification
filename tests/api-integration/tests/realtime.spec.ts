import { test, expect, APIRequestContext } from '@playwright/test';
import * as signalR from '@microsoft/signalr';
import { userAId, userAToken, userBId, userBToken, machineToken, hubUrl } from '../playwright.config';
import { NotificationDto, NotificationPushDto } from './types';

const BASE = process.env.NOTIFICATION_BASE_URL ?? 'http://localhost:5300';

function contextFor(request: APIRequestContext, token: string) {
  return request.newContext({ baseURL: BASE, extraHTTPHeaders: { Authorization: `Bearer ${token}` } });
}

function connect(token: string): signalR.HubConnection {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${hubUrl}/hubs/notifications`, { accessTokenFactory: () => token })
    .withAutomaticReconnect()
    .build();
}

async function waitForPush(connection: signalR.HubConnection, timeoutMs: number): Promise<NotificationPushDto | null> {
  return new Promise((resolve) => {
    const timer = setTimeout(() => resolve(null), timeoutMs);
    connection.on('ReceiveNotification', (n: NotificationPushDto) => {
      clearTimeout(timer);
      resolve(n);
    });
  });
}

test.afterAll(async ({ request }) => {
  const ctx = await contextFor(request, machineToken);
  try {
    await ctx.delete(`/api/test/notifications?userId=${encodeURIComponent(userAId)}`);
    await ctx.delete(`/api/test/notifications?userId=${encodeURIComponent(userBId)}`);
  } finally {
    await ctx.dispose();
  }
});

test('Connect_WithValidToken_Succeeds', async () => {
  const connection = connect(userAToken);
  try {
    await expect(connection.start()).resolves.toBeUndefined();
  } finally {
    await connection.stop();
  }
});

test('Connect_WithoutToken_Rejected', async () => {
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${hubUrl}/hubs/notifications`)
    .build();
  try {
    await expect(connection.start()).rejects.toBeDefined();
  } finally {
    await connection.stop();
  }
});

test('ReceivePush_OnNotificationCreate', async ({ request }) => {
  const connection = connect(userAToken);
  const machine = await contextFor(request, machineToken);
  try {
    await connection.start();
    const pushPromise = waitForPush(connection, 5000);

    const created = await (await machine.post('/api/v1/notifications', {
      data: { userId: userAId, source: 'realtime-spec', type: 'push-test', subject: 'Push subject', body: 'Push body' },
    })).json() as NotificationDto;

    const received = await pushPromise;
    expect(received).not.toBeNull();
    expect(received!.id).toBe(created.id);
    expect(received!.subject).toBe('Push subject');
  } finally {
    await connection.stop();
    await machine.dispose();
  }
});

test('NoPush_ForDifferentUser', async ({ request }) => {
  const connectionA = connect(userAToken);
  const machine = await contextFor(request, machineToken);
  try {
    await connectionA.start();
    const pushPromise = waitForPush(connectionA, 3000);

    await machine.post('/api/v1/notifications', {
      data: { userId: userBId, source: 'realtime-spec', type: 'push-test-other-user', subject: 'Not for A', body: 'Body' },
    });

    const received = await pushPromise;
    expect(received).toBeNull();
  } finally {
    await connectionA.stop();
    await machine.dispose();
  }
});

test('Reconnect_StillReceivesPushes', async ({ request }) => {
  const connection = connect(userAToken);
  const machine = await contextFor(request, machineToken);
  try {
    await connection.start();
    await connection.stop();
    await connection.start();

    const pushPromise = waitForPush(connection, 5000);
    const created = await (await machine.post('/api/v1/notifications', {
      data: { userId: userAId, source: 'realtime-spec', type: 'reconnect-test', subject: 'After reconnect', body: 'Body' },
    })).json() as NotificationDto;

    const received = await pushPromise;
    expect(received).not.toBeNull();
    expect(received!.id).toBe(created.id);
  } finally {
    await connection.stop();
    await machine.dispose();
  }
});
