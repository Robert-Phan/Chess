using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess
{
	static class Chess
	{
		internal static HashSet<Piece> Board { get; set; } = new();

		static string? errorMessage = null;

		static bool enPassantTurnsCheck = false;
		static Position? _enPassant;
		internal static Position? EnPassant { get { return _enPassant; } set { enPassantTurnsCheck = true; _enPassant = value; } }

		static bool CheckForCheckmate(bool color)
		{
			var checkedKing = Board.OfType<King>().Where(king => king.Color == color).Single();
			foreach (var position in checkedKing.PossiblePositions)
			{
				var movedOutOfCheck = checkedKing.TestMove(position);
				if (movedOutOfCheck)
					return false;
			}

			var checkingPieces = Board.Where(piece => piece.CanCaptureAtPosition(checkedKing.Position));
			foreach (var checkingPiece in checkingPieces)
			{
				foreach (var potentialSavingPiece in Board.Where(piece => piece.Color == color && piece is not King))
				{
					if (potentialSavingPiece.CanCaptureAtPosition(checkingPiece.Position))
						return false;

					if (checkingPiece is StraightLinePiece straightLinePiece)
						if (straightLinePiece.KingCheckLine.Overlaps(potentialSavingPiece.PossiblePositions))
							return false;
				}
			}

			return true;
		}

		internal static void SetBoard()
		{
			foreach (var colorRank in new[] { 0, 7 })
			{
				var color = colorRank == 0;
				Board.UnionWith(Enumerable.Range(0, 8)
					.Select(file => new Pawn { Color = color, Position = new(file, Math.Abs(colorRank - 1)) }));
				Board.UnionWith(new[] { 0, 7 }.Select(sideFile => new Rook { Color = color, Position = new(sideFile, colorRank) }));
				Board.UnionWith(new[] { 1, 6 }.Select(sideFile => new Knight { Color = color, Position = new(sideFile, colorRank) }));
				Board.UnionWith(new[] { 2, 5 }.Select(sideFile => new Bishop { Color = color, Position = new(sideFile, colorRank) }));
				Board.Add(new Queen { Color = color, Position = new(3, colorRank) });
				Board.Add(new King { Color = color, Position = new(4, colorRank) });
			}

			foreach (var piece in Board) piece.SetPossiblePositions();
		}

		internal static void Play()
		{
			SetBoard();
			var color = true;
			while (true)
			{
				Console.SetCursorPosition(0, 0);
				Console.Write(Printer.CreateBoardString());

				if (Board.First(piece => piece.Color == color).CheckIfKingIsInDanger())
				{
					if (CheckForCheckmate(color)) break;
				}

				Console.Write(new string(' ', Console.WindowWidth));
				Console.SetCursorPosition(0, Console.CursorTop);
				Console.WriteLine(errorMessage is not null ? $"Error: {errorMessage}" : null);
				errorMessage = null;

				var colorName = color ? "White" : "Black";
				Console.Write(new string(' ', Console.WindowWidth));
				Console.SetCursorPosition(0, Console.CursorTop);
				Console.Write($"{colorName}'s turn - Enter your move here: ");

				var playersInput = Console.ReadLine() ?? "";
				try
				{
					var moveInfo = Algebraic.ParseMove(playersInput, color);
					//PrintMoveInfo(moveInfo);
					ProcessMove(moveInfo);

					if (enPassantTurnsCheck) enPassantTurnsCheck = !enPassantTurnsCheck;
					else EnPassant = null;
					color = !color;
				}
				catch (Exception e)
				{
					errorMessage = e.Message;
				}
			}

			var winner = color ? "Black" : "White"; var loser = color ? "White" : "Black";
			Console.WriteLine(new string(' ', Console.WindowWidth));
			Console.Write(new string(' ', Console.WindowWidth));
			Console.SetCursorPosition(0, Console.CursorTop);
			Console.WriteLine($"{winner} has checkmated {loser}!\n{winner} wins!");
		}

		static void ProcessMove(MoveInfo moveInfo)
		{
			if (moveInfo.Piece is Pawn pawn)
			{
				if (moveInfo.Promotion is not null)
					typeof(Pawn).GetMethod("Promote")
						?.MakeGenericMethod(new[] { moveInfo.Promotion })
						.Invoke(pawn, null);
				else if (moveInfo.IsEnPassant) pawn.EnPassant();
				else pawn.Move(moveInfo.MovePosition);
			}
			else if (moveInfo.Piece is King king && moveInfo.CastleSide is not null)
				king.Castle(moveInfo.CastleSide ?? throw new Exception());
			else
				moveInfo.Piece.Move(moveInfo.MovePosition);
		}

		static void PrintBoard()
		{
			Console.WriteLine("@@@");
			foreach (var piece in Board)
			{
				Console.WriteLine("---");
				Console.WriteLine($"{piece}: ({piece.Position.File}, {piece.Position.Rank})");
				var posPos = piece.PossiblePositions.OrderBy(piece => piece.File).ThenBy(piece => piece.Rank);
				foreach (var position in posPos) Console.WriteLine(position);
			}
		}

		static void PrintMoveInfo(MoveInfo moveInfo)
		{
			Console.WriteLine("---");
			Console.WriteLine($"{moveInfo.Piece}; {moveInfo.Piece.Position}");
			Console.WriteLine("Move-to Position: {0}", moveInfo.MovePosition);
			Console.WriteLine("Castle: {0}", moveInfo.CastleSide);
			Console.WriteLine("Promotion: {0}", moveInfo.Promotion);
			Console.WriteLine("En Passant: {0}", moveInfo.IsEnPassant);
		}
	}
}
