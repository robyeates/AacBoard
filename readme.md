# AacBoard

A small .NET 8 project modelling an AAC (Augmentative and Alternative Communication)
board. I built this to prove to myself that the Java-to-C# transition is real, not
just "the concepts transfer" hand-waving. With help from Claude, I wanted to get into the real differences

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

## Notes

Things that were straightforward coming from Java: the type system, generics, collections,
async patterns, records, and the overall project structure. The `dotnet` CLI feels like
Maven lifecycle stages without XML.

Things that took a bit of studying: C# properties (no Lombok needed, auto-properties are
built in), the difference between `IEnumerable`, `ICollection`, `IList`, and their
read-only counterparts, and how nullable reference types interact with the compiler
warnings rather than being a runtime feature.

LINQ seems more composable than streams in practice, `async/await` is less verbose than `CompletableFuture` chaining. 

Data classes vs Records is interesting

WPF, XAML, and Avalonia are not covered here. I did not want to fake familiarity with
the UI layer. That is a real gap.

---

## Left to investigate

Exception handling -- how do exceptions bubble through async call chains, and what
does the equivalent of Spring's @ControllerAdvice look like in a .NET minimal API
or WPF application.
API contracts -- HTTP and RPC in C#. Minimal API vs controllers, gRPC with
Grpc.AspNetCore, and how contract-first design (OpenAPI, Protobuf) is typically
handled compared to Spring ecosystem tooling.
Security / SSO / JWT -- encryption at rest, in-memory secret handling, and whether
the Kubernetes-native patterns I know (Vault, etcd, sealed secrets, workload identity)
map cleanly onto Azure-hosted .NET services or whether there is a preferred
Microsoft.Extensions approach that replaces them.
Persistence -- ADO.NET connection management vs HikariCP and PgBouncer. How Entity
Framework Core handles connection pooling, and whether the tuning surface is comparable
to what I am used to on the JVM.
Observability -- hooking into APM tooling and exposing Prometheus metrics from a
.NET service. OpenTelemetry.Instrumentation.* looks like the right path but I have
not validated it end to end.
UI -- WPF and Avalonia vs Swing and JavaFX. This is the most significant gap for
a Smartbox context given Grid is a desktop-first product. It is a real ramp, not a
transferable-concepts hand-wave.
