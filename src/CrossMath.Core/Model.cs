namespace CrossMath.Core;

public enum CellKind { Blocked, Number, Operator, Equals }

public enum Op { Add, Sub, Mul, Div }

public enum Orientation { Horizontal, Vertical }

public enum Difficulty { Easy, Medium, Hard }

public readonly record struct Pos(int Row, int Col)
{
    public Pos Offset(int dRow, int dCol) => new(Row + dRow, Col + dCol);
}

public static class OpExtensions
{
    public static string ToSymbol(this Op op) => op switch
    {
        Op.Add => "+",
        Op.Sub => "−",
        Op.Mul => "×",
        Op.Div => "÷",
        _ => throw new ArgumentOutOfRangeException(nameof(op)),
    };
}

/// <summary>
/// A single equation laid out along a row or column:
/// <c>N op N = N</c> (2 operands) or <c>N op N op N = N</c> (3 operands).
/// </summary>
public sealed class Equation
{
    public Equation(Pos start, Orientation orientation, int operandCount)
    {
        if (operandCount is < 2 or > 3)
            throw new ArgumentOutOfRangeException(nameof(operandCount));

        Start = start;
        Orientation = orientation;
        OperandCount = operandCount;

        var cells = new Pos[Length];
        for (int i = 0; i < Length; i++)
            cells[i] = orientation == Orientation.Horizontal ? start.Offset(0, i) : start.Offset(i, 0);
        Cells = cells;

        Operands = Enumerable.Range(0, operandCount).Select(i => cells[i * 2]).ToArray();
        OperatorCells = Enumerable.Range(0, operandCount - 1).Select(i => cells[i * 2 + 1]).ToArray();
        EqualsCell = cells[Length - 2];
        Result = cells[Length - 1];
        NumberCells = [.. Operands, Result];
    }

    public Pos Start { get; }
    public Orientation Orientation { get; }
    public int OperandCount { get; }
    public int Length => OperandCount * 2 + 1;

    /// <summary>All cells in reading order.</summary>
    public IReadOnlyList<Pos> Cells { get; }
    public IReadOnlyList<Pos> Operands { get; }
    public IReadOnlyList<Pos> OperatorCells { get; }
    public Pos EqualsCell { get; }
    public Pos Result { get; }

    /// <summary>Operands followed by the result.</summary>
    public IReadOnlyList<Pos> NumberCells { get; }

    public CellKind KindAt(int index) =>
        index == Length - 2 ? CellKind.Equals : index % 2 == 0 ? CellKind.Number : CellKind.Operator;
}

/// <summary>A generated puzzle: grid layout, the full solution, and which numbers are hidden.</summary>
public sealed class Puzzle
{
    public Puzzle(
        int rows,
        int cols,
        IReadOnlyList<Equation> equations,
        IReadOnlyDictionary<Pos, Op> operators,
        IReadOnlyDictionary<Pos, int> solution,
        IReadOnlySet<Pos> blanks,
        Difficulty difficulty)
    {
        Rows = rows;
        Cols = cols;
        Equations = equations;
        Operators = operators;
        Solution = solution;
        Blanks = blanks;
        Difficulty = difficulty;
        TilePool = blanks.Select(p => solution[p]).OrderBy(v => v).ToArray();
    }

    public int Rows { get; }
    public int Cols { get; }
    public IReadOnlyList<Equation> Equations { get; }
    public IReadOnlyDictionary<Pos, Op> Operators { get; }

    /// <summary>The value of every number cell.</summary>
    public IReadOnlyDictionary<Pos, int> Solution { get; }

    /// <summary>Number cells the player must fill.</summary>
    public IReadOnlySet<Pos> Blanks { get; }

    /// <summary>The tiles available to the player (the hidden values), sorted ascending.</summary>
    public IReadOnlyList<int> TilePool { get; }

    public Difficulty Difficulty { get; }

    public CellKind KindAt(Pos pos)
    {
        if (Solution.ContainsKey(pos)) return CellKind.Number;
        if (Operators.ContainsKey(pos)) return CellKind.Operator;
        foreach (var eq in Equations)
            if (eq.EqualsCell == pos) return CellKind.Equals;
        return CellKind.Blocked;
    }

    public bool IsGiven(Pos pos) => Solution.ContainsKey(pos) && !Blanks.Contains(pos);
}
