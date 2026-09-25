# Dayweave architecture

## Data flow

The React application owns navigation and presentation. `AuthContext` selects an authenticated account or an explicit demo session. `TimelineContext` exposes one CRUD interface to the pages and chooses the data source behind it.

| Mode | Read/write path | Persistence | Identity |
| --- | --- | --- | --- |
| Demo | Browser-side sample generator and CRUD | `dayweave.demo.entries.v1` in localStorage | Explicit demo workspace |
| Account | Axios → bearer-protected Minimal API → EF Core | SQLite | GitHub OAuth user ID mapped to an application user |

Sample connections do not impersonate real integrations. Demo writes never reach the API. Signing out clears session keys; the separately named demo archive remains available for the next demo visit.

## API and synchronization

`Program.cs` configures DI, CORS, authentication, migration startup, and endpoint groups. `TimelineMapping` keeps transport DTOs separate from EF navigation properties and marks SQLite-loaded dates as UTC. `EntryValidation` validates required fields, lengths, allowed types, dates, and HTTP(S) links for both creates and edits.

Each provider implements `IThirdPartyApiService`. Typed HTTP clients prevent a provider's authorization header from leaking into another provider's client. Providers normalize their data into the shared timeline model.

The hourly worker and manual sync/disconnect endpoints use one `SyncCoordinator`. The semaphore deliberately serializes these operations in this single-process SQLite prototype. Each worker operation owns a fresh DI scope and DbContext. A failed cycle is isolated so later cycles can retry. This is not a distributed queue or a retry/backoff framework.

## Persistence rules

- Users are unique by `(OAuthProvider, OAuthId)`.
- Entries reference their owning user.
- Imported entries are unique by `(UserId, SourceApi, ExternalId)`.
- Manual entries receive generated external IDs on the server.
- Every application read or mutation scopes entries to the authenticated user.
- A user cannot edit a synced item. Imported items can be deleted, but may reappear on a later provider sync.
- Disconnect removes that provider's connection and imported entries, leaving manual entries intact.

The original migration remains in place. A follow-up migration changes the global external-event uniqueness rule to a user-scoped rule without deleting existing rows.

## Authentication decisions

GitHub OAuth middleware handles the primary sign-in exchange and correlation. After login, the API creates an eight-hour JWT and passes it in the frontend callback URL fragment. The frontend consumes and removes the fragment, rejects expired/malformed sessions, and returns to sign-in after unauthorized API responses.

The client stores the bearer token in localStorage for this prototype. Decoding the token on the client is a UX check, not signature verification; the API validates issuer, audience, lifetime, and signature on protected requests.

Spotify and YouTube linking use random 256-bit nonces, a ten-minute expiry, a server-side user/provider binding, a matching HttpOnly browser cookie, and atomic single-use consumption. The state is not a raw user ID. State is process-local, so an API restart invalidates in-flight links; a multi-instance deployment requires a shared expiring store.

## Frontend design

- Forest green navigation, warm neutral surfaces, and restrained per-source accents.
- Lightweight CSS shapes and inline SVG icons replace video backgrounds.
- Native HTML dialogs support focus management and Escape dismissal; forms show inline failures and disable repeated saves.
- Overview metrics and the activity chart are derived from entries, not decorative hard-coded numbers.
- Filters use local calendar dates; transport dates remain UTC.
- Unsafe URL schemes are rejected in both the editor and API, and imported external links are checked before rendering.
- Large entry descriptions wrap; mobile breakpoints rearrange navigation and columns; reduced-motion settings remove transitions.

## Current constraints

| Constraint | Consequence / follow-up |
| --- | --- |
| Provider tokens are stored in SQLite without field encryption | Add protected token storage, key management, and database access controls before using real personal data in production |
| Bearer tokens in localStorage | XSS would expose sessions; consider a secure server-side session or BFF and a deployment CSP |
| Single-process nonce store and sync semaphore | Use a durable shared store and distributed job coordination for multiple replicas |
| Full timeline read, no pagination | Introduce cursor pagination and bounded client rendering as activity grows |
| Finite provider response windows | Implement pagination/backfill where provider APIs permit it |
| No background sync retry backoff or operational monitoring | Add rate-limit handling, retry policies, status tracking, and observability |
| External APIs not exercised with live credentials by CI | Validate consent, scopes, quotas, and realistic failures in a configured test account |
| Google Fonts are optional network resources | Self-host licensed font assets if fully offline typography is needed |

No production-readiness or comprehensive accessibility certification is claimed by the current tests.
