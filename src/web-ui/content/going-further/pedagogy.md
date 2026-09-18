Stage 7 explains in one turn, to an [audience](term:audience) you picked from a dropdown. A real deployment — a support bot, a sales assistant, an internal help desk — rarely gets to stop there: the person asking has a history, and what they need next depends on what was already said.

### Personalising across a conversation

[Adaptive tutoring](term:adaptive-tutoring) is the research name for keeping a **user model** across turns: what this person already knows, what they've just been told, and what they got wrong or pushed back on. The next explanation is shaped by it. The move that makes it more than a longer answer is the **check** — asking a question back, and using the reply to decide whether to go deeper or move on. The same pattern runs any company's chat product, not only a classroom: a support assistant that remembers a customer asked about returns yesterday, or an internal tool that already knows which team someone is on, is doing this.

Our audience setting is the one-turn version: three fixed levels, chosen for the user rather than inferred. The natural next step — and usually the first thing a production assistant adds — is **conversation and user history as input**: recent chat turns, a stated role, past purchases or tickets, so the level is inferred from evidence and revised as the exchange continues, instead of picked once from a dropdown.

### What the ontology already gives you

Most of the machinery for adapting is already here. Adapting to an audience is largely **vocabulary**, and the ontology holds it: everyday [alternative labels](term:alt-label) for a novice ("power brick"), preferred labels and spec terms for an expert, and [definitions](term:skos-concept) to build a concept explanation on. Adding conversation state — chat history, a user's prior sessions, account or entitlement data from a CRM — does not mean adding a second knowledge source; it decides which audience level and which of the ontology's labels to reach for ([ADR-0017 · Pedagogy engine](adr:0017-pedagogy-engine)).

### Judging an explanation

Nothing in this repository scores the teaching. Compare the [baseline explanation](term:baseline-explanation) with the pedagogical one and you are the judge — which is the honest position, because the useful measures are human: could the reader make the _next_ decision without asking? Did the [near miss](term:near-miss) land as a counter-example, or as noise?

The research answer is to test the reader afterwards, not to score the text. In a company setting that usually means a proxy instead: did the customer resolve the query without escalating, did they ask the same question again, did the conversation end in a purchase or a ticket close. That is a long way from a metric you can run in CI, and it is worth knowing that the gap is real rather than pretending a number closes it.
