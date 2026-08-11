import { defineConfig } from '@playwright/test';
import { execSync } from 'node:child_process';
import { mkdirSync, writeFileSync, readFileSync, existsSync } from 'node:fs';
import path from 'node:path';

const authDir = path.join(__dirname, '.auth');
const tokenGenProject = path.resolve(
  __dirname, '../../src/Spydersoft.Notification.TokenGenerator');
const appHostProject = path.resolve(
  __dirname, '../../src/Spydersoft.Notification.AppHost');
const baseUrl = process.env.NOTIFICATION_BASE_URL ?? 'http://localhost:5300';
export const hubUrl = process.env.NOTIFICATION_HUB_URL ?? 'http://localhost:5301';

function runTokenGen(cacheFile: string, extraArgs: string): string {
  const file = path.join(authDir, cacheFile);
  if (!existsSync(file)) {
    try {
      const output = execSync(`dotnet run --project "${tokenGenProject}" -- ${extraArgs}`, {
        encoding: 'utf-8',
        stdio: ['ignore', 'pipe', 'ignore'],
        timeout: 60_000,
      });
      const json = output.split('\n').map((l: string) => l.trim()).find((l: string) => l.startsWith('{')) ?? '{}';
      const token = (JSON.parse(json) as { token?: string }).token ?? '';
      mkdirSync(authDir, { recursive: true });
      writeFileSync(file, JSON.stringify({ token }));
      return token;
    } catch {
      return '';
    }
  }
  try {
    return JSON.parse(readFileSync(file, 'utf-8')).token ?? '';
  } catch {
    return '';
  }
}

// Two distinct user tokens (for isolation/cross-user assertions) plus a machine token
// (notification:write only — represents PitStop's recall-check job creating notifications
// for arbitrary users). See plans/notifications/service-spec.md#authorization.
export const userAId = 'notification-test-user-a';
export const userBId = 'notification-test-user-b';
export const userAToken = runTokenGen('token-user-a.json', userAId);
export const userBToken = runTokenGen('token-user-b.json', userBId);
export const machineToken = runTokenGen('token-machine.json', `${userAId} --machine`);
export const readOnlyToken = runTokenGen('token-readonly.json', `${userAId} --read-only`);

export default defineConfig({
  testDir: './tests',
  globalTeardown: require.resolve('./global-teardown'),
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: 'html',
  use: {
    baseURL: baseUrl,
    ignoreHTTPSErrors: true,
    extraHTTPHeaders: {
      Authorization: `Bearer ${userAToken}`,
    },
  },
  webServer: {
    command: `dotnet run --project "${appHostProject}" --launch-profile Testing`,
    url: `${baseUrl}/livez`,
    timeout: 300_000,
    reuseExistingServer: !process.env.CI,
  },
});
