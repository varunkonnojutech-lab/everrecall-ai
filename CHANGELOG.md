# Changelog

All notable changes to EverRecall are documented here.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] — 2026-10-25

### Added

- ASP.NET Core 8 API with an episodic event graph (EF Core + SQLite): who did what,
  to which object, where, and when — each event with confidence and an evidence note.
- Recall Q&A via `POST /api/ask`: location and yes/no questions answered from stored
  events only, with an honest "I don't have a memory of that" for anything unseen.
- Checklist Guardian (`GET /api/guardian/status`, `POST /api/guardian/evaluate`):
  medication, stove-off, door-locked, and pet-fed checks, plus a stove-on-unattended
  hazard rule (over 30 minutes).
- Privacy by design: `POST /api/memory/forget` physically deletes the last N minutes
  of events.
- Meta provider abstractions — `IMetaVisionProvider` (SAM 3.1), `IMetaMemoryReasoner`
  (Muse Wonder), `IMetaImageAnnotator` (Muse Image), `IMetaTranscriber` (Muse Voice
  Transcribe) — with working offline simulated implementations and config-gated HTTP
  implementations that activate when a Meta Model API key is configured.
- `POST /api/ingest/events` pipeline endpoint converting SAM-style detection batches
  into memories; `POST /api/annotate` for evidence-frame annotation.
- React 18 + TypeScript + Vite web app: Ask, day timeline, Guardian status, Forget.
- Seeded demo day (9 events) so the full loop runs with zero setup and zero keys.
- Docker Compose one-command run (API :8080 with Swagger, web :3000).
- xUnit test suite (13 tests) and a 30-scenario evaluation runner (30/30, mean
  timestamp error 0.0s).
- GitHub Actions CI: backend build + test + evaluation, frontend build.
- VS Code F5 launch configuration, tasks, and a devcontainer for one-click setup.
