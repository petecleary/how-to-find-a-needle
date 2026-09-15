What the talk discusses but doesn't build, and where each topic sits in the pipeline.

| Where            | Topic                                                                             | Why a developer meets it                                                                            |
| ---------------- | --------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| Before retrieval | Query understanding and intent routing                                            | Conversational queries carry noise and several intents; routing them first stops irrelevant matches |
| Before retrieval | Language detection                                                                | Choosing language-specific analysers or labels before searching                                     |
| Before retrieval | LLM query rewriting                                                               | Turning an exploratory question into concrete searches: more recall, less inspectable               |
| Retrieval        | [Chunking](term:chunking)                                                         | Manuals and PDFs aren't product rows; how you split them decides what can be found                  |
| Retrieval        | [Learned sparse](term:learned-sparse) and multilingual models such as BGE-M3      | Dense and sparse vectors in one pass, and full-sentence cross-language search                       |
| Retrieval        | The vector landscape                                                              | Dedicated vector databases, index choices, and filtering at scale                                   |
| Ranking          | [Re-ranking](term:re-ranking) with [cross-encoders](term:cross-encoder)           | Precision on the top candidates before they reach a user or an LLM                                  |
| Knowledge        | [OWL](term:owl), [SHACL](term:shacl) and [knowledge graphs](term:knowledge-graph) | Formal inference, validating data, and multi-hop relationships                                      |
| Evaluation       | RAG metrics                                                                       | Measuring answers (faithfulness, context recall) as well as rankings                                |
| Explanation      | Adaptive, multi-turn tutoring                                                     | Teaching over a conversation instead of in one answer                                               |
