using EverRecall.Api.Models;

namespace EverRecall.Api.Services;

public record ChecklistItem(string Name, string Status, string Detail); // Status: ok | pending | alert

public record GuardianReport(
    DateTime EvaluatedAt,
    bool AllClear,
    IReadOnlyList<ChecklistItem> Items,
    IReadOnlyList<string> Alerts);

/// <summary>
/// The Checklist Guardian. Evaluates the daily safety list against the
/// event graph, plus one hard hazard rule: a stove left ON for more than
/// 30 minutes with no matching OFF is an immediate alert.
/// Pure function of (events, asOf) — fully unit-testable.
/// </summary>
public class GuardianService
{
    public static readonly TimeSpan StoveHazardWindow = TimeSpan.FromMinutes(30);
    private static readonly TimeOnly MedicationBy = new(9, 0);
    private static readonly TimeOnly DoorLockedBy = new(22, 0);

    public GuardianReport Evaluate(IReadOnlyList<MemoryEvent> events, DateTime asOf)
    {
        var items = new List<ChecklistItem>();
        var alerts = new List<string>();
        void Add(ChecklistItem item)
        {
            items.Add(item);
            if (item.Status == "alert") alerts.Add($"{item.Name}: {item.Detail}");
        }

        // 1) Medication taken (by 09:00)
        var pill = events.Where(e => e.ObjectName == "pill" && e.Action == "took")
                         .OrderBy(e => e.OccurredAt).LastOrDefault();
        if (pill is not null && pill.OccurredAt.TimeOfDay <= MedicationBy.ToTimeSpan())
            Add(new("Medication", "ok", $"Taken at {RecallEngine.Fmt(pill.OccurredAt)} (due by 9:00 AM)."));
        else if (pill is not null)
            Add(new("Medication", "ok", $"Taken at {RecallEngine.Fmt(pill.OccurredAt)} — later than the 9:00 AM target."));
        else if (asOf.TimeOfDay > MedicationBy.ToTimeSpan())
            Add(new("Medication", "alert", "No memory of medication being taken today."));
        else
            Add(new("Medication", "pending", "Not due yet (due by 9:00 AM), no dose seen so far."));

        // 2) Stove off + hazard rule
        var stoveEvents = events.Where(e => e.ObjectName == "stove")
                                .OrderBy(e => e.OccurredAt).ToList();
        var lastStove = stoveEvents.LastOrDefault();
        if (lastStove is null)
            Add(new("Stove off", "ok", "No stove use remembered today."));
        else if (lastStove.Action == "turned_off")
            Add(new("Stove off", "ok", $"Turned off at {RecallEngine.Fmt(lastStove.OccurredAt)}."));
        else if (lastStove.Action == "turned_on" && asOf - lastStove.OccurredAt > StoveHazardWindow)
            Add(new("Stove off", "alert",
                $"HAZARD: stove has been ON since {RecallEngine.Fmt(lastStove.OccurredAt)} with no OFF event — over the 30-minute unattended limit."));
        else
            Add(new("Stove off", "pending", $"Stove is on (since {RecallEngine.Fmt(lastStove.OccurredAt)}), still inside the 30-minute window."));

        // 3) Front door locked (by 22:00, or any lock event today)
        var locked = events.Where(e => e.ObjectName == "front door" && e.Action == "locked")
                           .OrderBy(e => e.OccurredAt).LastOrDefault();
        if (locked is not null)
            Add(new("Front door locked", "ok", $"Locked at {RecallEngine.Fmt(locked.OccurredAt)}."));
        else if (asOf.TimeOfDay >= DoorLockedBy.ToTimeSpan())
            Add(new("Front door locked", "alert", "No memory of the front door being locked, and it is past 10:00 PM."));
        else
            Add(new("Front door locked", "pending", "No lock event yet today (checked again at 10:00 PM)."));

        // 4) Pet fed
        var fed = events.Where(e => e.ObjectName == "dog" && e.Action == "fed")
                        .OrderBy(e => e.OccurredAt).LastOrDefault();
        if (fed is not null)
            Add(new("Pet fed", "ok", $"Dog fed at {RecallEngine.Fmt(fed.OccurredAt)}."));
        else
            Add(new("Pet fed", "alert", "No memory of the dog being fed today."));

        return new GuardianReport(asOf, alerts.Count == 0, items, alerts);
    }
}
