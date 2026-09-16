Every catalogue has a needle: the one product that actually fits.

This talk builds a search pipeline over one small catalogue, one technique at a time. Each stage fixes a problem the one before it showed, and each fails in a new way.

- **Search** asks _what is relevant?_ [Structured](term:parameterised-sql) filters, [keyword](term:fts) search, [vector](term:embedding) search and [hybrid](term:hybrid-search) fusion.
- **Ontology** asks _how is it related and constrained?_ A [SKOS](term:skos) vocabulary and a few [domain rules](term:domain-rule).
- **Pedagogy** asks _how should I explain it?_ An LLM that answers from the evidence, then explains it for its audience.

Everything you see is live. The same query goes to each stage, and every tab shows what really ran.
