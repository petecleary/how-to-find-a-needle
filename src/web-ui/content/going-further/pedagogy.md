Stage 7 teaches in one answer, to an [audience](term:audience) you picked from a dropdown. Real teaching is a conversation, and the learner tells you who they are.

### Adaptive, multi-turn tutoring

[Adaptive tutoring](term:adaptive-tutoring) keeps a **learner model**: what this person already knows, what they've just been told, and what they got wrong. The next explanation is shaped by it. The move that makes it teaching rather than answering is the **check** — asking a question back, and using the answer to decide whether to go deeper or move on.

Our audience setting is the one-turn version of this: three fixed levels, chosen for the learner rather than by them. The next step is inferring the level from what they say, and revising it as the conversation goes on.

### What the ontology already gives you

Most of the machinery for adapting is already here. Adapting to an audience is largely **vocabulary**, and the ontology holds it: everyday [alternative labels](term:alt-label) for a novice ("power brick"), preferred labels and spec terms for an expert, and [definitions](term:skos-concept) to build a concept explanation on. Adding conversation state does not mean adding a second knowledge source ([ADR-0017 · Pedagogy engine](adr:0017-pedagogy-engine)).

### Judging an explanation

Nothing in this repository scores the teaching. Compare the [baseline explanation](term:baseline-explanation) with the pedagogical one and you are the judge — which is the honest position, because the useful measures are human: could the reader make the _next_ decision without asking? Did the [near miss](term:near-miss) land as a counter-example, or as noise?

The research answer is to test the reader afterwards, not to score the text. That is a long way from a metric you can run in CI, and it is worth knowing that the gap is real rather than pretending a number closes it.
