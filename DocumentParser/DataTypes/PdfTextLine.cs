using DocumentParser.Utilities.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentParser.DataTypes
{
	/// <summary>
	/// Represents a sequence of text segments which may or may not have different style data.
	/// </summary>
	public class PdfTextLine :
		PdfTextMultiElement<PdfTextSegment>,
		PdfTextMultiElement<PdfTextLine>.ISubElement
	{
		#region Properties
		
		/// <summary>
		/// Element containing the line.
		/// </summary>
		public PdfTextMultiElement<PdfTextLine> Container { get; set; }

		/// <summary>
		/// List of all segments within the line.
		/// </summary>
		public IReadOnlyList<PdfTextSegment> Segments
		{
			get { return SubElements; }
		}
		
		#endregion
		
		#region Methods

		/// <summary>
		/// Verifies whether the segment can be added to the line.
		/// </summary>
		/// <param name="subElement">Segment to be added.</param>
		/// <returns>True if segment can be added, false otherwise.</returns>
		protected override bool IsValidSubElement(PdfTextSegment subElement)
		{
#warning TODO: EXPAND?
			return true;
		}
		
		/// <summary>
		/// Attempts to add the provided segment to the line.
		/// </summary>
		/// <param name="segment">Segment object to be added.</param>
		/// <returns>True if successful, false otherwise.</returns>
		internal bool AddSegment(PdfTextSegment segment)
		{
			//perform sanity test
			if (Segments.Contains(segment))
				return true;

			return AddSubElement(segment);
		}

		/// <summary>
		/// Merges the segments of the current instance into the provided line.
		/// </summary>
		/// <param name="line">Line to be merged into.</param>
		internal void MergeInto(PdfTextLine line)
		{
			//add segments to line
			foreach (var segment in Segments)
				line.AddSegment(segment);

			//remove instance from page
			Page.RemoveLine(this);
		}

		/// <summary>
		/// Sorts the segment into the line.
		/// </summary>
		/// <param name="list">List of segments within the line.</param>
		/// <param name="element">Segment to be added.</param>
		protected override void SortElementIntoList(List<PdfTextSegment> list, PdfTextSegment element)
		{
			//get element properties
			Point elementAscentStart = element.AscentLine.Start;
			Point elementDescentEnd = element.DescentLine.End;

			//calculate error margin
			float margin = element.BoundingBox.Height / 5;

			//iterate over list elements
			for (int i = 0; i < list.Count; i++)
			{
				//get segment
				PdfTextSegment segment = list[i];

				//get segment properties
				Point segmentAscentStart = segment.AscentLine.Start;
				Point segmentDescentEnd = segment.DescentLine.End;

				//check if element is before segment
				if (elementDescentEnd.Y + margin >= segmentAscentStart.Y || //line above segment
					(elementAscentStart.Y - margin <= segmentAscentStart.Y && elementDescentEnd.Y + margin >= segmentDescentEnd.Y && elementAscentStart.X < segmentAscentStart.X))//same line before segment
				{
					//insert element before segment
					list.Insert(i, element);

					return;
				}
			}

			//append element to the end of the list
			list.Add(element);
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
		/// <param name="page">Page containing the line.</param>
		internal PdfTextLine(PdfPage page) : base(page)
		{ }

		#endregion
	}
}
