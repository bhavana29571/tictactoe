using System;
using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Contracts;
using TicTacToe.Api.Domain;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/games")]
public class GamesController : ControllerBase
{
    private readonly GameStore _store;

    public GamesController(GameStore store) => _store = store;

    // POST /api/games
    [HttpPost]
    public ActionResult<GameStateDto> Create([FromBody] CreateGameRequest request)
    {
        if (!Enum.TryParse<GameMode>(request.Mode, ignoreCase: true, out var mode))
            return BadRequest(new { error = "Mode must be 'TwoPlayer' or 'Computer'." });

        var game = _store.Create(mode);
        return Ok(Mapper.ToDto(game, _store.Scoreboard));
    }

    // GET /api/games/{id}
    [HttpGet("{id:guid}")]
    public ActionResult<GameStateDto> Get(Guid id)
    {
        var game = _store.Find(id);
        if (game is null) return NotFound(new { error = "Game not found." });

        return Ok(Mapper.ToDto(game, _store.Scoreboard));
    }

    // POST /api/games/{id}/moves
    [HttpPost("{id:guid}/moves")]
    public ActionResult<GameStateDto> Move(Guid id, [FromBody] MoveRequest request)
    {
        var game = _store.Find(id);
        if (game is null) return NotFound(new { error = "Game not found." });

        var cell = request.ToCellIndex();
        if (cell is null)
            return BadRequest(new { error = "Provide either cell, or row and column." });

        var result = GameEngine.MakeMove(game, request.Player, cell.Value);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        // In computer mode the machine replies immediately, so a single
        // request returns the state after both moves.
        if (game.Mode == GameMode.Computer && game.Status == GameStatus.InProgress)
            GameEngine.PlayComputerMove(game);

        _store.RecordResultIfComplete(game);
        return Ok(Mapper.ToDto(game, _store.Scoreboard));
    }

    // POST /api/games/{id}/undo
    [HttpPost("{id:guid}/undo")]
    public ActionResult<GameStateDto> Undo(Guid id)
    {
        var game = _store.Find(id);
        if (game is null) return NotFound(new { error = "Game not found." });

        var result = GameEngine.Undo(game);
        if (!result.Success)
            return BadRequest(new { error = result.Error });

        return Ok(Mapper.ToDto(game, _store.Scoreboard));
    }

    // POST /api/games/{id}/reset
    [HttpPost("{id:guid}/reset")]
    public ActionResult<GameStateDto> Reset(Guid id)
    {
        var game = _store.Find(id);
        if (game is null) return NotFound(new { error = "Game not found." });

        GameEngine.Reset(game);
        return Ok(Mapper.ToDto(game, _store.Scoreboard));
    }
}
