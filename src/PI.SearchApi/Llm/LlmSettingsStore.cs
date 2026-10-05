using System.Text.Json;

namespace PI.SearchApi.Llm;

/// <summary>
/// Reads and writes <see cref="LlmSettings"/> as <c>settings.json</c> in the learner's home folder: <c>~/.needle</c>,
/// or the folder named by the <c>NEEDLE_HOME</c> setting (ADR-0019). It lives outside the repository so a clone
/// stays clean and a learner's addresses are never committed. It never holds a key.
/// </summary>
public sealed class LlmSettingsStore(IConfiguration configuration, ILogger<LlmSettingsStore> logger)
{
    public const string HomeSetting = "NEEDLE_HOME";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly Lock gate = new();
    private LlmSettings? current;

    /// <summary>The settings file's full path, shown in the UI so the learner knows where it is.</summary>
    public string FilePath { get; } = Path.Combine(
        configuration[HomeSetting] is { Length: > 0 } home
            ? home
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".needle"),
        "settings.json");

    /// <summary>The settings, read from disk on first use. A missing or unreadable file means the defaults.</summary>
    public LlmSettings Current
    {
        get
        {
            lock (gate)
            {
                return current ??= Load();
            }
        }
    }

    /// <summary>Applies a change and saves it. The file is written in full, then moved into place, so it is never half-written.</summary>
    public LlmSettings Update(Func<LlmSettings, LlmSettings> change)
    {
        lock (gate)
        {
            var updated = change(current ??= Load());
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

            var temporaryPath = FilePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(updated, JsonOptions));
            File.Move(temporaryPath, FilePath, overwrite: true);

            current = updated;
            return updated;
        }
    }

    private LlmSettings Load()
    {
        if (!File.Exists(FilePath))
        {
            return new LlmSettings();
        }

        try
        {
            return JsonSerializer.Deserialize<LlmSettings>(File.ReadAllText(FilePath), JsonOptions) ?? new LlmSettings();
        }
        catch (JsonException exception)
        {
            // A hand-edited file with a typo shouldn't stop the API: warn, and use the defaults until it's fixed.
            logger.LogWarning("Ignoring {SettingsFile}: {Reason}", FilePath, exception.Message);
            return new LlmSettings();
        }
    }
}
