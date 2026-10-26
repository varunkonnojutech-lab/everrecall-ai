namespace EverRecall.Api.Models;

/// <summary>
/// One remembered moment in the episodic event graph: who did what, to which
/// object, where, and when. In production these are produced by the vision
/// pipeline (SAM 3.1 via <see cref="Providers.IMetaVisionProvider"/>); in the
/// offline demo they come from the simulated provider or manual entry.
/// </summary>
public class MemoryEvent
{
    public int Id { get; set; }
    public DateTime OccurredAt { get; set; }

    /// <summary>Who acted: "You", "Delivery driver", "Family member", ...</summary>
    public string Actor { get; set; } = "You";

    /// <summary>
    /// Canonical action: took | placed | turned_on | turned_off | locked |
    /// fed | delivered | received | came_to_door (lowercase, snake_case).
    /// </summary>
    public string Action { get; set; } = "";

    /// <summary>Canonical object name: "keys", "pill", "stove", "front door", "dog", ...</summary>
    public string ObjectName { get; set; } = "";

    /// <summary>Specific place: "kitchen drawer", "bedroom dresser", "sofa".</summary>
    public string Location { get; set; } = "";

    /// <summary>Room: "Kitchen", "Bedroom", "Living room", "Entryway".</summary>
    public string Room { get; set; } = "";

    /// <summary>0..1 detection confidence from the producing provider.</summary>
    public double Confidence { get; set; } = 0.95;

    /// <summary>"simulated-vision" | "meta-model-api" | "manual".</summary>
    public string Source { get; set; } = "manual";

    /// <summary>Human-readable evidence pointer, e.g. "Frame 09:41:12 — hand placing keys in kitchen drawer."</summary>
    public string EvidenceNote { get; set; } = "";
}
