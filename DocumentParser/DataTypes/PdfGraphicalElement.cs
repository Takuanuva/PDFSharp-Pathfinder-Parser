using DocumentParser.Utilities.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentParser.DataTypes
{
	/// <summary>
	/// Represents a page element consisting of a type of graphic.
	/// </summary>
	public abstract class PdfGraphicalElement : PdfPageElement
	{
		#region Methods

		/// <summary>
		/// Checks whether the line intersects the graphic's edge.
		/// </summary>
		/// <param name="line">Line to be checked.</param>
		/// <returns>True if the line is intersected, false otherwise.</returns>
		public abstract bool IsLineIntersectingEdge(Line line);

		/// <summary>
		/// Attempts to split the text line into multiple parts based on how the graphic intersects it.
		/// </summary>
		/// <param name="line">Line to be split.</param>
		/// <returns>List containing the resulting line parts (or null if no intersections occur).</returns>
		internal IReadOnlyList<PdfTextLine> CutTextWithGraphics(PdfTextLine line)
		{
			//get bounding box intersection
			Rectangle boundingBoxIntersect = Rectangle.FromIntersect(new Rectangle[] { BoundingBox, line.BoundingBox });

			//check if bounding boxes have no intersect
			if (boundingBoxIntersect == null)
				return null;

			//initialize output list
			List<PdfTextLine> output = new List<PdfTextLine>();

			//declare current line and segment
			PdfTextLine currentLine = null;
			PdfTextSegment currentSegment = null;

			//declare reference point funtion
			Point generateReferencePoint(PdfTextCharacter character)
			{
				return character.BoundingBox.Center;
			}

			//get first character's refrence point
			Point previousReferencePoint = generateReferencePoint(line.Segments[0].Characters[0]);

			//iterate over segments in line
			foreach (PdfTextSegment segment in line.Segments)
			{
				//invalidate current segment
				currentSegment = null;

				//iterate over characters in segment
				foreach (PdfTextCharacter character in segment.Characters)
				{
					//get current reference point
					Point currentReferencePoint = generateReferencePoint(character);

					//generate reference line
					Line referenceLine = new Line(
						previousReferencePoint,
						currentReferencePoint);

					//replace previous reference point
					previousReferencePoint = currentReferencePoint;

					//check if reference line does not intersect the bounding box intersection
					if (boundingBoxIntersect.Intersects(referenceLine) != Range.IntersectData.BodyIntersect)
					{
						//check if line is intersected by graphics
						if (IsLineIntersectingEdge(referenceLine))
						{
							//invalidate current line
							currentLine = null;
						}
					}

					//check if current line is invalid
					if (currentLine == null)
					{
						//create new line
						currentLine = new PdfTextLine(character.Page);

						//add line to output
						output.Add(currentLine);

						//invalidate current segment
						currentSegment = null;
					}

					//check if current segment is invalid
					if (currentSegment == null)
					{
						//create new segment
						currentSegment = new PdfTextSegment(character.Page, character.CharacterStyle);

						//add segment to current line
						currentLine.AddSegment(currentSegment);
					}

					//add character to current segment
					currentSegment.AddCharacter(character);
				}
			}

			//check if intersect resulted in only one line (thus no intersections)
			if (output.Count < 2)
				return null;

			return output;
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="page">Page on which the graphic is located.</param>
		internal PdfGraphicalElement(PdfPage page) : base(page)
		{ }

		#endregion
	}
}
