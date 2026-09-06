using System;
using System.Collections.Generic;
using System.Linq;
using TicTacToe.Api.Domain;

namespace TicTacToe.Api.Contracts;

public record CreateGameRequest(string Mode = "TwoPlayer");

// The board is stored as a flat array of 9 internally because that makes win
// detection a simple lookup over eight index triples. The API accepts and
// returns row/column as well, matching the wording of the specification.
public record MoveRequest(char Player, int? Row, int? Column, int? Cell)
{
    public int? ToCellIndex()
    {
        if (Cell.HasValue) return Cell;
        if (Row.HasValue && Column.HasValue)
        {
            if (Row < 0 || Row > 2 || Column < 0 || Column > 2) return -1;
            return Row.Value * 3 + Column.Value;
        }
        return null;
    }
}

public record MoveDto(int MoveNumber, string Player, int Row, int Column, int Cell);

public record ScoreboardDto(int XWins, int OWins, int Draws);

public record GameStateDto(
    Guid GameId,
    string[] Board,
    string CurrentPlayer,
    string Mode,
    string Status,
    string? Winner,
    int[]? WinningCells,
    bool CanUndo,
    IEnumerable<MoveDto> MoveHistory,
    ScoreboardDto Scoreboard);

public static class Mapper
{
    public static GameStateDto ToDto(Game game, Scoreboard scoreboard) => new(
        game.Id,
        game.Board.Select(c => c == ' ' ? "" : c.ToString()).ToArray(),
        game.CurrentPlayer.ToString(),
        game.Mode.ToString(),
        game.Status.ToString(),
        game.Winner?.ToString(),
        game.WinningCells,
        GameEngine.CanUndo(game),
        game.History.Select(m =>
            new MoveDto(m.MoveNumber, m.Player.ToString(), m.Row, m.Column, m.Cell)),
        new ScoreboardDto(scoreboard.XWins, scoreboard.OWins, scoreboard.Draws));
}
