using DocumentParser.Utilities;
using DocumentParser.Utilities.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentParser.DataTypes
{
	/// <summary>
	/// Abstract class, represents a page element which expresses text data.
	/// </summary>
	public abstract class PdfTextElement : PdfPageElement
	{
		#region Properties

		#region Private storage fields

		/// <summary>
		/// Private storage field for the TextString property.
		/// </summary>
		protected string _TextString = null;

		/// <summary>
		/// Private storage field for the IsWhitespace property.
		/// </summary>
		protected bool? _IsWhitespace = null;

		/// <summary>
		/// Private storage field for the Baseline property.
		/// </summary>
		protected Polyline _Baseline = null;

		/// <summary>
		/// Private storage field for the AscentLine property.
		/// </summary>
		protected Polyline _AscentLine = null;

		/// <summary>
		/// Private storage field for the DescentLine property.
		/// </summary>
		protected Polyline _DescentLine = null;

		/// <summary>
		/// Private storage field for the CharacterStyleFamilies property.
		/// </summary>
		protected HashSet<PdfTextCharacter.Style.Family> _CharacterStyleFamilies = null;

		#endregion

		/// <summary>
		/// Raw text string.
		/// </summary>
		public string TextString
		{
			get
			{
				//check if null
				if (_TextString == null)
					//generate property values
					GeneratePropertyValues();

				return _TextString;
			}
		}

		/// <summary>
		/// Whitespace flag, true if entirety of the text consists of whitespace characters, false otherwise.
		/// </summary>
		public bool IsWhitespace
		{
			get
			{
				//check if null
				if (!_IsWhitespace.HasValue)
					//generate property values
					GeneratePropertyValues();

				return _IsWhitespace.Value;
			}
		}

		/// <summary>
		/// Text baseline.
		/// </summary>
		public Polyline Baseline
		{
			get
			{
				//check if null
				if (_Baseline == null)
					//generate property values
					GeneratePropertyValues();

				return _Baseline;
			}
		}

		/// <summary>
		/// Text ascent line.
		/// </summary>
		public Polyline AscentLine
		{
			get
			{
				//check if null
				if (_AscentLine == null)
					//generate property values
					GeneratePropertyValues();

				return _AscentLine;
			}
		}

		/// <summary>
		/// Text descent line.
		/// </summary>
		public Polyline DescentLine
		{
			get
			{
				//check if null
				if (_DescentLine == null)
					//generate property values
					GeneratePropertyValues();

				return _DescentLine;
			}
		}

		/// <summary>
		/// A set containing all character style families associated with the element.
		/// </summary>
		public ISet<PdfTextCharacter.Style.Family> CharacterStyleFamilies
		{
			get
			{
				//check if null
				if (_CharacterStyleFamilies == null)
					//generate property values
					GeneratePropertyValues();

				return _CharacterStyleFamilies;
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Property and parent invalidation method.
		/// </summary>
		internal abstract void Invalidate();

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="page">Page containing the element.</param>
		public PdfTextElement(PdfPage page) : base(page)
		{ }

		#endregion
	}
}
