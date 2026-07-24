# Broiler.Regex

An experimental .NET 10 regular-expression engine implementing ECMAScript matching
semantics for Broiler.JS. The implementation and limitations are documented in the
[engine README](Broiler.Regex/README.md).

> **Preview status:** This is unstable, substantially AI-assisted software with known
> unsupported ECMAScript features. Human-review approval is revision-scoped; consult
> [HUMAN_REVIEW.md](HUMAN_REVIEW.md) for the reviewed revision and conditions before
> describing the current checkout as approved.

Remaining implementation and Broiler.JS adoption work is tracked in the
[current roadmap](docs/roadmap.md).

Build and test with:

```bash
dotnet build Broiler.Regex.slnx
dotnet test Broiler.Regex.slnx
```

Broiler.Regex is licensed under the [Apache License 2.0](LICENSE), which provides the
software on an “AS IS” basis without warranties or conditions.
