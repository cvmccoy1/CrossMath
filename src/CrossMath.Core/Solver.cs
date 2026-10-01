namespace CrossMath.Core;

/// <summary>
/// Counts the ways the blank cells can be filled using exactly the given tiles.
/// </summary>
public static class Solver
{
    public static int CountSolutions(Puzzle puzzle, int limit = 2)
    {
        var givens = puzzle.Solution.Where(kv => !puzzle.Blanks.Contains(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        return CountSolutions(puzzle.Equations, puzzle.Operators, givens, puzzle.Blanks, puzzle.TilePool, limit);
    }

    public static int CountSolutions(
        IReadOnlyList<Equation> equations,
        IReadOnlyDictionary<Pos, Op> operators,
        IReadOnlyDictionary<Pos, int> givens,
        IReadOnlyCollection<Pos> blanks,
        IReadOnlyList<int> tiles,
        int limit = 2)
    {
        if (blanks.Count != tiles.Count) return 0;

        var values = new Dictionary<Pos, int>(givens);
        var remaining = tiles.GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());
        var equationsAt = blanks.ToDictionary(
            b => b,
            b => equations.Where(e => e.NumberCells.Contains(b)).ToArray());
        var open = new List<Pos>(blanks);
        int? ValueAt(Pos p) => values.TryGetValue(p, out var v) ? v : null;

        return Search(limit);

        int Search(int budget)
        {
            if (open.Count == 0) return 1;

            // Branch on the blank with the fewest workable tiles.
            int bestIndex = -1;
            List<int>? best = null;
            for (int i = 0; i < open.Count; i++)
            {
                var candidates = Candidates(open[i]);
                if (candidates.Count == 0) return 0;
                if (best is null || candidates.Count < best.Count)
                {
                    best = candidates;
                    bestIndex = i;
                }
            }

            var cell = open[bestIndex];
            open.RemoveAt(bestIndex);
            int found = 0;
            foreach (int v in best!)
            {
                values[cell] = v;
                remaining[v]--;
                found += Search(budget - found);
                remaining[v]++;
                values.Remove(cell);
                if (found >= budget) break;
            }
            open.Insert(bestIndex, cell);
            return found;
        }

        List<int> Candidates(Pos cell)
        {
            var result = new List<int>();
            foreach (var (v, count) in remaining)
            {
                if (count == 0) continue;
                values[cell] = v;
                if (equationsAt[cell].All(e => Evaluator.IsConsistent(e, operators, ValueAt)))
                    result.Add(v);
                values.Remove(cell);
            }
            return result;
        }
    }
}
