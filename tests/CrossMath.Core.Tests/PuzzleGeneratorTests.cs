using System.Diagnostics;
using CrossMath.Core;

namespace CrossMath.Core.Tests;

public class PuzzleGeneratorTests
{
    public static TheoryData<Difficulty, int> Cases()
    {
        var data = new TheoryData<Difficulty, int>();
        foreach (var d in Enum.GetValues<Difficulty>())
            for (int seed = 1; seed <= 15; seed++)
                data.Add(d, seed);
        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void GeneratesValidUniquePuzzle(Difficulty difficulty, int seed)
    {
        var settings = DifficultySettings.For(difficulty);
        var sw = Stopwatch.StartNew();
        var puzzle = new PuzzleGenerator(seed).Generate(difficulty);
        sw.Stop();

        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10), $"Generation took {sw.Elapsed}");
        Assert.InRange(puzzle.Equations.Count, settings.MinEquations, settings.MaxEquations);

        foreach (var eq in puzzle.Equations)
        {
            Assert.True(Evaluator.IsSatisfied(eq, puzzle.Operators, p => puzzle.Solution[p]));
            Assert.All(eq.OperatorCells, p => Assert.Contains(puzzle.Operators[p], settings.Operators));
            Assert.All(eq.Cells, p => Assert.True(p.Row < puzzle.Rows && p.Col < puzzle.Cols));
        }

        Assert.All(puzzle.Solution.Values, v => Assert.InRange(v, 1, settings.MaxValue));
        Assert.NotEmpty(puzzle.Blanks);
        Assert.Equal(puzzle.Blanks.Count, puzzle.TilePool.Count);
        Assert.Equal(1, Solver.CountSolutions(puzzle));
        AssertNoTrivialSteps(puzzle);
    }

    // No "× 1", "÷ 1", or "−"/"÷" steps giving 0 or 1 (x − x, x − (x − 1), x ÷ x).
    private static void AssertNoTrivialSteps(Puzzle puzzle)
    {
        foreach (var eq in puzzle.Equations)
        {
            long acc = puzzle.Solution[eq.Operands[0]];
            for (int i = 0; i < eq.OperatorCells.Count; i++)
            {
                var op = puzzle.Operators[eq.OperatorCells[i]];
                int right = puzzle.Solution[eq.Operands[i + 1]];
                if (op is Op.Mul or Op.Div) Assert.NotEqual(1, right);
                if (op == Op.Mul) Assert.NotEqual(1, acc);

                acc = Evaluator.Apply(acc, op, right)!.Value;
                if (op is Op.Sub or Op.Div)
                    Assert.True(acc >= 2, $"{op} step gives {acc}");
            }
        }
    }

    [Fact]
    public void SameSeed_GivesSamePuzzle()
    {
        var a = new PuzzleGenerator(42).Generate(Difficulty.Medium);
        var b = new PuzzleGenerator(42).Generate(Difficulty.Medium);

        Assert.Equal(a.Solution.OrderBy(kv => kv.Key.Row).ThenBy(kv => kv.Key.Col),
            b.Solution.OrderBy(kv => kv.Key.Row).ThenBy(kv => kv.Key.Col));
        Assert.Equal(a.Blanks.ToHashSet(), b.Blanks.ToHashSet());
    }

    [Fact]
    public void LayoutCellsNeverConflict()
    {
        var rng = new Random(7);
        var settings = DifficultySettings.For(Difficulty.Hard);
        for (int i = 0; i < 50; i++)
        {
            var layout = LayoutGenerator.TryGenerate(settings, 8, rng);
            if (layout is null) continue;

            var kinds = new Dictionary<Pos, CellKind>();
            foreach (var eq in layout.Equations)
                for (int k = 0; k < eq.Length; k++)
                {
                    var kind = eq.KindAt(k);
                    if (kinds.TryGetValue(eq.Cells[k], out var existing))
                        Assert.True(existing == CellKind.Number && kind == CellKind.Number);
                    kinds[eq.Cells[k]] = kind;
                }
        }
    }
}
