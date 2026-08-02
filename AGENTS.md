# Engineering Guidelines

## Scope and priorities

- Follow the requirements in `Docs/` and keep implementation decisions traceable to them.
- Prefer the smallest complete, maintainable change that satisfies the documented behavior.
- Preserve existing user work and avoid unrelated refactors.

## Architecture

- Use MVVM for UI behavior: keep views declarative, put presentation state and commands in view models, and isolate infrastructure behind interfaces.
- Keep domain models independent of WPF and persistence details.
- Apply dependency inversion at external boundaries such as storage, printing, dialogs, and device integration.
- Favor composition and focused types over inheritance and large multipurpose classes.
- Keep public APIs minimal and make invalid state difficult to represent.
- if the app currently running, simply ask the user to run the build. dont try anything else.

## C# conventions

- Target .NET 10 and do not downgrade the target framework to match stale documentation.
- Enable and respect nullable reference types and implicit usings.
- Use modern, idiomatic C# supported by the project's target framework.
- Use clear, intention-revealing names; avoid abbreviations unless they are established domain terms.
- Prefer immutable data and `readonly` members where practical.
- Validate arguments at boundaries and fail with actionable exceptions or user-facing errors.
- Do not swallow exceptions. Catch only when adding context, recovering, or translating to an appropriate result.
- Use async APIs for I/O and propagate `CancellationToken` where an operation may be long-running.
- Keep methods cohesive and short enough to understand without hidden side effects.

## WPF conventions

- Avoid business logic and mutable application state in code-behind; code-behind is limited to view-only concerns that cannot be expressed cleanly in XAML.
- Use bindings, commands, styles, templates, and resources instead of directly manipulating controls.
- Use `INotifyPropertyChanged` correctly and update UI-bound collections on the UI thread.
- Put reusable colors, spacing, typography, and control styles in resource dictionaries.
- Design for keyboard access, visible focus, readable contrast, screen readers, resizing, and high-DPI displays.
- Dispose subscriptions and other resources whose lifetime is shorter than the application lifetime.

## Quality and verification

- Add or update tests for domain rules, view-model behavior, serialization, and regressions.
- Structure time, filesystem, dialogs, printing, and other environmental dependencies so they can be tested deterministically.
- Run formatting, build, and relevant tests before considering work complete.
- Keep the build warning-free; do not suppress warnings without a documented reason.
- Check empty, invalid, boundary, and failure cases—not only the happy path.

## Security and data handling

- Treat imported files and user-entered values as untrusted input.
- Never commit secrets, credentials, machine-specific paths, or personal data.
- Avoid logging sensitive content; logs and error messages should contain useful context without exposing private data.
- Use safe, explicit serialization and file paths, and avoid deserializing arbitrary types.

## Documentation and change hygiene

- Document non-obvious design decisions and constraints, not self-evident syntax.
- Keep documentation aligned with behavior when requirements or workflows change.
- Use focused commits and concise commit messages when commits are requested.
- In handoff notes, state what changed, how it was verified, and any known limitations.
