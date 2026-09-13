# ADR-0001: Record architecture decisions

- **Status:** Proposed
- **Date:** 2026-09-13
- **Related:** [roadmap.md](roadmap.md)

## Context

This repo supports a developer talk and is meant to be learned from. The build makes a lot of decisions: search techniques, data modelling, AI hosting, UI structure. The reasons behind them are as valuable to learners as the code.

While we build, decisions will change as we learn (for example, the BGE-M3 verification in [ADR-0012](0012-bge-m3-dense-and-sparse.md)). Publishing half-settled decisions would confuse learners.

The repo is a single finished codebase on `main`. Learners do not step through branches or commits, so ADRs are the one place the reasoning is recorded.

## Decision

1. We use a lightweight [MADR](https://adr.github.io/madr/)-style format: **Context / Decision / Consequences / Alternatives considered / Teaching notes**. The template is in [README.md](README.md).
2. Working ADRs live in `docs/adr/`, which is **gitignored**. They stay private until the build is complete.
3. Every ADR has a **Teaching notes** section. When the build is complete (roadmap Phase 5), we write public, learner-facing ADRs from these notes and commit them to a separate public location (to be decided in Phase 5).
4. We change an ADR in place while it is **Proposed**. Once it is **Accepted** (implemented and verified), a change of direction gets a new ADR that supersedes it.
5. An ADR moves to **Accepted** when its roadmap acceptance criteria pass.

## Consequences

- Reasoning is captured while it is fresh, without exposing unfinished thinking.
- There is some duplicated effort in Phase 5 to produce the public versions. This is deliberate: public ADRs are written for learners, while these are written for the builders.
- Because `docs/adr/` is gitignored, **these files are not backed up by git**. Keep a copy elsewhere, or reconsider before Phase 5 if that becomes a risk.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Public ADRs from day one | Learners would see decisions that later change; the build is still exploratory |
| No ADRs, only a README | Loses the "why", which is the most transferable part of the talk |
| Heavyweight template (e.g. full arc42) | Too much ceremony for a teaching repo |

## Teaching notes

- ADRs capture *why* a decision was made, which code can't show.
- A good ADR lists the alternatives honestly, including ones that would be right in a different context.
