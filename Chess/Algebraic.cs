using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess
{
	record MoveInfo(Piece Piece, Position MovePosition, bool? CastleSide, Type? Promotion, bool IsEnPassant);

	static class Algebraic
	{
		static readonly string fileNames = "abcdefgh";
		static readonly string pieceNames = "NBRQK";

		static bool? ParseCastling(string moveStr)
		{
			if (moveStr.StartsWith("O-O") || moveStr.StartsWith("0-0"))
				return !(moveStr == "O-O-O" || moveStr == "0-0-0");
			return null;
		}

		static void ParseStringInner
			(string moveStr, out int? fileDisam, out int? rankDisam,
			out Position movePosition, out Type? promotion, out bool isCapturingMove)
		{
			(fileDisam, rankDisam, promotion, isCapturingMove) = (null, null, null, false);
			var (fileSet, rankSet) = (false, false);

			movePosition = new Position(-1, -1);
			foreach (var c in moveStr)
			{
				var character = c.ToString();
				if (fileNames.Contains(character))
				{
					if (!fileSet)
					{
						fileSet = true;
						movePosition.File = fileNames.IndexOf(character);
					}
					else
					{
						fileSet = false;
						fileDisam = movePosition.File;
						movePosition.File = fileNames.IndexOf(character);
					}
				}
				else if (int.TryParse(character, out _))
				{
					if (!rankSet)
					{
						rankSet = true;
						movePosition.Rank = int.Parse(character) - 1;
					}
					else
					{
						rankSet = false;
						rankDisam = movePosition.Rank;
						movePosition.Rank = int.Parse(character) - 1;
					}
				}
				else if (pieceNames.Contains(character)) promotion = GetPieceType(ref character);
			}
		}

		static Pawn? CheckForEnPassant(Type pieceType, IEnumerable<Piece> possiblePieces, Position movePosition)
		{
			if (pieceType == typeof(Pawn))
			{
				foreach (var piece in possiblePieces)
				{
					var pawn = (Pawn)piece;
					var willEnPassant = pawn.CheckWillEnPassant(movePosition);
					var canEnPassant = pawn.CheckCanEnPassant();
					if (willEnPassant && canEnPassant)
						return pawn;
				}
			}
			return null;
		}

		static void FilterPieces(int? fileDisam, int? rankDisam, bool color, Position movePosition, ref IEnumerable<Piece> possiblePieces)
		{
			possiblePieces = possiblePieces.Where(piece => piece.Color == color);
			if (fileDisam is not null)
				possiblePieces = possiblePieces
					.Where(piece => piece.Position.File == fileDisam);
			if (rankDisam is not null)
				possiblePieces = possiblePieces
					.Where(piece => piece.Position.Rank == rankDisam);
			if (possiblePieces.Count() == 0) throw new Exception("The piece you want to move does not exist");

			if (movePosition is { File: < 0 or > 7 } or { Rank: < 0 or > 7 })
				throw new Exception("The square you want to move to is either out of bounds or unspecified.");
			possiblePieces = possiblePieces.Where(piece => piece.PossiblePositions.Contains(movePosition));
			if (possiblePieces.Count() == 0) throw new Exception("The square you want to move to cannot be reached.");
		}

		static void CheckMissingPromotion(Type pieceType, bool color, Position movePosition, Type? promotion)
		{
			if (movePosition.Rank == (color ? 7 : 0)
				&& pieceType == typeof(Pawn)
				&& promotion is null) throw new Exception("No promotion piece for the pawn was specified.");
		}

		static internal MoveInfo
			ParseMove(string moveStr, bool color)
		{
			var possibleCastling = ParseCastling(moveStr);
			if (possibleCastling is not null)
			{
				var king = Chess.Board.OfType<King>().Single(piece => piece.Color == color);
				return new MoveInfo(king, new(-1, -1), possibleCastling, null, false);
			}

			var pieceType = GetPieceType(ref moveStr);

			ParseStringInner(moveStr, out var fileDisam, out var rankDisam,
				out var movePosition, out var promotion, out var isCapturingMove);
			CheckMissingPromotion(pieceType, color, movePosition, promotion);

			var possiblePieces = GetPiecesOfType(pieceType);

			Pawn? enPassantPawn = CheckForEnPassant(pieceType, possiblePieces, movePosition);
			if (enPassantPawn is not null)
				return new MoveInfo(enPassantPawn, movePosition, null, promotion, true);

			FilterPieces(fileDisam, rankDisam, color, movePosition, ref possiblePieces);

			Piece? movementPiece;
			try { movementPiece = possiblePieces.Single(); }
			catch { throw new Exception("Two or more pieces can't be disambiguated."); }

			return new MoveInfo(movementPiece, movePosition, null, promotion, false);
		}

		static IEnumerable<Piece> GetPiecesOfType(Type pieceType)
		{
			var hashSetType = typeof(Enumerable);
			var ofTypeMethod = hashSetType.GetMethod("OfType") ?? throw new Exception();
			ofTypeMethod = ofTypeMethod.MakeGenericMethod(pieceType);
			var pieces = (IEnumerable<Piece>)(ofTypeMethod.Invoke(null, new object[] { Chess.Board })
				?? throw new Exception());
			return pieces;
		}

		static Type GetPieceType(ref string moveStr)
		{
			Type pieceType;
			if (pieceNames.Contains(moveStr[0]))
			{
				var pieceStr = moveStr[0];
				var pieceStrToType = () => pieceStr switch
				{
					'N' => typeof(Knight),
					'B' => typeof(Bishop),
					'R' => typeof(Rook),
					'Q' => typeof(Queen),
					'K' => typeof(King),
					_ => throw new Exception(),
				};
				pieceType = pieceStrToType();
				moveStr = moveStr[1..^0];
			}
			else pieceType = typeof(Pawn);
			return pieceType;
		}
	}
}
