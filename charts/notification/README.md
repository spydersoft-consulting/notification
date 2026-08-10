# notification chart

Combined OCI chart for the Notification application's own controllers: `notification-api` (core storage, device registry, in-process dispatcher, router) and `notification-hub` (SignalR real-time push). Published from this repo (`ghcr.io/spydersoft-consulting/charts/notification`), versioned alongside the container images.

This chart does **not** own or create any Kubernetes `Secret`, `ConfigMap`, or backing infrastructure (PostgreSQL). It only references config/secrets **by name**, via `envFrom.secretRef`/`envFrom.configMapRef`, with the names themselves overridable values. Whoever composes this chart (today: `platform-helm-config`) is responsible for creating the referenced Secret/ConfigMap and for owning the shared PostgreSQL instance the API connects to.

## Values

- `controllers.notification-api.containers.main.image.tag` — notification-api image tag.
- `controllers.notification-hub.containers.main.image.tag` — notification-hub image tag.
- `controllers.notification-api.containers.main.envFrom` / `controllers.notification-hub.containers.main.envFrom` — supplied entirely by the caller; not defaulted here (every real caller overrides this in full to add its own `configMapRef`s — see the secrets contract below for what the secret must contain).
- `route.notification-api.hostnames` / `route.notification-hub.hostnames` — per-environment hostname(s); not defaulted here since every real caller supplies them. The hub route is public — browsers connect to it directly over WebSocket from outside the cluster.

## Secrets contract

The caller must create a secret named **`notification-secrets`** containing:

- `ConnectionStrings__notification` — PostgreSQL connection string
- `Notification__HubInternalToken` — shared-secret bearer token validated by the hub's `/internal/push` endpoint; the same value must be injected into both controllers

The secret name is not hardcoded in this chart — it's supplied via the caller's `envFrom.secretRef.name` override, so a different composing repo could name/source it however it wants without any chart change.
