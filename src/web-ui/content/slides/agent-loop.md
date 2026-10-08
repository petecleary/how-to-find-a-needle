- In an [agent loop](term:agent-loop), the model **proposes** each tool call.
- A call is JSON: a **search** to run, or an **action** to take.
- **Every edge is a contract**, written once in the ontology's vocabulary.
- Check before it runs: _does this charger fit this laptop?_
- Rejected? The reason goes back to the model, and it plans again.

```text
Plan ──▶ Propose call ──▶ ◆ Ontology check ──▶ Execute ──▶ Observe
 ▲                        │
 └─ rejected, with reason ┘
```
