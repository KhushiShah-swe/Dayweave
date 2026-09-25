# Running Dayweave

## 1. Browser demo

Install Node.js 22.13+ (CI uses Node 22). From the repository root:

```sh
cd personal-timeline-client
npm ci
npm run dev
```

Visit http://localhost:3000 and choose **Explore the demo**. All sample events are fictional. Personal demo edits stay in the current browser. They are not imported into a real account when signing in. Export JSON before resetting the demo if you want to keep it.

The Vite development server binds to localhost. For deliberate access from another device, use `npm run dev -- --host 0.0.0.0` on a trusted development network.

## 2. API prerequisites

- .NET 8 SDK
- A GitHub OAuth application for sign-in
- Optional Spotify developer app and Google OAuth client with YouTube Data API access

The API uses a local SQLite file. No external database server or paid cloud service is required.

## 3. Store local secrets

From the repository root:

```sh
dotnet restore PersonalTimeline.sln
cd PersonalTimeline.API
dotnet user-secrets set "Jwt:Key" "PASTE_A_NEW_RANDOM_SECRET_OF_AT_LEAST_32_BYTES"
dotnet user-secrets set "Authentication:GitHub:ClientId" "YOUR_GITHUB_CLIENT_ID"
dotnet user-secrets set "Authentication:GitHub:ClientSecret" "YOUR_GITHUB_CLIENT_SECRET"
```

Use a newly generated random JWT key, not the literal placeholder. `UserSecretsId` is already configured. User secrets stay outside the repository and are for local development; they are not an encrypted production vault. Never put provider secrets in Vite environment variables or commit a development configuration file.

An alternative is to copy `appsettings.Example.json` to `appsettings.Development.json` and fill its placeholders. The development file is ignored by Git. Do not use the original upload's credentials or database for a public deployment.

Optional connections:

```sh
dotnet user-secrets set "Authentication:Spotify:ClientId" "YOUR_SPOTIFY_CLIENT_ID"
dotnet user-secrets set "Authentication:Spotify:ClientSecret" "YOUR_SPOTIFY_CLIENT_SECRET"
dotnet user-secrets set "Authentication:YouTube:ClientId" "YOUR_GOOGLE_CLIENT_ID"
dotnet user-secrets set "Authentication:YouTube:ClientSecret" "YOUR_GOOGLE_CLIENT_SECRET"
```

GitHub sign-in credentials are only needed for real sign-in. A valid `Jwt:Key` is enough to start the API and check its health. Unconfigured providers return a setup error instead of using embedded credentials.

## 4. Match callback URLs

| App | Local callback registered with the provider |
| --- | --- |
| GitHub | `http://localhost:5167/signin-github` |
| Spotify | `http://localhost:5167/api/connect/Spotify/callback` |
| Google / YouTube | `http://localhost:5167/api/connect/YouTube/callback` |

These must match the actual application URLs and the provider's accepted redirect URI policy. Some providers restrict HTTP loopback hostnames or development-mode users; use an accepted loopback address or HTTPS development setup consistently across the provider, API configuration, and browser. Confirm provider console requirements when creating the OAuth app.

The internal `/signin-github-callback` endpoint follows the GitHub middleware callback. It is **not** the provider's registered callback.

If your API and frontend use different origins, update `Frontend:Url`, `Authentication:<Provider>:RedirectUri`, and the client API setting together. CORS permits the single configured frontend origin. Provider linking uses an HttpOnly correlation cookie with SameSite=Lax; use the same site for frontend/API in development, and design a suitable cookie/HTTPS setup before cross-site hosting.

## 5. Start the API

From `PersonalTimeline.API/`:

```sh
dotnet run
```

- API: http://localhost:5167
- Health: http://localhost:5167/health
- Swagger in Development: http://localhost:5167/swagger

The application applies the **committed migrations** at startup. Do not create another `InitialCreate`. The ignored `dayweave.db` file is created locally. Back up an existing database before applying migrations to important data.

## 6. Point the client at the API

In another terminal, from `personal-timeline-client/`, copy `.env.example` to `.env.local` if the default origin differs.

```dotenv
VITE_API_URL=http://localhost:5167
```

```sh
npm ci
npm run dev
```

Choose **Sign in with GitHub**, then connect providers from **Connections**. A sync runs on demand and during the API's hourly worker cycle. The API must remain running for scheduled syncs.

## API map

| Method | Route | Behavior |
| --- | --- | --- |
| GET | `/health` | Public readiness response after startup migrations |
| GET | `/login/github` | Starts GitHub sign-in |
| GET | `/api/profile` | Authenticated profile |
| GET | `/api/timeline` | Current user's timeline |
| POST | `/api/timeline` | Creates a manual entry; ignores a supplied source |
| PUT | `/api/timeline/{id}` | Updates an owned manual entry |
| DELETE | `/api/timeline/{id}` | Deletes an owned entry |
| GET | `/api/connections` | Connection metadata, excluding tokens |
| GET | `/api/connect/{provider}` | Begins Spotify/YouTube authorization |
| GET | `/api/connect/{provider}/callback` | Validates browser-bound OAuth state |
| POST | `/api/sync/{provider}` | Synchronizes one connected provider |
| DELETE | `/api/connections/{provider}` | Removes connection and that provider's imported entries |

All `/api` application endpoints require bearer authentication except the explicitly validated provider callback. Use ISO 8601 UTC timestamps ending in `Z` for manual-entry dates.

Example manual entry body:

```json
{
  "title": "Shipped a useful improvement",
  "description": "Made the timeline easier to explore.",
  "eventDate": "2026-09-24T15:30:00Z",
  "entryType": "Achievement",
  "category": "Development"
}
```

## Troubleshooting

| Symptom | Check |
| --- | --- |
| API exits with a Jwt:Key error | Configure a random key of at least 32 bytes in the active environment |
| GitHub login reports 503 | Set GitHub ClientId and ClientSecret on the API |
| OAuth returns an error | Match callback URLs, allowed developer users, cookie origins, and provider scopes |
| An old browser session stops working | Sign in again; bearer tokens expire after eight hours |
| No new sync entries | Check provider history windows and whether the items were already imported |
| Spotify/YouTube sync fails | Review server configuration, quota, consent, and account availability; errors do not mean zero new items |
| Demo cannot save | Allow browser storage or export before clearing/resetting it |
| Deep links fail on static hosting | Configure an SPA fallback to `index.html`; the router uses browser history |

The app is not deployed by the CI workflow. Production hosting, HTTPS, persistent storage, and token protection need a separate deployment plan.
