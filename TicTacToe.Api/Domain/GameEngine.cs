using System;
using System.Collections.Generic;
using System.Linq;

namespace TicTacToe.Api.Domain;

public enum GameMode { TwoPlayer, Computer }

public enum GameStatus { InProgress, Won, Draw }

public record Move(int MoveNumber, char Player, int Cell)
{
    public int Row => Cell / 3;
    public int Column => Cell % 3;
}

public class Game
{
    public Guid Id { get; } = Guid.NewGuid();
    public char[] Board { get; } = Enumerable.Repeat(' ', 9).ToArray();
    public char CurrentPlayer { get; set; } = 'X';
    public GameMode Mode { get; init; } = GameMode.TwoPlayer;
    public GameStatus Status { get; set; } = GameStatus.InProgress;
    public char? Winner { get; set; }
    public int[]? WinningCells { get; set; }
    public List<Move> History { get; } = new();

    // Guards the "scoreboard updates only once per completed game" rule.
    public bool ScoreCounted { get; set; }
}

public class MoveResult
{
    public bool Success { get; init; }
    public string? Error { get; init; }

    public static MoveResult Ok() => new() { Success = true };
    public static MoveResult Fail(string error) => new() { Success = false, Error = error };
}

public static class GameEngine
{
    // All eight winning lines, as flat board indices.
    private static readonly int[][] Lines =
    {
        new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 },   // rows
        new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 },   // columns
        new[] { 0, 4, 8 }, new[] { 2, 4, 6 }                       // diagonals
    };

    public static MoveResult MakeMove(Game game, char player, int cell)
    {
        if (game.Status != GameStatus.InProgress)
            return MoveResult.Fail("Game is already complete.");

        if (cell < 0 || cell > 8)
            return MoveResult.Fail("Cell is outside the board.");

        if (game.Board[cell] != ' ')
            return MoveResult.Fail("Cell is already occupied.");

        if (player != game.CurrentPlayer)
            return MoveResult.Fail($"It is {game.CurrentPlayer}'s turn.");

        Apply(game, player, cell);
        return MoveResult.Ok();
    }

    // Shared by human and computer moves so both go through the same state
    // transition. Assumes the move has already been validated.
    private static void Apply(Game game, char player, int cell)
    {
        game.Board[cell] = player;
        game.History.Add(new Move(game.History.Count + 1, player, cell));
        Evaluate(game);

        if (game.Status == GameStatus.InProgress)
            game.CurrentPlayer = Opponent(player);
    }

    // Recomputes status/winner from the board alone, so it is equally correct
    // after a move and after an undo.
    private static void Evaluate(Game game)
    {
        foreach (var line in Lines)
        {
            var a = game.Board[line[0]];
            if (a != ' ' && a == game.Board[line[1]] && a == game.Board[line[2]])
            {
                game.Status = GameStatus.Won;
                game.Winner = a;
                game.WinningCells = line;
                return;
            }
        }

        if (game.Board.All(c => c != ' '))
        {
            game.Status = GameStatus.Draw;
            game.Winner = null;
            game.WinningCells = null;
            return;
        }

        game.Status = GameStatus.InProgress;
        game.Winner = null;
        game.WinningCells = null;
    }

    public static char Opponent(char player) => player == 'X' ? 'O' : 'X';

    // --- Undo -------------------------------------------------------------
    // Option A: undo is disabled once the game is complete, so the scoreboard
    // for a finished game is final and never needs compensating adjustment.

    public static bool CanUndo(Game game) =>
        game.Status == GameStatus.InProgress && game.History.Count > 0;

    public static MoveResult Undo(Game game)
    {
        if (game.Status != GameStatus.InProgress)
            return MoveResult.Fail("Cannot undo a completed game.");

        if (game.History.Count == 0)
            return MoveResult.Fail("There are no moves to undo.");

        // Two-player: remove one move. Computer: remove the computer's move
        // and the human move that triggered it, so it is the human's turn again.
        var toRemove = game.Mode == GameMode.Computer
            ? Math.Min(2, game.History.Count)
            : 1;

        for (var i = 0; i < toRemove; i++)
        {
            var last = game.History[^1];
            game.Board[last.Cell] = ' ';
            game.History.RemoveAt(game.History.Count - 1);
        }

        Evaluate(game);
        game.CurrentPlayer = game.History.Count == 0
            ? 'X'
            : Opponent(game.History[^1].Player);

        return MoveResult.Ok();
    }

    // --- Computer opponent ------------------------------------------------
    // Priority: win > block > centre > corner > any.

    public static int? GetComputerMove(Game game)
    {
        if (game.Status != GameStatus.InProgress)
            return null;

        var computer = 'O';
        var human = 'X';

        var win = FindWinningCell(game, computer);
        if (win.HasValue) return win;

        var block = FindWinningCell(game, human);
        if (block.HasValue) return block;

        if (game.Board[4] == ' ') return 4;

        foreach (var corner in new[] { 0, 2, 6, 8 })
            if (game.Board[corner] == ' ') return corner;

        for (var i = 0; i < 9; i++)
            if (game.Board[i] == ' ') return i;

        return null;
    }

    // Returns the cell that would complete a line for the given player, if any.
    private static int? FindWinningCell(Game game, char player)
    {
        foreach (var line in Lines)
        {
            var cells = line.Select(i => game.Board[i]).ToArray();
            if (cells.Count(c => c == player) == 2 && cells.Contains(' '))
                return line[Array.IndexOf(cells, ' ')];
        }
        return null;
    }

    public static void PlayComputerMove(Game game)
    {
        var cell = GetComputerMove(game);
        if (cell.HasValue)
            Apply(game, 'O', cell.Value);
    }

    // --- Reset ------------------------------------------------------------
    // Clears the game but leaves the scoreboard alone (scoreboard lives in the
    // store, not on the Game).

    public static void Reset(Game game)
    {
        for (var i = 0; i < 9; i++) game.Board[i] = ' ';
        game.History.Clear();
        game.CurrentPlayer = 'X';
        game.Status = GameStatus.InProgress;
        game.Winner = null;
        game.WinningCells = null;
        game.ScoreCounted = false;
    }
}
