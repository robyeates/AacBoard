using AacBoard.Models;
using AacBoard.Services;

namespace AacBoard.Tests;

public class SymbolPredictorTests
{
    // -------------------------------------------------------------------------
    // Test fixtures
    // -------------------------------------------------------------------------

    private static Symbol MakeSymbol(string id, SymbolCategory category = SymbolCategory.Core) =>
        new(id, id, category, id);

    private static readonly Symbol I      = MakeSymbol("i");
    private static readonly Symbol Want   = MakeSymbol("want");
    private static readonly Symbol More   = MakeSymbol("more");
    private static readonly Symbol Help   = MakeSymbol("help");
    private static readonly Symbol Drink  = MakeSymbol("drink", SymbolCategory.Noun);
    private static readonly Symbol Eat    = MakeSymbol("eat",   SymbolCategory.Verb);

    private static SymbolPredictor TrainedPredictor()
    {
        var predictor = new SymbolPredictor();
        predictor.RegisterSymbols(new[] { I, Want, More, Help, Drink, Eat });

        // Train with three utterances
        predictor.RecordSelection(new[] { I, Want, More  });
        predictor.RecordSelection(new[] { I, Want, Drink });
        predictor.RecordSelection(new[] { I, Want, More  });

        return predictor;
    }

    // -------------------------------------------------------------------------
    // PredictNext() — cold start
    // -------------------------------------------------------------------------

    [Fact]
    public void PredictNext_NullLastSelected_ReturnsMostFrequent()
    {
        var predictor = TrainedPredictor();
        var results = predictor.PredictNext(lastSelected: null);

        // I appears 3 times — should be top cold-start suggestion
        Assert.Equal("i", results.First().Id);
    }

    [Fact]
    public void PredictNext_NullLastSelected_RespectsMaxResults()
    {
        var predictor = TrainedPredictor();
        var results = predictor.PredictNext(lastSelected: null, maxResults: 2);

        Assert.Equal(2, results.Count);
    }

    // -------------------------------------------------------------------------
    // PredictNext() — bigram context
    // -------------------------------------------------------------------------

    [Fact]
    public void PredictNext_AfterI_SuggestsWantFirst()
    {
        var predictor = TrainedPredictor();
        var results = predictor.PredictNext(lastSelected: I);

        // "want" followed "i" three times — must be top suggestion
        Assert.Equal("want", results.First().Id);
    }

    [Fact]
    public void PredictNext_AfterWant_RanksMoreAboveDrink()
    {
        var predictor = TrainedPredictor();
        var results = predictor.PredictNext(lastSelected: Want);

        var ids = results.Select(s => s.Id).ToList();

        // "more" followed "want" twice, "drink" once
        Assert.Equal("more",  ids[0]);
        Assert.Equal("drink", ids[1]);
    }

    [Fact]
    public void PredictNext_DoesNotSuggestLastSelectedSymbolItself()
    {
        var predictor = TrainedPredictor();
        var results = predictor.PredictNext(lastSelected: I);

        Assert.DoesNotContain(results, s => s.Id == "i");
    }

    [Fact]
    public void PredictNext_UnseenSymbol_FallsBackToColdStart()
    {
        var predictor = TrainedPredictor();

        // Help was never followed by anything — should fall back gracefully
        var results = predictor.PredictNext(lastSelected: Help);

        Assert.NotNull(results);
        Assert.True(results.Count > 0);
    }

    [Fact]
    public void PredictNext_InvalidMaxResults_ThrowsArgumentOutOfRangeException()
    {
        var predictor = new SymbolPredictor();
        Assert.Throws<ArgumentOutOfRangeException>(
            () => predictor.PredictNext(null, maxResults: 0));
    }

    // -------------------------------------------------------------------------
    // RecordSelectionAsync()
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RecordSelectionAsync_UpdatesModelIdenticallyToSync()
    {
        var syncPredictor  = new SymbolPredictor();
        var asyncPredictor = new SymbolPredictor();

        syncPredictor.RegisterSymbols(new[]  { I, Want, More });
        asyncPredictor.RegisterSymbols(new[] { I, Want, More });

        var selections = new[] { I, Want, More };

        syncPredictor.RecordSelection(selections);
        await asyncPredictor.RecordSelectionAsync(selections);

        var syncResults  = syncPredictor.PredictNext(lastSelected: I);
        var asyncResults = asyncPredictor.PredictNext(lastSelected: I);

        Assert.Equal(
            syncResults.Select(s => s.Id),
            asyncResults.Select(s => s.Id));
    }

    [Fact]
    public async Task RecordSelectionAsync_CancelledToken_ThrowsTaskCanceledException()
    {
        var predictor = new SymbolPredictor();
        predictor.RegisterSymbols(new[] { I, Want });

        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => predictor.RecordSelectionAsync(new[] { I, Want }, cts.Token));
    }
}