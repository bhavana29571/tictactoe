using TicTacToe.Api.Domain;
using Xunit;

namespace TicTacToe.Tests;

public class GameEngineTests
{
    private static Game NewGame(GameMode mode = GameMode.TwoPlayer) => new() { Mode = mode };

    // Plays a sequence of validated moves, alternating as given.
    private static void Play(Game game, params (char player, int cell)[] moves)
    {
        foreach (var (player, cell) in moves)
            GameEngine.MakeMove(game, player, cell);
    }

    // --- Valid / invalid moves -------------------------------------------

    [Fact]
    public void ValidMove_IsAccepted_AndMarksBoard()
    {
        var game = NewGame();
        var result = GameEngine.MakeMove(game, 'X', 0);

        Assert.True(result.Success);
        Assert.Equal('X', game.Board[0]);
        Assert.Single(game.History);
    }

    [Fact]
    public void MoveOnOccupiedCell_IsRejected()
    {
        var game = NewGame();
        GameEngine.MakeMove(game, 'X', 4);

        var result = GameEngine.MakeMove(game, 'O', 4);

        Assert.False(result.Success);
        Assert.Equal('X', game.Board[4]);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(9)]
    public void MoveOutsideBoard_IsRejected(int cell)
    {
        var game = NewGame();
        var result = GameEngine.MakeMove(game, 'X', cell);
        Assert.False(result.Success);
    }

    [Fact]
    public void MoveByWrongPlayer_IsRejected_AndTurnIsUnchanged()
    {
        var game = NewGame();

        var result = GameEngine.MakeMove(game, 'O', 0);

        Assert.False(result.Success);
        Assert.Equal('X', game.CurrentPlayer);
        Assert.Empty(game.History);
    }

    [Fact]
    public void MoveAfterCompletion_IsRejected()
    {
        var game = NewGame();
        Play(game, ('X', 0), ('O', 3), ('X', 1), ('O', 4), ('X', 2));

        var result = GameEngine.MakeMove(game, 'O', 5);

        Assert.False(result.Success);
        Assert.Equal(GameStatus.Won, game.Status);
    }

    // --- Turn switching ---------------------------------------------------

    [Fact]
    public void TurnAlternates_AfterEachValidMove()
    {
        var game = NewGame();
        Assert.Equal('X', game.CurrentPlayer);

        GameEngine.MakeMove(game, 'X', 0);
        Assert.Equal('O', game.CurrentPlayer);

        GameEngine.MakeMove(game, 'O', 1);
        Assert.Equal('X', game.CurrentPlayer);
    }

    // --- Win detection ----------------------------------------------------

    [Fact]
    public void RowWin_IsDetected_WithWinningCells()
    {
        var game = NewGame();
        Play(game, ('X', 0), ('O', 3), ('X', 1), ('O', 4), ('X', 2));

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal('X', game.Winner);
        Assert.Equal(new[] { 0, 1, 2 }, game.WinningCells);
    }

    [Fact]
    public void ColumnWin_IsDetected()
    {
        var game = NewGame();
        Play(game, ('X', 0), ('O', 1), ('X', 3), ('O', 2), ('X', 6));

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(new[] { 0, 3, 6 }, game.WinningCells);
    }

    [Fact]
    public void DiagonalWin_IsDetected()
    {
        var game = NewGame();
        Play(game, ('X', 0), ('O', 1), ('X', 4), ('O', 2), ('X', 8));

        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(new[] { 0, 4, 8 }, game.WinningCells);
    }

    // --- Draw -------------------------------------------------------------

    [Fact]
    public void FullBoardWithNoLine_IsDraw()
    {
        var game = NewGame();
        Play(game,
            ('X', 0), ('O', 1), ('X', 2),
            ('O', 4), ('X', 3), ('O', 5),
            ('X', 7), ('O', 6), ('X', 8));

        Assert.Equal(GameStatus.Draw, game.Status);
        Assert.Null(game.Winner);
        Assert.Null(game.WinningCells);
    }

    // --- Reset ------------------------------------------------------------

    [Fact]
    public void Reset_ClearsBoardHistoryAndStatus()
    {
        var game = NewGame();
        Play(game, ('X', 0), ('O', 3), ('X', 1), ('O', 4), ('X', 2));

        GameEngine.Reset(game);

        Assert.All(game.Board, c => Assert.Equal(' ', c));
        Assert.Empty(game.History);
        Assert.Equal('X', game.CurrentPlayer);
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Null(game.Winner);
        Assert.False(game.ScoreCounted);
    }

    // --- Undo -------------------------------------------------------------

    [Fact]
    public void Undo_TwoPlayerMode_RemovesOnlyLastMove()
    {
        var game = NewGame();
        Play(game, ('X', 0), ('O', 4));

        var result = GameEngine.Undo(game);

        Assert.True(result.Success);
        Assert.Single(game.History);
        Assert.Equal(' ', game.Board[4]);
        Assert.Equal('X', game.Board[0]);
        Assert.Equal('O', game.CurrentPlayer);
    }

    [Fact]
    public void Undo_ComputerMode_RemovesComputerAndHumanMove()
    {
        var game = NewGame(GameMode.Computer);
        GameEngine.MakeMove(game, 'X', 0);
        GameEngine.PlayComputerMove(game);

        var result = GameEngine.Undo(game);

        Assert.True(result.Success);
        Assert.Empty(game.History);
        Assert.Equal(' ', game.Board[0]);
        Assert.Equal('X', game.CurrentPlayer);
    }

    [Fact]
    public void Undo_WithNoMoves_IsRejected()
    {
        var game = NewGame();
        var result = GameEngine.Undo(game);

        Assert.False(result.Success);
        Assert.False(GameEngine.CanUndo(game));
    }

    [Fact]
    public void Undo_AfterCompletion_IsRejected()
    {
        // Option A: undo is disabled once the game is complete.
        var game = NewGame();
        Play(game, ('X', 0), ('O', 3), ('X', 1), ('O', 4), ('X', 2));

        var result = GameEngine.Undo(game);

        Assert.False(result.Success);
        Assert.Equal(GameStatus.Won, game.Status);
    }

    // --- Computer move selection -----------------------------------------

    [Fact]
    public void Computer_TakesWinningMove_BeforeBlocking()
    {
        var game = NewGame(GameMode.Computer);
        game.Board[0] = 'O'; game.Board[1] = 'O';   // O can win at 2
        game.Board[6] = 'X'; game.Board[7] = 'X';   // X threatens at 8

        Assert.Equal(2, GameEngine.GetComputerMove(game));
    }

    [Fact]
    public void Computer_BlocksOpponentWin()
    {
        var game = NewGame(GameMode.Computer);
        game.Board[6] = 'X'; game.Board[7] = 'X';

        Assert.Equal(8, GameEngine.GetComputerMove(game));
    }

    [Fact]
    public void Computer_TakesCentre_WhenAvailable()
    {
        var game = NewGame(GameMode.Computer);
        game.Board[0] = 'X';

        Assert.Equal(4, GameEngine.GetComputerMove(game));
    }

    [Fact]
    public void Computer_TakesCorner_WhenCentreTaken()
    {
        var game = NewGame(GameMode.Computer);
        game.Board[4] = 'X';

        Assert.Equal(0, GameEngine.GetComputerMove(game));
    }

    [Fact]
    public void Computer_DoesNotMove_AfterGameCompleted()
    {
        var game = NewGame(GameMode.Computer);
        Play(game, ('X', 0), ('O', 3), ('X', 1), ('O', 4), ('X', 2));

        Assert.Null(GameEngine.GetComputerMove(game));
    }
}
