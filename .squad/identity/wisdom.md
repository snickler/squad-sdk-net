# Squad Wisdom

> Lessons learned, patterns discovered, truths earned.

## Founding Wisdom (inherited from Skyflow)

1. **Sealed records are the default.** Mutable classes are the exception.
2. **Source-gen JSON everywhere.** No reflection-based serialization survives AOT.
3. **ILogger<T> is non-negotiable.** Console.WriteLine is a bug.
4. **Tests ship with code.** No exceptions.
5. **Central Package Management prevents version drift.** Always use Directory.Packages.props.
6. **ConfigureAwait(false) in library code.** Always.
7. **The build must pass before any handoff.** Broken builds break trust.
