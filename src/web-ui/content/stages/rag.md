## What it is

[Retrieval-augmented generation](term:rag): an [LLM](term:llm) answers the question in plain language, but only from the [evidence set](term:evidence-set) that Stage 5 found, with a [citation](term:citation) for every product it mentions.

## How it works

1. **Retrieve.** Run the whole Stage 5 pipeline. The results appear straight away.
2. **Choose the evidence.** Up to 5 compatible products, 3 [near misses](term:near-miss) with their reasons, and the concepts and rules behind the verdicts.
3. **Generate.** Send the rules and the evidence as a [prompt](term:prompt), and stream the answer back as [Server-Sent Events](term:server-sent-events).
4. **Validate.** When the text is complete, check every citation against the evidence, and look for an incompatible product being recommended.

## What to look for

**GQ-03**: the results are ready before the answer's [first token](term:time-to-first-token). The answer recommends the USB-C chargers and warns about the barrel charger, citing each one. Click a citation chip to see the product the model was given.

## Strength

The answer can be checked claim by claim. The model is given the negative evidence as well, so it warns about the near miss instead of recommending it.

## Failure mode

The LLM is still the least reliable part. A valid citation can sit next to a wrong detail: validation checks the form of an answer, not its truth. And a product left out of the evidence can't be mentioned at all.

## Try this

Open **Under the hood** and read the prompt: everything the model knew is there. Then ask about something the evidence can't answer, and look for the insufficient-evidence badge.

## Read the decision

[ADR-0016 · RAG grounding and citations](adr:0016-rag-grounding-and-citations)
