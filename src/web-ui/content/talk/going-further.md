Each stage's own **Going further** tab holds where that technique goes next. These are the ones that belong to no single stage: the work that sits around the pipeline rather than inside it.

| Where            | Topic                                                 | Why a developer meets it                                                                                |
| ---------------- | ----------------------------------------------------- | ------------------------------------------------------------------------------------------------------- |
| Before retrieval | Query understanding and intent routing                | Conversational queries carry noise and several intents; Stage 5's label matcher is the simplest version |
| Before retrieval | Language detection                                    | Choosing the analyser or the labels to search with, before searching                                    |
| Before retrieval | LLM query rewriting                                   | Turning an exploratory question into concrete searches: more recall, less inspectable                   |
| Watching it work | Zero-result, click and abandonment logs               | The queries that failed are the ones no golden query thought to ask                                     |
| Proving a change | A/B tests and interleaving                            | Golden queries say a change is correct; only real traffic says it helped                                |
| Keeping it fresh | Indexing pipelines, re-embedding, ontology versioning | Every structure built today has to be rebuilt when the model or the catalogue moves                     |
| Who is asking    | Personalisation and permission-aware search           | Relevance depends on the person — and results must never include what they aren't allowed to see        |
