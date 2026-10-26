using EverRecall.Api.Models;

namespace EverRecall.Api.Data;

/// <summary>
/// The demo day. Every recall/guardian answer in the README and the
/// evaluation ground truth come from these exact events.
/// The day is designed to be all-clear for the Guardian.
/// </summary>
public static class SeedData
{
    public static List<MemoryEvent> DemoDay(DateTime? day = null)
    {
        var d = (day ?? DateTime.Today).Date;
        MemoryEvent At(int h, int m, string actor, string action, string obj, string loc, string room, double conf, string evidence) =>
            new()
            {
                OccurredAt = d.AddHours(h).AddMinutes(m),
                Actor = actor,
                Action = action,
                ObjectName = obj,
                Location = loc,
                Room = room,
                Confidence = conf,
                Source = "simulated-vision",
                EvidenceNote = evidence
            };

        return new List<MemoryEvent>
        {
            At(8, 15, "You", "took", "pill", "kitchen counter", "Kitchen", 0.97,
                "Frame 08:15:04 — hand taking the blue pill bottle on the kitchen counter."),
            At(8, 20, "You", "turned_on", "stove", "stove", "Kitchen", 0.96,
                "Frame 08:20:31 — front-left burner knob rotated to ON."),
            At(8, 30, "You", "fed", "dog", "kitchen", "Kitchen", 0.95,
                "Frame 08:30:12 — food bowl filled and placed by the kitchen door."),
            At(8, 47, "You", "turned_off", "stove", "stove", "Kitchen", 0.97,
                "Frame 08:47:55 — all burner knobs back to OFF, pan removed."),
            At(9, 2, "You", "locked", "front door", "front door", "Entryway", 0.96,
                "Frame 09:02:18 — deadbolt turned to locked at the front door."),
            At(9, 41, "You", "placed", "keys", "kitchen drawer", "Kitchen", 0.98,
                "Frame 09:41:12 — hand placing keys into the kitchen drawer."),
            At(9, 55, "Delivery driver", "delivered", "package", "front door", "Entryway", 0.93,
                "Frame 09:55:40 — delivery driver leaving a package at the front door."),
            At(10, 12, "You", "placed", "blue folder", "sofa", "Living room", 0.95,
                "Frame 10:12:26 — blue folder set down on the left side of the sofa."),
            At(10, 20, "You", "placed", "wallet", "bedroom dresser", "Bedroom", 0.96,
                "Frame 10:20:08 — wallet placed on the bedroom dresser."),
        };
    }
}
