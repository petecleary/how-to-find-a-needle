## What it is

[Pedagogy](term:pedagogy): the checked answer, explained for an [audience](term:audience). A second LLM call turns _what to choose_ into _why_, using the ontology's definitions and labels, so the shopper can make the next decision alone.

## How it works

1. **Answer.** Exactly as Stage 6: evidence, prompt, stream, validate.
2. **Choose the words.** For a novice, everyday [alternative labels](term:alt-label) such as "power brick"; for an expert, preferred labels and spec terms.
3. **Explain.** Five fixed headings: Decision, Concepts, Near miss, Rule of thumb, Next step.
4. **Validate.** The Decision should be one compatible product, and the Near miss an incompatible one.

## What to look for

**GQ-01** for a novice, with **Apply pedagogy** off: a fair [baseline explanation](term:baseline-explanation). Turn it on: the same facts become a decision, the concepts behind it, and the [near miss](term:near-miss) as a counter-example.

## Strength

Explanation is a design choice. The toggle changes only the system prompt, so the difference on screen is what the teaching design adds.

## Failure mode

Two LLM calls make it the slowest stage. The structure can be checked, but understanding can't: an explanation can pass every check and still be muddled.

## Try this

Switch the audience to **expert** and compare: the product and the reasons stay the same, the words change. Then read both prompts in **Under the hood**.

## Read the decision

[ADR-0017 · Pedagogy engine](adr:0017-pedagogy-engine)
