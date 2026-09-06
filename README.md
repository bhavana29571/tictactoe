# Tic Tac Toe — React + .NET Web API

A browser-based Tic Tac Toe game. A React frontend talks to a .NET Web API over
REST; the backend owns the game rules, move history, game status and scoreboard.

Supports two-player and computer modes, move history, undo, win/draw detection
with highlighted winning cells, and a session-level scoreboard.

---

## 1. Project overview

The backend is the single source of truth. The frontend renders whatever game
state the API returns and evaluates no game rules of its own — every click is a
request, and the response replaces the UI state wholesale.

The game rules live in a pure C# class (`GameEngine`) with no dependency on
ASP.NET, so they can be unit tested directly without spinning up a web host.
The controllers are a thin layer over that engine.

---

## 2. Tech stack

| Layer | Choice |
|---|---|
| Frontend | React 19 + TypeScript + Vite |
| Backend | .NET 10 Web API (controllers) |
| API style | REST / JSON |
| Storage | In-memory (singleton `GameStore`) |
| Tests | xUnit |
| Source control | GitHub |

The brief allowed React or Angular for the frontend; I chose React based on
existing familiarity, which left more of the available time for the backend and
its tests.

`src/types.ts` mirrors the backend DTOs, so the API contract is declared once on
the frontend and the compiler catches any drift between the two.

---

## 3. Features implemented

- 3×3 board; empty cells clickable, filled cells locked
- Alternating turns with the current player displayed
- Win detection across rows, columns and both diagonals
- Winning cells highlighted; remaining cells dimmed
- Draw detection when the board fills with no winner
- Move history for the current game (move number, player, row and column)
- Undo last move, with mode-dependent behaviour (see Design decisions)
- Reset Game — clears the board and history, leaves the scoreboard intact
- Session scoreboard (X wins / O wins / draws), served by the backend
- Reset Scoreboard, separate from Reset Game
- Two game modes: Two Player, and Play Against Computer
- Rule-based computer opponent following the specified move priority
- Server-side validation of every move; invalid moves rejected with a message
- Keyboard accessible board with visible focus, ARIA labels per cell, and
  `prefers-reduced-motion` support

---

## 4. Running the backend

Requires the .NET 10 SDK.

```bash
cd TicTacToe.Api
dotnet run
```

The API listens on `http://localhost:5217`. CORS is configured to allow
`http://localhost:5173` (the Vite dev server).

---

## 5. Running the frontend

Requires Node 20 or later.

```bash
cd tictactoe-web
npm install
npm run dev
```

Open `http://localhost:5173`. The backend must be running first — the API base
URL is set at the top of `src/App.tsx`.

---

## 6. API endpoints

| Method | Endpoint | Purpose |
|---|---|---|
| POST | `/api/games` | Create a game. Body: `{ "mode": "TwoPlayer" \| "Computer" }` |
| GET | `/api/games/{id}` | Get current game state |
| POST | `/api/games/{id}/moves` | Submit a move. Body: `{ "player": "X", "row": 0, "column": 0 }` or `{ "player": "X", "cell": 0 }` |
| POST | `/api/games/{id}/undo` | Undo the last move (or move pair in computer mode) |
| POST | `/api/games/{id}/reset` | Reset the current game, keeping the scoreboard |
| GET | `/api/scoreboard` | Get the scoreboard |
| POST | `/api/scoreboard/reset` | Reset the scoreboard |

### Game state response

```json
{
  "gameId": "90a50ed1-41d1-4232-bef5-6622a57bc47d",
  "board": ["X", "", "O", "", "", "", "", "", ""],
  "currentPlayer": "X",
  "mode": "TwoPlayer",
  "status": "InProgress",
  "winner": null,
  "winningCells": null,
  "canUndo": true,
  "moveHistory": [
    { "moveNumber": 1, "player": "X", "row": 0, "column": 0, "cell": 0 }
  ],
  "scoreboard": { "xWins": 0, "oWins": 0, "draws": 0 }
}
```

`status` is one of `InProgress`, `Won`, `Draw`. Empty cells are `""`.

### Rejected moves

Invalid moves return `400` with `{ "error": "..." }`. Rejected cases: cell
outside the board, occupied cell, move after the game is complete, and a move
by the player whose turn it is not.

---

## 7. Running tests

```bash
dotnet test
```

29 tests.

`GameEngineTests` (21) covers the game rules: valid move, occupied cell,
out-of-bounds cell, wrong player, move after completion, turn switching, row /
column / diagonal wins, draw, reset, undo in two-player mode, undo in computer
mode, undo with no moves, undo after completion, and all five computer move
priorities including win-before-block.

`ScoreboardTests` (8) covers scoreboard behaviour: X win, O win and draw each
incrementing the right counter, a completed game counting only once across
repeated calls, an in-progress game not counting, Reset Game keeping the
scoreboard while allowing the next result to count, Reset Scoreboard zeroing all
counts, and separate games accumulating on the same session scoreboard.

---

## 8. AI tools and prompt summary

I used Claude (Anthropic) as an implementer, not as a designer — I settled the
architecture and the trade-offs first, then wrote prompts specific enough that
the output could be reviewed against a decision I had already made. Visual
Studio for the backend and tests, VS Code for the frontend.

**Decided before writing any prompt:** the flat 9-cell board representation with
row/column at the API boundary, Option A for undo, and React over Angular. Each
of these changes the shape of the API and the tests, so none could be left to
the model.

**Prompts, in order:**

1. Write a pure C# game engine for this spec — no ASP.NET dependency, flat
   9-cell board, move history exposing row and column, undo removing one move in
   two-player mode and two in computer mode and disabled once the game is
   complete, computer priority win → block → centre → corner → any.
