namespace CrossMath.Core;

public sealed record DifficultySettings(
    Difficulty Difficulty,
    IReadOnlyList<int> OperandCounts,
    IReadOnlyList<Op> Operators,
    int MaxValue,
    int MinEquations,
    int MaxEquations,
    double BlankRatio,
    int MaxGridSize,
    int LayoutCandidates)
{
    public static DifficultySettings For(Difficulty difficulty) => difficulty switch
    {
        Difficulty.Easy => new(difficulty, [2], [Op.Add, Op.Sub], 20, 4, 4, 0.40, 9, 2),
        Difficulty.Medium => new(difficulty, [2, 3], [Op.Add, Op.Sub, Op.Mul], 50, 6, 6, 0.55, 11, 4),
        Difficulty.Hard => new(difficulty, [2, 3], [Op.Add, Op.Sub, Op.Mul, Op.Div], 99, 8, 9, 0.65, 13, 4),
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty)),
    };
}
