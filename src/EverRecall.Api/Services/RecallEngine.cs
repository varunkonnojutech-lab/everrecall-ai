using EverRecall.Api.Models;

namespace EverRecall.Api.Services;

public record RecallResult(
    string Question,
    string Answer,
    double Confidence,
    bool Found,
    MemoryEvent? MatchedEvent,
    IReadOnlyList<MemoryEvent> RelatedEvents);

/// <summary>
/// The offline brain: answers natural-language questions over the episodic
/// event graph with zero external APIs. It only ever answers from events
/// that exist — an unknown question gets an honest "I don't have a memory
/// of that", never a guess. In production the same interface is served by
/// Muse Wonder (see Providers), which reasons over the same event graph.
/// </summary>
public class RecallEngine
{
    public RecallResult Ask(string question, IReadOnlyList<MemoryEvent> events)
    {
        var q = " " + Normalize(question) + " ";
        var ordered = events.OrderBy(e => e.OccurredAt).ToList();

        // --- Who came / who was at the door ---------------------------------
        if (q.Contains(" who ") || q.Contains("who came") || q.Contains("who was"))
        {
            var visitors = ordered
                .Where(e => e.Actor != "You" || e.Action == "delivered" || e.Location.Contains("front door"))
                .Where(e => e.Actor != "You")
                .ToList();
            if (visitors.Count == 0) return NotFound(question);
            var v = visitors[^1];
            return new RecallResult(question,
                $"{v.Actor} came to the {v.Location} at {Fmt(v.OccurredAt)} — {v.Action.Replace('_', ' ')} the {v.ObjectName}. Evidence: {v.EvidenceNote}",
                v.Confidence, true, v, visitors);
        }

        // --- Where is/are my X ----------------------------------------------
        if (q.Contains(" where "))
        {
            var obj = Synonyms.FindObject(q);
            if (obj is null) return NotFound(question);
            var match = ordered.LastOrDefault(e => e.ObjectName == obj);
            if (match is null) return NotFound(question, obj);
            return new RecallResult(question,
                $"Your {obj} — last seen at {match.Location} ({match.Room}) at {Fmt(match.OccurredAt)}. Evidence: {match.EvidenceNote}",
                match.Confidence, true, match, new[] { match });
        }

        // --- Is the stove off / is the door locked (state questions) --------
        if (q.TrimStart().StartsWith(" is ") || q.Contains(" is the ") || q.Contains(" is my "))
        {
            var obj = Synonyms.FindObject(q);
            if (obj is not null)
            {
                var related = ordered.Where(e => e.ObjectName == obj).ToList();
                if (related.Count > 0)
                {
                    var last = related[^1];
                    var state = last.Action switch
                    {
                        "turned_off" => "off",
                        "turned_on" => "on",
                        "locked" => "locked",
                        _ => last.Action.Replace('_', ' ')
                    };
                    return new RecallResult(question,
                        $"The {obj} is {state} — last event at {Fmt(last.OccurredAt)}. Evidence: {last.EvidenceNote}",
                        last.Confidence, true, last, related);
                }
            }
            return NotFound(question, obj);
        }

        // --- What happened / list my day ------------------------------------
        if (q.Contains("what happened") || q.Contains(" list ") || q.Contains(" timeline ") || q.Contains(" my day"))
        {
            if (ordered.Count == 0) return NotFound(question);
            var lines = ordered.Select(e => $"{Fmt(e.OccurredAt)} — {e.Actor} {e.Action.Replace('_', ' ')} {e.ObjectName} at {e.Location}");
            return new RecallResult(question,
                "Here is what I remember: " + string.Join("; ", lines) + ".",
                ordered.Min(e => e.Confidence), true, ordered[^1], ordered);
        }

        // --- Did I ...? (action recall) --------------------------------------
        var action = Synonyms.FindAction(q);
        var target = Synonyms.FindObject(q);
        if (action is not null || target is not null)
        {
            var candidates = ordered.Where(e =>
                (action is null || e.Action == action) &&
                (target is null || e.ObjectName == target)).ToList();

            // "Did I take my pill?" — action 'have/took' + object 'pill'; a
            // delivery driver acting is not "you", so prefer the user's own events.
            var own = candidates.Where(e => e.Actor == "You").ToList();
            var pool = own.Count > 0 ? own : candidates;

            if (pool.Count > 0 && action is not null && target is not null)
            {
                var m = pool[^1];
                return new RecallResult(question,
                    $"Yes — at {Fmt(m.OccurredAt)} you {PastTense(m.Action)} the {m.ObjectName} at {m.Location}. Evidence: {m.EvidenceNote}",
                    m.Confidence, true, m, pool);
            }
            if (pool.Count > 0 && target is not null && action is null)
            {
                var m = pool[^1];
                return new RecallResult(question,
                    $"I remember the {m.ObjectName}: {m.Actor} {m.Action.Replace('_', ' ')} it at {m.Location} at {Fmt(m.OccurredAt)}. Evidence: {m.EvidenceNote}",
                    m.Confidence, true, m, pool);
            }
            return NotFound(question, target);
        }

        return NotFound(question);
    }

    private static RecallResult NotFound(string question, string? obj = null) =>
        new(question,
            obj is null
                ? "I don't have a memory of that. I only answer from events I actually saw — try asking about your keys, pill, stove, front door, dog, package, blue folder or wallet."
                : $"I don't have a memory of the {obj}. Nothing in my event graph matches that, and I won't guess.",
            0, false, null, Array.Empty<MemoryEvent>());

    internal static string Normalize(string s) =>
        s.ToLowerInvariant().Trim().Replace("?", " ").Replace(".", " ").Replace(",", " ");

    internal static string Fmt(DateTime t) => t.ToString("h:mm tt");

    private static string PastTense(string action) => action switch
    {
        "took" => "took",
        "locked" => "locked",
        "turned_off" => "turned off",
        "turned_on" => "turned on",
        "fed" => "fed",
        "placed" => "placed",
        "received" => "received",
        "watered" => "watered",
        _ => action.Replace('_', ' ')
    };
}
