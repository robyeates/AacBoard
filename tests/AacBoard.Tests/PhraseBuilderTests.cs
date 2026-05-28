using AacBoard.Models;
using AacBoard.Services;

namespace AacBoard.Tests;

public class PhraseBuilderTests
{
    // -------------------------------------------------------------------------
    // Test fixtures
    // -------------------------------------------------------------------------

    private static Symbol MakeSymbol(string id, string speakText, SymbolCategory category = SymbolCategory.Core) =>
        new(id, id, category, speakText);

    private static PhraseBuilder BuilderWith(params Symbol[] symbols)
    {
        var builder = new PhraseBuilder();
        foreach (var s in symbols) builder.Select(s);
        return builder;
    }

    // -------------------------------------------------------------------------
    // Build()
    // -------------------------------------------------------------------------

    [Fact]
    public void Build_NoSelections_ReturnsEmpty()
    {
        var builder = new PhraseBuilder();
        Assert.Equal(string.Empty, builder.Build());
    }

    [Fact]
    public void Build_SingleSymbol_CapitalisesAndAppendsPeriod()
    {
        var builder = BuilderWith(MakeSymbol("want", "want"));
        Assert.Equal("Want.", builder.Build());
    }

    [Fact]
    public void Build_MultipleSymbols_JoinsWithSpacesAndNormalises()
    {
        var builder = BuilderWith(
            MakeSymbol("i",    "I"),
            MakeSymbol("want", "want"),
            MakeSymbol("more", "more"));

        Assert.Equal("I want more.", builder.Build());
    }

    [Fact]
    public void Build_PhraseAlreadyHasTerminator_DoesNotDoublePunctuate()
    {
        var builder = BuilderWith(MakeSymbol("hello", "hello!"));
        Assert.Equal("Hello!", builder.Build());
    }

    [Fact]
    public void Build_SymbolsWithExtraWhitespace_TrimsCorrectly()
    {
        var builder = BuilderWith(
            MakeSymbol("i",    "  I  "),
            MakeSymbol("want", " want "));

        Assert.Equal("I want.", builder.Build());
    }

    // -------------------------------------------------------------------------
    // RemoveLast()
    // -------------------------------------------------------------------------

    [Fact]
    public void RemoveLast_EmptyBuilder_ReturnsFalse()
    {
        var builder = new PhraseBuilder();
        Assert.False(builder.RemoveLast());
    }

    [Fact]
    public void RemoveLast_WithSelections_RemovesMostRecent()
    {
        var builder = BuilderWith(
            MakeSymbol("i",    "I"),
            MakeSymbol("want", "want"));

        builder.RemoveLast();

        Assert.Single(builder.Selections);
        Assert.Equal("I.", builder.Build());
    }

    [Fact]
    public void RemoveLast_WithSelections_ReturnsTrue()
    {
        var builder = BuilderWith(MakeSymbol("i", "I"));
        Assert.True(builder.RemoveLast());
    }

    // -------------------------------------------------------------------------
    // Clear()
    // -------------------------------------------------------------------------

    [Fact]
    public void Clear_ResetsSelectionsToEmpty()
    {
        var builder = BuilderWith(
            MakeSymbol("i",    "I"),
            MakeSymbol("want", "want"));

        builder.Clear();

        Assert.Empty(builder.Selections);
        Assert.Equal(string.Empty, builder.Build());
    }

    // -------------------------------------------------------------------------
    // SpeakAsync()
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SpeakAsync_WithSelections_ReturnsBuiltPhrase()
    {
        var builder = BuilderWith(
            MakeSymbol("i",    "I"),
            MakeSymbol("want", "want"),
            MakeSymbol("more", "more"));

        var result = await builder.SpeakAsync();

        Assert.Equal("I want more.", result);
    }

    [Fact]
    public async Task SpeakAsync_NoSelections_ThrowsInvalidOperationException()
    {
        var builder = new PhraseBuilder();
        await Assert.ThrowsAsync<InvalidOperationException>(() => builder.SpeakAsync());
    }

    [Fact]
    public async Task SpeakAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        var builder = BuilderWith(MakeSymbol("i", "I"));
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<TaskCanceledException>(
            () => builder.SpeakAsync(cts.Token));
    }

    // -------------------------------------------------------------------------
    // StatusSummary()
    // -------------------------------------------------------------------------

    [Fact]
    public void StatusSummary_NoSelections_ReturnsNoneMessage()
    {
        var builder = new PhraseBuilder();
        Assert.Equal("No symbols selected.", builder.StatusSummary());
    }

    [Fact]
    public void StatusSummary_OneSelection_ReturnsSingularForm()
    {
        var builder = BuilderWith(MakeSymbol("want", "want"));
        Assert.Equal("1 symbol: \"Want.\"", builder.StatusSummary());
    }

    [Fact]
    public void StatusSummary_MultipleSelections_ReturnsPluralForm()
    {
        var builder = BuilderWith(
            MakeSymbol("i",    "I"),
            MakeSymbol("want", "want"));

        Assert.StartsWith("2 symbols:", builder.StatusSummary());
    }
}