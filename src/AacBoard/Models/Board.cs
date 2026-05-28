namespace AacBoard.Models;

/// <summary>
/// A grid of symbols arranged in rows and columns, modelling a single AAC communication page.
/// </summary>
/// <remarks>
/// Symbols are stored in a flat dictionary keyed by <see cref="GridPosition"/> rather than
/// a jagged array — sparse boards (common in AAC) stay memory-efficient and position lookups
/// are O(1).<br/>
/// <b>Java note:</b> <c>IReadOnlyDictionary</c> is the rough equivalent of
/// <c>Collections.unmodifiableMap()</c>, but enforced at the type level rather than at runtime.
/// </remarks>
public sealed class Board
{
    private readonly Dictionary<GridPosition, Symbol> _cells = new();

    /// <summary>Display name for this page, e.g. "Home", "Food", "Feelings".</summary>
    public string Name { get; }

    /// <summary>Maximum number of rows on this board.</summary>
    public int Rows { get; }

    /// <summary>Maximum number of columns on this board.</summary>
    public int Columns { get; }

    /// <summary>Read-only view of all placed symbols, keyed by grid position.</summary>
    public IReadOnlyDictionary<GridPosition, Symbol> Cells => _cells;

    /// <param name="name">Display name for this board page.</param>
    /// <param name="rows">Row count of the grid.</param>
    /// <param name="columns">Column count of the grid.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when rows or columns are less than 1.
    /// </exception>
    public Board(string name, int rows, int columns)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(rows, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);

        Name = name;
        Rows = rows;
        Columns = columns;
    }

    /// <summary>Places a symbol at the given grid position.</summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the position falls outside the board's bounds.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the cell is already occupied.
    /// </exception>
    public void Place(Symbol symbol, GridPosition position)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        EnsureInBounds(position);

        if (_cells.ContainsKey(position))
            throw new InvalidOperationException(
                $"Cell {position} is already occupied by '{_cells[position].Label}'.");

        _cells[position] = symbol;
    }

    /// <summary>
    /// Returns the symbol at <paramref name="position"/>, or <c>null</c> if the cell is empty.
    /// </summary>
    /// <remarks>
    /// <b>Java note:</b> returns <c>Symbol?</c> (nullable reference type) rather than
    /// <c>Optional&lt;Symbol&gt;</c> — same intent, less wrapping ceremony at the call site.
    /// </remarks>
    public Symbol? SymbolAt(GridPosition position)
    {
        EnsureInBounds(position);
        return _cells.GetValueOrDefault(position);
    }

    /// <summary>Returns all symbols belonging to the given category.</summary>
    /// <remarks>
    /// <b>Java note:</b> LINQ <c>Where</c> + <c>Select</c> are direct equivalents of
    /// <c>Stream.filter()</c> + <c>Stream.map()</c>. The pipeline is lazily evaluated
    /// identically — nothing materialises until the caller iterates or calls
    /// <c>.ToList()</c> / <c>.ToArray()</c>.
    /// </remarks>
    public IEnumerable<Symbol> SymbolsByCategory(SymbolCategory category) =>
        _cells.Values
              .Where(s => s.Category == category)
              .OrderBy(s => s.Label);

    /// <summary>Returns all symbols whose label contains the search term (case-insensitive).</summary>
    public IEnumerable<Symbol> Search(string term) =>
        _cells.Values
              .Where(s => s.Label.Contains(term, StringComparison.OrdinalIgnoreCase)
                       || s.SpeakText.Contains(term, StringComparison.OrdinalIgnoreCase))
              .OrderBy(s => s.Label);

    /// <summary>Total number of symbols currently placed on the board.</summary>
    public int SymbolCount => _cells.Count;

    private void EnsureInBounds(GridPosition position)
    {
        if (position.Row < 0 || position.Row >= Rows ||
            position.Column < 0 || position.Column >= Columns)
            throw new ArgumentOutOfRangeException(nameof(position),
                $"{position} is outside board bounds ({Rows}×{Columns}).");
    }
}