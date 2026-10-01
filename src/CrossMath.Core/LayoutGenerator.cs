namespace CrossMath.Core;

public sealed record Layout(int Rows, int Cols, IReadOnlyList<Equation> Equations);

/// <summary>
/// Builds a crossword-style arrangement of equations. Each new equation crosses an existing
/// one at a number cell; equations never touch side-by-side or end-to-end.
/// </summary>
public static class LayoutGenerator
{
    private const int AttachAttempts = 400;

    public static Layout? TryGenerate(DifficultySettings settings, int equationCount, Random rng)
    {
        int size = settings.MaxGridSize;
        var occupied = new Dictionary<Pos, CellKind>();
        var numberOrientations = new Dictionary<Pos, HashSet<Orientation>>();
        var equations = new List<Equation>();

        int seedCount = Pick(settings.OperandCounts, rng);
        int seedLength = seedCount * 2 + 1;
        var seedOrientation = rng.Next(2) == 0 ? Orientation.Horizontal : Orientation.Vertical;
        var seedStart = seedOrientation == Orientation.Horizontal
            ? new Pos(size / 2, (size - seedLength) / 2)
            : new Pos((size - seedLength) / 2, size / 2);
        Place(new Equation(seedStart, seedOrientation, seedCount));

        for (int attempt = 0; attempt < AttachAttempts && equations.Count < equationCount; attempt++)
        {
            var anchors = numberOrientations.Where(kv => kv.Value.Count == 1).ToList();
            if (anchors.Count == 0) return null;

            var (anchor, orientations) = anchors[rng.Next(anchors.Count)];
            var orientation = orientations.Contains(Orientation.Horizontal) ? Orientation.Vertical : Orientation.Horizontal;
            int count = Pick(settings.OperandCounts, rng);
            int length = count * 2 + 1;
            int index = rng.Next((length + 1) / 2) * 2; // number cells sit at even indices
            var start = orientation == Orientation.Horizontal ? anchor.Offset(0, -index) : anchor.Offset(-index, 0);
            var candidate = new Equation(start, orientation, count);

            if (CanPlace(candidate)) Place(candidate);
        }

        return equations.Count == equationCount ? Crop(equations) : null;

        bool InBounds(Pos p) => p.Row >= 0 && p.Col >= 0 && p.Row < size && p.Col < size;

        bool CanPlace(Equation eq)
        {
            var (dr, dc) = eq.Orientation == Orientation.Horizontal ? (0, 1) : (1, 0);
            if (occupied.ContainsKey(eq.Start.Offset(-dr, -dc))) return false;
            if (occupied.ContainsKey(eq.Cells[^1].Offset(dr, dc))) return false;

            int crossings = 0;
            for (int i = 0; i < eq.Length; i++)
            {
                var p = eq.Cells[i];
                if (!InBounds(p)) return false;

                if (occupied.TryGetValue(p, out var existing))
                {
                    bool crossesNumber = existing == CellKind.Number
                        && eq.KindAt(i) == CellKind.Number
                        && !numberOrientations[p].Contains(eq.Orientation);
                    if (!crossesNumber) return false;
                    crossings++;
                }
                else if (occupied.ContainsKey(p.Offset(dc, dr)) || occupied.ContainsKey(p.Offset(-dc, -dr)))
                {
                    return false; // would sit alongside another equation
                }
            }
            return crossings > 0;
        }

        void Place(Equation eq)
        {
            for (int i = 0; i < eq.Length; i++)
            {
                var p = eq.Cells[i];
                var kind = eq.KindAt(i);
                occupied[p] = kind;
                if (kind == CellKind.Number)
                {
                    if (!numberOrientations.TryGetValue(p, out var set))
                        numberOrientations[p] = set = [];
                    set.Add(eq.Orientation);
                }
            }
            equations.Add(eq);
        }
    }

    private static Layout Crop(List<Equation> equations)
    {
        var cells = equations.SelectMany(e => e.Cells).ToList();
        int minRow = cells.Min(p => p.Row), minCol = cells.Min(p => p.Col);
        int rows = cells.Max(p => p.Row) - minRow + 1, cols = cells.Max(p => p.Col) - minCol + 1;
        var shifted = equations
            .Select(e => new Equation(e.Start.Offset(-minRow, -minCol), e.Orientation, e.OperandCount))
            .ToArray();
        return new Layout(rows, cols, shifted);
    }

    private static T Pick<T>(IReadOnlyList<T> items, Random rng) => items[rng.Next(items.Count)];
}
