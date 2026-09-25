# From PersonalTimeline to Dayweave

## Starting point

The supplied project contained a React/TypeScript client, a .NET 8 Minimal API, SQLite/EF Core persistence, GitHub/Spotify/YouTube adapters, a background sync worker, and initial tests. The accompanying screen recording demonstrated the original application. The archive's README credited Jayesh Patil; that attribution is preserved in [AUTHORS.md](../AUTHORS.md).

## Portfolio enhancement

This edition is maintained in Khushi Shah's portfolio repository. It develops the inherited foundation through these concrete improvements:

| Area | Change |
| --- | --- |
| Product identity | Dayweave branding, a coherent visual system, original SVG repository banner |
| Reviewer experience | A working browser-only demo without provider credentials |
| Interaction design | Overview, grouped timeline, combined filters, date sorting, modal forms, export, explicit destructive-action confirmation |
| State handling | Persisted demo changes, expired-session rejection, unauthorized-response handling, stale fetch guards, and visible failures |
| API validation | Shared validation for create/update and safe external URL handling |
| Account isolation | User-scoped duplicate prevention and consistent authorization/ownership checks |
| OAuth correlation | Single-use, expiring, user/provider/browser-bound state for secondary providers |
| Synchronization | Separate typed provider clients, serialized worker/manual operations, error isolation, and truthful provider failure responses |
| Repository quality | Corrected tests, CI, setup guide, architecture notes, contribution templates, and documented limitations |
| Source hygiene | Excluded local credentials, the working database, nested Git state, dependencies, generated output, and large video media |

The existing C# project names remain unchanged so the solution, migrations, and provider code remain recognizable. The repo slug stays `PersonalTimeline`; the application and documentation use Dayweave.

## Validation

Local verification on September 25, 2026: **9 client tests passed**, **11 API tests passed**, the client production build succeeded, and `npm audit` reported **0 vulnerabilities** for the updated lockfile.

The client suite covers the demo's create/edit/delete path, persistence across mounts, canceling a delete, source/search filtering, read-only imported events, explicit sample connection labels, URL schemes, date filtering, and session expiry.

The API suite covers anonymous authorization rejection, per-user reads/mutations, invalid updates, forced manual-source creation, forged OAuth state, UTC serialization, and relational uniqueness. Provider parsing is tested with mocked HTTP responses. No real provider credentials or the uploaded SQLite database are used.

The GitHub Actions workflow runs the client tests and production build plus the .NET test suite. Its public run is the authoritative record for the current commit.

A cloud-browser policy prevented local preview access during preparation. **Desktop/mobile screenshots, visual layout review, and live provider OAuth/sync have not been verified by this workflow.** The repository banner is an identity graphic, not a screenshot. Before recording a new walkthrough, manually check the actual app at desktop and phone widths, keyboard navigation, dialogs, and configured provider consent flows.

## Interview discussion prompts

- Why keep demo and authenticated data paths behind the same context interface?
- How can one user's deduplication key accidentally block another user's import?
- Why is a raw user ID unsafe as OAuth state, and what additional browser binding is needed?
- Why does the UI group by local date while the API stores and returns UTC?
- What changes when this SQLite prototype grows into a multi-instance service?
- Which parts are inherited foundation, and which improvements belong to the Dayweave portfolio edition?
