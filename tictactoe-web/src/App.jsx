import { useState, useEffect, useCallback } from "react";
import "./App.css";

const API = "http://localhost:5217/api";

export default function App() {
  const [game, setGame] = useState(null);
  const [mode, setMode] = useState("TwoPlayer");
  const [error, setError] = useState(null);
  const [busy, setBusy] = useState(false);

  // Every call returns the full game state, which becomes the single source
  // of truth for the UI. No game rules are evaluated on the client.
  const call = useCallback(async (path, options = {}) => {
    setBusy(true);
    setError(null);
    try {
      const res = await fetch(`${API}${path}`, {
        headers: { "Content-Type": "application/json" },
        ...options,
      });
      const data = await res.json();
      if (!res.ok) {
        setError(data.error ?? "Request failed.");
        return null;
      }
      return data;
    } catch {
      setError("Cannot reach the server. Is the API running on port 5217?");
      return null;
    } finally {
      setBusy(false);
    }
  }, []);

  const newGame = useCallback(
    async (gameMode) => {
      const data = await call("/games", {
        method: "POST",
        body: JSON.stringify({ mode: gameMode }),
      });
      if (data) setGame(data);
    },
    [call]
  );

  useEffect(() => {
    newGame(mode);
  }, [mode, newGame]);

  const play = async (cell) => {
    if (!game || busy) return;
    if (game.status !== "InProgress") return;
    if (game.board[cell] !== "") return;

    const data = await call(`/games/${game.gameId}/moves`, {
      method: "POST",
      body: JSON.stringify({ player: game.currentPlayer, cell }),
    });
    if (data) setGame(data);
  };

  const undo = async () => {
    const data = await call(`/games/${game.gameId}/undo`, { method: "POST" });
    if (data) setGame(data);
  };

  const reset = async () => {
    const data = await call(`/games/${game.gameId}/reset`, { method: "POST" });
    if (data) setGame(data);
  };

  const resetScoreboard = async () => {
    const data = await call("/scoreboard/reset", { method: "POST" });
    if (data) setGame({ ...game, scoreboard: data });
  };

  if (!game) {
    return (
      <div className="app">
        <h1>Tic Tac Toe</h1>
        <p className="error">{error ?? "Loading..."}</p>
      </div>
    );
  }

  const winning = game.winningCells ?? [];

  const message =
    game.status === "Won"
      ? `Player ${game.winner} wins`
      : game.status === "Draw"
      ? "It's a draw"
      : `Turn: ${game.currentPlayer}${
          game.mode === "Computer" && game.currentPlayer === "O"
            ? " (computer)"
            : ""
        }`;

  return (
    <div className="app">
      <h1>Tic Tac Toe</h1>

      <div className="modes">
        <label>
          <input
            type="radio"
            checked={mode === "TwoPlayer"}
            onChange={() => setMode("TwoPlayer")}
          />
          Two Player
        </label>
        <label>
          <input
            type="radio"
            checked={mode === "Computer"}
            onChange={() => setMode("Computer")}
          />
          Play Against Computer
        </label>
      </div>

      <p className={`status ${game.status !== "InProgress" ? "final" : ""}`}>
        {message}
      </p>

      <div className="board">
        {game.board.map((value, i) => (
          <button
            key={i}
            className={`cell ${winning.includes(i) ? "winning" : ""}`}
            onClick={() => play(i)}
            disabled={value !== "" || game.status !== "InProgress" || busy}
          >
            {value}
          </button>
        ))}
      </div>

      <div className="controls">
        <button onClick={undo} disabled={!game.canUndo || busy}>
          Undo Last Move
        </button>
        <button onClick={reset} disabled={busy}>
          Reset Game
        </button>
        <button onClick={resetScoreboard} disabled={busy}>
          Reset Scoreboard
        </button>
      </div>

      {error && <p className="error">{error}</p>}

      <div className="panels">
        <section>
          <h2>Scoreboard</h2>
          <table>
            <tbody>
              <tr>
                <td>X wins</td>
                <td>{game.scoreboard.xWins}</td>
              </tr>
              <tr>
                <td>O wins</td>
                <td>{game.scoreboard.oWins}</td>
              </tr>
              <tr>
                <td>Draws</td>
                <td>{game.scoreboard.draws}</td>
              </tr>
            </tbody>
          </table>
        </section>

        <section>
          <h2>Move History</h2>
          {game.moveHistory.length === 0 ? (
            <p className="empty">No moves yet.</p>
          ) : (
            <table>
              <thead>
                <tr>
                  <th>#</th>
                  <th>Player</th>
                  <th>Position</th>
                </tr>
              </thead>
              <tbody>
                {game.moveHistory.map((m) => (
                  <tr key={m.moveNumber}>
                    <td>{m.moveNumber}</td>
                    <td>{m.player}</td>
                    <td>
                      Row {m.row + 1}, Column {m.column + 1}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>
      </div>
    </div>
  );
}
