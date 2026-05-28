namespace AacBoard.Models;

/// <summary>
/// The atomic unit of an AAC communication board — a single selectable symbol.
/// </summary>
/// <remarks>
/// Modelled as a C# <c>record</c> for structural equality and immutability.
/// Java equivalent: a Java 16+ record, or a Lombok <c>@Value</c> class.
/// The optional <c>ImagePath</c> uses a nullable reference type (<c>string?</c>) —
/// compiler-enforced nullability without Optional wrapping/unwrapping ceremony.
/// </remarks>
/// <param name="Id">Stable unique identifier (e.g. "sym_want").</param>
/// <param name="Label">Short display text shown beneath the symbol image.</param>
/// <param name="Category">Semantic grouping used for colour-coding and filtering.</param>
/// <param name="SpeakText">The word or phrase spoken aloud when this symbol is activated.</param>
/// <param name="ImagePath">Optional path to the symbol's image asset.</param>
public sealed record Symbol(
    string Id,
    string Label,
    SymbolCategory Category,
    string SpeakText,
    string? ImagePath = null
);

/// <summary>
/// Broad semantic categories used by AAC software to organise and colour-code symbols.
/// </summary>
/// <remarks>
/// Java note: identical to a Java <c>enum</c>. C# enums are int-backed value types by default.
/// </remarks>
public enum SymbolCategory
{
    /// <summary>High-frequency words: I, want, more, stop, help.</summary>
    Core,
    Verb,
    Noun,
    Descriptor,
    /// <summary>Greetings and social phrases: hello, thank you, yes, no.</summary>
    Social,
    /// <summary>Board-level navigation actions: back, home, speak.</summary>
    Navigation
}