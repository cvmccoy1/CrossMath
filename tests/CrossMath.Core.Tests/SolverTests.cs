using CrossMath.Core;

namespace CrossMath.Core.Tests;

public class SolverTests
{
    // Layout:          a + b = c      (row 0)
    //                  -              a - d = e (column 0)
    //                  d
    //                  =
    //                  e
    private static readonly Equation Across = new(new Pos(0, 0), Orientation.Horizontal, 2);
    private static readonly Equation Down = new(new Pos(0, 0), Orientation.Vertical, 2);

    private static readonly Dictionary<Pos, Op> Ops = new()
    {
        [Across.OperatorCells[0]] = Op.Add,
        [Down.OperatorCells[0]] = Op.Sub,
    };

    // a=9, b=3, c=12, d=4, e=5
    private static readonly Dictionary<Pos, int> Solution = new()
    {
        [new Pos(0, 0)] = 9,
        [new Pos(0, 2)] = 3,
        [new Pos(0, 4)] = 12,
        [new Pos(2, 0)] = 4,
        [new Pos(4, 0)] = 5,
    };

    private static Puzzle MakePuzzle(params Pos[] blanks) =>
        new(5, 5, [Across, Down], Ops, Solution, blanks.ToHashSet(), Difficulty.Easy);

    [Fact]
    public void UniquePuzzle_HasOneSolution()
    {
        // Hide a and b: tiles {9, 3}. a + b = 12 and a - 4 = 5 force a = 9, b = 3.
        Assert.Equal(1, Solver.CountSolutions(MakePuzzle(new Pos(0, 0), new Pos(0, 2))));
    }

    [Fact]
    public void AmbiguousPuzzle_ReportsMultipleSolutions()
    {
        // Only the across equation, with both operands hidden: tiles {9, 3} fit either way round.
        var puzzle = new Puzzle(1, 5, [Across], Ops.Where(kv => kv.Key == Across.OperatorCells[0]).ToDictionary(),
            Solution.Where(kv => kv.Key.Row == 0).ToDictionary(), new HashSet<Pos> { new(0, 0), new(0, 2) },
            Difficulty.Easy);

        Assert.Equal(2, Solver.CountSolutions(puzzle)); // 9 + 3 and 3 + 9
    }

    [Fact]
    public void FullyHiddenPuzzle_StillCountsCorrectly()
    {
        var all = Solution.Keys.ToArray();
        int count = Solver.CountSolutions(MakePuzzle(all), limit: 10);
        Assert.True(count >= 1);
    }
}
