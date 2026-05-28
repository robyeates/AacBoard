# AacBoard

A small .NET 8 project modelling an AAC (Augmentative and Alternative Communication)
board. I built this to prove to myself that the Java-to-C# transition is real, not
just "the concepts transfer" hand-waving. It is aimed at the Smartbox interview context
but the domain model is genuine.

AAC software like Grid 3 lets people with speech disabilities communicate by selecting
symbols on a grid, which are assembled into spoken phrases. This project models the
core of that loop: a symbol board, a phrase builder, and a simple predictor that learns
which symbols tend to follow each other.

---

## Structure

```
AacBoard.sln
src/
  AacBoard/
    Models/
      Symbol.cs          -- record type, SymbolCategory enum
      GridPosition.cs    -- record for (row, col) addressing
      Board.cs           -- sparse grid, LINQ queries
    Services/
      PhraseBuilder.cs   -- assembles and speaks utterances
      SymbolPredictor.cs -- bigram co-occurrence model
    Program.cs           -- runnable demo
tests/
  AacBoard.Tests/
    PhraseBuilderTests.cs
    SymbolPredictorTests.cs
```

---

## Running it

```bash
# Run the demo
dotnet run --project src/AacBoard

# Run the tests
dotnet test
```

Requires .NET 8 SDK. No database, no external services, nothing to configure.

---

## What the code demonstrates

**Records.** `Symbol` and `GridPosition` are C# records. Structural equality, immutability,
and a clean primary constructor syntax. Java has had records since Java 16 and they work
the same way. The difference is that C# nullable reference types (`string? ImagePath`)
let the compiler enforce null safety without the `Optional<T>` wrapping and unwrapping
you would do in Java.

**LINQ.** Used in `Board.SymbolsByCategory`, `Board.Search`, and throughout the predictor
and program. It reads identically to the Java Stream API once you map the names:
`Where` is `filter`, `Select` is `map`, `OrderBy` is `sorted`. The lazy evaluation
model is the same. The main practical difference is that LINQ works on `IEnumerable<T>`
which is a much broader surface than Java streams, so you end up writing it more often.

**async/await.** `PhraseBuilder.SpeakAsync` and `SymbolPredictor.RecordSelectionAsync`
simulate async I/O paths. In a real device these would call a TTS engine and persist to
a user profile store. The model maps cleanly from Java: `Task<T>` is
`CompletableFuture<T>`, `await` replaces `.thenApply()` chaining, and `CancellationToken`
is the equivalent of passing an `ExecutorService` or using `Future.cancel()`. The
compiler generates the state machine so there is no manual threading code.

**Pattern matching.** The switch expression in `PhraseBuilder.StatusSummary` and the
`when` guard in `SymbolPredictor.PredictNext` are the clearest examples. Java has
switch expressions from Java 14 and pattern matching for `instanceof` from Java 16,
so this is familiar territory, just with slightly different syntax.

**Nullable reference types.** Enabled project-wide. The compiler warns if you
dereference a nullable without a null check. `Board.SymbolAt` returns `Symbol?` rather
than `Optional<Symbol>` — same intent, less ceremony at the call site.

---

## Java to C# reference

| Concept | Java | C# |
|---|---|---|
| Value type / data class | `record` (Java 16+) or Lombok `@Value` | `record` |
| Null safety | `Optional<T>` | Nullable reference types (`T?`) |
| Stream pipeline | `Stream<T>` | `IEnumerable<T>` + LINQ |
| Filter | `.filter(x -> ...)` | `.Where(x => ...)` |
| Map | `.map(x -> ...)` | `.Select(x => ...)` |
| Collect to list | `.collect(Collectors.toList())` | `.ToList()` |
| Async return type | `CompletableFuture<T>` | `Task<T>` |
| Async composition | `.thenApply()` / `.thenCompose()` | `await` |
| Cancellation | `Future.cancel()` / `ExecutorService` | `CancellationToken` |
| Connection pooling | HikariCP | ADO.NET built-in / connection strings |
| ORM | Spring Data JPA / Hibernate | Entity Framework Core |
| IoC container | Spring `@Component` / `@Autowired` | `Microsoft.Extensions.DependencyInjection` |
| Test framework | JUnit 5 | xUnit |
| Mocking | Mockito | Moq / NSubstitute |
| Build tool | Maven / Gradle | MSBuild / `dotnet` CLI |

---

## Honest notes

Things that were straightforward coming from Java: the type system, generics, collections,
async patterns, records, and the overall project structure. The `dotnet` CLI feels like
Maven with less XML.

Things that took a bit of reading: C# properties (no Lombok needed, auto-properties are
built in), the difference between `IEnumerable`, `ICollection`, `IList`, and their
read-only counterparts, and how nullable reference types interact with the compiler
warnings rather than being a runtime feature.

Things I think are genuinely better than Java: LINQ is more composable than streams in
practice, `async/await` is less verbose than `CompletableFuture` chaining, and not
needing Lombok for data classes is a relief.

WPF, XAML, and Avalonia are not covered here. I did not want to fake familiarity with
the UI layer. That is a real gap and a short ramp.

---

## What is AAC

Augmentative and Alternative Communication covers any method that supplements or
replaces speech for people who cannot rely on it. Symbol-based AAC systems like Grid 3
display a grid of images and words. A user selects symbols in sequence to build a
phrase, which the device then speaks aloud. The order and layout of symbols matters
enormously for communication speed, which is why vocabulary research and symbol
prediction are active areas of work in this space.