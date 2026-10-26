using System.Net.Http.Json;
using EverRecall.Api.Models;
using EverRecall.Api.Services;
using Microsoft.Extensions.Options;

namespace EverRecall.Api.Providers;

/// <summary>
/// Meta Model API configuration. Ships EMPTY (no key) — the app then runs
/// fully offline on the simulated providers below. Add your key in
/// appsettings.json / environment (MetaModelApi__ApiKey) after the
/// hackathon kickoff to switch to the real models. Model ids are the ones
/// announced for the Meta Global AI Developer Hackathon.
/// </summary>
public class MetaModelApiOptions
{
    public const string SectionName = "MetaModelApi";
    public string BaseUrl { get; set; } = "https://api.meta.com/v1"; // placeholder host — confirm at kickoff
    public string ApiKey { get; set; } = "";
    public string VisionModel { get; set; } = "sam-3.1";
    public string MemoryModel { get; set; } = "muse-wonder-1.0";
    public string ImageModel { get; set; } = "muse-image-1.0";
    public string VoiceModel { get; set; } = "muse-voice-transcribe-1.0";
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
}

// ---------------------------------------------------------------------------
// Interfaces — one per Meta model, so each can be swapped independently.
// ---------------------------------------------------------------------------

/// <summary>SAM 3.1: turn frames/video detections into episodic events.</summary>
public interface IMetaVisionProvider
{
    Task<List<MemoryEvent>> ExtractEventsAsync(IngestRequest request, CancellationToken ct = default);
}

/// <summary>Muse Wonder: reason over the event graph to answer recall questions.</summary>
public interface IMetaMemoryReasoner
{
    Task<RecallResult> AnswerAsync(string question, IReadOnlyList<MemoryEvent> events, CancellationToken ct = default);
}

/// <summary>Muse Image: annotate an evidence frame (circle the object, mark the moment).</summary>
public interface IMetaImageAnnotator
{
    Task<string> AnnotateAsync(MemoryEvent ev, CancellationToken ct = default);
}

/// <summary>Muse Voice Transcribe: speech-to-text for hands-free questions.</summary>
public interface IMetaTranscriber
{
    Task<string> TranscribeAsync(byte[] audio, CancellationToken ct = default);
}

// ---------------------------------------------------------------------------
// Simulated implementations — work offline TODAY, honestly labelled.
// They simulate the pipeline shape, never a Meta model response.
// ---------------------------------------------------------------------------

public class SimulatedVisionProvider : IMetaVisionProvider
{
    public Task<List<MemoryEvent>> ExtractEventsAsync(IngestRequest request, CancellationToken ct = default)
    {
        var source = string.IsNullOrWhiteSpace(request.SourceLabel) ? "simulated-vision" : request.SourceLabel!;
        var events = request.Detections.Select(d => new MemoryEvent
        {
            OccurredAt = d.OccurredAt ?? DateTime.Now,
            Actor = string.IsNullOrWhiteSpace(d.Actor) ? "You" : d.Actor!,
            Action = d.Action,
            ObjectName = d.ObjectName,
            Location = d.Location,
            Room = d.Room,
            Confidence = d.Confidence ?? 0.9,
            Source = source,
            EvidenceNote = d.EvidenceNote ?? ""
        }).ToList();
        return Task.FromResult(events);
    }
}

public class SimulatedMemoryReasoner : IMetaMemoryReasoner
{
    private readonly RecallEngine _engine = new();
    public Task<RecallResult> AnswerAsync(string question, IReadOnlyList<MemoryEvent> events, CancellationToken ct = default) =>
        Task.FromResult(_engine.Ask(question, events));
}

public class SimulatedImageAnnotator : IMetaImageAnnotator
{
    public Task<string> AnnotateAsync(MemoryEvent ev, CancellationToken ct = default) =>
        Task.FromResult(
            $"[SIMULATED annotation — no image was generated] Circle '{ev.ObjectName}' in the evidence frame " +
            $"at {ev.OccurredAt:h:mm tt} ({ev.Location}). Note: {ev.EvidenceNote} " +
            "Configure MetaModelApi:ApiKey to generate a real annotated image with Muse Image.");
}

public class SimulatedTranscriber : IMetaTranscriber
{
    public Task<string> TranscribeAsync(byte[] audio, CancellationToken ct = default) =>
        Task.FromResult(
            "[SIMULATED transcriber — audio is not transcribed offline. " +
            "Configure MetaModelApi:ApiKey to transcribe with Muse Voice Transcribe, or type your question instead.]");
}

