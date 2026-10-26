# Contributing to EverRecall

Thanks for your interest in improving EverRecall. This is a small, focused codebase —
keep changes small, grounded, and honest (no invented behavior, no fake integrations).

## Prerequisites

- .NET SDK 8.0+
- Node.js 20+ and npm
- Docker Desktop (optional — only for the Compose quickstart)
- VS Code with the C# Dev Kit extension (optional — only for the F5 experience)

## Build, test, run

```bash
# Backend
dotnet restore EverRecall.sln
dotnet build EverRecall.sln -c Release
dotnet test EverRecall.sln -c Release

# Evaluation (30 scenarios — must stay 30/30)
dotnet run --project tools/EverRecall.Evaluation -c Release

# Frontend
cd web && npm ci && npm run build

# Everything at once
docker compose up --build   # web :3000, API :8080, Swagger :8080/swagger
```

## Ground rules

- **Event graph first.** Answers must stay grounded in stored `MemoryEvent`s — a change
  that lets the engine answer without an event is a regression, not a feature.
- **Honest providers.** Simulated outputs must stay labelled `SIMULATED`. Never present a
  placeholder or offline result as a real Meta model response.
- **No secrets.** Never commit a real API key. Configuration goes through the
  `MetaModelApi` section / environment variables (see `appsettings.Example.json`).
- **Keep the demo day stable.** Seed data is shared by the README examples, the tests,
  and the evaluation ground truth — changing it means updating all three.

## Pull requests

- One topic per PR; describe what changed and why.
- Backend changes: `dotnet build -c Release` clean (0 warnings, 0 errors), `dotnet test`
  green, evaluation still 30/30.
- Frontend changes: `npm run build` green.
- Update the README / CHANGELOG when behavior, endpoints, or configuration change.
