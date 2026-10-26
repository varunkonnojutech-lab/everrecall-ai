// EverRecall evaluation runner.
//
// Runs 30 staged scenarios (ground truth = the seeded demo day) through the
// REAL RecallEngine and reports the exact metrics promised in the hackathon
// plan: object-recall accuracy, action-recall accuracy, mean timestamp error.
//
// Usage:  dotnet run --project tools/EverRecall.Evaluation
using EverRecall.Api.Data;
using EverRecall.Api.Services;

var day = new DateTime(2026, 10, 26);
var events = SeedData.DemoDay(day);
var engine = new RecallEngine();

DateTime At(int h, int m) => day.AddHours(h).AddMinutes(m);

var scenarios = new List<Scenario>
{
    // --- Object recall (15): "Where is/are my X?" -> correct object, location, time ---
    new("object", "Where are my keys?", true, "keys", null, At(9, 41)),
    new("object", "Where is my wallet?", true, "wallet", null, At(10, 20)),
    new("object", "Where did I leave the blue folder?", true, "blue folder", null, At(10, 12)),
    new("object", "Where is my medication?", true, "pill", null, At(8, 15)),
    new("object", "Where is the package?", true, "package", null, At(9, 55)),
    new("object", "Where did I put my keys?", true, "keys", null, At(9, 41)),
    new("object", "Where is my folder?", true, "blue folder", null, At(10, 12)),
    new("object", "Where is my pet?", true, "dog", null, At(8, 30)),
    new("object", "Where are my pills?", true, "pill", null, At(8, 15)),
    new("object", "Where did I leave my wallet?", true, "wallet", null, At(10, 20)),
    new("object", "Where is my parcel?", true, "package", null, At(9, 55)),
    new("object", "Where is the pill?", true, "pill", null, At(8, 15)),
    new("object", "Where did I put the blue folder?", true, "blue folder", null, At(10, 12)),
    new("object", "Where is my dog?", true, "dog", null, At(8, 30)),
    new("object", "Where did I keep my wallet?", true, "wallet", null, At(10, 20)),

    // --- Action recall (15): "Did I ...?" -> correct yes/no + matched event ---
    new("action", "Did I take my pill?", true, "pill", "took", At(8, 15)),
    new("action", "Did I take my medication?", true, "pill", "took", At(8, 15)),
    new("action", "Did I lock the front door?", true, "front door", "locked", At(9, 2)),
    new("action", "Did I lock the door?", true, "front door", "locked", At(9, 2)),
    new("action", "Did I turn off the stove?", true, "stove", "turned_off", At(8, 47)),
    new("action", "Did I feed the dog?", true, "dog", "fed", At(8, 30)),
    new("action", "Did I feed my pet?", true, "dog", "fed", At(8, 30)),
    new("action", "Did the package get delivered?", true, "package", "delivered", At(9, 55)),
    new("action", "Did I switch off the stove?", true, "stove", "turned_off", At(8, 47)),
    new("action", "Did I take my pills?", true, "pill", "took", At(8, 15)),
    new("action", "Did I turn on the stove?", true, "stove", "turned_on", At(8, 20)),
    new("action", "Did I water the plants?", false),
    new("action", "Did I lock the garage door?", false),
    new("action", "Did I feed the cat?", false),
    new("action", "Did I close the garage?", false),
};

Console.WriteLine("EverRecall evaluation — 30 staged scenarios vs. seeded demo day (2026-10-26)");
Console.WriteLine(new string('=', 96));

var byKind = new Dictionary<string, (int correct, int total)>();
var timestampErrors = new List<double>();

foreach (var s in scenarios)
{
    var r = engine.Ask(s.Question, events);
    var correct = r.Found == s.ExpectedFound;
    if (correct && s.ExpectedFound)
    {
        correct = r.MatchedEvent is not null
                  && (s.ExpectedObject is null || r.MatchedEvent.ObjectName == s.ExpectedObject)
                  && (s.ExpectedAction is null || r.MatchedEvent.Action == s.ExpectedAction);
        if (correct && s.ExpectedTime is not null && r.MatchedEvent is not null)
            timestampErrors.Add(Math.Abs((r.MatchedEvent.OccurredAt - s.ExpectedTime.Value).TotalSeconds));
    }

    var (c, t) = byKind.GetValueOrDefault(s.Kind);
    byKind[s.Kind] = (c + (correct ? 1 : 0), t + 1);
    Console.WriteLine($"{(correct ? "PASS" : "FAIL"),-4} [{s.Kind}] {s.Question}");
    if (!correct) Console.WriteLine($"     -> engine said: {r.Answer}");
}

Console.WriteLine(new string('=', 96));
foreach (var (kind, (c, t)) in byKind)
    Console.WriteLine($"{kind}-recall accuracy: {c}/{t} = {(double)c / t:P0}");
var totalCorrect = byKind.Values.Sum(v => v.correct);
Console.WriteLine($"Overall accuracy:      {totalCorrect}/{scenarios.Count} = {(double)totalCorrect / scenarios.Count:P0}");
Console.WriteLine(timestampErrors.Count > 0
    ? $"Mean timestamp error:  {timestampErrors.Average():F1} seconds over {timestampErrors.Count} timed scenarios"
    : "Mean timestamp error:  n/a");

record Scenario(string Kind, string Question, bool ExpectedFound,
                string? ExpectedObject = null, string? ExpectedAction = null, DateTime? ExpectedTime = null);