// ---------------------------------------------------------------------------
// HTTP implementations — config-gated. With no key they throw a clear
// "not configured" error (wired only when a key IS present). The request
// shape below is a documented placeholder: endpoint paths/fields must be
// confirmed against the official Meta Model API docs at kickoff.
// These NEVER fabricate a model response.
// ---------------------------------------------------------------------------

public abstract class MetaHttpBase
{
    protected readonly MetaModelApiOptions Options;
    protected readonly HttpClient Http;
    protected MetaHttpBase(IOptions<MetaModelApiOptions> options, HttpClient http)
    {
        Options = options.Value;
        Http = http;
    }
    protected void EnsureConfigured()
    {
        if (!Options.IsConfigured)
            throw new InvalidOperationException(
                "Meta Model API is not configured: set MetaModelApi:ApiKey (appsettings or env MetaModelApi__ApiKey). " +
                "Until then the app runs on the simulated providers.");
    }
}

public class MetaVisionProvider : MetaHttpBase, IMetaVisionProvider
{
    public MetaVisionProvider(IOptions<MetaModelApiOptions> o, HttpClient h) : base(o, h) { }
    public async Task<List<MemoryEvent>> ExtractEventsAsync(IngestRequest request, CancellationToken ct = default)
    {
        EnsureConfigured();
        // PLACEHOLDER (confirm at kickoff): POST {BaseUrl}/models/{VisionModel}:segment
        // with the frame batch; map returned masks/tracks to DetectedEventDto, then
        // reuse the simulated converter for persistence shape.
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{Options.BaseUrl}/models/{Options.VisionModel}:segment");
        req.Headers.Add("Authorization", $"Bearer {Options.ApiKey}");
        req.Content = JsonContent.Create(request.Detections);
        using var res = await Http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        var detections = await res.Content.ReadFromJsonAsync<List<DetectedEventDto>>(ct) ?? new();
        return await new SimulatedVisionProvider()
            .ExtractEventsAsync(new IngestRequest("meta-model-api", detections), ct);
    }
}

public class MetaMemoryReasoner : MetaHttpBase, IMetaMemoryReasoner
{
    public MetaMemoryReasoner(IOptions<MetaModelApiOptions> o, HttpClient h) : base(o, h) { }
    public async Task<RecallResult> AnswerAsync(string question, IReadOnlyList<MemoryEvent> events, CancellationToken ct = default)
    {
        EnsureConfigured();
        // PLACEHOLDER (confirm at kickoff): POST {BaseUrl}/models/{MemoryModel}:generate
        // with the question + serialized event graph as long-context input, asking for
        // structured output {answer, matchedEventId, confidence}. Until the response
        // contract is confirmed, fall back to the deterministic engine for parsing —
        // we do not invent a Muse Wonder response here.
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{Options.BaseUrl}/models/{Options.MemoryModel}:generate");
        req.Headers.Add("Authorization", $"Bearer {Options.ApiKey}");
        req.Content = JsonContent.Create(new { question, events });
        using var res = await Http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        return new RecallEngine().Ask(question, events);
    }
}

public class MetaImageAnnotator : MetaHttpBase, IMetaImageAnnotator
{
    public MetaImageAnnotator(IOptions<MetaModelApiOptions> o, HttpClient h) : base(o, h) { }
    public async Task<string> AnnotateAsync(MemoryEvent ev, CancellationToken ct = default)
    {
        EnsureConfigured();
        // PLACEHOLDER (confirm at kickoff): POST {BaseUrl}/models/{ImageModel}:edit with
        // the evidence frame + instruction "circle the {ObjectName}". Returns an image URL.
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{Options.BaseUrl}/models/{Options.ImageModel}:edit");
        req.Headers.Add("Authorization", $"Bearer {Options.ApiKey}");
        req.Content = JsonContent.Create(new { instruction = $"Circle the {ev.ObjectName}", evidence = ev.EvidenceNote });
        using var res = await Http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsStringAsync(ct);
    }
}

public class MetaTranscriber : MetaHttpBase, IMetaTranscriber
{
    public MetaTranscriber(IOptions<MetaModelApiOptions> o, HttpClient h) : base(o, h) { }
    public async Task<string> TranscribeAsync(byte[] audio, CancellationToken ct = default)
    {
        EnsureConfigured();
        // PLACEHOLDER (confirm at kickoff): POST {BaseUrl}/models/{VoiceModel}:transcribe (audio bytes).
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{Options.BaseUrl}/models/{Options.VoiceModel}:transcribe");
        req.Headers.Add("Authorization", $"Bearer {Options.ApiKey}");
        req.Content = new ByteArrayContent(audio);
        using var res = await Http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsStringAsync(ct);
    }
}
