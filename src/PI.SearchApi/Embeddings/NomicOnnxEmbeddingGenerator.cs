using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.Tokenizers;

namespace PI.SearchApi.Embeddings;

// Embeddings — Nomic Embed Text v1.5 on ONNX Runtime
//
// What:     A local embedding model, step by step: a BERT WordPiece tokenizer turns text into
//           token IDs; the ONNX network turns each token into a 768-number vector in context;
//           mean pooling averages the token vectors into one; L2 normalisation scales it to
//           length 1 so cosine similarity compares only direction.
// Strength: Runs offline on a laptop, and every stage of "how an embedding is made" is visible
//           code rather than an API call.
// Failure:  Each step is a place bugs hide: a wrong tokenizer, a missing prefix or pooling over
//           padding all still produce plausible-looking numbers. Only a golden query notices.
// Decision: docs/decisions/0009-local-embeddings-onnx-runtime.md
public sealed class NomicOnnxEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    public const string ModelId = "nomic-embed-text-v1.5-int8";

    public const int Dimensions = 768;

    // BERT-family models were trained on at most 512 tokens, including the [CLS] and [SEP] markers.
    private const int MaxTokens = 512;

    private const string ModelFileName = "model_int8.onnx";
    private const string TokenizerFileName = "tokenizer.json";

    private readonly string _modelDirectory;
    private readonly ILogger<NomicOnnxEmbeddingGenerator> _logger;
    private readonly EmbeddingGeneratorMetadata _metadata;

    // Created on first use, once, and shared: InferenceSession.Run is thread-safe.
    private readonly Lazy<LoadedModel> _model;

    public NomicOnnxEmbeddingGenerator(string modelDirectory, ILogger<NomicOnnxEmbeddingGenerator> logger)
    {
        _modelDirectory = modelDirectory;
        _logger = logger;
        _metadata = new EmbeddingGeneratorMetadata("nomic-onnx", providerUri: null, ModelId, Dimensions);
        _model = new Lazy<LoadedModel>(Load, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <summary>True when both model files are on disk.</summary>
    public bool IsAvailable =>
        File.Exists(Path.Combine(_modelDirectory, ModelFileName))
        && File.Exists(Path.Combine(_modelDirectory, TokenizerFileName));

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable)
        {
            throw new EmbeddingModelUnavailableException(
                $"The Nomic embedding model isn't downloaded: expected {ModelFileName} and {TokenizerFileName} in {_modelDirectory}. " +
                "Follow src/PI.SearchApi/assets/models/README.md, then restart the AppHost. Stages 1–2 work without it.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var model = _model.Value;
        var tokenised = values.Select(text => Tokenise(model.Tokenizer, text)).ToList();
        var embeddings = new GeneratedEmbeddings<Embedding<float>>(tokenised.Count);

        if (tokenised.Count > 0)
        {
            var vectors = Infer(model.Session, tokenised);

            for (var i = 0; i < vectors.Count; i++)
            {
                embeddings.Add(new Embedding<float>(vectors[i])
                {
                    ModelId = ModelId,
                    // Kept with the vector so the trace can show how the text was tokenised.
                    AdditionalProperties = new AdditionalPropertiesDictionary
                    {
                        ["tokenCount"] = tokenised[i].Ids.Length,
                        ["truncated"] = tokenised[i].Truncated,
                    },
                });
            }
        }

        return Task.FromResult(embeddings);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceKey is not null ? null
        : serviceType == typeof(EmbeddingGeneratorMetadata) ? _metadata
        : serviceType.IsInstanceOfType(this) ? this
        : null;

    public void Dispose()
    {
        if (_model.IsValueCreated)
        {
            _model.Value.Session.Dispose();
        }
    }

    private LoadedModel Load()
    {
        var session = new InferenceSession(Path.Combine(_modelDirectory, ModelFileName));

        // The input names are a contract between this code and the exported model. Log them, so a
        // different export (e.g. without token_type_ids) is obvious rather than mysterious.
        _logger.LogInformation(
            "Loaded {ModelId}: inputs [{Inputs}], outputs [{Outputs}]",
            ModelId,
            string.Join(", ", session.InputMetadata.Keys),
            string.Join(", ", session.OutputMetadata.Keys));

        return new LoadedModel(session, LoadTokenizer(Path.Combine(_modelDirectory, TokenizerFileName)));
    }

    private static BertTokenizer LoadTokenizer(string tokenizerJsonPath)
    {
        // Microsoft.ML.Tokenizers' BertTokenizer reads a vocab.txt (one token per line, line number = ID).
        // Nomic ships only Hugging Face's tokenizer.json, whose "model.vocab" is the same WordPiece
        // vocabulary as a token → ID map. Writing it out in ID order gives exactly that vocab.txt.
        using var document = JsonDocument.Parse(File.ReadAllText(tokenizerJsonPath));
        var vocabulary = document.RootElement.GetProperty("model").GetProperty("vocab")
            .EnumerateObject()
            .OrderBy(token => token.Value.GetInt32())
            .Select(token => token.Name);

        using var vocabStream = new MemoryStream(Encoding.UTF8.GetBytes(string.Join('\n', vocabulary)));

        // tokenizer.json's normalizer says lowercase: true with strip_accents unset, which in BERT
        // means "strip accents when lower-casing": "portátil" is tokenised as "portatil".
        return BertTokenizer.Create(vocabStream, new BertOptions
        {
            LowerCaseBeforeTokenization = true,
            RemoveNonSpacingMarks = true,
        });
    }

    private static TokenisedText Tokenise(BertTokenizer tokenizer, string text)
    {
        var contentIds = tokenizer.EncodeToIds(text, addSpecialTokens: false);
        var maxContentTokens = MaxTokens - 2; // leave room for [CLS] and [SEP]
        var truncated = contentIds.Count > maxContentTokens;

        long[] ids =
        [
            tokenizer.ClassificationTokenId,
            .. contentIds.Take(maxContentTokens).Select(id => (long)id),
            tokenizer.SeparatorTokenId,
        ];

        return new TokenisedText(ids, truncated);
    }

    private static List<float[]> Infer(InferenceSession session, IReadOnlyList<TokenisedText> batch)
    {
        // Pad every sequence to the longest in the batch. Padding tokens get attention mask 0, so
        // attention and mean pooling skip them. Even so, the int8 model's dynamic quantisation
        // measures value ranges over the whole padded tensor, so a padded text's vector shifts a
        // little. Callers therefore embed one text per call (ADR-0009); batching still works.
        var batchSize = batch.Count;
        var sequenceLength = batch.Max(t => t.Ids.Length);

        var inputIds = new long[batchSize * sequenceLength];
        var attentionMask = new long[batchSize * sequenceLength];
        var tokenTypeIds = new long[batchSize * sequenceLength]; // all zeros: a single sentence, no pair

        for (var i = 0; i < batchSize; i++)
        {
            var ids = batch[i].Ids;
            ids.CopyTo(inputIds.AsSpan(i * sequenceLength));
            attentionMask.AsSpan(i * sequenceLength, ids.Length).Fill(1);
        }

        long[] shape = [batchSize, sequenceLength];
        using var inputIdsValue = OrtValue.CreateTensorValueFromMemory(inputIds, shape);
        using var attentionMaskValue = OrtValue.CreateTensorValueFromMemory(attentionMask, shape);
        using var tokenTypeIdsValue = OrtValue.CreateTensorValueFromMemory(tokenTypeIds, shape);

        var available = new Dictionary<string, OrtValue>
        {
            ["input_ids"] = inputIdsValue,
            ["attention_mask"] = attentionMaskValue,
            ["token_type_ids"] = tokenTypeIdsValue,
        };
        var inputs = available
            .Where(kv => session.InputMetadata.ContainsKey(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        var outputName = session.OutputMetadata.ContainsKey("last_hidden_state")
            ? "last_hidden_state"
            : session.OutputMetadata.Keys.First();

        using var runOptions = new RunOptions();
        using var outputs = session.Run(runOptions, inputs, [outputName]);

        // last_hidden_state: [batch, tokens, 768] — one contextual vector per token.
        var hiddenStates = outputs[0].GetTensorDataAsSpan<float>();
        var vectors = new List<float[]>(batchSize);

        for (var i = 0; i < batchSize; i++)
        {
            var sequence = hiddenStates.Slice(i * sequenceLength * Dimensions, sequenceLength * Dimensions);
            var mask = attentionMask.AsSpan(i * sequenceLength, sequenceLength);

            var pooled = EmbeddingMath.MeanPool(sequence, mask, Dimensions);
            vectors.Add(EmbeddingMath.L2Normalise(pooled));
        }

        return vectors;
    }

    private sealed record LoadedModel(InferenceSession Session, BertTokenizer Tokenizer);

    private sealed record TokenisedText(long[] Ids, bool Truncated);
}
