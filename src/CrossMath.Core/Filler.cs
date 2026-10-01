namespace CrossMath.Core;

public sealed record Filling(IReadOnlyDictionary<Pos, Op> Operators, IReadOnlyDictionary<Pos, int> Values);

/// <summary>
/// Assigns random operators to a layout, then finds numbers (1..MaxValue) that satisfy every
/// equation using randomized backtracking.
/// </summary>
public static class Filler
{
    public static Filling? TryFill(Layout layout, DifficultySettings settings, Random rng, int nodeLimit = 20_000)
    {
        var operators = new Dictionary<Pos, Op>();
        foreach (var eq in layout.Equations)
            foreach (var p in eq.OperatorCells)
                operators[p] = settings.Operators[rng.Next(settings.Operators.Count)];

        var equationsAt = new Dictionary<Pos, List<Equation>>();
        var order = new List<Pos>();
        foreach (var eq in layout.Equations)
        {
            foreach (var p in eq.NumberCells)
            {
                if (!equationsAt.TryGetValue(p, out var list))
                {
                    equationsAt[p] = list = [];
                    order.Add(p);
                }
                list.Add(eq);
            }
        }

        var values = new Dictionary<Pos, int>();
        int? ValueAt(Pos p) => values.TryGetValue(p, out var v) ? v : null;
        int nodes = 0;

        return Search() ? new Filling(operators, values) : null;

        bool Search()
        {
            if (++nodes > nodeLimit) return false;

            // A result whose operands are all known is forced; fill it first.
            foreach (var eq in layout.Equations)
            {
                if (values.ContainsKey(eq.Result) || !eq.Operands.All(values.ContainsKey)) continue;

                var ops = eq.OperatorCells.Select(p => operators[p]).ToArray();
                long? forced = Evaluator.Evaluate(eq.Operands.Select(p => values[p]).ToArray(), ops);
                if (forced is not long f || f < 1 || f > settings.MaxValue) return false;
                return TryAssign(eq.Result, (int)f);
            }

            var next = order.FirstOrDefault(p => !values.ContainsKey(p), new Pos(-1, -1));
            if (next.Row < 0) return true;

            foreach (int v in Shuffled(settings.MaxValue, rng))
            {
                if (TryAssign(next, v)) return true;
                if (nodes > nodeLimit) return false;
            }
            return false;
        }

        bool TryAssign(Pos p, int v)
        {
            values[p] = v;
            if (equationsAt[p].All(eq => Evaluator.IsConsistent(eq, operators, ValueAt) && LooksGood(eq)) && Search())
                return true;
            values.Remove(p);
            return false;
        }

        // Avoid trivial-looking steps like "× 1" or "÷ 1".
        bool LooksGood(Equation eq)
        {
            for (int i = 0; i < eq.OperatorCells.Count; i++)
            {
                var op = operators[eq.OperatorCells[i]];
                if (op is not (Op.Mul or Op.Div)) continue;
                if (ValueAt(eq.Operands[i + 1]) == 1) return false;
                if (i == 0 && op == Op.Mul && ValueAt(eq.Operands[0]) == 1) return false;
            }
            return true;
        }
    }

    private static int[] Shuffled(int max, Random rng)
    {
        var values = Enumerable.Range(1, max).ToArray();
        rng.Shuffle(values);
        return values;
    }
}
