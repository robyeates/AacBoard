using AacBoard.Models;

namespace AacBoard.Services;

/// <summary>
/// Assembles a spoken sentence from a sequence of user-selected symbols.
/// </summary>
/// <remarks>
/// In a real AAC device this sits between the symbol grid (UI) and the TTS engine.
/// The builder accumulates selections, normalises them into a grammatical phrase,
/// then hands the result to the speech synthesiser.<br/>
/// <b>Java note:</b> <c>async/await</c> is the C# equivalent of
/// <c>CompletableFuture.supplyAsync()</c> — but it composes with <c>await</c> inline
/// rather than chaining <c>.thenApply()</c>. The <c>async</c> keyword on the method
/// signature is the only declaration overhead; the compiler generates the state machine.
/// </remarks>
public sealed class PhraseBuilder
{
    private readonly List<Symbol> _selections = new();

    /// <summary>Read-only view of the symbols selected so far, in order.</summary>
    public IReadOnlyList<Symbol> Selections => _selections;

    /// <summary>Appends a symbol to the current phrase.</summary>
    public void Select(Symbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        _selections.Add(symbol);
    }

    /// <summary>Removes the most recently selected symbol (backspace behaviour).</summary>
    /// <returns><c>true</c> if a symbol was removed; <c>false</c> if the phrase was empty.</returns>
    public bool RemoveLast()
    {
        if (_selections.Count == 0) return false;
        _selections.RemoveAt(_selections.Count - 1);
        return true;
    }

    /// <summary>Clears all selections, resetting the builder for a new utterance.</summary>
    public void Clear() => _selections.Clear();

    /// <summary>
    /// Builds the spoken phrase by joining each symbol's <see cref="Symbol.SpeakText"/>,
    /// applying light normalisation so the result reads naturally to a TTS engine.
    /// </summary>
    /// <returns>
    /// A normalised sentence string, or <see cref="string.Empty"/> if no symbols
    /// have been selected.
    /// </returns>
    /// <remarks>
    /// <b>Java note:</b> <c>string.Join</c> is equivalent to
    /// <c>Collectors.joining(" ")</c> on a stream. LINQ <c>Select</c> here
    /// projects each symbol to its speak text before joining — same as
    /// <c>Stream.map(Symbol::getSpeakText)</c>.
    /// </remarks>
    public string Build()
    {
        if (_selections.Count == 0) return string.Empty;

        var raw = string.Join(" ",
            _selections.Select(s => s.SpeakText.Trim()));

        return Normalise(raw);
    }

    /// <summary>
    /// Simulates dispatching the built phrase to a TTS engine asynchronously.
    /// </summary>
    /// <returns>The spoken phrase that was dispatched.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when there are no symbols selected to speak.
    /// </exception>
    /// <remarks>
    /// <b>Java note:</b> <c>Task&lt;string&gt;</c> maps to <c>CompletableFuture&lt;String&gt;</c>.
    /// <c>Task.Delay</c> is the equivalent of <c>Thread.sleep()</c> inside an async context —
    /// it yields the thread rather than blocking it.
    /// </remarks>
    public async Task<string> SpeakAsync(CancellationToken cancellationToken = default)
    {
        var phrase = Build();

        if (string.IsNullOrEmpty(phrase))
            throw new InvalidOperationException("No symbols selected — nothing to speak.");

        // Simulate TTS dispatch latency (real impl would call Windows.Media.SpeechSynthesis
        // or Azure Cognitive Services TTS here)
        await Task.Delay(50, cancellationToken);

        return phrase;
    }

    /// <summary>
    /// Returns a summary of the current phrase state for display in the UI bar.
    /// </summary>
    /// <remarks>
    /// Demonstrates C# pattern matching on the selection count —
    /// Java equivalent would be an if/else chain or a switch expression (Java 14+).
    /// </remarks>
    public string StatusSummary() => _selections.Count switch
    {
        0 => "No symbols selected.",
        1 => $"1 symbol: \"{Build()}\"",
        _ => $"{_selections.Count} symbols: \"{Build()}\""
    };

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Capitalises the first character and appends a full stop if absent.
    /// </summary>
    private static string Normalise(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw;

        // Pattern match on the first char to decide capitalisation
        var trimmed = raw.Trim();
        var sentence = char.ToUpperInvariant(trimmed[0]) + trimmed[1..];

        return sentence.EndsWith('.') || sentence.EndsWith('?') || sentence.EndsWith('!')
            ? sentence
            : sentence + ".";
    }
}