using TicTacToe.Api.Domain;
using Xunit;

namespace TicTacToe.Tests;

public class ScoreboardTests
{
    private static void PlayXWin(Game game)
    {
        GameEngine.MakeMove(game, 'X', 0);
        GameEngine.MakeMove(game, 'O', 3);
        GameEngine.MakeMove(game, 'X', 1);
        GameEngine.MakeMove(game, 'O', 4);
        GameEngine.MakeMove(game, 'X', 2);
    }

    private static void PlayOWin(Game game)
    {
        GameEngine.MakeMove(game, 'X', 0);
        GameEngine.MakeMove(game, 'O', 3);
        GameEngine.MakeMove(game, 'X', 1);
        GameEngine.MakeMove(game, 'O', 4);
        GameEngine.MakeMove(game, 'X', 6);
        GameEngine.MakeMove(game, 'O', 5);
    }

    private static void PlayDraw(Game game)
    {
        GameEngine.MakeMove(game, 'X', 0);
        GameEngine.MakeMove(game, 'O', 1);
        GameEngine.MakeMove(game, 'X', 2);
        GameEngine.MakeMove(game, 'O', 4);
        GameEngine.MakeMove(game, 'X', 3);
        GameEngine.MakeMove(game, 'O', 5);
        GameEngine.MakeMove(game, 'X', 7);
        GameEngine.MakeMove(game, 'O', 6);
        GameEngine.MakeMove(game, 'X', 8);
    }

    [Fact]
    public void XWin_IncrementsXWins()
    {
        var store = new GameStore();
        var game = store.Create(GameMode.TwoPlayer);

        PlayXWin(game);
        store.RecordResultIfComplete(game);

        Assert.Equal(1, store.Scoreboard.XWins);
        Assert.Equal(0, store.Scoreboard.OWins);
        Assert.Equal(0, store.Scoreboard.Draws);
    }

    [Fact]
    public void OWin_IncrementsOWins()
    {
        var store = new GameStore();
        var game = store.Create(GameMode.TwoPlayer);

        PlayOWin(game);
        store.RecordResultIfComplete(game);

        Assert.Equal(1, store.Scoreboard.OWins);
        Assert.Equal(0, store.Scoreboard.XWins);
    }

    [Fact]
    public void Draw_IncrementsDraws()
    {
        var store = new GameStore();
        var game = store.Create(GameMode.TwoPlayer);

        PlayDraw(game);
        store.RecordResultIfComplete(game);

        Assert.Equal(1, store.Scoreboard.Draws);
        Assert.Equal(0, store.Scoreboard.XWins);
        Assert.Equal(0, store.Scoreboard.OWins);
    }

    [Fact]
    public void CompletedGame_IsCountedOnlyOnce()
    {
        // The controller calls this after every state change, so repeat calls
        // must be harmless.
        var store = new GameStore();
        var game = store.Create(GameMode.TwoPlayer);

        PlayXWin(game);
        store.RecordResultIfComplete(game);
        store.RecordResultIfComplete(game);
        store.RecordResultIfComplete(game);

        Assert.Equal(1, store.Scoreboard.XWins);
        Assert.True(game.ScoreCounted);
    }

    [Fact]
    public void GameInProgress_DoesNotAffectScoreboard()
    {
        var store = new GameStore();
        var game = store.Create(GameMode.TwoPlayer);

        GameEngine.MakeMove(game, 'X', 0);
        store.RecordResultIfComplete(game);

        Assert.Equal(0, store.Scoreboard.XWins);
        Assert.Equal(0, store.Scoreboard.OWins);
        Assert.Equal(0, store.Scoreboard.Draws);
        Assert.False(game.ScoreCounted);
    }

    [Fact]
    public void ResetGame_KeepsScoreboard_AndAllowsTheNextGameToCount()
    {
        var store = new GameStore();
        var game = store.Create(GameMode.TwoPlayer);

        PlayXWin(game);
        store.RecordResultIfComplete(game);

        GameEngine.Reset(game);

        Assert.Equal(1, store.Scoreboard.XWins);   // reset does not clear scores
        Assert.False(game.ScoreCounted);           // but the next result can count

        PlayXWin(game);
        store.RecordResultIfComplete(game);

        Assert.Equal(2, store.Scoreboard.XWins);
    }

    [Fact]
    public void ResetScoreboard_ZeroesAllCounts()
    {
        var store = new GameStore();
        var game = store.Create(GameMode.TwoPlayer);

        PlayXWin(game);
        store.RecordResultIfComplete(game);

        store.Scoreboard.Reset();

        Assert.Equal(0, store.Scoreboard.XWins);
        Assert.Equal(0, store.Scoreboard.OWins);
        Assert.Equal(0, store.Scoreboard.Draws);
    }

    [Fact]
    public void SeparateGames_AccumulateOnTheSameScoreboard()
    {
        // The scoreboard is session-level, shared across games in the process.
        var store = new GameStore();

        var first = store.Create(GameMode.TwoPlayer);
        PlayXWin(first);
        store.RecordResultIfComplete(first);

        var second = store.Create(GameMode.TwoPlayer);
        PlayDraw(second);
        store.RecordResultIfComplete(second);

        Assert.Equal(1, store.Scoreboard.XWins);
        Assert.Equal(1, store.Scoreboard.Draws);
    }
}
