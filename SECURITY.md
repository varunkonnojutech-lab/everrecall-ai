# Security Policy

## Reporting a vulnerability

Please report security issues **privately** — do not open a public issue for a
vulnerability. Use GitHub's private vulnerability reporting on this repository
(**Security → Report a vulnerability**), or contact the maintainer through their
GitHub profile.

You can expect an acknowledgement within a few days. Please include steps to reproduce
and the affected component (API, web, Docker setup) when you can.

## Scope and posture of this demo

EverRecall is a local-first demo, and its security posture follows from that design:

- **No real user data.** The shipped build runs on simulated Meta providers and a
  seeded, fictional demo day. It processes no real camera feeds and stores no real
  personal information.
- **Local storage only.** Events live in a local SQLite file (or a Docker volume);
  there is no account system, cloud sync, or telemetry.
- **No bundled secrets.** The repository contains no API keys. The Meta Model API key
  is supplied at runtime via the `MetaModelApi` configuration section or environment
  variables (`MetaModelApi__ApiKey`); `.env` files are gitignored.
- **Privacy control is built in.** `POST /api/memory/forget` physically deletes recent
  events — forgetting is a designed feature, not an afterthought.

## Supported versions

The latest `main` branch is the supported version. Fixes land there first.
