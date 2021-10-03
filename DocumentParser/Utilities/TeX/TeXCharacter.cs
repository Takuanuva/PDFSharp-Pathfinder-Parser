using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentParser.Utilities.TeX
{
	/// <summary>
	/// Represents a single character's TeX representation.
	/// </summary>
	public class TeXCharacter : TeXElement
	{
		#region Properties

		/// <summary>
		/// Character value.
		/// </summary>
		private char Character { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Generates the value of the RawValue property.
		/// </summary>
		/// <returns>Generated value.</returns>
		protected override string GenerateRawValue()
		{
			return "" + Character;
		}

		/// <summary>
		/// Generates the value of the TeXValue property.
		/// </summary>
		/// <returns>Generated value.</returns>
		protected override string GenerateTeXValue()
		{
			//check character value
			switch (Character)
			{
				case '&':
				case '%':
				case '$':
				case '#':
				case '_':
				case '{':
				case '}':
					return @"\" + Character;
				case '~':
					return @"\textasciitilde{}";
				case '^':
					return @"\textasciicircum{}";
				case '\\':
					return @"\textbackslash{}";
				default:
					return "" + Character;
			}
		}

		/// <summary>
		/// Generates the value of the HasNonWhitespaceCharacters property.
		/// </summary>
		/// <returns>Generated value.</returns>
		protected override bool GenerateHasNonWhitespaceCharacters()
		{
			return !char.IsWhiteSpace(Character);
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="character">Character value.</param>
		public TeXCharacter(char character)
		{
			//store character value
			Character = character;
		}

		#endregion
	}
}
