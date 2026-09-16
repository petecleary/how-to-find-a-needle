# ADR-0001: Record architecture decisions

- **Status:** Proposed
- **Date:** 2026-09-13
- **Related:** [roadmap.md](roadmap.md)

## Context

This repo supports a developer talk and is meant to be learned from. The build makes a lot of decisions: search techniques, data modelling, AI hosting, UI structure. The reasons behind them are as valuable to learners as the code.

While we build, decisions will change as we learn (for example, the device-name trap found while building Phase 2, recorded in [ADR-0013](0013-domain-ontology-and-compatibility.md)). Publishing half-settled decisions would confuse learners.

The repo is a single finished codebase on `main`. Learners do not step through branches or commits, so ADRs are the one place the reasoning is recorded.

## Decision

1. We use a lightweight [MADR](https://adr.github.io/madr/)-style format: **Context / Decision / Consequences / Alternatives considered / Teaching notes**. The template is in [README.md](README.md).
2. Working ADRs live in `docs/adr/` and are committed on the **build branch**. They are not presented to learners until the build is complete.
3. Every ADR has a **Teaching notes** section. When the build is complete (roadmap Phase 5), we write public, learner-facing ADRs from these notes and commit them to `docs/decisions/`, with the same numbers and file names (decided in Phase 5). The working ADRs are not merged to `main`.
4. We change an ADR in place while it is **Proposed**. Once it is **Accepted** (implemented and verified), a change of direction is recorded in a new ADR.
   - If the new ADR replaces the whole decision, the old one is marked **Superseded**.
   - If it changes only part of it (for example, removing a stage or renumbering stages), the old ADR is **amended in place** so it stays a correct reference, and its status line links the ADR that caused the change.
   - If the amendment changes what the code does, the ADR goes back to **Proposed** until that is built and verified. If only names, numbers or comments change, it stays **Accepted**.
   - A proposal we decide not to build is marked **Rejected** and kept, with the reason.
5. An ADR moves to **Accepted** when its roadmap acceptance criteria pass.

## Consequences

- Reasoning is captured while it is fresh, without exposing unfinished thinking.
- There is some duplicated effort in Phase 5 to produce the public versions. This is deliberate: public ADRs are written for learners, while these are written for the builders.
- The working ADRs are versioned on the build branch, so they are backed up and reviewable. Before the final merge to `main`, decide whether they stay alongside the public versions or are replaced by them.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Public ADRs from day one | Learners would see decisions that later change; the build is still exploratory |
| No ADRs, only a README | Loses the "why", which is the most transferable part of the talk |
| Heavyweight template (e.g. full arc42) | Too much ceremony for a teaching repo |

## Teaching notes

- ADRs capture *why* a decision was made, which code can't show.
- A good ADR lists the alternatives honestly, including ones that would be right in a different context.
