namespace EverRecall.Api.Services;

/// <summary>
/// Plain, inspectable language matching for the offline recall engine.
/// Canonical object/action names (as stored on MemoryEvent) map to the words
/// people actually use. This is deliberately a dictionary, not a model:
/// it is honest, testable, and never invents an answer.
/// </summary>
public static class Synonyms
{
    public static readonly IReadOnlyDictionary<string, string[]> Objects =
        new Dictionary<string, string[]>
        {
            ["keys"] = new[] { "keys", "key" },
            ["pill"] = new[] { "pill", "pills", "medication", "medicine", "meds", "tablet", "tablets" },
            ["stove"] = new[] { "stove", "oven", "burner", "hob" },
            ["front door"] = new[] { "front door", "door" },
            ["dog"] = new[] { "dog", "pet", "puppy" },
            ["package"] = new[] { "package", "parcel", "delivery" },
            ["blue folder"] = new[] { "blue folder", "folder", "file" },
            ["wallet"] = new[] { "wallet", "purse" },
            ["plants"] = new[] { "plants", "plant" },
            ["garage door"] = new[] { "garage door", "garage" },
            ["car"] = new[] { "car" },
            ["phone"] = new[] { "phone", "mobile" },
            ["glasses"] = new[] { "glasses", "spectacles" },
            ["umbrella"] = new[] { "umbrella" },
        };

    public static readonly IReadOnlyDictionary<string, string[]> Actions =
        new Dictionary<string, string[]>
        {
            ["took"] = new[] { "take", "took", "taken", "taking", "have" },
            ["locked"] = new[] { "lock", "locked", "locking" },
            ["turned_off"] = new[] { "turn off", "turned off", "switch off", "switched off", "shut off", "turn the stove off" },
            ["turned_on"] = new[] { "turn on", "turned on", "switch on", "switched on" },
            ["fed"] = new[] { "feed", "fed", "feeding" },
            ["placed"] = new[] { "put", "place", "placed", "leave", "left", "keep", "kept" },
            ["delivered"] = new[] { "deliver", "delivered", "drop off", "dropped off", "came", "arrived", "come" },
            ["received"] = new[] { "receive", "received", "got", "collect", "collected" },
            ["watered"] = new[] { "water", "watered", "watering" },
        };

    /// <summary>Find the canonical object a question mentions (longest alias wins, so "front door" beats "door").</summary>
    public static string? FindObject(string normalizedQuestion)
    {
        string? best = null;
        var bestLen = -1;
        foreach (var (canonical, aliases) in Objects)
        {
            foreach (var alias in aliases)
            {
                if (normalizedQuestion.Contains(" " + alias + " ") ||
                    normalizedQuestion.Contains(" " + alias + "?") ||
                    normalizedQuestion.EndsWith(" " + alias) ||
                    normalizedQuestion.StartsWith(alias + " "))
                {
                    if (alias.Length > bestLen) { best = canonical; bestLen = alias.Length; }
                }
            }
        }
        return best;
    }

    /// <summary>Find the canonical action a question mentions (longest alias wins, so "turn off" beats "turn on" ties).</summary>
    public static string? FindAction(string normalizedQuestion)
    {
        string? best = null;
        var bestLen = -1;
        foreach (var (canonical, aliases) in Actions)
        {
            foreach (var alias in aliases)
            {
                if (normalizedQuestion.Contains(alias))
                {
                    if (alias.Length > bestLen) { best = canonical; bestLen = alias.Length; }
                }
            }
        }
        return best;
    }
}
