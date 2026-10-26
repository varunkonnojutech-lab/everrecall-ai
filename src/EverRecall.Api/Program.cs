using EverRecall.Api.Data;
using EverRecall.Api.Models;
using EverRecall.Api.Providers;
using EverRecall.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<EverRecallDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=everrecall.db"));

// --- Meta providers: simulated offline by default; real Meta Model API when a key is configured ---
builder.Services.Configure<MetaModelApiOptions>(builder.Configuration.GetSection(MetaModelApiOptions.SectionName));
var metaConfigured = !string.IsNullOrWhiteSpace(builder.Configuration["MetaModelApi:ApiKey"]);
builder.Services.AddHttpClient();
if (metaConfigured)
{
    builder.Services.AddScoped<IMetaVisionProvider, MetaVisionProvider>();
    builder.Services.AddScoped<IMetaMemoryReasoner, MetaMemoryReasoner>();
    builder.Services.AddScoped<IMetaImageAnnotator, MetaImageAnnotator>();
    builder.Services.AddScoped<IMetaTranscriber, MetaTranscriber>();
}
else
{
    builder.Services.AddScoped<IMetaVisionProvider, SimulatedVisionProvider>();
    builder.Services.AddScoped<IMetaMemoryReasoner, SimulatedMemoryReasoner>();
    builder.Services.AddScoped<IMetaImageAnnotator, SimulatedImageAnnotator>();
    builder.Services.AddScoped<IMetaTranscriber, SimulatedTranscriber>();
}

var app = builder.Build();

// --- Create + seed the demo day on first run ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EverRecallDbContext>();
    db.Database.EnsureCreated();
    if (!db.MemoryEvents.Any())
    {
        db.MemoryEvents.AddRange(SeedData.DemoDay());
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --- Endpoints ---------------------------------------------------------------

app.MapGet("/api/health", (IOptions<MetaModelApiOptions> meta) => Results.Ok(new
{
    status = "ok",
    service = "EverRecall.Api",
    time = DateTime.Now,
    metaProvider = meta.Value.IsConfigured ? "meta-model-api" : "simulated"
}));

app.MapGet("/api/events", async (EverRecallDbContext db, string? room, string? @object, DateTime? from, DateTime? to) =>
{
    var q = db.MemoryEvents.AsQueryable();
    if (room is not null) q = q.Where(e => e.Room == room);
    if (@object is not null) q = q.Where(e => e.ObjectName == @object);
    if (from is not null) q = q.Where(e => e.OccurredAt >= from);
    if (to is not null) q = q.Where(e => e.OccurredAt <= to);
    return Results.Ok(await q.OrderBy(e => e.OccurredAt).ToListAsync());
});

app.MapPost("/api/events", async (EverRecallDbContext db, CreateEventRequest req) =>
{
    var ev = ToEvent(req, "manual");
    db.MemoryEvents.Add(ev);
    await db.SaveChangesAsync();
    return Results.Created($"/api/events/{ev.Id}", ev);
});

app.MapPost("/api/ingest/events", async (EverRecallDbContext db, IMetaVisionProvider vision, IngestRequest req) =>
{
    // The real SAM 3.1 pipeline plugs in here: frames/video -> detections -> memories.
    var events = await vision.ExtractEventsAsync(req);
    db.MemoryEvents.AddRange(events);
    await db.SaveChangesAsync();
    return Results.Ok(new { ingested = events.Count, events });
});

app.MapPost("/api/ask", async (EverRecallDbContext db, IMetaMemoryReasoner reasoner, AskRequest req) =>
{
    var events = await db.MemoryEvents.OrderBy(e => e.OccurredAt).ToListAsync();
    var r = await reasoner.AnswerAsync(req.Question, events);
    return Results.Ok(new AskResponse(r.Question, r.Answer, r.Confidence, r.Found, r.MatchedEvent, r.RelatedEvents));
});

app.MapGet("/api/guardian/status", async (EverRecallDbContext db) =>
{
    var events = await db.MemoryEvents.OrderBy(e => e.OccurredAt).ToListAsync();
    return Results.Ok(new GuardianService().Evaluate(events, DateTime.Now));
});

app.MapPost("/api/guardian/evaluate", (EvaluateRequest req) =>
{
    // Evaluate ANY supplied event set without saving it — this is how the
    // hazard path (stove on, unattended) is exercised safely.
    var events = (req.Events ?? new()).Select(r => ToEvent(r, "evaluate")).ToList();
    return Results.Ok(new GuardianService().Evaluate(events, req.AsOf ?? DateTime.Now));
});

app.MapPost("/api/memory/forget", async (EverRecallDbContext db, ForgetRequest req) =>
{
    // Privacy by design: "forget the last hour" really deletes.
    var cutoff = DateTime.Now.AddMinutes(-Math.Abs(req.Minutes));
    var doomed = await db.MemoryEvents.Where(e => e.OccurredAt >= cutoff).ToListAsync();
    db.MemoryEvents.RemoveRange(doomed);
    await db.SaveChangesAsync();
    return Results.Ok(new ForgetResponse(Math.Abs(req.Minutes), doomed.Count, cutoff));
});

app.MapPost("/api/annotate", async (IMetaImageAnnotator annotator, MemoryEvent ev) =>
    Results.Ok(new { annotation = await annotator.AnnotateAsync(ev) }));

app.Run();

static MemoryEvent ToEvent(CreateEventRequest r, string defaultSource) => new()
{
    OccurredAt = r.OccurredAt ?? DateTime.Now,
    Actor = string.IsNullOrWhiteSpace(r.Actor) ? "You" : r.Actor!,
    Action = r.Action,
    ObjectName = r.ObjectName,
    Location = r.Location,
    Room = r.Room,
    Confidence = r.Confidence ?? 0.95,
    Source = string.IsNullOrWhiteSpace(r.Source) ? defaultSource : r.Source!,
    EvidenceNote = r.EvidenceNote ?? ""
};
