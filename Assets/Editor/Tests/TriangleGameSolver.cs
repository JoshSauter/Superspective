using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace Editor.Tests {
    public class TriangleGameSolver {
        public enum Direction {
            UpLeft,
            UpRight,
            Left,
            Right,
            DownLeft,
            DownRight
        }
        
        // 0 1 2 3 4 // Row 0
        //  5 6 7 8  // Row 1
        //   9 0 1   // 0 1 representing 10 and 11 here and forward (just keeping to single digit for ASCII)
        //    2 3    // Row 3
        //     4     // Row 4
        public class Board {
            private readonly int sideLength; // How many pegs per triangle side
            public readonly int size; // Length of data array, total number of pins
            private readonly Spot[] board;

            /// <summary>
            /// Returns a new Board with the given side length and starting spot (where the non-peg spot is to begin the game)
            /// </summary>
            /// <param name="sideLength">How many pegs make up a side of the triangle</param>
            /// <param name="startingSpotIndex">The index of the starting spot where a peg is not placed</param>
            public Board(int sideLength, int startingSpotIndex) {
                this.sideLength = sideLength;
                this.size = TriangularNumber(sideLength);
                this.board = new Spot[size];

                for (int i = 0; i < size; i++) {
                    this.board[i] = new Spot() {
                        index = i,
                        hasPeg = i != startingSpotIndex
                    };
                }

                for (int i = 0; i < size; i++) {
                    this.board[i].neighbors = GetNeighbors(i);
                }
            }

            private Board(Board fromBoard) {
                this.sideLength = fromBoard.sideLength;
                this.size = fromBoard.size;
                this.board = new Spot[fromBoard.size];
                for (int i = 0; i < size; i++) {
                    this.board[i] = new Spot() {
                        hasPeg = fromBoard.board[i].hasPeg,
                        index = i,
                        neighbors = fromBoard.GetNeighbors(i)
                    };
                }
            }

            public int NumberOfPegsRemaining => board.Count(spot => spot.hasPeg);

            public int BoardHash {
                get {
                    StringBuilder sb = new StringBuilder();
                    foreach (Spot spot in board)
                    {
                        sb.Append(spot.hasPeg ? '1' : '0'); // Appending '1' if spot has a peg, '0' otherwise
                    }
                    string stateString = sb.ToString();
                    return stateString.GetHashCode();
                }
            }
            
            public static int TriangularNumber(int t) => t * (t + 1) / 2;
            private int StartOfRow(int row) => size - TriangularNumber(sideLength - row);
            private int EndOfRow(int row) => StartOfRow(row) + (sideLength - row - 1);
            public int RowOfIndex(int i) {
                int row = 0;
                while (StartOfRow(row + 1) <= i) {
                    row++;
                }

                return row;
            }

            public override string ToString() {
                StringBuilder sb = new StringBuilder();
                for (int row = 0; row < sideLength; row++) {
                    sb.Append("\n");
                    sb.Append(new string(' ', row));
                    for (int indexInRow = StartOfRow(row); indexInRow <= EndOfRow(row); indexInRow++) {
                        sb.Append(board[indexInRow].hasPeg ? "X" : "o");
                        if (indexInRow < EndOfRow(row)) {
                            sb.Append(" ");
                        }
                    }
                }

                return sb.ToString();
            }

            public static string PlayedGameString(Board startingBoard, List<Move> moves) {
                Board copy = new Board(startingBoard);
                StringBuilder sb = new StringBuilder();
                foreach (Move move in moves) {
                    copy = copy.PlayMove(move);
                    sb.Append(copy);
                    sb.Append("\n----------");
                }

                return sb.ToString();
            }

            private Dictionary<Direction, int> GetNeighbors(int index) {
                int row = RowOfIndex(index);
                
                int rowStartIndex = StartOfRow(row);
                int rowEndIndex = EndOfRow(row);
                
                int upRowStartIndex = StartOfRow(row - 1);
                upRowStartIndex = upRowStartIndex >= 0 ? upRowStartIndex : -1; // -1 are flag values representing invalid index
                int downRowStartIndex = StartOfRow(row + 1);
                downRowStartIndex = downRowStartIndex < size ? downRowStartIndex : -1;

                int offsetFromRowStart = index - rowStartIndex;
                
                Dictionary<Direction, int> neighbors = new Dictionary<Direction, int>();
                // If a row exists above, up-left and up-right are automatically valid (larger rows above)
                if (upRowStartIndex >= 0) {
                    // Up-Left
                    neighbors.Add(Direction.UpLeft, upRowStartIndex + offsetFromRowStart);
                    // Up-Right
                    neighbors.Add(Direction.UpRight, upRowStartIndex + offsetFromRowStart + 1);
                }
                // Left
                if (index - 1 >= rowStartIndex) { // If the neighbor to the left is inbounds
                    neighbors.Add(Direction.Left, index - 1);
                }
                // Right
                if (index + 1 <= rowEndIndex) { // If the neighbor to the right is inbounds
                    neighbors.Add(Direction.Right, index + 1);
                }
                if (downRowStartIndex >= 0) {
                    // Down-Left
                    if (offsetFromRowStart > 0) { // We are not all the way to the left and a down row exists
                        neighbors.Add(Direction.DownLeft, downRowStartIndex + offsetFromRowStart - 1);
                    }
                    // Down-Right
                    if (index < rowEndIndex) { // We are not all the way to the right and a down row exists
                        neighbors.Add(Direction.DownRight, downRowStartIndex + offsetFromRowStart);
                    }
                }

                return neighbors;
            }

            private bool ValidateMove(int indexOfPiece, Direction moveDirection) {
                Spot spot = board[indexOfPiece];
                if (!spot.hasPeg) return false;

                Dictionary<Direction, int> neighbors = spot.neighbors;
                
                // Must not move off the board
                if (!neighbors.ContainsKey(moveDirection)) return false;
                
                Spot jumpedPeg = board[neighbors[moveDirection]];
                
                // Must jump over an existing peg
                if (!jumpedPeg.hasPeg) return false;

                Dictionary<Direction, int> neighborNeighbors = jumpedPeg.neighbors;
                
                // Must not move off the board
                if (!neighborNeighbors.ContainsKey(moveDirection)) return false;
                
                Spot landingSpot = board[neighborNeighbors[moveDirection]];
                
                // Must land in an empty space
                if (landingSpot.hasPeg) return false;

                return true;
            }

            public HashSet<Move> ValidMoves(int indexOfPiece) {
                Direction[] directions = (Direction[])Enum.GetValues(typeof(Direction));
                var result = directions
                    .Where(d => ValidateMove(indexOfPiece, d))
                    .Select(d => new Move() { index = indexOfPiece, direction = d })
                    .ToHashSet();

                return result;
            }

            /// <summary>
            /// Attempts to play a move given the index of a piece to move and a move direction
            /// </summary>
            /// <param name="indexOfPiece">Index of piece to move</param>
            /// <param name="moveDirection">Direction to attempt a move in</param>
            /// <returns>New Board representing the updated state after playing the valid move</returns>
            public Board PlayMove(Move move) {
                int indexOfPiece = move.index;
                Direction moveDirection = move.direction;
                if (!ValidateMove(indexOfPiece, moveDirection)) {
                    ValidateMove(indexOfPiece, moveDirection);
                    throw new Exception($"Cannot make move for piece {indexOfPiece} in direction {moveDirection.ToString()}");
                }
                
                // Valid move: Jumped peg gets removed, piece being moved gets transferred to landing spot
                Dictionary<Direction, int> neighbors = board[indexOfPiece].neighbors;
                Spot jumpedPeg = board[neighbors[moveDirection]];
                Dictionary<Direction, int> neighborNeighbors = jumpedPeg.neighbors;
                Spot landingSpot = board[neighborNeighbors[moveDirection]];

                Board newBoard = new Board(this);
                newBoard.board[jumpedPeg.index].hasPeg = false;
                newBoard.board[indexOfPiece].hasPeg = false;
                newBoard.board[landingSpot.index].hasPeg = true;
                return newBoard;
            }
        }

        public struct Spot {
            public int index;
            public bool hasPeg;
            public Dictionary<Direction, int> neighbors;
        }

        public struct Move {
            public int index;
            public Direction direction;
        }

        [Test]
        public void RowOfIndexTest() {
            Board board = new Board(5, 0);
            for (int i = 0; i < 15; i++) {
                switch (i) {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                        Assert.That(board.RowOfIndex(i) == 0);
                        break;
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                        Assert.That(board.RowOfIndex(i) == 1);
                        break;
                    case 9:
                    case 10:
                    case 11:
                        Assert.That(board.RowOfIndex(i) == 2);
                        break;
                    case 12:
                    case 13:
                        Assert.That(board.RowOfIndex(i) == 3);
                        break;
                    case 14:
                        Assert.That(board.RowOfIndex(i) == 4);
                        break;
                }
            }
        }
        
        [Test]
        public void SolveTriangleGame() {
            int triangleSize = 5;
            int size = Board.TriangularNumber(triangleSize);
            for (int i = 0; i < 15; i++) {
                Board board = new Board(triangleSize, i);

                Tuple<int, List<Move>> solution = SolveBoard(board);
                Debug.Log($"Starting board: {board}\nNumber of pegs remaining: {solution.Item1}\nPlayed game: {Board.PlayedGameString(board, solution.Item2)}");
            }
        }

        private Tuple<int, List<Move>> SolveBoard(Board board) {
            // BoardHash -> Num pegs remaining in solution
            // TODO: This doesn't actually save any processing... figure out a better way
            Dictionary<int, int> memoizedAnswers = new Dictionary<int, int>();
            
            Tuple<int, List<Move>> SolveBoardRecursively(Board boardState, List<Move> moves) {
                List<Move> allValidMoves = Enumerable.Range(0, boardState.size)
                    .SelectMany(boardState.ValidMoves)
                    .ToList();

                if (allValidMoves.Count == 0) {
                    int hash = boardState.BoardHash;
                    int numPegsRemaining = boardState.NumberOfPegsRemaining;
                    if (!memoizedAnswers.ContainsKey(hash) || memoizedAnswers[hash] > numPegsRemaining) {
                        memoizedAnswers[boardState.BoardHash] = numPegsRemaining;
                    }
                    return Tuple.Create(numPegsRemaining, moves);
                }
                
                return allValidMoves.Select(move => SolveBoardRecursively(boardState.PlayMove(move), moves.Append(move).ToList()))
                    .OrderBy(solution => solution.Item1)
                    .FirstOrDefault();
            }

            return SolveBoardRecursively(board, new List<Move>());
        }
    }
}