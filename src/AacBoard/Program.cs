using AacBoard.Models;
using AacBoard.Services;

Console.WriteLine("=== AacBoard Demo ===");
Console.WriteLine();

// -------------------------------------------------------------------------
// 1. Build a board
// -------------------------------------------------------------------------

var homeBoard = new Board("Home", rows: 4, columns: 6);

var symbols = new[]
{
    new Symbol("i",         "I",         SymbolCategory.Core,       "I"),
    new Symbol("want",      "want",      SymbolCategory.Core,       "want"),
    new Symbol("more",      "more",      SymbolCategory.Core,       "more"),
    new Symbol("help",      "help",      SymbolCategory.Core,       "help"),
    new Symbol("stop",      "stop",      SymbolCategory.Core,       "stop"),
    new Symbol("yes",       "yes",       SymbolCategory.Social,     "yes"),
    new Symbol("no",        "no",        SymbolCategory.Social,     "no"),
    new Symbol("hello",     "hello",     SymbolCategory.Social,     "hello"),
    new Symbol("eat",       "eat",       SymbolCategory.Verb,       "eat"),
    new Symbol("drink",     "drink",     SymbolCategory.Verb,       "drink"),
    new Symbol("go",        "go",        SymbolCategory.Verb,       "go"),
    new Symbol("biscuit",   "biscuit",   SymbolCategory.Noun,       "biscuit"),
    new Symbol("water",     "water",     SymbolCategory.Noun,       "water"),
    new Symbol("outside",   "outside",   SymbolCategory.Descriptor, "outside"),
    new Symbol("tired",     "tired",     SymbolCategory.Descriptor, "tired"),
};

// Place symbols row by row
var positions = symbols
    .Select((sym, idx) => (sym, pos: new GridPosition(idx / homeBoard.Columns, idx % homeBoard.Columns)))
    .ToList();

foreach (var (sym, pos) in positions)
    homeBoard.Place(sym, pos);

Console.WriteLine($"Board: \"{homeBoard.Name}\" — {homeBoard.SymbolCount} symbols placed on a {homeBoard.Rows}×{homeBoard.Columns} grid");
Console.WriteLine();

// -------------------------------------------------------------------------
// 2. LINQ queries against the board
// -------------------------------------------------------------------------

var coreSymbols = homeBoard.SymbolsByCategory(SymbolCategory.Core).ToList();
Console.WriteLine($"Core symbols ({coreSymbols.Count}): {string.Join(", ", coreSymbols.Select(s => s.Label))}");

var searchResults = homeBoard.Search("o").ToList();
Console.WriteLine($"Search \"o\" ({searchResults.Count}): {string.Join(", ", searchResults.Select(s => s.Label))}");
Console.WriteLine();

// -------------------------------------------------------------------------
// 3. PhraseBuilder — assemble and speak an utterance
// -------------------------------------------------------------------------

Console.WriteLine("--- PhraseBuilder ---");

var builder = new PhraseBuilder();

var selectedIds = new[] { "i", "want", "more", "water" };
foreach (var id in selectedIds)
{
    var sym = homeBoard.Cells.Values.First(s => s.Id == id);
    builder.Select(sym);
    Console.WriteLine($"  + selected: {sym.Label}   [{builder.StatusSummary()}]");
}

Console.WriteLine();

var phrase = await builder.SpeakAsync();
Console.WriteLine($"Spoken: \"{phrase}\"");
Console.WriteLine();

// Demonstrate backspace
builder.RemoveLast();
Console.WriteLine($"After backspace: \"{builder.Build()}\"");
Console.WriteLine();

// -------------------------------------------------------------------------
// 4. SymbolPredictor — train and predict
// -------------------------------------------------------------------------

Console.WriteLine("--- SymbolPredictor ---");

var predictor = new SymbolPredictor();
predictor.RegisterSymbols(symbols);

// Simulate historical utterances from a user's profile
var trainingData = new[]
{
    new[] { "i", "want", "more" },
    new[] { "i", "want", "drink" },
    new[] { "i", "want", "eat" },
    new[] { "i", "want", "more" },
    new[] { "i", "help" },
    new[] { "i", "want", "biscuit" },
};

foreach (var utterance in trainingData)
{
    var syms = utterance
        .Select(id => symbols.First(s => s.Id == id))
        .ToArray();

    await predictor.RecordSelectionAsync(syms);
}

Console.WriteLine("Trained on 6 historical utterances.");
Console.WriteLine();

// Cold start — no prior context
var coldStart = predictor.PredictNext(lastSelected: null, maxResults: 3);
Console.WriteLine($"Cold-start top 3: {string.Join(", ", coldStart.Select(s => s.Label))}");

// After selecting "I"
var afterI = predictor.PredictNext(lastSelected: symbols.First(s => s.Id == "i"), maxResults: 3);
Console.WriteLine($"After \"I\"  top 3: {string.Join(", ", afterI.Select(s => s.Label))}");

// After selecting "want"
var afterWant = predictor.PredictNext(lastSelected: symbols.First(s => s.Id == "want"), maxResults: 3);
Console.WriteLine($"After \"want\" top 3: {string.Join(", ", afterWant.Select(s => s.Label))}");

Console.WriteLine();
Console.WriteLine("=== Done ===");