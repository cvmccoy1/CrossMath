using CrossMath.App.Services;
using CrossMath.App.ViewModels;
using CrossMath.Core;

namespace CrossMath.App.Tests;

public class MainViewModelTests
{
    // Board (5×5):   9 + _ = 12        a = (0,0) given 9, b = (0,2) blank 3, c = (0,4) given 12
    //                −                 d = (2,0) blank 4
    //                _                 e = (4,0) given 5
    //                =
    //                5
    // Tiles: 3, 4. Only b = 3, d = 4 works.
    private static readonly Pos B = new(0, 2);
    private static readonly Pos D = new(2, 0);

    private sealed class FakeGenerator : IPuzzleGenerator
    {
        public Puzzle Generate(Difficulty difficulty)
        {
            var across = new Equation(new Pos(0, 0), Orientation.Horizontal, 2);
            var down = new Equation(new Pos(0, 0), Orientation.Vertical, 2);
            var ops = new Dictionary<Pos, Op>
            {
                [across.OperatorCells[0]] = Op.Add,
                [down.OperatorCells[0]] = Op.Sub,
            };
            var solution = new Dictionary<Pos, int>
            {
                [new Pos(0, 0)] = 9, [B] = 3, [new Pos(0, 4)] = 12, [D] = 4, [new Pos(4, 0)] = 5,
            };
            return new Puzzle(5, 5, [across, down], ops, solution, new HashSet<Pos> { B, D }, difficulty);
        }
    }

    private sealed class FakeTimer : ITimerService
    {
        public event EventHandler? Tick;
        public bool IsRunning { get; private set; }
        public void Start() => IsRunning = true;
        public void Stop() => IsRunning = false;
        public void RaiseTick() => Tick?.Invoke(this, EventArgs.Empty);
    }

    private static async Task<(MainViewModel Vm, FakeTimer Timer)> StartGameAsync()
    {
        var timer = new FakeTimer();
        var vm = new MainViewModel(new FakeGenerator(), timer);
        await vm.NewGameCommand.ExecuteAsync(null);
        return (vm, timer);
    }

    private static CellViewModel Cell(MainViewModel vm, Pos p) => vm.Cells[p.Row * vm.Columns + p.Col];

    private static TileViewModel Tile(MainViewModel vm, int value) => vm.Pool.Tiles.Single(t => t.Value == value);

    private static void Move(MainViewModel vm, object source, object target) =>
        vm.MoveTileCommand.Execute(new DropRequest(source, target));

    [Fact]
    public async Task NewGame_BuildsBoardPoolAndStartsTimer()
    {
        var (vm, timer) = await StartGameAsync();

        Assert.Equal(25, vm.Cells.Count);
        Assert.Equal([3, 4], vm.Pool.Tiles.Select(t => t.Value));
        Assert.True(Cell(vm, B).IsBlank);
        Assert.Equal("9", Cell(vm, new Pos(0, 0)).DisplayText);
        Assert.Equal("+", Cell(vm, new Pos(0, 1)).DisplayText);
        Assert.Equal(CellKind.Blocked, Cell(vm, new Pos(1, 1)).Kind);
        Assert.True(timer.IsRunning);
    }

    [Fact]
    public async Task TimerTicks_AdvanceElapsed()
    {
        var (vm, timer) = await StartGameAsync();
        timer.RaiseTick();
        timer.RaiseTick();
        Assert.Equal(TimeSpan.FromSeconds(2), vm.Elapsed);
    }

    [Fact]
    public async Task MoveTile_PlacesSwapsAndReturns()
    {
        var (vm, _) = await StartGameAsync();
        var tile4 = Tile(vm, 4);

        Move(vm, tile4, Cell(vm, B));
        Assert.Same(tile4, Cell(vm, B).Tile);
        Assert.DoesNotContain(tile4, vm.Pool.Tiles);

        Move(vm, Cell(vm, B), Cell(vm, D)); // move to an empty blank
        Assert.Null(Cell(vm, B).Tile);
        Assert.Same(tile4, Cell(vm, D).Tile);

        var tile3 = Tile(vm, 3);
        Move(vm, tile3, Cell(vm, B));
        Move(vm, Cell(vm, B), Cell(vm, D)); // swap two placed tiles
        Assert.Same(tile4, Cell(vm, B).Tile);
        Assert.Same(tile3, Cell(vm, D).Tile);

        Move(vm, Cell(vm, D), vm.Pool);
        Assert.Null(Cell(vm, D).Tile);
        Assert.Contains(tile3, vm.Pool.Tiles);
    }

    [Fact]
    public async Task PlacingOnFilledCell_ReturnsPreviousTileToPool()
    {
        var (vm, _) = await StartGameAsync();
        var tile3 = Tile(vm, 3);
        var tile4 = Tile(vm, 4);

        Move(vm, tile3, Cell(vm, B));
        Move(vm, tile4, Cell(vm, B));

        Assert.Same(tile4, Cell(vm, B).Tile);
        Assert.Equal([3], vm.Pool.Tiles.Select(t => t.Value));
    }

