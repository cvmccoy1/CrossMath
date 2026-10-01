namespace CrossMath.Core;

public interface IPuzzleGenerator
{
    Puzzle Generate(Difficulty difficulty);
}

/// <summary>
/// Generates puzzles with exactly one solution: layout, then fill, then hide numbers one at a
/// time, keeping each hidden only while the solver still finds a single solution.
/// </summary>
public sealed class PuzzleGenerator : IPuzzleGenerator
{
    private const int MaxAttempts = 300;
    private readonly Random _rng;

    public PuzzleGenerator(int? seed = null)
    {
        _rng = seed is int s ? new Random(s) : new Random();
    }

    public Puzzle Generate(Difficulty difficulty)
    {
        var settings = DifficultySettings.For(difficulty);
        Puzzle? best = null;

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            int equationCount = _rng.Next(settings.MinEquations, settings.MaxEquations + 1);
            var layout = LayoutGenerator.TryGenerate(settings, equationCount, _rng);
            if (layout is null) continue;

            var filling = Filler.TryFill(layout, settings, _rng);
            if (filling is null) continue;

            var numberCells = filling.Values.Keys.ToArray();
            int target = (int)Math.Round(settings.BlankRatio * numberCells.Length);
            var blanks = SelectBlanks(layout, filling, numberCells, target);
            var puzzle = new Puzzle(layout.Rows, layout.Cols, layout.Equations, filling.Operators,
                filling.Values, blanks, difficulty);

            if (blanks.Count >= target) return puzzle;
            if (best is null || blanks.Count * best.Solution.Count > best.Blanks.Count * numberCells.Length)
                best = puzzle;
        }

        return best ?? throw new InvalidOperationException($"Could not generate a {difficulty} puzzle.");
    }

    private HashSet<Pos> SelectBlanks(Layout layout, Filling filling, Pos[] numberCells, int target)
    {
        var shuffled = (Pos[])numberCells.Clone();
        _rng.Shuffle(shuffled);

        var blanks = new HashSet<Pos>();
        foreach (var p in shuffled)
        {
            if (blanks.Count >= target) break;
            blanks.Add(p);

            var givens = filling.Values.Where(kv => !blanks.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value);
            var tiles = blanks.Select(b => filling.Values[b]).ToArray();
            if (Solver.CountSolutions(layout.Equations, filling.Operators, givens, blanks, tiles) != 1)
                blanks.Remove(p);
        }
        return blanks;
    }
}
