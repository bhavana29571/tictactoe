// These types mirror the DTOs the .NET API returns. The backend is the source
// of truth for game state, so the contract is defined here once and consumed
// throughout the component.

export type Player = "X" | "O";

export type Cell = Player | "";

export type GameMode = "TwoPlayer" | "Computer";

export type GameStatus = "InProgress" | "Won" | "Draw";

export interface Move {
  moveNumber: number;
  player: Player;
  row: number;
  column: number;
  cell: number;
}

export interface Scoreboard {
  xWins: number;
  oWins: number;
  draws: number;
}

export interface GameState {
  gameId: string;
  board: Cell[];
  currentPlayer: Player;
  mode: GameMode;
  status: GameStatus;
  winner: Player | null;
  winningCells: number[] | null;
  canUndo: boolean;
  moveHistory: Move[];
  scoreboard: Scoreboard;
}

export interface ApiError {
  error: string;
}
