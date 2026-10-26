namespace EverRecall.Api.Models;

public record AskRequest(string Question);

public record AskResponse(
    string Question,
    string Answer,
    double Confidence,
    bool Found,
    MemoryEvent? MatchedEvent,
    IReadOnlyList<MemoryEvent> RelatedEvents);

public record CreateEventRequest(
    DateTime? OccurredAt,
    string? Actor,
    string Action,
    string ObjectName,
    string Location,
    string Room,
    double? Confidence,
    string? Source,
    string? EvidenceNote);

/// <summary>One object/action detection, in the shape a SAM 3.1 pipeline would emit per frame segment.</summary>
public record DetectedEventDto(
    DateTime? OccurredAt,
    string? Actor,
    string Action,
    string ObjectName,
    string Location,
    string Room,
    double? Confidence,
    string? EvidenceNote);

/// <summary>Body of POST /api/ingest/events — a batch of vision detections to turn into memories.</summary>
public record IngestRequest(string? SourceLabel, List<DetectedEventDto> Detections);

public record ForgetRequest(int Minutes);

public record ForgetResponse(int RequestedMinutes, int DeletedCount, DateTime Cutoff);

public record EvaluateRequest(List<CreateEventRequest>? Events, DateTime? AsOf);