    [Fact]
    public async Task CannotDropOntoGivenCells()
    {
        var (vm, _) = await StartGameAsync();
        var request = new DropRequest(Tile(vm, 3), Cell(vm, new Pos(0, 0)));
        Assert.False(vm.MoveTileCommand.CanExecute(request));
    }

    [Fact]
    public async Task ClickToSelectThenClickCell_PlacesTile()
    {
        var (vm, _) = await StartGameAsync();
        var tile3 = Tile(vm, 3);

        vm.SelectTileCommand.Execute(tile3);
        Assert.True(tile3.IsSelected);

        vm.CellClickedCommand.Execute(Cell(vm, B));
        Assert.Same(tile3, Cell(vm, B).Tile);
        Assert.False(tile3.IsSelected);
    }

    [Fact]
    public async Task Check_MarksCorrectAndWrongCells()
    {
        var (vm, _) = await StartGameAsync();

        Move(vm, Tile(vm, 4), Cell(vm, B)); // 9 + 4 = 12 is wrong
        vm.CheckCommand.Execute(null);
        Assert.Equal(CellState.Wrong, Cell(vm, B).State);
        Assert.Equal(CellState.Normal, Cell(vm, D).State); // its equation isn't complete yet

        Move(vm, Cell(vm, B), vm.Pool);
        Move(vm, Tile(vm, 3), Cell(vm, B)); // 9 + 3 = 12 is right
        vm.CheckCommand.Execute(null);
        Assert.Equal(CellState.Correct, Cell(vm, B).State);
    }

    [Fact]
    public async Task Check_StatesClearOnNextMove()
    {
        var (vm, _) = await StartGameAsync();
        Move(vm, Tile(vm, 4), Cell(vm, B));
        vm.CheckCommand.Execute(null);

        Move(vm, Cell(vm, B), vm.Pool);
        Assert.Equal(CellState.Normal, Cell(vm, B).State);
    }

    [Fact]
    public async Task Hint_FillsFromPoolAndLocks()
    {
        var (vm, _) = await StartGameAsync();

        vm.HintCommand.Execute(null);

        var hinted = new[] { Cell(vm, B), Cell(vm, D) }.Single(c => c.IsLocked);
        Assert.Equal(CellState.Hinted, hinted.State);
        Assert.Equal(hinted.Pos == B ? 3 : 4, hinted.Value);
        Assert.Single(vm.Pool.Tiles);
        Assert.Equal(1, vm.HintsUsed);
        Assert.False(vm.MoveTileCommand.CanExecute(new DropRequest(hinted, vm.Pool)));
    }

    [Fact]
    public async Task Hint_FixesWrongCellUsingMisplacedTile()
    {
        var (vm, _) = await StartGameAsync();
        Move(vm, Tile(vm, 4), Cell(vm, B));
        Move(vm, Tile(vm, 3), Cell(vm, D)); // both wrong, pool empty

        vm.HintCommand.Execute(null);

        // B was wrong; its 3 comes from D, and the 4 that was in B goes back to the pool.
        Assert.Equal(3, Cell(vm, B).Value);
        Assert.True(Cell(vm, B).IsLocked);
        Assert.Null(Cell(vm, D).Tile);
        Assert.Equal([4], vm.Pool.Tiles.Select(t => t.Value));
    }

    [Fact]
    public async Task SolvingPuzzle_StopsTimerAndBlocksFurtherMoves()
    {
        var (vm, timer) = await StartGameAsync();
        timer.RaiseTick();

        Move(vm, Tile(vm, 3), Cell(vm, B));
        Assert.False(vm.IsSolved);
        Move(vm, Tile(vm, 4), Cell(vm, D));

        Assert.True(vm.IsSolved);
        Assert.False(timer.IsRunning);
        Assert.Contains("00:01", vm.StatusMessage);
        Assert.False(vm.MoveTileCommand.CanExecute(new DropRequest(Cell(vm, B), vm.Pool)));
        Assert.False(vm.HintCommand.CanExecute(null));
    }

    [Fact]
    public async Task Reset_ReturnsAllTilesAndRestartsClock()
    {
        var (vm, timer) = await StartGameAsync();
        vm.HintCommand.Execute(null);
        vm.HintCommand.Execute(null);
        Assert.True(vm.IsSolved);

        vm.ResetCommand.Execute(null);

        Assert.Equal(2, vm.Pool.Tiles.Count);
        Assert.All(vm.Cells.Where(c => c.IsBlank), c => Assert.False(c.IsLocked));
        Assert.False(vm.IsSolved);
        Assert.Equal(0, vm.HintsUsed);
        Assert.Equal(TimeSpan.Zero, vm.Elapsed);
        Assert.True(timer.IsRunning);
    }
}
