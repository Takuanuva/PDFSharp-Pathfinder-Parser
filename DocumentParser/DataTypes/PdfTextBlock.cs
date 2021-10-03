using DocumentParser.Utilities.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentParser.DataTypes
{
	/// <summary>
	/// Helper class used for initial detection of text structures.
	/// </summary>
	public class PdfTextBlock :
		PdfTextMultiElement<PdfTextLine>
	{
		#region Sub-types

		/// <summary>
		/// Possible text alignments within the block.
		/// </summary>
		[Flags]
		public enum TextAlignment
		{
			NoAlignment = 0,
			LeftAligned = 1 << 0,
			CenterAligned = 1 << 1,
			RightAligned = 1 << 2,
			Justified = 1 << 3,
			StartingValue = LeftAligned | CenterAligned | RightAligned
		}

		#endregion

		#region Properties

		#region Private storage fields
		
		/// <summary>
		/// Merge mode flag, if true, line addition always succeeds.
		/// </summary>
		private bool MergeMode = false;

		#endregion

		/// <summary>
		/// Lines contained within the block.
		/// </summary>
		public IReadOnlyList<PdfTextLine> Lines
		{
			get { return SubElements; }
		}

		/// <summary>
		/// Text element alignment within the block.
		/// </summary>
		public TextAlignment Alignment { get; private set; } = TextAlignment.StartingValue;

		/// <summary>
		/// Multiline flag, true if block has more than one line.
		/// </summary>
		public bool IsMultiline
		{
			get { return Lines.Count > 1; }
		}
		
		#endregion

		#region Methods

		/// <summary>
		/// Attempts to add line into the block.
		/// </summary>
		/// <param name="line">Line to be added.</param>
		/// <returns>True if successfully added, false otherwise.</returns>
		internal bool AddLine(PdfTextLine line)
		{
			//perform sanity test
			if (Lines.Contains(line))
				throw new Exception("Attempted to add line into a block which already contains it!");

			return AddSubElement(line);
		}
		
		/// <summary>
		/// Tests whether the two blocks can be merged.
		/// </summary>
		/// <param name="first">First block.</param>
		/// <param name="second">Second block.</param>
		/// <returns>True if the two blocks can be merged, false otherwise.</returns>
		private static bool IsMergeViable(
			PdfTextBlock first,
			PdfTextBlock second)
		{
			//int firstID = first.GetHashCode();
			//int secondID = second.GetHashCode();
			//if ((firstID ^ secondID) == (0x02ba3cdf ^ 0x00575713))
			//{
			//	int i = 0;
			//}

			//check if line lists overlap
			if (first.Lines.ToHashSet().Overlaps(second.Lines.ToHashSet()))
				return true;

			//check if blocks don't overlap horizontally
			if (first.BoundingBox.HorizontalRange.DoesIntersect(second.BoundingBox.HorizontalRange) != Range.IntersectData.BodyIntersect)
				return false;

			//check if blocks have matching line spacing and are separated by the same amount
			{
				//declare calculation function
				float calculateLineOffset(PdfTextBlock block)
				{
					//check if block has only one line (or less)
					if (block.Lines.Count <= 1)
						return float.NaN;

					//initialize output
					float output = 0;

					//iterate over line pairs
					for (int i = 1; i < block.Lines.Count; i++)
						//add distance between lines to output
						output += Math.Abs(block.Lines[i - 1].Baseline.Start.Y - block.Lines[i].Baseline.Start.Y);

					return output / (block.Lines.Count - 1);
				}

				//calculate offsets for both lines
				float firstOffset = calculateLineOffset(first);
				float secondOffset = calculateLineOffset(second);

				//check which of the blocks have valid offset values
				if (!float.IsNaN(firstOffset) && !float.IsNaN(secondOffset))
				{
					//both have valid offsets

					//calculate error margin
					float margin = Math.Min(firstOffset, secondOffset) / 5;

					//check if style family sets overlap
					if (first.CharacterStyleFamilies.Overlaps(second.CharacterStyleFamilies))
					{
						//check if both line offsets are close enough
						if (Math.Abs(firstOffset - secondOffset) < margin)
						{
							//check if blocks are separated by the appropriate range
							if (Math.Abs(first.Lines.Last().Baseline.Start.Y - second.Lines.First().Baseline.Start.Y - firstOffset) < margin ||
								Math.Abs(second.Lines.Last().Baseline.Start.Y - first.Lines.First().Baseline.Start.Y - firstOffset) < margin)
								return true;
						}
					}

					//check if a situation occurs where first line of one block and the last line of the other block overlap with the opposing block
					if ((first.BoundingBox.Contains(second.Lines.First().BoundingBox.Center) & second.BoundingBox.Contains(first.Lines.Last().BoundingBox.Center)) != Range.IntersectData.NoIntersect ||
						(first.BoundingBox.Contains(second.Lines.Last().BoundingBox.Center) & second.BoundingBox.Contains(first.Lines.First().BoundingBox.Center)) != Range.IntersectData.NoIntersect)
						return true;

					//check if one of the blocks is justified and contains most of the other within itself
					if ((first.Alignment == TextAlignment.Justified && first.BoundingBox.Contains(second.BoundingBox.Center) == Range.IntersectData.BodyIntersect) ||
						(second.Alignment == TextAlignment.Justified && second.BoundingBox.Contains(first.BoundingBox.Center) == Range.IntersectData.BodyIntersect))
						return true;
				}
				else if (!float.IsNaN(firstOffset))
				{
					//first has a valid offset

					//calculate error margin
					float margin = firstOffset / 5;

					//check if style family sets overlap
					if (first.CharacterStyleFamilies.Overlaps(second.CharacterStyleFamilies))
					{
						//check if blocks are separated by the appropriate range
						if (Math.Abs(first.Lines.Last().Baseline.Start.Y - second.Lines.First().Baseline.Start.Y - firstOffset) < margin ||
						Math.Abs(second.Lines.Last().Baseline.Start.Y - first.Lines.First().Baseline.Start.Y - firstOffset) < margin)
							return true;
					}
				}
				else if (!float.IsNaN(secondOffset))
				{
					//second has a valid offset

					//calculate error margin
					float margin = secondOffset / 5;
					//check if style family sets overlap
					if (first.CharacterStyleFamilies.Overlaps(second.CharacterStyleFamilies))
					{
						//check if blocks are separated by the appropriate range
						if (Math.Abs(first.Lines.Last().Baseline.Start.Y - second.Lines.First().Baseline.Start.Y - secondOffset) < margin ||
						Math.Abs(second.Lines.Last().Baseline.Start.Y - first.Lines.First().Baseline.Start.Y - secondOffset) < margin)
							return true;
					}
				}
			}
			
			//check if one of the blocks is fully contained within the other
			if ((first.BoundingBox.Contains(second.Lines.First().BoundingBox.Center) != Range.IntersectData.NoIntersect && first.BoundingBox.Contains(second.Lines.Last().BoundingBox.Center) != Range.IntersectData.NoIntersect) ||
			(second.BoundingBox.Contains(first.Lines.First().BoundingBox.Center) != Range.IntersectData.NoIntersect && second.BoundingBox.Contains(first.Lines.Last().BoundingBox.Center) != Range.IntersectData.NoIntersect))
			return true;

			return false;
		}

		/// <summary>
		/// Attempts to merge the block's contents into the provided block.
		/// </summary>
		/// <param name="block">Block to be merged into.</param>
		/// <returns>True if block was merged into, false otherwise.</returns>
		internal bool TryMergeInto(PdfTextBlock block)
		{
			//sanity test
			if (block == this)
				throw new Exception("Block to be merged into must not be self!");

			//check if the merge is viable
			if (IsMergeViable(this, block))
			{
				//get set of lines not occuring within the other block
				var lineSet = Lines.ToHashSet();
				lineSet.ExceptWith(block.Lines.ToHashSet());
				
				//set other block into merge mode
				block.MergeMode = true;

				//add remaining lines to the other block
				foreach (var line in lineSet)
					block.AddLine(line);

				//unset other block from merge mode
				block.MergeMode = false;

				return true;
			}
			else
				return false;
		}

		/// <summary>
		/// Verifies whether the provided line can be a part of the block.
		/// </summary>
		/// <param name="subElement">Line to be tested.</param>
		/// <returns>True if valid part of the block, false otherwise.</returns>
		protected override bool IsValidSubElement(PdfTextLine subElement)
		{
			//check if first element
			if (SubElements.Count == 0)
				return true;
			
			//check if line shares no style families with the block
			if (!CharacterStyleFamilies.Overlaps(subElement.CharacterStyleFamilies))
				if (!MergeMode) return false;

			//initialize alignment flag
			TextAlignment lineAlignment = TextAlignment.NoAlignment;

			//calculate error margin
			float margin = subElement.BoundingBox.Height / 3;

			//perform alignment checks if block has any alignment other than justified
			if (Alignment != TextAlignment.Justified)
			{
				//perform left alignment check
				if ((Alignment & TextAlignment.LeftAligned) != TextAlignment.NoAlignment)
				{
					//check alignment
					if (Math.Abs(BoundingBox.LeftX - subElement.BoundingBox.LeftX) < margin)
						lineAlignment |= TextAlignment.LeftAligned;
				}

				//perform center alignment check
				if ((Alignment & TextAlignment.CenterAligned) != TextAlignment.NoAlignment)
				{
					//get bounding box center position
					float center = BoundingBox.Center.X;

					//check alignment
					if (Math.Abs(center - subElement.BoundingBox.Center.X) < margin)
						lineAlignment |= TextAlignment.CenterAligned;
				}

				//perform right alignment check
				if ((Alignment & TextAlignment.RightAligned) != TextAlignment.NoAlignment)
				{
					//check alignment
					if (Math.Abs(BoundingBox.RightX - subElement.BoundingBox.RightX) < margin)
						lineAlignment |= TextAlignment.CenterAligned;
				}

				//check if no alignment checks were passed
				if (lineAlignment == TextAlignment.NoAlignment)
				{
					//set block alignment to justified if merge mode is enabled
					if (MergeMode)
					{
						Alignment = TextAlignment.Justified;
						lineAlignment = TextAlignment.Justified;
					}
					else
						return false;
				}

				//update block alignment
				Alignment &= lineAlignment;
			}
			
			return true;
		}

		/// <summary>
		/// Sorts the line into the block, merging it with another one if needed.
		/// </summary>
		/// <param name="list">List of lines within the block.</param>
		/// <param name="element">Line to be added.</param>
		protected override void SortElementIntoList(List<PdfTextLine> list, PdfTextLine element)
		{
			//get element properties
			float elementTopCoord = element.AscentLine.Start.Y;
			float elementBottomCoord = element.DescentLine.End.Y;
			float elementMiddleCoord = (elementTopCoord + elementBottomCoord) / 2;

			//calculate error margin
			float margin = Math.Abs(elementTopCoord - elementBottomCoord) / 5;
			
			//iterate over list elements
			for (int i = 0; i < list.Count; i++)
			{
				//get line
				PdfTextLine line = list[i];

				//get line properties
				float lineTopCoord = line.AscentLine.Start.Y;
				float lineBottomCoord = line.DescentLine.End.Y;
				float lineMiddleCoord = (lineTopCoord + lineBottomCoord) / 2;

				//check if element is above line
				if (elementBottomCoord + margin >= lineTopCoord)
				{
					//insert element before line
					list.Insert(i, element);

					return;
				}

				//check if element is within line
				if (element.BoundingBox.VerticalRange.DoesIntersect(line.BoundingBox.VerticalRange) == Range.IntersectData.BodyIntersect)
				{
					//merge element into line
					element.MergeInto(line);

					return;
				}
			}

			//append element to the end of the list
			list.Add(element);
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="initialLine">First line of the block.</param>
		internal PdfTextBlock(
			PdfTextLine initialLine) :
			base(initialLine.Page)
		{
			//add line to block
			AddSubElement(initialLine);
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="lines">Line collection.</param>
		internal PdfTextBlock(
			IEnumerable<PdfTextLine> lines) :
			base(lines.First().Page)
		{
			//add lines to block
			MergeMode = true;
			foreach (var line in lines)
				AddLine(line);
			MergeMode = false;
		}

		#endregion
	}
}
