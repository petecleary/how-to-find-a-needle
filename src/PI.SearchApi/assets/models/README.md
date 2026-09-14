# Local Models (ONNX)

The API generates embeddings locally with ONNX Runtime — no external API calls. The model files are large, so they are **not committed**. Download them into this folder before running the app.

The commands below use the [Hugging Face CLI](https://huggingface.co/docs/huggingface_hub/guides/cli) and should be run from this directory (`src/PI.SearchApi/assets/models`).

## 1. Nomic Embed Text v1.5 — dense, 768 dimensions

Used by the **Vector**, **Hybrid** and **Ontology** stages.

- **Source:** [nomic-ai/nomic-embed-text-v1.5](https://huggingface.co/nomic-ai/nomic-embed-text-v1.5)
- **Files:** `onnx/model_int8.onnx`, `tokenizer.json`

```bash
huggingface-cli download nomic-ai/nomic-embed-text-v1.5 onnx/model_int8.onnx tokenizer.json --local-dir ./nomic
mv ./nomic/onnx/model_int8.onnx ./nomic/model_int8.onnx && rmdir ./nomic/onnx
```

## Expected layout

```text
assets/models/
  nomic/
    model_int8.onnx
    tokenizer.json
```
