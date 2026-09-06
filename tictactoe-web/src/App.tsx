import { useState, useEffect, useCallback } from "react";
import type { GameState, GameMode, Cell, Scoreboard } from "./types";
import "./App.css";

const API = "http://localhost:5217/api";

// Marks are drawn as SVG strokes so they can animate in, the way they would
// be drawn on paper.
function Mark({ player }: { player: Cell }) {
  if (player === "X") {
    return (
      <svg className="mark mark-x" viewBox="0 0 100 100" aria-hidden="true">
        <line className="stroke s1" x1="24" y1="24" x2="76" y2="76" />
        <line className="stroke s2" x1="76" y1="24" x2="24" y2="76" />
      </svg>
    );
  }
  if (player === "O") {
    return (
      <svg className="mark mark-o" viewBox="0 0 100 100" aria-hidden="true">
        <circle className="stroke s1" cx="50" cy="50" r="27" />
      </svg>
    );
  }
  return null;
}

export default function App() {
  const [game, setGame] = useState<GameState | null>(null);
  const [mode, setMode] = useState<GameMode>("TwoPlayer");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  // Every call returns the full game state, which becomes the single source
  // of truth for the UI. No game rules are evaluated on the client.
  const call = useCallback(
    async <T,>(path: string, options: RequestInit = {}): Promise<T | null> => {
      setBusy(true);
      setError(null);
      try {
        const res = await fetch(`${API}${path}`, {
          headers: { "Content-Type": "application/json" },
          ...options,
        });
        const data = await res.json();
        if (!res.ok) {
          setError(data.error ?? "That move could not be played.");
          return null;
        }
        return data as T;
      } catch {
        setError("Cannot reach the server. Check the API is running on port 5217.");
        return null;
      } finally {
        setBusy(false);
      }
    },
    []
  );

  const newGame = useCallback(
    async (gameMode: GameMode) => {
      const data = await call<GameState>("/games", {
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

  const play = async (cell: number) => {
    if (!game || busy) return;
    if (game.status !== "InProgress") return;
    if (game.board[cell] !== "") return;

    const data = await call<GameState>(`/games/${game.gameId}/moves`, {
      method: "POST",
      body: JSON.stringify({ player: game.currentPlayer, cell }),
    });
    if (data) setGame(data);
  };

  const undo = async () => {
    if (!game) return;
    const data = await call<GameState>(`/games/${game.gameId}/undo`, {
      method: "POST",
    });
    if (data) setGame(data);
  };

  const reset = async () => {
    if (!game) return;
    const data = await call<GameState>(`/games/${game.gameId}/reset`, {
      method: "POST",
    });
    if (data) setGame(data);
  };

  const resetScoreboard = async () => {
    if (!game) return;
    const data = await call<Scoreboard>("/scoreboard/reset", { method: "POST" });
    if (data) setGame({ ...game, scoreboard: data });
  };

  if (!game) {
    return (
      <div className="app">
        <h1>Tic Tac Toe</h1>
        <p className="error">{error ?? "Starting a game..."}</p>
      </div>
    );
  }

  const winning = game.winningCells ?? [];
  const over = game.status !== "InProgress";
  const computerTurn = game.mode === "Computer" && game.currentPlayer === "O";

  return (
    <div className="app">
      <header>
        <h1>Tic Tac Toe</h1>
        <p className="tagline">Line up three. Row, column or diagonal.</p>
        <div className="modes" role="radiogroup" aria-label="Game mode">
          <button
            className={mode === "TwoPlayer" ? "mode on" : "mode"}
            onClick={() => setMode("TwoPlayer")}
            role="radio"
            aria-checked={mode === "TwoPlayer"}
          >
            Two players
          </button>
          <button
            className={mode === "Computer" ? "mode on" : "mode"}
            onClick={() => setMode("Computer")}
            role="radio"
            aria-checked={mode === "Computer"}
          >
            Against computer
          </button>
        </div>
      </header>

      <div className="stage">
        <p className="status" aria-live="polite">
          {game.status === "Won" ? (
            <>
              <span className={`token ${game.winner === "X" ? "x" : "o"}`}>
                {game.winner}
              </span>
              <span>wins this round</span>
            </>
          ) : game.status === "Draw" ? (
            <span>Board full &mdash; nobody wins</span>
          ) : (
            <>
              <span className={`token ${game.currentPlayer === "X" ? "x" : "o"}`}>
                {game.currentPlayer}
              </span>
              <span>{computerTurn ? "is the computer" : "to play"}</span>
            </>
          )}
        </p>

        <div className={`board ${over ? "over" : ""}`}>
          {game.board.map((value, i) => (
            <button
              key={i}
              className={[
                "cell",
                value ? "filled" : "",
                winning.includes(i) ? "winning" : "",
              ].join(" ")}
              onClick={() => play(i)}
              disabled={value !== "" || over || busy}
              aria-label={`Row ${Math.floor(i / 3) + 1}, column ${(i % 3) + 1}${
                value ? `, ${value}` : ", empty"
              }`}
            >
              <Mark player={value} />
            </button>
          ))}
        </div>

        <div className="controls">
          <button onClick={undo} disabled={!game.canUndo || busy}>
            Undo last move
          </button>
          <button onClick={reset} disabled={busy}>
            New round
          </button>
          <button className="quiet" onClick={resetScoreboard} disabled={busy}>
            Clear scores
          </button>
        </div>

        {error && <p className="error">{error}</p>}
      </div>

      <div className="panels">
        <section className="scores">
          <h2>Scores</h2>
          <div className="score-row">
            <span className="token small x">X</span>
            <span className="count">{game.scoreboard.xWins}</span>
          </div>
          <div className="score-row">
            <span className="token small o">O</span>
            <span className="count">{game.scoreboard.oWins}</span>
          </div>
          <div className="score-row">
            <span className="token small draw">&ndash;</span>
            <span className="count">{game.scoreboard.draws}</span>
          </div>
        </section>

        <section className="history">
          <h2>Moves this round</h2>
          {game.moveHistory.length === 0 ? (
            <p className="empty">Pick a square to start.</p>
          ) : (
            <ol>
              {game.moveHistory.map((m) => (
                <li key={m.moveNumber}>
                  <span className="num">{m.moveNumber}</span>
                  <span className={`token small ${m.player === "X" ? "x" : "o"}`}>
                    {m.player}
                  </span>
                  <span className="pos">
                    row {m.row + 1}, column {m.column + 1}
                  </span>
                </li>
              ))}
            </ol>
          )}
        </section>
      </div>
    </div>
  );
}
