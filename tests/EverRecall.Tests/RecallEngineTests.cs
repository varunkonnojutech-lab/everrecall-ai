using EverRecall.Api.Data;
using EverRecall.Api.Models;
using EverRecall.Api.Services;

namespace EverRecall.Tests;

public class RecallEngineTests
{
    private readonly RecallEngine _engine = new();
    private readonly List<MemoryEvent> _day = SeedData.DemoDay(new DateTime(2026, 10, 26));

    [Fact]
    public void Where_are_my_keys_returns_kitchen_drawer_at_941()
    {
        var r = _engine.Ask("Where are my keys?", _day);
        Assert.True(r.Found);
        Assert.NotNull(r.MatchedEvent);
        Assert.Equal("keys", r.MatchedEvent!.ObjectName);
        Assert.Equal("kitchen drawer", r.MatchedEvent.Location);
        Assert.Equal(new DateTime(2026, 10, 26, 9, 41, 0), r.MatchedEvent.OccurredAt);
        Assert.Contains("kitchen drawer", r.Answer);
    }

    [Fact]
    public void Where_is_my_wallet_returns_bedroom_dresser()
    {
        var r = _engine.Ask("Where is my wallet?", _day);
        Assert.True(r.Found);
        Assert.Equal("bedroom dresser", r.MatchedEvent!.Location);
        Assert.Equal(new DateTime(2026, 10, 26, 10, 20, 0), r.MatchedEvent.OccurredAt);
    }

    [Fact]
    public void Did_I_take_my_pill_answers_yes_with_time()
    {
        var r = _engine.Ask("Did I take my morning pill?", _day);
        Assert.True(r.Found);
        Assert.StartsWith("Yes", r.Answer);
        Assert.Equal(new DateTime(2026, 10, 26, 8, 15, 0), r.MatchedEvent!.OccurredAt);
    }

    [Fact]
    public void Did_I_lock_the_front_door_answers_yes()
    {
        var r = _engine.Ask("Did I lock the front door?", _day);
        Assert.True(r.Found);
        Assert.StartsWith("Yes", r.Answer);
        Assert.Equal("locked", r.MatchedEvent!.Action);
    }

    [Fact]
    public void Did_I_turn_off_the_stove_answers_yes()
    {
        var r = _engine.Ask("Did I turn off the stove?", _day);
        Assert.True(r.Found);
        Assert.StartsWith("Yes", r.Answer);
        Assert.Equal("turned_off", r.MatchedEvent!.Action);
    }

    [Fact]
    public void Unknown_action_gets_honest_no_memory()
    {
        var r = _engine.Ask("Did I water the plants?", _day);
        Assert.False(r.Found);
        Assert.Contains("don't have a memory", r.Answer);
        Assert.Null(r.MatchedEvent);
    }

    [Fact]
    public void Unknown_object_gets_honest_no_memory()
    {
        var r = _engine.Ask("Where are my glasses?", _day);
        Assert.False(r.Found);
        Assert.Contains("don't have a memory", r.Answer);
    }

    [Fact]
    public void Who_came_to_the_door_identifies_delivery_driver()
    {
        var r = _engine.Ask("Who came to the door?", _day);
        Assert.True(r.Found);
        Assert.Contains("Delivery driver", r.Answer);
    }

    [Fact]
    public void Is_the_stove_off_reports_state()
    {
        var r = _engine.Ask("Is the stove off?", _day);
        Assert.True(r.Found);
        Assert.Contains("off", r.Answer);
    }
}
