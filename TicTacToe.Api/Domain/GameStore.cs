using System;
using System.Collections.Concurrent;

namespace TicTacToe.Api.Domain;

public class Scoreboard
{
    public int XWins { get; set; }
    public int OWins { get; set; }
    public int Draws { get; set; }

    public void Reset()
    {
        XWins = 0;
        OWins = 0;
        Draws = 0;
    }
}

// Registered as a singleton, so game state and scoreboard live for the
// lifetime of the process. No database is required for this exercise.
public class GameStore
{
    private readonly ConcurrentDictionary<Guid, Game> _games = new();

    public Scoreboard Scoreboard { get; } = new();

    public Game Create(GameMode mode)
    {
        var game = new Game { Mode = mode };
        _games[game.Id] = game;
        return game;
    }

    public Game? Find(Guid id) => _games.TryGetValue(id, out var game) ? game : null;

    // Applies a completed game to the scoreboard exactly once. Called after
    // every state change; the ScoreCounted flag makes repeat calls harmless.
    public void RecordResultIfComplete(Game game)
    {
        if (game.ScoreCounted || game.Status == GameStatus.InProgress)
            return;

        if (game.Status == GameStatus.Draw)
            Scoreboard.Draws++;
        else if (game.Winner == 'X')
            Scoreboard.XWins++;
        else if (game.Winner == 'O')
            Scoreboard.OWins++;

        game.ScoreCounted = true;
    }
}
