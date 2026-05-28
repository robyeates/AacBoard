using AacBoard.Models;

namespace AacBoard.Services;

/// <summary>
/// Suggests the next likely symbol based on co-occurrence frequency of prior selections.
/// </summary>
/// <remarks>
/// Uses a simple bigram frequency model: for each symbol A, we record how often
/// symbol B followed it. On prediction, we return the top-N candidates ordered
/// by frequency.<br/>
/// This mirrors how real AAC software like Grid 3 learns a user's communication
/// patterns over time without requiring an ML model.<br/>
/// <b>Java note:</b> <c>Dictionary&lt;TKey, TValue&gt;</c> is <c>HashMap</c>.
/// The nested dictionary here (<c>Dictionary&lt;string, Dictionary&lt;string, int&gt;&gt;</c>)
/// would typically be written the same way in Java — or with Guava's <c>Table</c>.
/// </remarks>
public sealed class SymbolPredictor
{
    // co[A][B] = number of times B was selected immediately after A
    private readonly Dictionary<string, Dictionary<string, int>> _coOccurrence = new();

    // Flat frequency count for cold-start predictions (no prior context)
    private readonly Dictionary<string, int> _frequency = new();

    // Symbol registry so we can resolve IDs back to Symbol records
    private readonly Dictionary<string, Symbol> _registry = new();

    /// <summary>
    /// Registers symbols that the predictor is allowed to suggest.
    /// Must be called before <see cref="RecordSelection"/> or <see cref="PredictNext"/>.
    /// </summary>
    public void RegisterSymbols(IEnumerable<Symbol> symbols)
    {
        ArgumentNullException.ThrowIfNull(symbols);

        foreach (var symbol in symbols)
            _registry[symbol.Id] = symbol;
    }

    /// <summary>
    /// Records a sequence of symbol selections to train the frequency model.
    /// </summary>
    /// <remarks>
    /// <b>Java note:</b> <c>Enumerable.Zip</c> pairs adjacent elements — equivalent to
    /// <c>IntStream.range(0, list.size() - 1).forEach(i -> ...)</c> but more expressive.
    /// </remarks>
    /// <param name="selections">Ordered list of symbols from a completed utterance.</param>
    public void RecordSelection(IReadOnlyList<Symbol> selections)
    {
        ArgumentNullException.ThrowIfNull(selections);

        // Update flat frequency counts
        foreach (var symbol in selections)
        {
            _frequency[symbol.Id] = _frequency.GetValueOrDefault(symbol.Id) + 1;
            _registry.TryAdd(symbol.Id, symbol);
        }

        // Update co-occurrence counts for each consecutive pair (bigram)
        // Zip pairs: [A, B, C] → (A,B), (B,C)
        foreach (var (current, next) in selections.Zip(selections.Skip(1)))
        {
            if (!_coOccurrence.ContainsKey(current.Id))
                _coOccurrence[current.Id] = new Dictionary<string, int>();

            var followers = _coOccurrence[current.Id];
            followers[next.Id] = followers.GetValueOrDefault(next.Id) + 1;
        }
    }

    /// <summary>
    /// Predicts the top <paramref name="maxResults"/> symbols likely to follow
    /// <paramref name="lastSelected"/>.
    /// </summary>
    /// <param name="lastSelected">The most recently activated symbol, or <c>null</c>
    /// for a cold-start prediction.</param>
    /// <param name="maxResults">Maximum number of suggestions to return.</param>
    /// <returns>
    /// Predicted symbols ordered by descending likelihood, excluding
    /// <paramref name="lastSelected"/> itself.
    /// </returns>
    /// <remarks>
    /// <b>Java note:</b> LINQ <c>OrderByDescending().Take()</c> is
    /// <c>Stream.sorted(Comparator.reverseOrder()).limit(n)</c>.
    /// </remarks>
    public IReadOnlyList<Symbol> PredictNext(Symbol? lastSelected, int maxResults = 5)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxResults, 1);

        // Pattern match on whether we have prior context
        var candidates = lastSelected switch
        {
            null => ColdStartCandidates(),
            var s when _coOccurrence.ContainsKey(s.Id) => BigramCandidates(s.Id),
            _ => ColdStartCandidates()
        };

        return candidates
            .Where(pair => pair.symbolId != lastSelected?.Id)
            .Where(pair => _registry.ContainsKey(pair.symbolId))
            .OrderByDescending(pair => pair.score)
            .Take(maxResults)
            .Select(pair => _registry[pair.symbolId])
            .ToList();
    }

    /// <summary>
    /// Asynchronously records selections — models the real-world case where
    /// persistence to a user profile store is awaited before returning.
    /// </summary>
    /// <remarks>
    /// <b>Java note:</b> equivalent to returning a <c>CompletableFuture&lt;Void&gt;</c>
    /// from a service method that writes to a database.
    /// </remarks>
    public async Task RecordSelectionAsync(
        IReadOnlyList<Symbol> selections,
        CancellationToken cancellationToken = default)
    {
        // Simulate async profile persistence (e.g. EF Core SaveChangesAsync)
        await Task.Delay(10, cancellationToken);
        RecordSelection(selections);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private IEnumerable<(string symbolId, int score)> BigramCandidates(string currentId) =>
        _coOccurrence[currentId]
            .Select(kvp => (symbolId: kvp.Key, score: kvp.Value));

    private IEnumerable<(string symbolId, int score)> ColdStartCandidates() =>
        _frequency
            .Select(kvp => (symbolId: kvp.Key, score: kvp.Value));
}