<div align="center">

<img src="docs/assets/dayweave-banner.svg" alt="Dayweave — Your days, connected." width="100%" />

# Dayweave

**Your days, connected.**

A personal activity journal for your code, music, discoveries, and everyday moments.

[![CI](https://github.com/KhushiShah-swe/Dayweave/actions/workflows/ci.yml/badge.svg)](https://github.com/KhushiShah-swe/Dayweave/actions/workflows/ci.yml)
![React](https://img.shields.io/badge/React-18-294b3d?style=flat-square)
![TypeScript](https://img.shields.io/badge/TypeScript-strict-54794e?style=flat-square)
![.NET](https://img.shields.io/badge/.NET-8-294b3d?style=flat-square)
![SQLite](https://img.shields.io/badge/SQLite-EF_Core-54794e?style=flat-square)

[Features](#features) · [Getting started](#getting-started) · [Technology](#technology) · [Documentation](#documentation)

</div>

## Overview

Dayweave brings activity from GitHub, Spotify, and YouTube together with moments you add yourself. Browse everything in one chronological timeline, revisit a particular day, and see how your activity changes throughout the week.

The dashboard summarizes your entries, active days, and activity sources. Search and date filters help you find specific moments, while connected accounts keep supported activity in sync. Each signed-in user has a separate timeline and provider connections.

## Features

| Feature | Description |
| --- | --- |
| Activity dashboard | View entry counts, active days, recent moments, source breakdowns, and a seven-day activity chart. |
| Unified timeline | Browse activity grouped by local calendar date, with newest-first or oldest-first sorting. |
| Personal moments | Create, edit, and delete manual entries with a title, description, date, type, and category. |
| Search and filters | Combine text search, activity source, and date range to find relevant entries. |
| Connected accounts | Sign in with GitHub and connect Spotify or YouTube through OAuth. |
| Activity sync | Import supported provider activity on demand or through the hourly background sync. |
| JSON export | Download the entries matching your current timeline filters. |
| Sample data mode | Explore the app with fictional activity and save changes in your browser. |
| Responsive interface | Use layouts for desktop and mobile, keyboard focus indicators, and reduced-motion support. |

## Integrations

| Source | Timeline activity |
| --- | --- |
| GitHub | Supported events from the account's recent activity response. |
| Spotify | Recently played tracks. |
| YouTube | Up to ten liked-video playlist items per sync. |
| Manual entries | Notes, achievements, and other moments you add directly. |

Provider connections require configured OAuth credentials and account consent. Available history depends on each provider's API response limits, permissions, and quotas. Imported entries can be deleted; editing is available for manual entries.

## Technology

| Layer | Tools |
| --- | --- |
| Frontend | React 18, TypeScript, React Router, Vite, CSS |
| Client state and requests | React Context, Axios, localStorage |
| Backend | C#, .NET 8 Minimal APIs, JWT bearer authentication, OAuth |
| Database | SQLite, Entity Framework Core 9, migrations |
| Synchronization | Typed HTTP clients, hosted background service, shared sync coordinator |
| Testing | Vitest, React Testing Library, xUnit, Moq, WebApplicationFactory |
| CI | GitHub Actions |

## Getting started

### Prerequisites

- **Node.js 22.13 or later** and npm.
- **.NET 8 SDK** to run the API.
- A **GitHub OAuth application** for account sign-in.
- Optional **Spotify** and **Google/YouTube** OAuth credentials for those connections.

The API uses SQLite, so no separate database server is required. Sample data mode only needs Node.js and npm.

### Clone the repository

```sh
git clone https://github.com/KhushiShah-swe/Dayweave.git
cd Dayweave
```

### Start the frontend

```sh
cd personal-timeline-client
npm ci
npm run dev
```

Open **[http://localhost:3000](http://localhost:3000)**.

Choose **Explore the demo** to use fictional sample activity immediately. You can add, edit, filter, delete, and export entries. Changes stay in the current browser and remain separate from signed-in account data.

To use connected accounts, keep the frontend running and complete the API setup below.

### Configure and start the API

In another terminal, from the repository root:

```sh
dotnet restore PersonalTimeline.sln
cd PersonalTimeline.API

dotnet user-secrets set "Jwt:Key" "YOUR_RANDOM_SECRET_AT_LEAST_32_BYTES"
dotnet user-secrets set "Authentication:GitHub:ClientId" "YOUR_GITHUB_CLIENT_ID"
dotnet user-secrets set "Authentication:GitHub:ClientSecret" "YOUR_GITHUB_CLIENT_SECRET"

dotnet run
```

Replace the placeholders with your own values. Generate a random JWT signing key of at least 32 bytes. Store credentials with user secrets or server environment variables; keep them out of committed files and frontend configuration.

For the default local setup, register **`http://localhost:5167/signin-github`** as the GitHub OAuth callback URL.

The API applies its committed migrations at startup and creates the local SQLite database. Return to the frontend and choose **Sign in with GitHub**.

| Service | Local URL |
| --- | --- |
| Frontend | [http://localhost:3000](http://localhost:3000) |
| API | [http://localhost:5167](http://localhost:5167) |
| Health check | [http://localhost:5167/health](http://localhost:5167/health) |
| Swagger, in Development | [http://localhost:5167/swagger](http://localhost:5167/swagger) |

The frontend uses `http://localhost:5167` as its default API origin. To change it, set `VITE_API_URL` in `personal-timeline-client/.env.local`. Update the API's `Frontend:Url` setting and OAuth callback configuration when changing application origins.

See the [setup guide](docs/SETUP.md) for Spotify and YouTube credentials, callback URLs, configuration options, and troubleshooting.

## Data and synchronization

- Account entries are stored in SQLite and scoped to the authenticated user.
- Imported activity is deduplicated per user, provider, and external event.
- Manual entries are validated by the API before they are saved.
- Dates are stored and returned in UTC; the interface displays and filters by local calendar dates.
- Manual sync and the hourly worker coordinate database writes. The API must be running for scheduled syncs.
- Disconnecting a provider removes its connection and imported entries while keeping manual entries.
- Sample data and edits are stored in the current browser and can be reset from **Connections**.

## Testing and builds

Run frontend tests and create the production client build:

```sh
cd personal-timeline-client
npm test
npm run build
```

The build performs TypeScript checking and writes the client assets to `personal-timeline-client/dist/`.

Run backend tests from the repository root:

```sh
dotnet restore PersonalTimeline.sln
dotnet test PersonalTimeline.sln --configuration Release
```

Tests cover timeline editing and persistence, search and filters, session expiry, input validation, account isolation, OAuth state handling, and imported-event deduplication.

[GitHub Actions](https://github.com/KhushiShah-swe/Dayweave/actions/workflows/ci.yml) runs the frontend tests, client build, and backend tests on pushes to `main` and pull requests. It also uploads the client build and API test results as workflow artifacts. Live provider authorization flows require separate checks with configured accounts.

## Project structure

| Path | Contents |
| --- | --- |
| `personal-timeline-client/src/pages/` | Dashboard, timeline, connections, login, and callback views |
| `personal-timeline-client/src/components/` | Navigation, entry cards, dialogs, and shared interface elements |
| `personal-timeline-client/src/context/` | Authentication and timeline state |
| `personal-timeline-client/src/lib/` | API client, sample data, filtering, dates, and session utilities |
| `PersonalTimeline.API/` | API endpoints, models, database migrations, authentication, and provider services |
| `PersonalTimeline.Tests/` | Backend unit and integration tests |
| `.github/workflows/` | Continuous integration |
| `docs/` | Setup and technical documentation |

## Documentation

- [Setup guide](docs/SETUP.md): configuration, OAuth callbacks, API routes, and troubleshooting.
- [Architecture](docs/ARCHITECTURE.md): data flow, authentication, persistence, and deployment constraints.
- [Contributing](CONTRIBUTING.md): development workflow and contribution guidelines.

## Author

**[Khushi Shah](https://github.com/KhushiShah-swe)**
