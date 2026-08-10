# IdentityServer Configuration — Per-Env Runbook

The notification service depends on two OAuth2 scopes (`notification:read`,
`notification:write`) and a small set of clients/resources defined in IdentityServer.
**None of this is in code** — scope and client management is configured at runtime through
the IdentityServer admin UI. Each environment (test, stage, production) is configured
independently.

This document is the runbook for that configuration. Follow it once per environment.

---

## Per-Environment Admin URLs

| Env | Admin UI | Discovery endpoint |
| --- | --- | --- |
| test | `https://auth.mattgerega.net` | `https://auth.mattgerega.net/.well-known/openid-configuration` |
| stage | `https://auth.mattgerega.org` | `https://auth.mattgerega.org/.well-known/openid-configuration` |
| production | `https://auth.mattgerega.com` | `https://auth.mattgerega.com/.well-known/openid-configuration` |

Sign in with an admin-level credential. The shape of the admin UI may shift across
IdentityServer versions; the field names below are the conceptual names — match them to
whatever the current UI calls them.

---

## What to Configure

### 1. API resources

| Name | Display Name | Description |
| --- | --- | --- |
| `notification-api` | `Spydersoft Notification API` | HTTP API for notifications, devices, dispatch |
| `notification-hub` | `Spydersoft Notification Hub` | SignalR real-time push (JWT-validated WebSocket connections only — no separate scope) |

The hub validates the same `notification-api` audience/token; it does not need its own
scope, only its own `Audience` value if IdentityServer requires a resource entry per
audience string.

### 2. API scopes: `notification:read`, `notification:write`

Defined on the `notification-api` resource above.

| Field | `notification:read` | `notification:write` |
| --- | --- | --- |
| Display Name | `Read notifications` | `Create/manage notifications` |
| Description | List/get/unread-count for the caller's own notifications, devices, preferences | Create notifications (machine clients, for any user) + mark-read/device-register/preference-writes (user tokens, self only) |
| Required | no | no |
| Emphasize | no | no |
| Show in Discovery | yes | yes |

See [service-spec.md → Authorization](../../plans/notifications/service-spec.md#authorization)
for exactly which endpoints require which scope and whose identity they're scoped to —
`notification:write` is a wider trust grant than usual because notification *creation* is a
machine-to-machine, create-for-any-user operation, unlike the FileStore/Audit precedent.

### 3. Clients granted these scopes

| Client ID | Type | Allowed scopes | Why |
| --- | --- | --- | --- |
| `pitstop-api` | Machine (client_credentials) | `notification:write` | PitStop's recall-check job calls `POST /api/v1/notifications` as a machine client (SPY-3) |
| End-user token clients (existing PitStop interactive client, etc.) | Interactive (auth code + PKCE) | `notification:read` + `notification:write` | Browser calls list/read/mark-read/devices directly against the public API — see [consumer-integration.md](../../plans/notifications/consumer-integration.md#direct-frontend-to-service-calls) |

If `pitstop-api` already exists as a machine client (e.g. it also publishes audit events),
add `notification:write` to its existing allowed-scopes list rather than creating a new
client.

---

## Per-Field Cheat Sheet

For the `pitstop-api` machine client (new, or scope addition to an existing one):

| Field | Value |
| --- | --- |
| Client ID | `pitstop-api` |
| Client Name | `PitStop API` |
| Allowed Grant Types | Client Credentials |
| Require Client Secret | yes |
| Allowed Scopes | ...existing scopes..., `notification:write` |
| Token Lifetime | default (3600 s) |

For an existing interactive client picking up notification scopes, add
`notification:read notification:write` to its Allowed Scopes list — no other field
changes.

The client secret goes into Vault (or whatever secret store the consumer uses), never the
admin UI display.

---

## Verify

After saving:

```bash
curl -s https://auth.mattgerega.net/.well-known/openid-configuration \
  | jq '.scopes_supported'
```

You should see `"notification:read"` and `"notification:write"` in the array.

For a smoke test of the machine client:

```bash
curl -s -X POST https://auth.mattgerega.net/connect/token \
  -d grant_type=client_credentials \
  -d client_id=pitstop-api \
  -d client_secret=<from-vault> \
  -d scope=notification:write \
  | jq .
```

Expect a JWT in `access_token`. Decode it at <https://jwt.io> and confirm:

- `iss` matches the env's authority URL
- `aud` includes `notification-api`
- `scope` (or `scp`) contains `notification:write`

Hit the API:

```bash
curl -s -X POST https://notify.mattgerega.net/api/v1/notifications \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"userId":"smoke-test-user","source":"smoke-test","type":"ping","subject":"Smoke test","body":"Hello"}' \
  | jq .
```

Expect `201 Created` with `status: "Created"`.

---

## Rollback

To remove the configuration (e.g. accidentally created in the wrong env):

1. Disable the `notification-api` resource (don't delete — disable is reversible).
2. Disable any clients granted `notification:read`/`notification:write`.
3. Confirm `.well-known/openid-configuration` no longer advertises either scope.

The notification-api/notification-hub services remain deployed but every request returns
401 once the authority no longer issues tokens for these scopes.

---

## Promotion Checklist

When promoting a deploy from test to stage, then to production:

- [ ] Sign in to **stage** admin UI (`auth.mattgerega.org`)
- [ ] Repeat sections 1, 2, 3 above using the same field values
- [ ] Run the verify-curl against the stage discovery endpoint
- [ ] Sign in to **production** admin UI (`auth.mattgerega.com`)
- [ ] Repeat sections 1, 2, 3 above
- [ ] Run the verify-curl against the production discovery endpoint

The configuration does **not** replicate from test automatically. Each env is a manual,
identical pass.
