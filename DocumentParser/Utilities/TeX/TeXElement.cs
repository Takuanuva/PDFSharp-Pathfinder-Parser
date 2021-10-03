using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentParser.Utilities.TeX
{
	/// <summary>
	/// Abstract base class, represents an element which can be converted into a TeX-compliant string.
	/// </summary>
	public abstract class TeXElement
	{
		#region Properties

		#region Private storage fields

		/// <summary>
		/// Private storage field for the RawValue property.
		/// </summary>
		private string _RawValue = null;

		/// <summary>
		/// Private storage field for the TeXValue property.
		/// </summary>
		private string _TeXValue = null;

		/// <summary>
		/// Private storage field for the HasNonwhitespaceCharacters property.
		/// </summary>
		private VolatileField<bool> _HasNonWhitespaceCharacters;

		#endregion
		
		/// <summary>
		/// Raw value of the stored string.
		/// </summary>
		public string RawValue
		{
			get
			{
				//generate value if invalid
				if (_RawValue == null)
					_RawValue = GenerateRawValue();

				return _RawValue;
			}
		}

		/// <summary>
		/// TeX-compliant version of the stored string.
		/// </summary>
		public string TeXValue
		{
			get
			{
				//generate value if invalid
				if (_TeXValue == null)
					_TeXValue = GenerateTeXValue();

				return _TeXValue;
			}
		}

		/// <summary>
		/// True if text contains non-whitespace characters, false otherwise.
		/// </summary>
		public bool HasNonWhitespaceCharacters
		{
			get { return _HasNonWhitespaceCharacters.Value; }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Generates the value of the RawValue property.
		/// </summary>
		/// <returns>Generated value.</returns>
		protected abstract string GenerateRawValue();

		/// <summary>
		/// Generates the value of the TeXValue property.
		/// </summary>
		/// <returns>Generated value.</returns>
		protected abstract string GenerateTeXValue();

		/// <summary>
		/// Generates the value of the HasNonWhitespaceCharacters property.
		/// </summary>
		/// <returns>Generated value.</returns>
		protected abstract bool GenerateHasNonWhitespaceCharacters();

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public TeXElement()
		{
			//initialize volatile fields
			_HasNonWhitespaceCharacters = new VolatileField<bool>(GenerateHasNonWhitespaceCharacters);
		}

		#endregion
	}
}
