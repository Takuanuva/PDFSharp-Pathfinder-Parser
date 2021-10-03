using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentParser.DataTypes
{
	/// <summary>
	/// Represents a PDF text elements consisting of a sequence of characters which all share one style.
	/// </summary>
	public class PdfTextSegment :
		PdfTextMultiElement<PdfTextCharacter>,
		PdfTextLine.ISubElement
	{
		#region Properties

		/// <summary>
		/// Line containing the segment.
		/// </summary>
		public PdfTextMultiElement<PdfTextSegment> Container { get; set; }

		/// <summary>
		/// Character style associated with the segment elements.
		/// </summary>
		public PdfTextCharacter.Style CharacterStyle { get; }

		/// <summary>
		/// List of all text character objects within the segment.
		/// </summary>
		public IReadOnlyList<PdfTextCharacter> Characters
		{
			get { return SubElements; }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Verifies whether the character can be added to the segment.
		/// </summary>
		/// <param name="subElement">Character to be added.</param>
		/// <returns>True if character can be added, false otherwise.</returns>
		protected override bool IsValidSubElement(PdfTextCharacter subElement)
		{
#warning TODO: EXPAND?
			return subElement.CharacterStyle == CharacterStyle;
		}

		/// <summary>
		/// Attempts to add the provided character to the segment.
		/// </summary>
		/// <param name="character">Character object to be added.</param>
		/// <returns>True if successful, false otherwise.</returns>
		public bool AddCharacter(PdfTextCharacter character)
		{
			return AddSubElement(character);
		}

		/// <summary>
		/// Invalidation method.
		/// </summary>
		internal override void Invalidate()
		{
			base.Invalidate();
			InvalidateContainer();
		}

		/// <summary>
		/// Container invalidation method.
		/// </summary>
		public void InvalidateContainer()
		{
			//check if not null
			if (Container != null) Container.Invalidate();
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="page">Page containing the segment.</param>
		/// <param name="characterStyle">Character style associated with the segment elements</param>
		internal PdfTextSegment(
			PdfPage page,
			PdfTextCharacter.Style characterStyle) :
			base(page)
		{
			//store property values
			CharacterStyle = characterStyle;
		}

		#endregion
	}
}
