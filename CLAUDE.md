# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

TauronApp is a Polish electricity billing calculator based on TAURON tariffs. It helps users calculate energy costs under the various TAURON Group G tariff plans (G11, G12, G12w, G13, G13s) for residential customers. The project is in an early/planning stage — no application code exists yet.

## Development Workflow

This project uses **OpenSpec** for spec-driven development. New features follow this flow:

1. **Propose**: `/opsx:propose` — describe what to build; generates `proposal.md`, `design.md`, and `tasks.md` under `openspec/changes/<name>/`
2. **Implement**: `/opsx:apply` — work through tasks from the generated `tasks.md`
3. **Archive**: `/opsx:archive` — finalize and archive a completed change

OpenSpec config is at `openspec/config.yaml`. Change artifacts live at `openspec/changes/<name>/`.

### Branching & Pull Requests

Every change must be implemented on its own branch and merged into `main` via a pull request. Never commit directly to `main`. Branch names should reflect the change (e.g. `feature/tariff-calculator`, `fix/g12-zone-calc`). Each PR requires the CI check to pass and a codeowner approval before merging.

## Code Quality

Before committing any C# changes, run `dotnet format` on the solution to ensure consistent formatting:

```bash
dotnet format src/TauronApp.slnx
```

## Domain Knowledge: TAURON Tariffs

All 2026 tariff rates, time zones, and fee structures for Group G plans (G11, G12, G12w, G13, G13s) are documented in [`tarrifs/tauron_taryfy_2026.md`](tarrifs/tauron_taryfy_2026.md).
