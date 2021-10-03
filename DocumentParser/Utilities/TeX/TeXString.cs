using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentParser.Utilities.TeX
{
	/// <summary>
	/// Represents a string of <see cref="TeXElement"/> objects.
	/// </summary>
	public class TeXString : TeXElement
	{
		#region Properties

		/// <summary>
		/// List of elements in the string.
		/// </summary>
		private List<TeXElement> Elements = new List<TeXElement>();

		#endregion

		#region Methods

		/// <summary>
		/// Generates the value of the RawValue property.
		/// </summary>
		/// <returns>Generated value.</returns>
		protected override string GenerateRawValue()
		{
			//generate value
			string output = "";
			foreach (TeXElement element in Elements)
				output += element.RawValue;

			return output;
		}

		/// <summary>
		/// Generates the value of the TeXValue property.
		/// </summary>
		/// <returns>Generated value.</returns>
		protected override string GenerateTeXValue()
		{
			//generate value
			string output = "";
			foreach (TeXElement element in Elements)
				output += element.TeXValue;

			return output;
		}

		/// <summary>
		/// Generates the value of the HasNonWhitespaceCharacters property.
		/// </summary>
		/// <returns>Generated value.</returns>
		protected override bool GenerateHasNonWhitespaceCharacters()
		{
			//check if any stored elements contain non-whitespace characters
			foreach (TeXElement element in Elements)
				if (element.HasNonWhitespaceCharacters)
					return true;

			return false;
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="elements">Element list.</param>
		public TeXString(
			IReadOnlyList<TeXElement> elements)
		{
			//store element list
			foreach (TeXElement element in elements)
				Elements.Add(element);
		}

		#endregion
	}
}