2. Write an xUnit suite against that engine covering the thirteen scenarios the
   brief lists.
3. Add the in-memory store, DTOs and controllers for the seven endpoints. The
   move endpoint accepts either a cell index or row/column; in computer mode the
   reply happens inside the same request; the scoreboard increments exactly once
   per completed game; CORS configured for the Vite dev server.
4. Write a React component that renders backend state only — no local board
   array, no client-side win checking.
5. Restyle the UI as a game rather than a form, then several rounds of specific
   layout and colour corrections.
6. Add scoreboard tests against `GameStore`, and convert the frontend to
   TypeScript with the API contract declared in a types module.

**Generated:** `GameEngine.cs`, `GameEngineTests.cs`, `ScoreboardTests.cs`,
`GameStore.cs`, `Dtos.cs`, both controllers, `Program.cs`, `App.tsx`,
`types.ts`, `main.tsx`, `App.css`.

**Reviewed and corrected by me:**

- Claude suggested upgrading `Microsoft.OpenApi` to clear an NU1903 advisory.
  The upgrade broke the build — the newer package makes
  `IOpenApiMediaType.Example` read-only and .NET 10's OpenAPI source generator
  still assigns to it. I reverted it and documented the advisory instead.
- `aspect-ratio: 1` had been set on the board container rather than the cells,
  producing non-square cells and off-centre marks. Caught visually.
- The CSS grid stretched the panel column, pushing the board off centre.
- The heading was large enough to push the control buttons below the fold at
  laptop height.
- Auditing the finished solution against the brief line by line surfaced two
  gaps: the scoreboard had no test coverage despite being on the required list,
  and the frontend had been scaffolded as plain JSX when the brief specifies
  TypeScript. Both were closed before submission.

**Verified independently:** the API was exercised in Postman before any frontend
existed — a full win sequence confirming `status`, `winner`, `winningCells`,
`canUndo` and the scoreboard increment, and a further move on the completed game
correctly returning `400`. The UI was then checked by hand across both modes,
undo in each, and both reset actions.

Fuller detail, including the prompts verbatim, is in `AI_NOTES.md`.

---

## 9. Design decisions

**Backend owns the state.** The frontend holds no board array of its own and
never decides whether a move is legal or whether the game is won. It posts a
move and replaces its state with the response. This makes the two consistent by
construction rather than by discipline.

**Flat 9-cell board internally, row/column at the API boundary.** Win detection
becomes a lookup over eight index triples rather than nested loops, which is
less code and easier to reason about. The API accepts either `cell` or
`row`/`column` on a move, and always returns both in the move history, matching
the wording of the brief.

**`Evaluate()` recomputes status from the board alone.** Nothing is tracked
incrementally. This is what makes undo simple — it deletes moves from the board
and re-evaluates, so the same code path is correct after a move and after an
undo. There is no separate "recalculate win status" branch to keep in sync.

**Computer replies within the same request.** In computer mode, posting X's
move returns the state after O has already responded. One round trip, nothing
for the frontend to orchestrate, and no window in which the board is renderable
mid-turn.

**Scoreboard guarded by a flag.** `RecordResultIfComplete` runs after every
state change, but a `ScoreCounted` boolean on the game makes repeat calls
harmless. The "update only once per completed game" rule is therefore enforced
in one place rather than across several branches.

**API contract declared once on the frontend.** `types.ts` mirrors the backend
DTOs. Nothing in the component parses or reshapes the response — it consumes the
typed state directly, so a change to a DTO surfaces as a compile error rather
than a runtime one.

---

## 10. Clarifications and assumptions

**Undo after completion — Option A.** Once a game is won or drawn, undo is
disabled and the scoreboard entry for that game is final.

I considered Option B and rejected it. Reversing a completed result correctly
requires compensating scoreboard adjustments and re-deriving game status across
an undo that crosses the win boundary — more surface area for the same visible
feature. Option A keeps the scoreboard provably consistent, and the brief
explicitly permits either.

Other assumptions:

- The scoreboard is session-level and shared across all games in the process,
  as the brief describes it, rather than per game session.
- In computer mode the human is always X and moves first.
- Undo in computer mode removes two moves; where only one move exists, it
  removes that one and returns the turn to X.
- Games are never deleted; the store grows for the lifetime of the process,
  which is acceptable for a local exercise.

---

## 11. Known limitations

- **In-memory storage.** All games and scores are lost when the API restarts.
  SQLite was permitted but not required, and persistence added nothing to the
  behaviour under review.
- **No frontend tests.** The brief prefers backend tests for game rules and
  treats frontend tests as optional. Given the time available, I put the effort
  into covering the engine and the scoreboard thoroughly rather than covering
  both layers thinly.
- **`Microsoft.OpenApi` 2.0.0 advisory.** The .NET 10 web API template pulls in
  a version carrying GHSA-v5pm-xwqc-g5wc. Upgrading it breaks the OpenAPI source
  generator (see section 8), so the templated version was retained. The API is
  local-only and the advisory is not exploitable in this context.
- **Rule-based computer opponent.** It follows the priority list in the brief
  rather than playing optimally, so it is beatable — as specified.
- **No concurrency handling beyond the store.** `ConcurrentDictionary` protects
  the game collection, but two simultaneous moves on the same game are not
  serialised. Not reachable through the UI.

---

## 12. Future improvements

- Persist games and scoreboard to SQLite so state survives a restart
- Per-session scoreboards rather than one shared across the process
- Minimax computer opponent, with a difficulty selector
- Frontend component tests and an end-to-end test covering a full round
- Optimistic UI updates so moves render before the response arrives
- Generate `types.ts` from the backend's OpenAPI document rather than hand-
  maintaining it
