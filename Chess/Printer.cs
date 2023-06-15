using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess
{
	class Printer
	{
		internal static string CreateBoardString()
		{
			var boardString = new StringBuilder();
			var rowBorder = "   +---+---+---+---+---+---+---+---+";
			for (var rank = 7; rank >= 0; rank--)
			{
				boardString.AppendLine(rowBorder);
				boardString.Append($" {rank + 1} ");
				for (var file = 0; file <= 7; file++)
				{
					var piece = Chess.Board.SingleOrDefault(piece => piece.Position == new Position(file, rank));
					var pieceString = PieceToString(piece);
					boardString.Append($"| {pieceString} ");
				}
				boardString.AppendLine("|");
			}
			boardString.AppendLine(rowBorder);
			boardString.AppendLine("   " + string.Join(null, "abcdefgh".Select(c => $"  {c} ")));
			return boardString.ToString();
		}

		static string PieceToString(Piece? piece)
		{
			if (piece is null) return " ";

			var pieceStringFunc = () => piece switch
			{
				Pawn => "p",
				King => "k",
				Queen => "q",
				Rook => "r",
				Bishop => "b",
				Knight => "n",
				_ => throw new Exception()
			};

			var pieceString = pieceStringFunc();
			if (piece.Color) return pieceString.ToUpper();
			return pieceString;
		}
	}
}
