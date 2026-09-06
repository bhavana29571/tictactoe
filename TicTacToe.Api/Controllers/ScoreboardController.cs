using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Contracts;
using TicTacToe.Api.Domain;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/scoreboard")]
public class ScoreboardController : ControllerBase
{
    private readonly GameStore _store;

    public ScoreboardController(GameStore store) => _store = store;

    // GET /api/scoreboard
    [HttpGet]
    public ActionResult<ScoreboardDto> Get()
    {
        var s = _store.Scoreboard;
        return Ok(new ScoreboardDto(s.XWins, s.OWins, s.Draws));
    }

    // POST /api/scoreboard/reset
    [HttpPost("reset")]
    public ActionResult<ScoreboardDto> Reset()
    {
        _store.Scoreboard.Reset();
        var s = _store.Scoreboard;
        return Ok(new ScoreboardDto(s.XWins, s.OWins, s.Draws));
    }
}
