# ADR-0001: Record architecture decisions

- **Status:** Accepted
- **Area:** Foundation
- **Related:** every other decision in this folder

## Context

This repository supports the talk *How to Find a Needle*. It builds one search pipeline in seven stages, from a SQL `WHERE` clause to an LLM that explains its answer. The code shows *what* each stage does. It can't show *why* it was built that way, what else was considered, or what went wrong along the way. For a developer new to search, the reasons are often the most useful part.

The repository is one finished codebase. There are no per-stage branches or tags to step through, so the reasoning needs a home of its own.

## Decision

- Each significant decision gets a short **Architecture Decision Record** (ADR) in `docs/decisions/`, numbered `NNNN-kebab-case-title.md`.
- Every record uses the same headings: **Context**, **Decision**, **Consequences**, **Alternatives considered** and **What to take away**.
- **Numbers identify decisions, not stages.** Stage 5 (Ontology) is ADR-0013; the index in [README.md](README.md) maps stages to records.
- **A status line on every record:** *Accepted* (built and verified) or *Rejected* (considered, not built, kept with the reason).
- **Alternatives are listed honestly**, including ones that would be the right choice in a different project. The "Why not" column always says *for this repository*.
- The web UI renders these files on its **Decisions** page, straight from this folder ([ADR-0014](0014-web-ui-architecture.md)).

### How these records were written

While the repository was being built, a separate set of working records captured every decision as it was made, including the ones that changed. When the build was complete, those working notes were rewritten as the records in this folder, for learners rather than for builders. The build history stays out of the way. The findings that taught something stay in, under **What to take away**.

## Consequences

- A learner can read why a stage works the way it does without reading every commit.
- The records describe the finished repository. If you change the code, change the record too, or add a new one that supersedes it.
- Writing a second, learner-facing version took extra effort. It kept half-settled decisions out of view while the build was still changing its mind.

## Alternatives considered

| Option | Why not (for this repository) |
|---|---|
| No ADRs, only a README | Loses the "why", which is the most transferable part of the talk |
| Publish the working records as they were | Full of build history and decisions that later changed; confusing to someone learning |
| A heavyweight template (e.g. arc42) | Too much ceremony for a teaching repository |
| Decisions as code comments only | Comments explain a line or a class; a decision often spans the API, the data and the UI |

## What to take away

- An ADR records *why*, which is the one thing code can't show.
- A good ADR lists the options you didn't choose, and says when they would be right.
- Record what you learned while building, not just what you planned. Several of the most useful lessons in these records came from tests failing.
