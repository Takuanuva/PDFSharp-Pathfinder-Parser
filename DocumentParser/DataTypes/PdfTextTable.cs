using DocumentParser.Utilities.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentParser.DataTypes
{
	/// <summary>
	/// Represents a table defined within a PDF page consisting of a table header, body, an optional title and an optional footer.
	/// </summary>
	public class PdfTextTable : PdfTextElement
	{
		#region Sub-types
		
		/// <summary>
		/// Table cell.
		/// </summary>
		public class Cell
		{
			#region Properties

			/// <summary>
			/// Parent table.
			/// </summary>
			public PdfTextTable Parent { get; }

			/// <summary>
			/// Row index.
			/// </summary>
			public int RowIndex { get; }

			/// <summary>
			/// Row span.
			/// </summary>
			public int RowSpan { get; }

			/// <summary>
			/// Column index.
			/// </summary>
			public int ColumnIndex { get; }

			/// <summary>
			/// Column span.
			/// </summary>
			public int ColumnSpan { get; }

			/// <summary>
			/// Cell bounding box.
			/// </summary>
			public Rectangle BoundingBox { get; }

			/// <summary>
			/// Cell contents.
			/// </summary>
			public PdfTextBlock Contents { get; }

			#endregion

			#region Constructors

			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="parent">Parent table.</param>
			/// <param name="lines">Line collection containing contents of the cell.</param>
			/// <param name="columnIndex">Column index.</param>
			/// <param name="rowIndex">Row index.</param>
			/// <param name="columnSpan">Column span.</param>
			/// <param name="rowSpan">Row span.</param>
			public Cell(
				PdfTextTable parent,
				IEnumerable<PdfTextLine> lines,
				int columnIndex,
				int rowIndex,
				int columnSpan,
				int rowSpan)
			{
				//store property values
				Parent = parent;
				ColumnIndex = columnIndex;
				RowIndex = rowIndex;
				ColumnSpan = columnSpan;
				RowSpan = rowSpan;

				//generate bounding box
				BoundingBox = new Rectangle(
					new Point(
						Parent.ColumnBounds[ColumnIndex],
						Parent.RowBounds[RowIndex + RowSpan]),
					new Point(
						Parent.ColumnBounds[ColumnIndex + ColumnSpan],
						Parent.RowBounds[RowIndex]));

				//generate cell contents
				Contents = new PdfTextBlock(lines);
			}

			#endregion
		}

		#endregion

		#region Properties

#warning REMOVE!
		public List<(PdfTextBlock.TextAlignment, (float, float, float))> alignments = new List<(PdfTextBlock.TextAlignment, (float, float, float))>();
		public Dictionary<int, List<(float, float)>> segmentSpans = new Dictionary<int, List<(float, float)>>();

		/// <summary>
		/// Overall number of table rows.
		/// </summary>
		public int RowCount { get; }

		/// <summary>
		/// Number of table columns.
		/// </summary>
		public int ColumnCount { get; }

		/// <summary>
		/// Row bounds.
		/// </summary>
		public IReadOnlyList<float> RowBounds { get; }

		/// <summary>
		/// Column bounds.
		/// </summary>
		public IReadOnlyList<float> ColumnBounds { get; }
		
		/// <summary>
		/// True if the title block is not null, false otherwise.
		/// </summary>
		public bool HasTitle
		{
			get { return TitleBlock != null; }
		}

		/// <summary>
		/// List of all cells within the table header and body.
		/// </summary>
		public IReadOnlyList<Cell> Cells { get; }

		/// <summary>
		/// Block containing the table title. Can be null.
		/// </summary>
		public PdfTextBlock TitleBlock { get; } = null;

		/// <summary>
		/// True if the footer block is not null, false otherwise.
		/// </summary>
		public bool HasFooter
		{
			get { return FooterBlock != null; }
		}

		/// <summary>
		/// Block containing the table footer. Can be null.
		/// </summary>
		public PdfTextBlock FooterBlock { get; } = null;

		#endregion

		#region Methods

		/// <summary>
		/// Should never be needed to be called, throws exception otherwise.
		/// </summary>
		protected override void GeneratePropertyValues()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Should never be needed to be called, throws exception otherwise.
		/// </summary>
		internal override void Invalidate()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="titleBlock">Block containing the table title. Can be null.</param>
		/// <param name="headerBlocks">List of blocks containing the table header data.</param>
		/// <param name="bodyBlocks">List of block containing the table body data.</param>
		/// <param name="footerBlock">Block containing the table footer. Can be null.</param>
		internal PdfTextTable(
			PdfTextBlock titleBlock,
			IReadOnlyList<PdfTextBlock> headerBlocks,
			IReadOnlyList<PdfTextBlock> bodyBlocks,
			PdfTextBlock footerBlock) :
			base(headerBlocks.First().Page)
		{
			//store title and footer blocks
			TitleBlock = titleBlock;
			FooterBlock = footerBlock;

			//create line lists
			List<PdfTextLine> headerLines = new List<PdfTextLine>();
			List<PdfTextLine> bodyLines = new List<PdfTextLine>();
			List<PdfTextLine> tableLines = new List<PdfTextLine>();
			foreach (var block in headerBlocks)
				headerLines.AddRange(block.Lines);
			foreach (var block in bodyBlocks)
				bodyLines.AddRange(block.Lines);
			tableLines.AddRange(headerLines);
			tableLines.AddRange(bodyLines);

			//declare column/row span calculation functions
			(int, int) getColumnSpan(float left, float right)
			{
				int leftCol = 0;
				while (
					leftCol < ColumnBounds.Count - 2 &&
					left >= ColumnBounds[leftCol + 1])
					leftCol++;
				int rightCol = leftCol;
				while (
					rightCol < ColumnBounds.Count - 2 &&
					right > ColumnBounds[rightCol + 1])
					rightCol++;
				return (leftCol, rightCol);
			}
			(int, int) getRowSpan(float top, float bottom)
			{
				int topRow = 0;
				while (
					topRow < RowBounds.Count - 2 &&
					top <= RowBounds[topRow + 1])
					topRow++;
				int bottomRow = topRow;
				while (
					bottomRow < RowBounds.Count - 2 &&
					bottom < RowBounds[bottomRow + 1])
					bottomRow++;
				return (topRow, bottomRow);
			}

			//generate table row/column properties
			List<PdfTextBlock.TextAlignment> columnAlignments = new List<PdfTextBlock.TextAlignment>();
			List<(float, float, float)> columnAlignmentBaselines = new List<(float, float, float)>();
			List<(float, float, float)> columnAlignmentMargins = new List<(float, float, float)>();
			{
				//create table bounding box list
				List<Rectangle> tableBoundingBoxes = new List<Rectangle>();
				foreach (var line in tableLines)
				{
					tableBoundingBoxes.Add(line.BoundingBox);
					foreach (var segment in line.Segments)
					{
						tableBoundingBoxes.Add(segment.BoundingBox);
						foreach (var character in segment.Characters)
							tableBoundingBoxes.Add(character.BoundingBox);
					}
				}
				foreach (var block in headerBlocks)
					tableBoundingBoxes.Add(block.BoundingBox);
				foreach (var block in bodyBlocks)
					tableBoundingBoxes.Add(block.BoundingBox);
				
				//generate row bounds
				{
					//sort bounding box list by vertical position
					tableBoundingBoxes.Sort(new Comparison<Rectangle>((Rectangle left, Rectangle right) =>
					{
						return left.Center.Y.CompareTo(right.Center.Y);
					}));

					//initialize row bounds list
					List<(float, float)> rowBounds = new List<(float, float)>();

					//get initial rows
					foreach (var box in tableBoundingBoxes)
					{
						//initialize row index
						int rowIndex = 0;

						//iterate over rows
						for (; rowIndex < rowBounds.Count; rowIndex++)
						{
							//check if bounding box overlaps with the row
							if (box.TopY > rowBounds[rowIndex].Item2 &&
								box.BottomY < rowBounds[rowIndex].Item1)
								break;
						}

						//check if no row was matched
						if (rowIndex >= rowBounds.Count)
						{
							//add new row
							rowBounds.Add((
								box.TopY,
								box.BottomY));
						}
					}

					//sort rows
					rowBounds.Sort(new Comparison<(float, float)>(((float, float) left, (float, float) right) =>
					{
						return right.Item1.CompareTo(left.Item1);
					}));

					//expand rows
					foreach (var box in tableBoundingBoxes)
					{
						//initialize row index
						int rowIndex = 0;

						//iterate over rows
						for (; rowIndex < rowBounds.Count; rowIndex++)
						{
							//check if bounding box does not overlap with the row
							if (box.TopY <= rowBounds[rowIndex].Item2 ||
								box.BottomY >= rowBounds[rowIndex].Item1)
								continue;

							//check if bounding box overlaps with the next row
							if (rowIndex + 1 < rowBounds.Count &&
								box.BottomY < rowBounds[rowIndex + 1].Item1)
								break;

							//adjust row bounds
							rowBounds[rowIndex] = (
								Math.Max(rowBounds[rowIndex].Item1, box.TopY),
								Math.Min(rowBounds[rowIndex].Item2, box.BottomY));

							break;
						}
					}

					//initialize final row bounds list
					List<float> finalRowBounds = new List<float>();

					//check if invalid row count
					if (rowBounds.Count < 2)
						return;

					//add top bound
					finalRowBounds.Add(rowBounds[0].Item1 + Math.Abs((rowBounds[1].Item1 - rowBounds[0].Item2) / 2));

					//add middle bounds
					for (int i = 0; i < rowBounds.Count - 1; i++)
						finalRowBounds.Add((rowBounds[i + 1].Item1 + rowBounds[i].Item2) / 2);

					//add bottom bound
					finalRowBounds.Add(rowBounds[rowBounds.Count - 1].Item2 - Math.Abs((rowBounds[rowBounds.Count - 1].Item1 - rowBounds[rowBounds.Count - 2].Item2) / 2));

					//save final row bounds
					RowBounds = finalRowBounds;

					//save row count
					RowCount = RowBounds.Count - 1;
				}

				//determine column bounds and alignment properties
				{
					//sort bounding boxes by width
					tableBoundingBoxes.Sort(new Comparison<Rectangle>((Rectangle left, Rectangle right) =>
					{
						return left.Width.CompareTo(right.Width);
					}));

					//get minimum bounding box width
					float minimumBoundingBoxWidth = tableBoundingBoxes.First().Width;

					//get horizontal table bounds
					float tableLeftX = float.PositiveInfinity;
					float tableRightX = float.NegativeInfinity;
					foreach (var block in bodyBlocks)
					{
						tableLeftX = Math.Min(tableLeftX, block.BoundingBox.LeftX);
						tableRightX = Math.Max(tableRightX, block.BoundingBox.RightX);
					}
					foreach (var block in headerBlocks)
					{
						tableLeftX = Math.Min(tableLeftX, block.BoundingBox.LeftX);
						tableRightX = Math.Max(tableRightX, block.BoundingBox.RightX);
					}

					//create row-separated continuous segment span lists
					Dictionary<int, List<(float, float)>> rowContinuousSegmentSpans = new Dictionary<int, List<(float, float)>>();
					for (int rowIndex = 0; rowIndex < RowCount; rowIndex++)
						rowContinuousSegmentSpans[rowIndex] = new List<(float, float)>();
					foreach (var line in tableLines)
						foreach (var segment in line.Segments)
							if (!segment.IsWhitespace)
							{
								//check if segment does not fit within a single row
								var rowSpan = getRowSpan(segment.BoundingBox.TopY, segment.BoundingBox.BottomY);
								if (rowSpan.Item1 != rowSpan.Item2)
									continue;

								//get span list
								var spanList = rowContinuousSegmentSpans[rowSpan.Item1];
							
								//initialize left edge buffer
								float leftX = segment.Characters.First().BoundingBox.LeftX;
							
								//iterate over characters
								for (int characterIndex = 0; characterIndex < segment.Characters.Count - 1; characterIndex++)
								{
									//get characters
									var leftChar = segment.Characters[characterIndex];
									var rightChar = segment.Characters[characterIndex + 1];

									//get minimum width
									float minWidth = Math.Min(leftChar.BoundingBox.Width, rightChar.BoundingBox.Width);

									//check if character bounding boxes are not continuous
									if (rightChar.BoundingBox.LeftX - leftChar.BoundingBox.RightX > minWidth / 3)
									{
										//add span to list
										spanList.Add((leftX, leftChar.BoundingBox.RightX));

										//replace left coordinate
										leftX = rightChar.BoundingBox.LeftX;
									}
								}

								//add last section to list
								spanList.Add((leftX, segment.Characters.Last().BoundingBox.RightX));
							}

					//merge close spans
					for (int rowIndex = 0; rowIndex < RowCount; rowIndex++)
					{
						//get span list
						var spanList = rowContinuousSegmentSpans[rowIndex];

						//sort span list
						spanList.Sort(new Comparison<(float, float)>(((float, float) left, (float, float) right) =>
						{
							return left.Item1.CompareTo(right.Item1);
						}));

						//iterate over spans
						for (int spanIndex = 0; spanIndex < spanList.Count - 1; spanIndex++)
						{
							//check if spans overlap sufficiently
							if (spanList[spanIndex + 1].Item1 - spanList[spanIndex].Item2 < minimumBoundingBoxWidth / 5)
							{
								//generate merged span
								spanList[spanIndex] = (spanList[spanIndex].Item1, spanList[spanIndex + 1].Item2);

								//remove span
								spanList.RemoveAt(spanIndex + 1);

								//decrement index
								spanIndex--;
							}
						}
					}
					
					//generate span set
					HashSet<(float, float)> spanSet = new HashSet<(float, float)>();
					foreach (var kv in rowContinuousSegmentSpans)
						foreach (var span in kv.Value)
							spanSet.Add(span);

					//generate overlap lists
					Dictionary<(float, float), List<(float, float)>> spanOverlaps = new Dictionary<(float, float), List<(float, float)>>();
					foreach (var span in spanSet)
					{
						//initialize List
						List<(float, float)> overlapList = new List<(float, float)>();

						//add overlaping spans to list
						foreach (var overlapSpan in spanSet)
							if (span.Item1 < overlapSpan.Item2 &&
								span.Item2 > overlapSpan.Item1)
								overlapList.Add(overlapSpan);

						//add list to dictionary
						spanOverlaps[span] = overlapList;
					}

					//calculate element alignment errors for each span
					Dictionary<(float, float), (PdfTextBlock.TextAlignment, float, float, float, float, float)> potentialColumnAlignmentErrors = new Dictionary<(float, float), (PdfTextBlock.TextAlignment, float, float, float, float, float)>();
					foreach (var span in spanSet)
					{
						//initialize alignment error sums
						float errorSumLeft = 0;
						float errorSumCenter = 0;
						float errorSumRight = 0;

						//calculate alignment error sums
						{
							//get alignment baselines
							float baselineLeft = span.Item1;
							float baselineCenter = (span.Item1 + span.Item2) / 2;
							float baselineRight = span.Item2;

							//get overlaping span set
							var overlapingSpans = spanOverlaps[span];

							//get span width
							float spanWidth = span.Item2 - span.Item1;

							//iterate over spans
							foreach (var overlapingSpan in overlapingSpans)
							{
								//calculate weight
								float weight = (float)Math.Pow(Math.Pow(spanWidth, 2) / (overlapingSpan.Item2 - overlapingSpan.Item1), 2);

								//increment error sums
								errorSumLeft += weight * (float)Math.Pow(Math.Abs(baselineLeft - overlapingSpan.Item1), 2);
								errorSumCenter += weight * (float)Math.Pow(Math.Abs(baselineCenter - ((overlapingSpan.Item1 + overlapingSpan.Item2) / 2)), 2);
								errorSumRight += weight * (float)Math.Pow(Math.Abs(baselineRight - overlapingSpan.Item2), 2);
							}

							//calculate final values
							errorSumLeft /= overlapingSpans.Count;
							errorSumCenter /= overlapingSpans.Count;
							errorSumRight /= overlapingSpans.Count;
						}

						//find minimum and maximum values
						float errorSumMinimum = Math.Min(Math.Min(errorSumLeft, errorSumCenter), errorSumRight);
						float errorSumMaximum = Math.Max(Math.Max(errorSumLeft, errorSumCenter), errorSumRight);

						//determine alignment
						PdfTextBlock.TextAlignment alignment = PdfTextBlock.TextAlignment.NoAlignment;
						if (errorSumLeft == errorSumMinimum) alignment |= PdfTextBlock.TextAlignment.LeftAligned;
						if (errorSumCenter == errorSumMinimum) alignment |= PdfTextBlock.TextAlignment.CenterAligned;
						if (errorSumRight == errorSumMinimum) alignment |= PdfTextBlock.TextAlignment.RightAligned;

						//add to dictionary
						potentialColumnAlignmentErrors[span] = (
							alignment,
							errorSumLeft,
							errorSumCenter,
							errorSumRight,
							errorSumMinimum,
							errorSumMaximum);
					}

					//get best candidate spans for each column
					List<(float, float)> columnCandidates = new List<(float, float)>();
					{
						//create candidate/weight list
						List<((float, float), float)> candidateWeights = new List<((float, float), float)>();
						foreach (var kv in potentialColumnAlignmentErrors)
							candidateWeights.Add((kv.Key, kv.Value.Item5));

						//sort list by weight
						candidateWeights.Sort(new Comparison<((float, float), float)>((((float, float), float) left, ((float, float), float) right) =>
						{
							return left.Item2.CompareTo(right.Item2);
						}));

						//iterate over candidates
						foreach (var candidateWeight in candidateWeights)
						{
							//get candidate span
							var candidate = candidateWeight.Item1;

							//check for collisions with the already accepted candidates
							bool colision = false;
							foreach (var acceptedSpan in columnCandidates)
								if (candidate.Item1 < acceptedSpan.Item2 &&
									candidate.Item2 > acceptedSpan.Item1)
								{
									colision = true;
									break;
								}

							//add to list if no collisions occured
							if (!colision)
								columnCandidates.Add(candidate);
						}
					}

					//sort column candidate list
					columnCandidates.Sort(new Comparison<(float, float)>(((float, float) left, (float, float) right) =>
					{
						return left.Item1.CompareTo(right.Item2);
					}));

					//generate final spans for each column
					Dictionary<(float, float), (float, float)> finalColumnSpans = new Dictionary<(float, float), (float, float)>();
					for (int spanIndex = 0; spanIndex < columnCandidates.Count; spanIndex++)
					{
						//get permited span bounds
						float leftBounds;
						if (spanIndex > 0)
							leftBounds = columnCandidates[spanIndex - 1].Item2;
						else
							leftBounds = float.NegativeInfinity;
						float rightBounds;
						if (spanIndex < columnCandidates.Count - 1)
							rightBounds = columnCandidates[spanIndex + 1].Item1;
						else
							rightBounds = float.PositiveInfinity;

						//initialize column span
						float leftX = float.PositiveInfinity;
						float rightX = float.NegativeInfinity;

						//extend bounds with spans which are within the permited area
						foreach (var span in spanSet)
							if (span.Item1 > leftBounds &&
								span.Item2 < rightBounds)
							{
								leftX = Math.Min(leftX, span.Item1);
								rightX = Math.Max(rightX, span.Item2);
							}

						//add span to dictionary
						finalColumnSpans[columnCandidates[spanIndex]] = (leftX, rightX);
					}
						
					//initialize final column bounds list
					List<float> finalColumnBounds = new List<float>();

					//check if invalid column count
					if (finalColumnSpans.Count < 2)
						return;

					//add left bound
					finalColumnBounds.Add(finalColumnSpans[columnCandidates[0]].Item1 - Math.Abs((finalColumnSpans[columnCandidates[1]].Item1 - finalColumnSpans[columnCandidates[0]].Item2) / 2));

					//add middle bounds
					for (int i = 0; i < columnCandidates.Count - 1; i++)
						finalColumnBounds.Add((finalColumnSpans[columnCandidates[i + 1]].Item1 + finalColumnSpans[columnCandidates[i]].Item2) / 2);

					//add right bound
					finalColumnBounds.Add(finalColumnSpans[columnCandidates[columnCandidates.Count - 1]].Item2 + Math.Abs((finalColumnSpans[columnCandidates[columnCandidates.Count - 1]].Item1 - finalColumnSpans[columnCandidates[columnCandidates.Count - 2]].Item2) / 2));

					//save final column bounds
					ColumnBounds = finalColumnBounds;

					//save column count
					ColumnCount = ColumnBounds.Count - 1;

					//save column alignment properties
					foreach (var span in columnCandidates)
					{
						var spanAlignmentProperties = potentialColumnAlignmentErrors[span];
						columnAlignments.Add(spanAlignmentProperties.Item1);
						columnAlignmentBaselines.Add((
							span.Item1,
							(span.Item1 + span.Item2) / 2,
							span.Item2));
						columnAlignmentMargins.Add((
							minimumBoundingBoxWidth, 
							minimumBoundingBoxWidth, 
							minimumBoundingBoxWidth));
					}







					foreach (var kv in finalColumnSpans)
						alignments.Add((
							potentialColumnAlignmentErrors[kv.Key].Item1,
							(kv.Value.Item1,
							(kv.Value.Item1 + kv.Value.Item2) / 2,
							kv.Value.Item2)));

					alignments.Sort(new Comparison<(PdfTextBlock.TextAlignment, (float, float, float))>(((PdfTextBlock.TextAlignment, (float, float, float)) left, (PdfTextBlock.TextAlignment, (float, float, float)) right) =>
					{
						if (left.Item2.Item1 == right.Item2.Item1)
							return left.Item2.Item3.CompareTo(right.Item2.Item3);
						return left.Item2.Item1.CompareTo(right.Item2.Item1);
					}));
					
					segmentSpans = rowContinuousSegmentSpans;

					int breakpoint = 0;
				}

				//foreach (var columnLines in columnLineLists)
				//{
				//	//initialize bounds buffers
				//	float leftMin = float.PositiveInfinity;
				//	float leftMax = float.NegativeInfinity;
				//	float centerMin = float.PositiveInfinity;
				//	float centerMax = float.NegativeInfinity;
				//	float rightMin = float.PositiveInfinity;
				//	float rightMax = float.NegativeInfinity;

				//	//initialize minimum width buffer
				//	float minWidth = float.PositiveInfinity;

				//	//iterate over lines in column
				//	foreach (var line in columnLines)
				//	{
				//		//adjust bounds
				//		leftMin = Math.Min(leftMin, line.BoundingBox.LeftX);
				//		leftMax = Math.Max(leftMax, line.BoundingBox.LeftX);
				//		centerMin = Math.Min(centerMin, line.BoundingBox.Center.X);
				//		centerMax = Math.Max(centerMax, line.BoundingBox.Center.X);
				//		rightMin = Math.Min(rightMin, line.BoundingBox.RightX);
				//		rightMax = Math.Max(rightMax, line.BoundingBox.RightX);

				//		//adjust minimum width
				//		minWidth = Math.Min(minWidth, line.BoundingBox.Width);
				//	}

				//	//calculate ranges
				//	float leftRange = leftMax - leftMin;
				//	float centerRange = centerMax - centerMin;
				//	float rightRange = rightMax - rightMin;

				//	//determine column alignment
				//	PdfTextBlock.TextAlignment alignment = PdfTextBlock.TextAlignment.NoAlignment;
				//	if (leftRange <= centerRange &&
				//		leftRange <= rightRange)
				//		alignment |= PdfTextBlock.TextAlignment.LeftAligned;
				//	if (centerRange <= leftRange &&
				//		centerRange <= rightRange)
				//		alignment |= PdfTextBlock.TextAlignment.CenterAligned;
				//	if (rightRange <= leftRange &&
				//		rightRange <= centerRange)
				//		alignment |= PdfTextBlock.TextAlignment.RightAligned;

				//	//store column alignment properties
				//	columnAlignments.Add(alignment);
				//	columnAlignmentBaselines.Add((
				//		(leftMax + leftMin) / 2,
				//		(centerMax + centerMin) / 2,
				//		(rightMax + rightMin) / 2));
				//	columnAlignmentMargins.Add((
				//		Math.Max(leftRange * 0.75f, minWidth * 0.15f),
				//		Math.Max(centerRange * 0.75f, minWidth * 0.15f),
				//		Math.Max(rightRange * 0.75f, minWidth * 0.15f)));
				//}

			}

			//determine number of header rows
			int headerRowCount = 0;
			foreach (var line in headerLines)
				while (line.BoundingBox.BottomY < RowBounds[headerRowCount])
					headerRowCount++;

			//initialize column line lists
			List<List<PdfTextLine>> columnLineLists = new List<List<PdfTextLine>>();
			for (int i = 0; i < ColumnBounds.Count - 1; i++)
				columnLineLists.Add(new List<PdfTextLine>());

			//sort lines into columns
			foreach (var line in tableLines)
			{
				//get line's column span
				var span = getColumnSpan(line.BoundingBox.LeftX, line.BoundingBox.RightX);

				//check if line spans multiple columns
				if (span.Item1 != span.Item2)
					continue;

				//add line to column
				columnLineLists[span.Item1].Add(line);
			}
			
			//initialize proto-cell dictionary
			Dictionary<(int, int), HashSet<PdfTextLine>> protoCells = new Dictionary<(int, int), HashSet<PdfTextLine>>();
			for (int x = 0; x < ColumnBounds.Count - 1; x++)
				for (int y = 0; y < RowBounds.Count - 1; y++)
					protoCells[(x, y)] = new HashSet<PdfTextLine>();

			//map lines to proto-cells
			foreach (var line in tableLines)
			{
				//determine column span
				var lineColumnSpan = getColumnSpan(line.BoundingBox.LeftX, line.BoundingBox.RightX);
				int startColumn = lineColumnSpan.Item1;
				int endColumn = lineColumnSpan.Item2;

				//determine row span
				var lineRowSpan = getRowSpan(line.BoundingBox.TopY, line.BoundingBox.BottomY);
				int startRow = lineRowSpan.Item1;
				int endRow = lineRowSpan.Item2;

				//declare multi-cell line addition function
				void addMultiCellLine(
					PdfTextLine val,
					int startX,
					int endX,
					int startY,
					int endY)
				{
					//get line list
					var list = protoCells[(startX, startY)];

					//add line to list
					list.Add(val);

					//expand list into cells within range
					//for (int x = startX; x <= endX; x++)
					//	for (int y = startY; y <= endY; y++)
					//	{
					//		//generate coordinates
					//		var coords = (x, y);
					//
					//		//get target list
					//		var targetList = protoCells[coords];
					//
					//		//check if different lists
					//		if (list != targetList)
					//		{
					//			//merge target into list
					//			list.UnionWith(targetList);
					//
					//			//replace target
					//			protoCells[coords] = list;
					//		}
					//	}
				}

				//check if line fits within a single column
				if (startColumn == endColumn)
				{
					//single-column line

					//add line
					addMultiCellLine(line, startColumn, endColumn, startRow, endRow);
				}
				else
				{
					//multi-column line

					//initialize section start and end index buffers
					int sectionStartColumnIndex = startColumn;
					int sectionEndColumnIndex = startColumn;
					int sectionStartSegmentIndex = 0;
					int sectionEndSegmentIndex = 0;
					int sectionStartCharacterIndex = 0;
					int sectionEndCharacterIndex = 0;

					//declare sub-line addition function
					void addSubLine()
					{
						//generate sub-line
						PdfTextLine subLine = new PdfTextLine(Page);
						{
							//check if line will only contain a single segment
							if (sectionStartSegmentIndex == sectionEndSegmentIndex)
							{
								//single-segment line

								//get line segment
								var segment = line.Segments[sectionStartSegmentIndex];

								//generate sub-segment
								PdfTextSegment subSegment = new PdfTextSegment(Page, segment.CharacterStyle);
								for (int i = sectionStartCharacterIndex; i <= sectionEndCharacterIndex; i++)
									subSegment.AddCharacter(segment.Characters[i]);

								//add sub-segment to sub-line
								subLine.AddSegment(subSegment);
							}
							else
							{
								//multi-segment line

								//generate first sub-segment
								{
									//check if all characters of the first segment should be added
									if (sectionStartCharacterIndex == 0)
									{
										//full segment

										//add whole segment to sub-line
										subLine.AddSegment(line.Segments[sectionStartSegmentIndex]);
									}
									else
									{
										//under-full segment

										//get line segment
										var segment = line.Segments[sectionStartSegmentIndex];

										//generate sub-segment
										PdfTextSegment subSegment = new PdfTextSegment(Page, segment.CharacterStyle);
										for (int i = sectionStartCharacterIndex; i < segment.Characters.Count; i++)
											subSegment.AddCharacter(segment.Characters[i]);

										//add sub-segment to sub-line
										subLine.AddSegment(subSegment);
									}
								}

								//copy middle segments
								for (int i = sectionStartSegmentIndex + 1; i < sectionEndSegmentIndex; i++)
									subLine.AddSegment(line.Segments[i]);

								//generate last sub-segment
								{
									//get line segment
									var segment = line.Segments[sectionEndSegmentIndex];

									//check if all characters of the last segment should be added
									if (sectionEndCharacterIndex == segment.Characters.Count - 1)
									{
										//full segment

										//add whole segment to sub-line
										subLine.AddSegment(segment);
									}
									else
									{
										//under-full segment

										//generate sub-segment
										PdfTextSegment subSegment = new PdfTextSegment(Page, segment.CharacterStyle);
										for (int i = 0; i <= sectionEndCharacterIndex; i++)
											subSegment.AddCharacter(segment.Characters[i]);

										//add sub-segment to sub-line
										subLine.AddSegment(subSegment);
									}
								}
							}
						}

						//add sub-line to cell list
						addMultiCellLine(subLine, sectionStartColumnIndex, sectionEndColumnIndex, startRow, endRow);
					}

					//iterate over line segments/characters
					for (int segmentIndex = 0; segmentIndex < line.Segments.Count; segmentIndex++)
						for (int characterIndex = 0; characterIndex < line.Segments[segmentIndex].Characters.Count; characterIndex++)
						{
							//get character
							var character = line.Segments[segmentIndex].Characters[characterIndex];

							//check if right edge of the character goes past the current column bounds
							if (character.BoundingBox.RightX > ColumnBounds[sectionEndColumnIndex + 1])
							{
								//check if character state permits splitting
								if (character.BoundingBox.LeftX >= ColumnBounds[sectionEndColumnIndex + 1] ||
									character.IsWhitespace)
								{
									//trim whitespace from section start/end
									{
										//trim end
										while (line.Segments[sectionEndSegmentIndex].Characters[sectionEndCharacterIndex].IsWhitespace &&
											(sectionStartSegmentIndex != sectionEndSegmentIndex || sectionStartCharacterIndex != sectionEndCharacterIndex))
										{
											//decrement character index
											sectionEndCharacterIndex--;

											//check for character index underflow
											if (sectionEndCharacterIndex < 0)
											{
												//decrement segment index
												sectionEndSegmentIndex--;

												//reset character index
												sectionEndCharacterIndex = line.Segments[sectionEndSegmentIndex].Characters.Count - 1;
											}
										}

										//trim start
										while (
											line.Segments[sectionStartSegmentIndex].Characters[sectionStartCharacterIndex].IsWhitespace &&
											(sectionStartSegmentIndex != sectionEndSegmentIndex || sectionStartCharacterIndex != sectionEndCharacterIndex))
										{
											//increment character index
											sectionStartCharacterIndex++;

											//check for character index overflow
											if (sectionStartCharacterIndex >= line.Segments[sectionStartSegmentIndex].Characters.Count)
											{
												//reset character index
												sectionStartCharacterIndex = 0;

												//increment segment index
												sectionStartSegmentIndex++;
											}
										}
									}

									//get section bounds
									float sectionLeftX = line.Segments[sectionStartSegmentIndex].Characters[sectionStartCharacterIndex].BoundingBox.LeftX;
									float sectionRightX = line.Segments[sectionEndSegmentIndex].Characters[sectionEndCharacterIndex].BoundingBox.RightX;
									var sectionColumnSpan = getColumnSpan(sectionLeftX, sectionRightX);
									sectionStartColumnIndex = sectionColumnSpan.Item1;
									sectionEndColumnIndex = sectionColumnSpan.Item2;

									//get column span alignment properties
									PdfTextBlock.TextAlignment columnAlignment = PdfTextBlock.TextAlignment.NoAlignment;
									float alignmentBaselineLeft = columnAlignmentBaselines[sectionStartColumnIndex].Item1;
									float alignmentMarginLeft = columnAlignmentMargins[sectionStartColumnIndex].Item1;
									float alignmentBaselineRight = columnAlignmentBaselines[sectionEndColumnIndex].Item3;
									float alignmentMarginRight = columnAlignmentMargins[sectionEndColumnIndex].Item3;
									float alignmentBaselineCenter = 0;
									float alignmentMarginCenter = 0;
									for (int i = sectionStartColumnIndex; i <= sectionEndColumnIndex; i++)
									{
										columnAlignment |= columnAlignments[i];
										alignmentBaselineCenter += columnAlignmentBaselines[i].Item2;
										alignmentMarginCenter += columnAlignmentMargins[i].Item2;
									}
									alignmentBaselineCenter /= 1 + (sectionEndColumnIndex - sectionStartColumnIndex);
									alignmentMarginCenter /= 1 + (sectionEndColumnIndex - sectionStartColumnIndex);

									//check section alignment
									PdfTextBlock.TextAlignment sectionAlignment = PdfTextBlock.TextAlignment.NoAlignment;
									if (Math.Abs(alignmentBaselineLeft - sectionLeftX) <= alignmentMarginLeft)
										sectionAlignment |= PdfTextBlock.TextAlignment.LeftAligned;
									if (Math.Abs(alignmentBaselineCenter - ((sectionLeftX + sectionRightX) / 2)) <= alignmentMarginCenter)
										sectionAlignment |= PdfTextBlock.TextAlignment.CenterAligned;
									if (Math.Abs(alignmentBaselineRight - sectionRightX) <= alignmentMarginRight)
										sectionAlignment |= PdfTextBlock.TextAlignment.RightAligned;

									//check if section alignment in any way matches column alignment
									if ((columnAlignment & sectionAlignment) != PdfTextBlock.TextAlignment.NoAlignment)
									{
										//add sub-line
										addSubLine();

										//update section start bounds
										sectionStartSegmentIndex = segmentIndex;
										sectionStartCharacterIndex = characterIndex;
										var startSpan = getColumnSpan(character.BoundingBox.LeftX, float.PositiveInfinity);
										sectionStartColumnIndex = startSpan.Item1;
									}
								}
							}

							//update section end bounds
							sectionEndSegmentIndex = segmentIndex;
							sectionEndCharacterIndex = characterIndex;
							var endSpan = getColumnSpan(float.NegativeInfinity, character.BoundingBox.RightX);
							sectionEndColumnIndex = endSpan.Item2;
						}

					//check if line end was reached without any sub-lines being generated
					if (sectionStartSegmentIndex == 0 &&
						sectionStartCharacterIndex == 0)
					{
						//add whole line
						addMultiCellLine(line, startColumn, endColumn, startRow, endRow);
					}
					else
					{
						//add last sub-line
						addSubLine();
					}
				}
			}

			//generate cell span map
			Dictionary<(int, int), (int, int, int, int)> cellSpans = new Dictionary<(int, int), (int, int, int, int)>();
			for (int x = 0; x < ColumnBounds.Count - 1; x++)
				for (int y = 0; y < RowBounds.Count - 1; y++)
				{
				//get line set
				var lines = protoCells[(x, y)];

				//check if line set contains any non-whitespace lines
				bool isWhitespace = true;
				foreach (var line in lines)
					if (!line.IsWhitespace)
					{
						isWhitespace = false;
						break;
					}
				if (!isWhitespace)
				{
						//determine column span
						int columnSpan = 1;
						while (
							x + columnSpan < ColumnBounds.Count - 1 &&
							protoCells[(x + columnSpan, y)] == lines)
							columnSpan++;

						//determine row span
						int rowSpan = 1;
						while (
							y + rowSpan < RowBounds.Count - 1 &&
							protoCells[(x, y + rowSpan)] == lines)
							rowSpan++;

						//add cell span data to dictionary
						var spanData = (x, y, columnSpan, rowSpan);
						for (int a = 0; a < columnSpan; a++)
							for (int b = 0; b < rowSpan; b++)
								cellSpans[(x + a, y + b)] = spanData;
					}
			}

			//extend cells to overlap empty spots
#warning TODO!
			{
				//merge cells in header rows
				{
					
				}

				//merge cells in body rows
				{

				}

				//iterate over body rows
				for (int rowIndex = headerRowCount; rowIndex < RowBounds.Count - 1; rowIndex++) 
				{
					//check 

					//check if row has empty 
				}
			}

			//generate table cells
			List<Cell> cells = new List<Cell>();
			for (int y = 0; y < RowBounds.Count - 1; y++)
				for (int x = 0; x < ColumnBounds.Count - 1; x++)
				{
#warning TODO: REMOVE!
					if (!cellSpans.ContainsKey((x, y)))
						continue;

					//get span
					var span = cellSpans[(x, y)];

					//check if coords are for span's upper left corner
					if (span.Item1 == x &&
						span.Item2 == y)
					{
						//get all lines within the cell span
						HashSet<PdfTextLine> lines = new HashSet<PdfTextLine>();
						for (int a = 0; a < span.Item3; a++)
							for (int b = 0; b < span.Item4; b++)
								lines.UnionWith(protoCells[(x + a, y + b)]);

						//create and add cell
						cells.Add(new Cell(
							this,
							lines,
							span.Item1,
							span.Item2,
							span.Item3,
							span.Item4));
					}
				}

			//store cell list
			Cells = cells;

			//generate property values
			{
				//generate bounding box
				{
					List<Point> points = new List<Point>();
					if (HasTitle)
						points.AddRange(TitleBlock.BoundingBox.Points);
					if (HasFooter)
						points.AddRange(footerBlock.BoundingBox.Points);
					points.Add(new Point(
						ColumnBounds.First(),
						RowBounds.First()));
					points.Add(new Point(
						ColumnBounds.Last(),
						RowBounds.Last()));

					_BoundingBox = Rectangle.Containing(points);
				}

				//generate text string
				{
					_TextString = "";
					if (HasTitle)
						_TextString += TitleBlock.TextString;
					foreach (var cell in Cells)
						if (cell.Contents != null)
							_TextString += cell.Contents.TextString;
					if (HasFooter)
						_TextString += FooterBlock.TextString;
				}
				
				//generate baseline
				{
					List<Point> points = new List<Point>();
					if (HasTitle)
						points.AddRange(TitleBlock.Baseline.Points);
					foreach (var cell in Cells)
						if (cell.Contents != null)
							points.AddRange(cell.Contents.Baseline.Points);
					if (HasFooter)
						points.AddRange(FooterBlock.Baseline.Points);
					_Baseline = new Polyline(points);
				}

				//generate ascent line
				{
					List<Point> points = new List<Point>();
					if (HasTitle)
						points.AddRange(TitleBlock.AscentLine.Points);
					foreach (var cell in Cells)
						if (cell.Contents != null)
							points.AddRange(cell.Contents.AscentLine.Points);
					if (HasFooter)
						points.AddRange(FooterBlock.AscentLine.Points);
					_AscentLine = new Polyline(points);
				}

				//generate descent line
				{
					List<Point> points = new List<Point>();
					if (HasTitle)
						points.AddRange(TitleBlock.DescentLine.Points);
					foreach (var cell in Cells)
						if (cell.Contents != null)
							points.AddRange(cell.Contents.DescentLine.Points);
					if (HasFooter)
						points.AddRange(FooterBlock.DescentLine.Points);
					_DescentLine = new Polyline(points);
				}

				//generate character style family set
				{
					_CharacterStyleFamilies = new HashSet<PdfTextCharacter.Style.Family>();
					if (HasTitle)
						_CharacterStyleFamilies.UnionWith(TitleBlock.CharacterStyleFamilies);
					foreach (var cell in Cells)
						if (cell.Contents != null)
							_CharacterStyleFamilies.UnionWith(cell.Contents.CharacterStyleFamilies);
					if (HasFooter)
						_CharacterStyleFamilies.UnionWith(FooterBlock.CharacterStyleFamilies);
				}
			}
		}

		#endregion
	}
}
