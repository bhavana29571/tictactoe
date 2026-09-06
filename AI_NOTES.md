# AI-Assisted Development Notes

Tool: Claude (Anthropic). Visual Studio for the backend and tests, VS Code for
the frontend.

I used Claude as an implementer, not as a designer. I decided the architecture,
the data model and the trade-offs first, then wrote prompts specific enough that
the output was reviewable against a decision I had already made.

---

## Decisions I made before writing any prompt

Three things had to be settled first, because each one changes the shape of the
API and the tests:

**Board representation — flat array of nine, internally.** Win detection then
becomes a lookup over eight index triples instead of nested row/column loops.
The brief's move-history example is written as "Row 1, Column 1", so I kept
row/column at the API boundary and converted at the edge.

**Undo after completion — Option A.** The brief permits either. Option B means
reversing scoreboard increments and re-deriving status across the win boundary.
I chose the smaller, provably consistent surface.

**React over Angular.** Allowed by the brief; chosen for familiarity so the time
went into the backend and its tests.

---

## Prompts, in order

**1. Game engine**

> Write a pure C# game engine class for this Tic Tac Toe spec. No ASP.NET
> dependency — it must be unit testable on its own. Flat 9-cell board
> internally, with move history exposing row and column. Undo removes one move
> in two-player mode and two in computer mode, and is disabled once the game is
> complete. Computer move priority: win, block, centre, corner, any.

**2. Tests**

> Write an xUnit suite against that engine covering the thirteen scenarios the
> brief lists: valid move, invalid move, turn switching, row/column/diagonal
> win, draw, reset, undo in both modes, scoreboard update, computer move
> selection, and move after completion.

**3. API layer**

> Add the in-memory store, DTOs and controllers for the seven endpoints. The
> move endpoint should accept either a cell index or row/column. In computer
> mode, the computer's reply should happen inside the same request. The
> scoreboard must increment exactly once per completed game. Configure CORS for
> the Vite dev server.

**4. Frontend**

> Write a React component that renders backend state only — no local board
> array, no client-side win checking. Every click posts a move and replaces
> state with the response.

**5. Styling**

> Restyle the UI as a game rather than a form.

Followed by several rounds of specific corrections: square cells, board
centring, panel placement, heading size, and colour palette.

**6. Closing the audit gaps**

> Add scoreboard tests against `GameStore` — each result type incrementing the
> right counter, a completed game counting only once, Reset Game keeping the
> scoreboard, Reset Scoreboard zeroing it.

> Convert the frontend to TypeScript, with the API contract declared in a types
> module rather than inline.

---

## What I reviewed and changed

**OpenAPI build failure.** Claude suggested upgrading `Microsoft.OpenApi` to
clear an NU1903 advisory. The upgrade broke the build — the newer package makes
`IOpenApiMediaType.Example` read-only and .NET 10's OpenAPI source generator
still assigns to it (CS0200). I reverted the upgrade and documented the advisory
as a known limitation instead. A wrong suggestion, caught by building rather
than trusting it.

**Non-square cells.** `aspect-ratio: 1` had been placed on the board container
rather than the cells, so padding and gaps squeezed the rows into rectangles and
the marks sat off-centre. I spotted it visually and had the property moved onto
`.cell`.

**Off-centre board.** The CSS grid was stretching the panel column to fill the
width, pushing the board away from centre. Fixed by constraining that column and
adding a balancing empty one.

**Controls below the fold.** The heading was large enough to push the three
control buttons off screen at laptop height. I cut the heading size and page
padding until the board and buttons fit together without scrolling.

**Two gaps found by auditing against the brief.** With the solution working, I
went back through the specification line by line rather than trusting that it
was complete. Two things came out of it: the scoreboard had no test coverage
even though "scoreboard update" is on the brief's required list, and the
frontend had been scaffolded from the plain React Vite template when the tech
expectations name TypeScript. Both were closed before submission — eight
scoreboard tests against `GameStore`, and a TypeScript conversion with the API
contract in `src/types.ts`.

---

## How I verified it

I tested the API in Postman before connecting any frontend:

- Created a two-player game and checked the response carried every field the
  brief requires
- Played a full five-move win sequence — confirmed `status: "Won"`,
  `winner: "X"`, `winningCells: [0,1,2]`, `canUndo: false`, `xWins: 1`
- Sent a further move on the completed game and confirmed it returned `400`
- Checked the scoreboard endpoint returned the incremented count

Then exercised the UI by hand: both modes, undo in each (one move removed in
two-player, two in computer mode), reset game leaving the scoreboard intact, and
reset scoreboard zeroing it.

---

## What I did not delegate

Framework choice, board representation, the undo option, the diagnosis of the
build failure, every layout and visual judgement, the audit against the brief
that surfaced the two gaps, and the decision about what to cut under time
pressure — frontend tests and SQLite persistence. Claude wrote code to those
decisions; it did not make them.
