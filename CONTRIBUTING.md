# Contributing to Dayweave

Start with [README.md](README.md) and [docs/SETUP.md](docs/SETUP.md). Use the browser demo or synthetic test data while developing.

1. Create a focused branch describing the change.
2. Follow the existing TypeScript and C# structure. Keep provider-specific behavior in its adapter and shared validation in the API service layer.
3. Add behavior-focused tests for changes to authentication, data ownership, persistence, or external-provider parsing.
4. Run `npm test` and `npm run build` from `personal-timeline-client/`, plus `dotnet test PersonalTimeline.sln --configuration Release` from the root.
5. Explain the problem, resulting behavior, and validation in your pull request.

Never commit real provider secrets, access tokens, personal activity exports, databases, dependencies, or build output. Keep demo content clearly fictional. Preserve contributor attribution when updating documentation.
