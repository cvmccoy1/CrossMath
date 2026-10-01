using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CrossMath.App.Services;
using CrossMath.Core;

namespace CrossMath.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IPuzzleGenerator _generator;
    private readonly ITimerService _timer;
    private Puzzle? _puzzle;
    private object? _selection;

    public MainViewModel(IPuzzleGenerator generator, ITimerService timer)
    {
        _generator = generator;
        _timer = timer;
        _timer.Tick += (_, _) => Elapsed += TimeSpan.FromSeconds(1);
    }

    public IReadOnlyList<Difficulty> Difficulties { get; } = Enum.GetValues<Difficulty>();

    public TilePoolViewModel Pool { get; } = new();

    /// <summary>The board, row by row (Rows × Columns cells).</summary>
    [ObservableProperty]
    private IReadOnlyList<CellViewModel> _cells = [];

    [ObservableProperty]
    private int _rows;

    [ObservableProperty]
    private int _columns;

    [ObservableProperty]
    private Difficulty _selectedDifficulty = Difficulty.Easy;

    [ObservableProperty]
    private TimeSpan _elapsed;

    [ObservableProperty]
    private int _hintsUsed;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyCanExecuteChangedFor(nameof(CheckCommand), nameof(HintCommand), nameof(ResetCommand))]
    private bool _isGenerating;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CheckCommand), nameof(HintCommand))]
    private bool _isSolved;

    public bool IsIdle => !IsGenerating;

    private bool HasPuzzle => _puzzle is not null && !IsGenerating;

    private bool IsPlaying => HasPuzzle && !IsSolved;

    private IEnumerable<CellViewModel> BlankCells => Cells.Where(c => c.IsBlank);

    partial void OnSelectedDifficultyChanged(Difficulty value) => NewGameCommand.Execute(null);

    [RelayCommand]
    private async Task NewGameAsync()
    {
        IsGenerating = true;
        _timer.Stop();
        StatusMessage = "Generating puzzle…";
        try
        {
            var difficulty = SelectedDifficulty;
            var puzzle = await Task.Run(() => _generator.Generate(difficulty));
            Load(puzzle);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not generate a puzzle: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    private void Load(Puzzle puzzle)
    {
        _puzzle = puzzle;
        _selection = null;

        var cells = new List<CellViewModel>(puzzle.Rows * puzzle.Cols);
        for (int r = 0; r < puzzle.Rows; r++)
        {
            for (int c = 0; c < puzzle.Cols; c++)
            {
                var pos = new Pos(r, c);
                var kind = puzzle.KindAt(pos);
                cells.Add(kind switch
                {
                    CellKind.Number when puzzle.Blanks.Contains(pos) => new CellViewModel(pos, kind, isBlank: true),
                    CellKind.Number => new CellViewModel(pos, kind, false, puzzle.Solution[pos].ToString(), puzzle.Solution[pos]),
                    CellKind.Operator => new CellViewModel(pos, kind, false, puzzle.Operators[pos].ToSymbol()),
                    CellKind.Equals => new CellViewModel(pos, kind, false, "="),
                    _ => new CellViewModel(pos, kind, false),
                });
            }
        }

        Rows = puzzle.Rows;
        Columns = puzzle.Cols;
        Cells = cells;

        Pool.Clear();
        foreach (int value in puzzle.TilePool)
            Pool.Add(new TileViewModel(value));

        StartClock();
        StatusMessage = "Drag each tile onto an empty square so every equation is true.";
    }

    [RelayCommand(CanExecute = nameof(CanMoveTile))]
    private void MoveTile(DropRequest request)
    {
        switch (request.Source, request.Target)
        {
            case (TileViewModel tile, CellViewModel cell):
                Pool.Remove(tile);
                if (cell.Tile is not null) Pool.Add(cell.Tile);
                cell.Tile = tile;
                break;

            case (CellViewModel from, CellViewModel to):
                (from.Tile, to.Tile) = (to.Tile, from.Tile);
                break;

            case (CellViewModel from, TilePoolViewModel or TileViewModel):
                Pool.Add(from.Tile!);
                from.Tile = null;
                break;
        }
        AfterBoardChanged();
    }

    private bool CanMoveTile(DropRequest? request)
    {
        if (request is null || !IsPlaying) return false;
        return (request.Source, request.Target) switch
        {
            (TileViewModel tile, CellViewModel cell) => Pool.Contains(tile) && IsOpenBlank(cell),
            (CellViewModel from, CellViewModel to) => from.CanDrag && IsOpenBlank(to) && from != to,
            (CellViewModel from, TilePoolViewModel or TileViewModel) => from.CanDrag,
            _ => false,
        };

        static bool IsOpenBlank(CellViewModel cell) => cell.IsBlank && !cell.IsLocked;
    }

    /// <summary>Click a pool tile to select it; if a board tile is selected, this sends it back to the pool.</summary>
    [RelayCommand]
    private void SelectTile(TileViewModel tile)
    {
        if (!IsPlaying) return;

        if (_selection is CellViewModel cell)
        {
            MoveTile(new DropRequest(cell, Pool));
            return;
        }
        Select(_selection == tile ? null : tile);
    }

    /// <summary>Click a cell: place the selected tile there, or select the cell's own tile.</summary>
    [RelayCommand]
    private void CellClicked(CellViewModel cell)
    {
        if (!IsPlaying) return;

        if (_selection is not null && _selection != cell)
        {
            var request = new DropRequest(_selection, cell);
            if (CanMoveTile(request))
            {
                MoveTile(request);
                return;
            }
        }
        Select(cell.CanDrag && _selection != cell ? cell : null);
    }

    [RelayCommand]
    private void ReturnTile(CellViewModel cell)
    {
        var request = new DropRequest(cell, Pool);
        if (CanMoveTile(request)) MoveTile(request);
    }

    [RelayCommand(CanExecute = nameof(IsPlaying))]
    private void Check()
    {
        var wrong = new HashSet<CellViewModel>();
        var correct = new HashSet<CellViewModel>();
        int complete = 0;

        foreach (var eq in _puzzle!.Equations)
        {
            var cells = eq.NumberCells.Select(CellAt).ToArray();
            if (cells.Any(c => c.Value is null)) continue;

            complete++;
            var blanks = cells.Where(c => c.IsBlank && !c.IsLocked);
            if (Evaluator.IsSatisfied(eq, _puzzle.Operators, p => CellAt(p).Value))
                correct.UnionWith(blanks);
            else
                wrong.UnionWith(blanks);
        }

        foreach (var cell in BlankCells.Where(c => !c.IsLocked))
            cell.State = wrong.Contains(cell) ? CellState.Wrong
                : correct.Contains(cell) ? CellState.Correct
                : CellState.Normal;

        StatusMessage = complete == 0 ? "Fill in a whole equation, then check it."
            : wrong.Count == 0 ? $"All {complete} completed equation(s) are correct."
            : "Some equations don't add up — the red squares are involved.";
    }

    /// <summary>Puts the correct tile into one empty or wrong square and locks it.</summary>
    [RelayCommand(CanExecute = nameof(IsPlaying))]
    private void Hint()
    {
        var open = BlankCells.Where(c => !c.IsLocked).ToList();
        var target = open.FirstOrDefault(c => c.Tile is not null && c.Tile.Value != SolutionAt(c))
                     ?? open.FirstOrDefault(c => c.Tile is null);
        if (target is null) return;

        int needed = SolutionAt(target);
        var tile = Pool.Tiles.FirstOrDefault(t => t.Value == needed);
        if (tile is not null)
        {
            Pool.Remove(tile);
        }
        else
        {
            // Every tile of this value is on the board, so at least one sits in a square it doesn't belong to.
            var holder = open.First(c => c != target && c.Tile?.Value == needed && SolutionAt(c) != needed);
            tile = holder.Tile!;
            holder.Tile = null;
        }

        if (target.Tile is not null) Pool.Add(target.Tile);
        target.Tile = tile;
        target.IsLocked = true;
        target.State = CellState.Hinted;
        HintsUsed++;

        AfterBoardChanged();
        if (!IsSolved) StatusMessage = $"Hint used ({HintsUsed}).";
    }

    [RelayCommand(CanExecute = nameof(HasPuzzle))]
    private void Reset()
    {
        foreach (var cell in BlankCells)
        {
            if (cell.Tile is not null) Pool.Add(cell.Tile);
            cell.Tile = null;
            cell.IsLocked = false;
            cell.State = CellState.Normal;
        }
        Select(null);
        StartClock();
        StatusMessage = "Board cleared.";
    }

    private void StartClock()
    {
        IsSolved = false;
        HintsUsed = 0;
        Elapsed = TimeSpan.Zero;
        _timer.Start();
    }

    private void AfterBoardChanged()
    {
        Select(null);
        foreach (var cell in BlankCells.Where(c => !c.IsLocked))
            cell.State = CellState.Normal;

        bool solved = BlankCells.All(c => c.Tile is not null)
            && _puzzle!.Equations.All(eq => Evaluator.IsSatisfied(eq, _puzzle.Operators, p => CellAt(p).Value));
        if (!solved) return;

        _timer.Stop();
        IsSolved = true;
        string hints = HintsUsed == 0 ? "" : $" with {HintsUsed} hint{(HintsUsed == 1 ? "" : "s")}";
        StatusMessage = $"Solved in {Elapsed:mm\\:ss}{hints}!";
    }

    private void Select(object? item)
    {
        SetSelected(_selection, false);
        _selection = item;
        SetSelected(_selection, true);

        static void SetSelected(object? item, bool value)
        {
            if (item is TileViewModel tile) tile.IsSelected = value;
            else if (item is CellViewModel cell) cell.IsSelected = value;
        }
    }

    private CellViewModel CellAt(Pos pos) => Cells[pos.Row * Columns + pos.Col];

    private int SolutionAt(CellViewModel cell) => _puzzle!.Solution[cell.Pos];
}
