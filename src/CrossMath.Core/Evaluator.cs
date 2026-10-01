namespace CrossMath.Core;

/// <summary>
/// Evaluates equations strictly left-to-right (no operator precedence).
/// Every intermediate value must be a non-negative integer and division must be exact.
/// </summary>
public static class Evaluator
{
    private const long Limit = 1_000_000_000;

    /// <summary>Returns the left-to-right value, or null if any step is invalid.</summary>
    public static long? Evaluate(IReadOnlyList<int> operands, IReadOnlyList<Op> ops)
    {
        if (operands.Count == 0 || ops.Count != operands.Count - 1)
            throw new ArgumentException("Need exactly one fewer operator than operands.");

        long acc = operands[0];
        if (acc < 0) return null;
        for (int i = 0; i < ops.Count; i++)
        {
            long? next = Apply(acc, ops[i], operands[i + 1]);
            if (next is null) return null;
            acc = next.Value;
        }
        return acc;
    }

    public static long? Apply(long left, Op op, long right)
    {
        long value;
        switch (op)
        {
            case Op.Add: value = left + right; break;
            case Op.Sub: value = left - right; break;
            case Op.Mul: value = left * right; break;
            case Op.Div:
                if (right == 0 || left % right != 0) return null;
                value = left / right;
                break;
            default: throw new ArgumentOutOfRangeException(nameof(op));
        }
        return value is < 0 or > Limit ? null : value;
    }

    /// <summary>True when every number cell has a value and the equation holds.</summary>
    public static bool IsSatisfied(Equation eq, IReadOnlyDictionary<Pos, Op> operators, Func<Pos, int?> valueAt)
    {
        var operands = new int[eq.OperandCount];
        for (int i = 0; i < operands.Length; i++)
        {
            if (valueAt(eq.Operands[i]) is not int v) return false;
            operands[i] = v;
        }
        if (valueAt(eq.Result) is not int result) return false;

        var ops = eq.OperatorCells.Select(p => operators[p]).ToArray();
        return Evaluate(operands, ops) == result;
    }

    /// <summary>
    /// False only if the known values already make the equation impossible:
    /// a complete equation that does not hold, or an invalid known prefix
    /// (a negative or non-exact intermediate can never be repaired by later steps).
    /// </summary>
    public static bool IsConsistent(Equation eq, IReadOnlyDictionary<Pos, Op> operators, Func<Pos, int?> valueAt)
    {
        if (valueAt(eq.Operands[0]) is not int first) return true;

        long acc = first;
        for (int i = 1; i < eq.OperandCount; i++)
        {
            if (valueAt(eq.Operands[i]) is not int v) return true;
            long? next = Apply(acc, operators[eq.OperatorCells[i - 1]], v);
            if (next is null) return false;
            acc = next.Value;
        }
        return valueAt(eq.Result) is not int result || result == acc;
    }
}
