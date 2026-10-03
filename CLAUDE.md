# CLAUDE.md

CrossMath: a crossword-style math puzzle game. C# / .NET 10, WPF, MVVM. See README.md for the game rules and the generation algorithm.

## Commands

```sh
dotnet build CrossMath.slnx           # must stay at 0 warnings
dotnet test                           # all tests; generator tests take ~10 s
dotnet test --project tests/CrossMath.Core.Tests
dotnet test --coverage --coverage-output-format cobertura   # report lands in TestResults/ (ignored)
dotnet run --project src/CrossMath.App
dotnet publish src/CrossMath.App -p:PublishProfile=SingleFile   # self-contained single exe -> src/CrossMath.App/bin/publish/
```

Tests use **xUnit v3** on **Microsoft Testing Platform** (opted in via `"test": { "runner": ... }` in `global.json`). Test projects are executables (`<OutputType>Exe</OutputType>`), so `tests/*/bin/Debug/<tfm>/*.Tests.exe` can also be run directly. `dotnet test` takes `--project`, not a bare path. Don't re-add `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio` or `coverlet.collector`. They are VSTest-only, and the .NET 10 SDK rejects VSTest runs for MTP projects.

## Architecture

- `src/CrossMath.Core` (net10.0) — pure engine, no UI dependencies.
  - `Model.cs` — `Pos`, `Equation` (2 or 3 operands; cells laid out `N op N = N` / `N op N op N = N`), `Puzzle`.
  - `Evaluator` — the single source of truth for equation math. Used by the filler, the solver and the app's Check/win logic.
  - `LayoutGenerator` → `Filler` → `PuzzleGenerator.SelectBlanks` (uses `Solver.CountSolutions`) → `Puzzle`.
  - `DifficultySettings.For(...)` holds all per-difficulty tuning.
- `src/CrossMath.App` (net10.0-windows, WPF) — MVVM with CommunityToolkit.Mvvm; composition root in `App.xaml.cs`.
- `tests/CrossMath.Core.Tests` (net10.0) and `tests/CrossMath.App.Tests` (net10.0-windows, references the App project).

## Rules to preserve

- **Left-to-right evaluation**, no precedence. Intermediate results must be non-negative; division must be exact. Don't add a second evaluator.
- **Every generated puzzle has exactly one solution** given its tile pool. `PuzzleGeneratorTests` enforces this across seeds and difficulties; keep it passing.
- Aesthetic constraints (e.g. no `× 1` / `÷ 1`) belong in `Filler.LooksGood`, not in `Evaluator`. The solver must accept any mathematically valid answer.
- `PuzzleGenerator(seed)` must stay deterministic for a given seed.

## MVVM conventions (strict)

- Code-behind contains only `InitializeComponent()`. UI behavior goes through bindings, commands, attached behaviors (`Behaviors/`) and converters (`Converters/`).
- View models never reference WPF UI types (`Brush`, `Visibility`, controls, `Dispatcher`). Expose enums/bools and convert in XAML.
- Services are interfaces injected via the constructor (`IPuzzleGenerator`, `ITimerService`, `ISettingsService`) so view models are testable with fakes. Register new services in `App.xaml.cs`.
- Use `[ObservableProperty]` / `[RelayCommand]` source generators; follow the existing `_camelCase` field style.
- Drag-and-drop: `DragSource` puts the element's DataContext on the drag; `DropTarget` invokes a command with `DropRequest(source, target)`. Game rules for moves live in `MainViewModel.CanMoveTile` / `MoveTile`.
- New view-model behavior gets a test in `tests/CrossMath.App.Tests/MainViewModelTests.cs` (uses a fixed 2-blank puzzle).

## Gotchas

- `.gitignore` (from the .NET template) contains the macOS rule `*.app`, which matches the `CrossMath.App` folder case-insensitively on Windows. It is re-included with `!src/CrossMath.App/` at the bottom — keep that line, and check new folders aren't silently ignored (`git check-ignore -v <path>`).
