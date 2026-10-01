using CrossMath.Core;

namespace CrossMath.Core.Tests;

public class EvaluatorTests
{
    [Fact]
    public void EvaluatesLeftToRight_IgnoringPrecedence()
    {
        Assert.Equal(20, Evaluator.Evaluate([2, 3, 4], [Op.Add, Op.Mul]));
    }

    [Theory]
    [InlineData(7, Op.Add, 5, 12)]
    [InlineData(7, Op.Sub, 5, 2)]
    [InlineData(7, Op.Mul, 5, 35)]
    [InlineData(35, Op.Div, 5, 7)]
    public void AppliesEachOperator(int a, Op op, int b, int expected)
    {
        Assert.Equal(expected, Evaluator.Evaluate([a, b], [op]));
    }

    [Fact]
    public void RejectsNonExactDivision() => Assert.Null(Evaluator.Evaluate([7, 2], [Op.Div]));

    [Fact]
    public void RejectsDivisionByZero() => Assert.Null(Evaluator.Evaluate([7, 0], [Op.Div]));

    [Fact]
    public void RejectsNegativeIntermediate()
    {
        // 2 - 5 + 10 would be 7 with precedence-free math, but the intermediate -3 is not allowed.
        Assert.Null(Evaluator.Evaluate([2, 5, 10], [Op.Sub, Op.Add]));
    }

    [Fact]
    public void IsSatisfied_ChecksResultCell()
    {
        var eq = new Equation(new Pos(0, 0), Orientation.Horizontal, 2);
        var ops = new Dictionary<Pos, Op> { [eq.OperatorCells[0]] = Op.Add };
        var values = new Dictionary<Pos, int> { [eq.Operands[0]] = 3, [eq.Operands[1]] = 4, [eq.Result] = 7 };

        Assert.True(Evaluator.IsSatisfied(eq, ops, p => values.TryGetValue(p, out var v) ? v : null));
        values[eq.Result] = 8;
        Assert.False(Evaluator.IsSatisfied(eq, ops, p => values.TryGetValue(p, out var v) ? v : null));
    }

    [Fact]
    public void IsConsistent_AllowsUnknownsAndRejectsBadPrefix()
    {
        var eq = new Equation(new Pos(0, 0), Orientation.Vertical, 3);
        var ops = new Dictionary<Pos, Op> { [eq.OperatorCells[0]] = Op.Div, [eq.OperatorCells[1]] = Op.Add };
        var values = new Dictionary<Pos, int> { [eq.Operands[0]] = 9 };
        int? At(Pos p) => values.TryGetValue(p, out var v) ? v : null;

        Assert.True(Evaluator.IsConsistent(eq, ops, At));
        values[eq.Operands[1]] = 2; // 9 ÷ 2 is not exact
        Assert.False(Evaluator.IsConsistent(eq, ops, At));
        values[eq.Operands[1]] = 3;
        Assert.True(Evaluator.IsConsistent(eq, ops, At));
    }
}
