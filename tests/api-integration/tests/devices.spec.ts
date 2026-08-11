import { test, expect, request as apiRequest, APIRequestContext } from '@playwright/test';
import { userAToken, userBToken } from '../playwright.config';
import { DeviceDto } from './types';

const BASE = process.env.NOTIFICATION_BASE_URL ?? 'http://localhost:5300';

function contextFor(_request: APIRequestContext, token: string) {
  return apiRequest.newContext({ baseURL: BASE, extraHTTPHeaders: { Authorization: `Bearer ${token}` } });
}

test('RegisterDevice_Returns201', async ({ request }) => {
  const userA = await contextFor(request, userAToken);
  try {
    const response = await userA.post('/api/v1/devices', {
      data: { deviceType: 'Web', label: 'Playwright Chrome' },
    });
    expect(response.status()).toBe(201);
    const dto = (await response.json()) as DeviceDto;
    expect(dto.isActive).toBe(true);
  } finally {
    await userA.dispose();
  }
});

test('ListDevices_ActiveOnlyByDefault_IncludeInactiveShowsBoth', async ({ request }) => {
  const userA = await contextFor(request, userAToken);
  try {
    const first = await (await userA.post('/api/v1/devices', { data: { deviceType: 'Web', label: 'Device 1' } })).json() as DeviceDto;
    await userA.post('/api/v1/devices', { data: { deviceType: 'Web', label: 'Device 2' } });

    await userA.delete(`/api/v1/devices/${first.id}`);

    const activeOnly = (await (await userA.get('/api/v1/devices')).json()) as DeviceDto[];
    expect(activeOnly.find((d) => d.id === first.id)).toBeUndefined();

    const withInactive = (await (await userA.get('/api/v1/devices?includeInactive=true')).json()) as DeviceDto[];
    expect(withInactive.find((d) => d.id === first.id)).toBeDefined();
  } finally {
    await userA.dispose();
  }
});

test('DeregisterDevice_OtherUsersDevice_Returns404', async ({ request }) => {
  const userA = await contextFor(request, userAToken);
  const userB = await contextFor(request, userBToken);
  try {
    const device = await (await userA.post('/api/v1/devices', { data: { deviceType: 'Web', label: 'User A device' } })).json() as DeviceDto;

    const response = await userB.delete(`/api/v1/devices/${device.id}`);
    expect(response.status()).toBe(404);
  } finally {
    await userA.dispose();
    await userB.dispose();
  }
});
