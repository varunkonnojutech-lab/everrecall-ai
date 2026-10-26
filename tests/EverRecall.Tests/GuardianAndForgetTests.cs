using EverRecall.Api.Data;
using EverRecall.Api.Models;
using EverRecall.Api.Services;

namespace EverRecall.Tests;

public class GuardianTests
{
    private readonly GuardianService _guardian = new();
    private static readonly DateTime Day = new(2026, 10, 26);

    [Fact]
    public void Seeded_demo_day_is_all_clear()
    {
        var report = _guardian.Evaluate(SeedData.DemoDay(Day), Day.AddHours(12));
        Assert.True(report.AllClear);
        Assert.Empty(report.Alerts);
        Assert.All(report.Items, i => Assert.NotEqual("alert", i.Status));
    }

    [Fact]
    public void Stove_on_unattended_over_30_minutes_alerts()
    {
        var events = new List<MemoryEvent>
        {
            new() { OccurredAt = Day.AddHours(8), Actor = "You", Action = "turned_on", ObjectName = "stove", Location = "stove", Room = "Kitchen", Confidence = 0.95, Source = "test", EvidenceNote = "on" },
            new() { OccurredAt = Day.AddHours(8).AddMinutes(5), Actor = "You", Action = "took", ObjectName = "pill", Location = "kitchen counter", Room = "Kitchen", Confidence = 0.95, Source = "test", EvidenceNote = "pill" },
            new() { OccurredAt = Day.AddHours(8).AddMinutes(10), Actor = "You", Action = "fed", ObjectName = "dog", Location = "kitchen", Room = "Kitchen", Confidence = 0.95, Source = "test", EvidenceNote = "fed" },
            new() { OccurredAt = Day.AddHours(8).AddMinutes(15), Actor = "You", Action = "locked", ObjectName = "front door", Location = "front door", Room = "Entryway", Confidence = 0.95, Source = "test", EvidenceNote = "locked" },
        };
        // Evaluate 45 minutes after the stove was turned on, with no OFF event.
        var report = _guardian.Evaluate(events, Day.AddHours(8).AddMinutes(45));
        Assert.False(report.AllClear);
        Assert.Contains(report.Alerts, a => a.Contains("HAZARD"));
    }

    [Fact]
    public void Missing_medication_after_9am_alerts()
    {
        var report = _guardian.Evaluate(new List<MemoryEvent>(), Day.AddHours(12));
        Assert.Contains(report.Alerts, a => a.Contains("Medication"));
    }
}

public class ForgetLogicTests
{
    // The forget endpoint deletes events with OccurredAt >= DateTime.Now - minutes.
    // This test pins the same window rule against an in-memory list.
    [Fact]
    public void Forget_window_removes_only_events_inside_it()
    {
        var now = DateTime.Now;
        var events = new List<MemoryEvent>
        {
            new() { OccurredAt = now.AddMinutes(-10), Action = "placed", ObjectName = "keys" },
            new() { OccurredAt = now.AddMinutes(-120), Action = "took", ObjectName = "pill" },
        };
        var cutoff = now.AddMinutes(-60);
        var remaining = events.Where(e => e.OccurredAt < cutoff).ToList();
        Assert.Single(remaining);
        Assert.Equal("pill", remaining[0].ObjectName);
    }
}
