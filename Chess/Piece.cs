using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Chess
{
	internal record struct Position(int File, int Rank)
	{
		public override string ToString()
		{
			return $"{File}, {Rank}";
		}
	}

	abstract class Piece
	{
		internal bool Color { get; set; }
		internal Position Position { get; set; }
		internal HashSet<Position> PossiblePositions { get; set; } = new();
		internal HashSet<Position> Blockers { get; set; } = new();

		internal Piece() { }
		internal Piece(bool color, Position position)
		{
			Color = color; Position = position;
		}

		internal abstract void SetPossiblePositions();

		internal bool AddPositionToPossiblePositions(Position possiblePosition)
		{
			var stoppingPiece = Chess.Board
					.FirstOrDefault(piece => piece.Position == possiblePosition);

			if (stoppingPiece is null)
			{
				PossiblePositions.Add(possiblePosition);
				return true;
			}

			if (stoppingPiece.Color != Color)
				PossiblePositions.Add(possiblePosition);
			else
				Blockers.Add(possiblePosition);
			return false;
		}

		internal bool TestMove(Position newPosition)
		{
			var moveValid = false;
			if (PossiblePositions.Contains(newPosition))
			{
				var oldPosition = Position;
				Position = newPosition;
				var capturedPiece = Capture();
				SetAllPiecesPossiblePositions(oldPosition);

				moveValid = !CheckIfKingIsInDanger();

				Position = oldPosition;
				if (capturedPiece is not null)
					Chess.Board.Add(capturedPiece);
				SetAllPiecesPossiblePositions(newPosition);
			}
			return moveValid;
		}

		internal void Move(Position newPosition)
		{
			if (PossiblePositions.Contains(newPosition))
			{
				var oldPosition = Position;
				Position = newPosition;
				var capturedPiece = Capture();
				SetAllPiecesPossiblePositions(oldPosition);

				if (CheckIfKingIsInDanger())
				{

					Position = oldPosition;
					if (capturedPiece is not null)
						Chess.Board.Add(capturedPiece);
					SetAllPiecesPossiblePositions(newPosition);
					throw new Exception("The move you made put the king in danger.");
				}
				return;
			}
			throw new Exception("This code should not have been reached.");
		}

		Piece? Capture()
		{
			var capturedPiece = Chess.Board.SingleOrDefault(piece => piece.Color != Color && piece.Position == Position);
			if (capturedPiece is not null)
				Chess.Board.Remove(capturedPiece);
			return capturedPiece;
		}

		void SetAllPiecesPossiblePositions(Position formerPosition)
		{
			SetPossiblePositions();

			var formerStoppedPieces = Chess.Board
				.Where(piece => piece.CanCaptureAtPosition(formerPosition) || piece.Blockers.Contains(formerPosition));

			var newStoppedPieces = Chess.Board
				.Where(piece => piece.CanCaptureAtPosition(Position) || piece.Blockers.Contains(Position));

			foreach (var piece in formerStoppedPieces.Union(newStoppedPieces))
				piece.SetPossiblePositions();
		}

		internal bool CheckIfKingIsInDanger()
		{
			var king = Chess.Board.OfType<King>().Single(piece => piece.Color == Color);
			return Chess.Board.Any(piece => piece.CanCaptureAtPosition(king.Position));
		}

		internal virtual bool CanCaptureAtPosition(Position position)
		{
			return PossiblePositions.Contains(position);
		}
	}

	class King : Piece
	{
		internal override void SetPossiblePositions()
		{
			PossiblePositions = new();
			for (var fileOffset = -1; fileOffset <= 1; fileOffset++)
			{
				for (var rankOffset = -1; rankOffset <= 1; rankOffset++)
				{
					if (fileOffset == 0 && rankOffset == 0) continue;
					var possiblePosition = new Position(Position.File + fileOffset, Position.Rank + rankOffset);
					if (possiblePosition.File < 0 || possiblePosition.Rank < 0
						|| possiblePosition.File > 7 || possiblePosition.Rank > 7) continue;
					AddPositionToPossiblePositions(possiblePosition);
				}
			}
		}

		internal bool HasMoved { get; set; } = false;

		internal new void Move(Position newPosition)
		{
			base.Move(newPosition);
			HasMoved = true;
		}

		bool CheckCastlingSpacesClear(bool castleSide)
		{
			var kingSideCheckingPositions = new[] { 5, 6 };
			var queenSideCheckingPositions = new[] { 1, 2, 3 };
			foreach (var file in castleSide ? kingSideCheckingPositions : queenSideCheckingPositions)
			{
				var checkingPosition = new Position(file, Position.Rank);

				if (Chess.Board.Any(piece => piece.Position == checkingPosition)) return false;
				if (file == 1 && !castleSide) continue;

				if (Chess.Board.Any(piece => piece.Color != Color
					&& piece.CanCaptureAtPosition(checkingPosition))) return false;
			}
			return true;
		}

		internal void Castle(bool castleSide)
		{
			if (HasMoved) throw new Exception("You cannot castle, as the king has already moved.");
			if (CheckIfKingIsInDanger()) throw new Exception("You cannot castle, as the king is in check.");

			if (!CheckCastlingSpacesClear(castleSide))
				throw new Exception("You cannot castle, as the spaces between are not clear/in check.");

			var rook = Chess.Board.OfType<Rook>().SingleOrDefault(piece =>
				piece.Color == Color
				&& piece.Position == new Position(castleSide ? 7 : 0, Position.Rank)
				&& !piece.HasMoved)
				?? throw new Exception("There's no rook for you to castle with, or the rook has already moved.");
			rook.Castle(castleSide);

			var castlingPosition = Position with { File = castleSide ? 6 : 2 };
			PossiblePositions.Add(castlingPosition);
			Move(castlingPosition);
		}
	}

	class StraightLinePiece : Piece
	{
		internal HashSet<Position> KingCheckLine { get; set; } = new();

		protected void GoIntoDirection(int fileDirection, int rankDirection)
		{
			var possibleKingCheckLine = new HashSet<Position>();
			for (int i = 1; i < 8; i++)
			{
				var possibleFile = Position.File + i * fileDirection;
				var possibleRank = Position.Rank + i * rankDirection;
				var possiblePosition = new Position(possibleFile, possibleRank);

				var outOfBounds = (possibleFile is > 7 or < 0) || (possibleRank is > 7 or < 0);
				if (outOfBounds) break;

				possibleKingCheckLine.Add(possiblePosition);

				var positionResult = AddPositionToPossiblePositions(possiblePosition);

				if (positionResult) continue;

				var stoppingPiece = Chess.Board
					.FirstOrDefault(piece => piece.Position == possiblePosition);
				if (stoppingPiece is King && stoppingPiece.Color != Color) KingCheckLine = possibleKingCheckLine;

				return;
			}
		}

		internal override void SetPossiblePositions()
		{
			PossiblePositions = new();
		}
	}

	class Rook : StraightLinePiece
	{
		internal override void SetPossiblePositions()
		{
			base.SetPossiblePositions();
			KingCheckLine = new();

			GoIntoDirection(1, 0);
			GoIntoDirection(-1, 0);
			GoIntoDirection(0, 1);
			GoIntoDirection(0, -1);
		}

		internal bool HasMoved { get; set; } = false;

		internal new void Move(Position newPosition)
		{
			base.Move(newPosition);
			HasMoved = true;
		}

		internal void Castle(bool castleSide)
		{
			var castlingPosition = new Position(castleSide ? 5 : 3, Position.Rank);
			Move(castlingPosition);
		}
	}

	class Bishop : StraightLinePiece
	{
		internal override void SetPossiblePositions()
		{
			base.SetPossiblePositions();
			KingCheckLine = new();

			GoIntoDirection(1, 1);
			GoIntoDirection(1, -1);
			GoIntoDirection(-1, 1);
			GoIntoDirection(-1, -1);
		}
	}

	class Queen : StraightLinePiece
	{
		internal override void SetPossiblePositions()
		{
			base.SetPossiblePositions();
			KingCheckLine = new();

			GoIntoDirection(1, 0);
			GoIntoDirection(-1, 0);
			GoIntoDirection(0, 1);
			GoIntoDirection(0, -1);

			GoIntoDirection(1, 1);
			GoIntoDirection(1, -1);
			GoIntoDirection(-1, 1);
			GoIntoDirection(-1, -1);
		}
	}

	class Knight : Piece
	{
		internal override void SetPossiblePositions()
		{
			PossiblePositions = new();
			foreach (var i in new int[] { -1, 1 })
			{
				foreach (var j in new int[] { -1, 1 })
				{
					var longFilePosition = Position with { File = Position.File + 2 * i, Rank = Position.Rank + j };
					var longRankPosition = Position with { File = Position.File + j, Rank = Position.Rank + 2 * i };
					foreach (var position in new[] { longFilePosition, longRankPosition })
					{
						if (position.File >= 0 && position.File <= 7 && position.Rank >= 0 && position.Rank <= 7)
							AddPositionToPossiblePositions(position);
					}
				}
			}
		}
	}

	class Pawn : Piece
	{
		internal HashSet<Position> CapturePositions
		{
			get
			{
				HashSet<Position> capturePos = new();

				var forward = Color ? 1 : -1;
				var forwardPositions = Position with { Rank = Position.Rank + forward };
				if (Position.File - 1 >= 0)
					capturePos.Add(forwardPositions with { File = Position.File - 1 });
				if (Position.File + 1 <= 7)
					capturePos.Add(forwardPositions with { File = Position.File + 1 });

				return capturePos;
			}
		}

		internal override bool CanCaptureAtPosition(Position position)
		{
			return CapturePositions.Contains(position);
		}

		internal override void SetPossiblePositions()
		{
			PossiblePositions = new();
			var forward = Color ? 1 : -1;
			var firstSquareVacant = AddPositionToPossiblePositions(Position with { Rank = Position.Rank + forward });

			var isFirstMove = Position.Rank == (Color ? 1 : 6);
			if (isFirstMove && firstSquareVacant)
				AddPositionToPossiblePositions(Position with { Rank = Position.Rank + forward * 2 });

			foreach (var position in CapturePositions)
			{
				AddPositionToPossiblePositions(position, true);
			}
		}

		internal bool AddPositionToPossiblePositions(Position possiblePosition, bool isCapturePosition = false)
		{
			var stoppingPiece = Chess.Board
				.FirstOrDefault(piece => piece.Position == possiblePosition);

			if (stoppingPiece is null)
			{
				if (!isCapturePosition)
					PossiblePositions.Add(possiblePosition);
				return true;
			}

			if (stoppingPiece.Color != Color && isCapturePosition)
				PossiblePositions.Add(possiblePosition);
			Blockers.Add(possiblePosition);
			return false;
		}

		internal new void Move(Position newPosition)
		{
			var longMove = Position with { Rank = Position.Rank + (Color ? 2 : -2) };
			base.Move(newPosition);
			if (newPosition == longMove) Chess.EnPassant = longMove;
		}

		internal bool CheckCanEnPassant()
		{
			object? tempBoxEnPassant = Chess.EnPassant;
			if (tempBoxEnPassant is null) return false;
			Position enPassantPiecePos = (Position)tempBoxEnPassant;

			foreach (var fileOffset in new[] { 1, -1 })
				if (Position with { File = Position.File + fileOffset } == enPassantPiecePos) return true;
			return false;
		}

		internal bool CheckWillEnPassant(Position movePosition)
		{
			object? tempBoxEnPassant = Chess.EnPassant;
			if (tempBoxEnPassant is null) return false;
			Position enPassantPiecePos = (Position)tempBoxEnPassant;

			var sidewaysPosition = movePosition with { Rank = movePosition.Rank - (Color ? 1 : -1) };
			if (sidewaysPosition == enPassantPiecePos) return true;
			return false;
		}

		public void EnPassant()
		{
			Position enPassantPiecePos = Chess.EnPassant ?? throw new Exception("There's no en passant opportunity on the board.");
			if (!CheckCanEnPassant()) throw new Exception("This pawn cannot capture en passant.");

			AddPositionToPossiblePositions(enPassantPiecePos, true);
			Move(enPassantPiecePos);
			var finalPosition = enPassantPiecePos with { Rank = Position.Rank + (Color ? 1 : -1) };
			AddPositionToPossiblePositions(finalPosition);
			Move(finalPosition);
		}

		public void Promote<T>() where T : Piece, new()
		{
			Chess.Board.RemoveWhere(piece => piece.Position == Position);
			var promotedPiece = new T
			{
				Color = Color,
				Position = Position
			};
			Chess.Board.Add(promotedPiece);
			promotedPiece.SetPossiblePositions();
		}
	}
}
