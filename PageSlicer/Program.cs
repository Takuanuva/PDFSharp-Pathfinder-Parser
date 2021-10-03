using DocumentParser.Utilities.Geometry;
using PdfDocumentData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PageSlicer
{
	/// <summary>
	/// Represents a section of the page's content that is separated from the other content in some way.
	/// </summary>
	public class ContentSection
	{
		#region Sub-types

		/// <summary>
		/// Possible types of slices (and their priorities)
		/// </summary>
		[Flags]
		private enum SliceTypes : ulong
		{
			NoSlices = 0,
			PageWithSidebars = (ulong)1 << (63 - 0),
			FillingTableBody = (ulong)1 << (63 - 1),
			ContentColumns = (ulong)1 << (63 - 2),
			HorizontalSeparators = (ulong)1 << (63 - 3),
			InlineTableBody = (ulong)1 << (63 - 4),
			StartingSlice = PageWithSidebars | ContentColumns
		}

		/// <summary>
		/// Possible section content type flags.
		/// </summary>
		[Flags]
		public enum ContentTypeFlags
		{
			NoFlags = 0,
			LeafNode = 1 << 0,
			BranchNode = 1 << 1,
			RootNode = (1 << 2) | BranchNode,
			NoSubsections = LeafNode,
			ContentIsHorizontal = 1 << 3,
			ContentIsVertical = 1 << 4,
			ContentIsTable = 1 << 5,
			ParentIsHorizontal = 1 << 6,
			ParentIsVertical = 1 << 7,
			ParentIsTable = 1 << 8,




			horizontalSidebar = (1 << 3) | BranchNode,
			textBody = (1 << 4) | BranchNode,
			textColumn = (1 << 5) | BranchNode,
			textSection = (1 << 6) | LeafNode,
			tableBody = (1 << 7) | LeafNode,
			titleHeader = (1 << 8) | LeafNode
		}

		/// <summary>
		/// Represents a possible event which can occur while reading text along a line.
		/// </summary>
		public class LinearReadEvent
#warning TODO: MAKE PRIVATE!
		{
			#region Sub-types

			/// <summary>
			/// Possible read event flags.
			/// </summary>
			[Flags]
			public enum EventFlags
			{
				NoEvents = 0,
				AreaEdge = 1 << 0,
				//NonCharacter = 1 << 1,
				Character = 1 << 2,
				NonWhitespaceCharacter = (1 << 3) | Character,
				WhitespaceCharacter = (1 << 4) | Character,
				CharacterOverlap = 1 << 5,
				UnexpectedSpacing = 1 << 6,
				GraphicsCollision = 1 << 7,
				BitmapCollision = (1 << 8) | GraphicsCollision,
				PathCollision = (1 << 9) | GraphicsCollision,
				FontHeightMismatch = 1 << 10,
				FontStyleFamilyMismatch = 1 << 11,
				ReadStart = (1 << 12) | UnexpectedSpacing | AreaEdge,
				ReadEnd = (1 << 13) | UnexpectedSpacing | AreaEdge
			}

			#endregion

			#region Properties

			#region Private storage fields

			/// <summary>
			/// Property generation flag.
			/// </summary>
			private bool ValuesGenerated = false;

			/// <summary>
			/// Private storage field for the PreviousCharacterEvent property.
			/// </summary>
			private LinearReadEvent _PreviousCharacterEvent = null;

			/// <summary>
			/// Private storage field for the PreviousNonCharacterEvent property.
			/// </summary>
			private LinearReadEvent _PreviousNonCharacterEvent = null;

			/// <summary>
			/// Private storage field for the PreviousWhitespaceCharacterEvent property.
			/// </summary>
			private LinearReadEvent _PreviousWhitespaceCharacterEvent = null;

			/// <summary>
			/// Private storage field for the PreviousNonWhitespaceCharacterEvent property.
			/// </summary>
			private LinearReadEvent _PreviousNonWhitespaceCharacterEvent = null;

			/// <summary>
			/// Private storage field for the NextCharacterEvent property.
			/// </summary>
			private LinearReadEvent _NextCharacterEvent = null;

			/// <summary>
			/// Private storage field for the NextNonCharacterEvent property.
			/// </summary>
			private LinearReadEvent _NextNonCharacterEvent = null;

			/// <summary>
			/// Private storage field for the NextWhitespaceCharacterEvent property.
			/// </summary>
			private LinearReadEvent _NextWhitespaceCharacterEvent = null;

			/// <summary>
			/// Private storage field for the NextNonWhitespaceCharacterEvent property.
			/// </summary>
			private LinearReadEvent _NextNonWhitespaceCharacterEvent = null;

			#endregion

			/// <summary>
			/// Event flags.
			/// </summary>
			public EventFlags Flags { get; }

			/// <summary>
			/// Span of the line for which the event is defined.
			/// </summary>
			public Range LineSpan { get; }

			/// <summary>
			/// Previous event in the line.
			/// </summary>
			public LinearReadEvent PreviousEvent { get; }

			/// <summary>
			/// Next event in the line.
			/// </summary>
			public LinearReadEvent NextEvent { get; private set; } = null;

			#region Generated properties

			/// <summary>
			/// Previous character event.
			/// </summary>
			public LinearReadEvent PreviousCharacterEvent
			{
				get
				{
					//generate values if required
					if (!ValuesGenerated)
						GenerateValues();

					return _PreviousCharacterEvent;
				}
			}

			/// <summary>
			/// Previous non-character event.
			/// </summary>
			public LinearReadEvent PreviousNonCharacterEvent
			{
				get
				{
					//generate values if required
					if (!ValuesGenerated)
						GenerateValues();

					return _PreviousNonCharacterEvent;
				}
			}

			/// <summary>
			/// Previous whitespace character event.
			/// </summary>
			public LinearReadEvent PreviousWhitespaceCharacterEvent
			{
				get
				{
					//generate values if required
					if (!ValuesGenerated)
						GenerateValues();

					return _PreviousWhitespaceCharacterEvent;
				}
			}

			/// <summary>
			/// Previous non-whitespace character event.
			/// </summary>
			public LinearReadEvent PreviousNonWhitespaceCharacterEvent
			{
				get
				{
					//generate values if required
					if (!ValuesGenerated)
						GenerateValues();

					return _PreviousNonWhitespaceCharacterEvent;
				}
			}

			/// <summary>
			/// Next character event.
			/// </summary>
			public LinearReadEvent NextCharacterEvent
			{
				get
				{
					//generate values if required
					if (!ValuesGenerated)
						GenerateValues();

					return _NextCharacterEvent;
				}
			}

			/// <summary>
			/// Next non-character event.
			/// </summary>
			public LinearReadEvent NextNonCharacterEvent
			{
				get
				{
					//generate values if required
					if (!ValuesGenerated)
						GenerateValues();

					return _NextNonCharacterEvent;
				}
			}

			/// <summary>
			/// Next whitespace character event.
			/// </summary>
			public LinearReadEvent NextWhitespaceCharacterEvent
			{
				get
				{
					//generate values if required
					if (!ValuesGenerated)
						GenerateValues();

					return _NextWhitespaceCharacterEvent;
				}
			}

			/// <summary>
			/// Next non-whitespace character event.
			/// </summary>
			public LinearReadEvent NextNonWhitespaceCharacterEvent
			{
				get
				{
					//generate values if required
					if (!ValuesGenerated)
						GenerateValues();

					return _NextNonWhitespaceCharacterEvent;
				}
			}

			#endregion

			#endregion

			#region Methods

			/// <summary>
			/// Value generation function.
			/// </summary>
			private void GenerateValues()
			{
#warning BEP

				//declare finder functions
				LinearReadEvent previousFinder(EventFlags targetFlags, bool targetValue = true)
				{
					//string ident = $"PREV : { Convert.ToString((int)targetFlags, 2).PadLeft(16, '0') } : { (targetValue ? 'T' : 'F') }";
					//Console.WriteLine($"[ { ident } ]: Starting seek.");
					LinearReadEvent readEvent = PreviousEvent;
					while (readEvent != null)
					{
						//Console.WriteLine($"[ { ident } ]: Testing event: [ { readEvent.GetHashCode().ToString("X8") } : { Convert.ToString((int)(readEvent.Flags), 2).PadLeft(16, '0') } ]");
						if (((readEvent.Flags & targetFlags) == targetFlags) == targetValue)
							break;
						else
							readEvent = readEvent.PreviousEvent;
					}
					//if (readEvent != null)
					//	Console.WriteLine($"[ { ident } ]: Final event: [ { readEvent.GetHashCode().ToString("X8") } : { Convert.ToString((int)(readEvent.Flags), 2).PadLeft(16, '0') } ]");
					//else
					//	Console.WriteLine($"[ { ident } ]: Final event: [ NULL ]");
					return readEvent;
				}
				LinearReadEvent nextFinder(EventFlags targetFlags, bool targetValue = true)
				{
					//string ident = $"NEXT : { Convert.ToString((int)targetFlags, 2).PadLeft(16, '0') } : { (targetValue ? 'T' : 'F') }";
					//Console.WriteLine($"[ { ident } ]: Starting seek.");
					LinearReadEvent readEvent = NextEvent;
					while (readEvent != null)
					{
						//Console.WriteLine($"[ { ident } ]: Testing event: [ { readEvent.GetHashCode().ToString("X8") } : { Convert.ToString((int)(readEvent.Flags), 2).PadLeft(16, '0') } ]");
						if (((readEvent.Flags & targetFlags) == targetFlags) == targetValue)
							break;
						else
							readEvent = readEvent.NextEvent;
					}
					//if (readEvent != null)
					//	Console.WriteLine($"[ { ident } ]: Final event: [ { readEvent.GetHashCode().ToString("X8") } : { Convert.ToString((int)(readEvent.Flags), 2).PadLeft(16, '0') } ]");
					//else
					//	Console.WriteLine($"[ { ident } ]: Final event: [ NULL ]");
					return readEvent;
				}

				//find previous character event
				_PreviousCharacterEvent = previousFinder(EventFlags.Character);

				//find previous non-character event
				_PreviousNonCharacterEvent = previousFinder(EventFlags.Character, false);

				//find previous whitespace character event
				_PreviousWhitespaceCharacterEvent = previousFinder(EventFlags.WhitespaceCharacter);

				//find previous non-whitespace character event
				_PreviousNonWhitespaceCharacterEvent = previousFinder(EventFlags.NonWhitespaceCharacter);

				//find next character event
				_NextCharacterEvent = nextFinder(EventFlags.Character);

				//find next non-character event
				_NextNonCharacterEvent = nextFinder(EventFlags.Character, false);

				//find next whitespace character event
				_NextWhitespaceCharacterEvent = nextFinder(EventFlags.WhitespaceCharacter);

				//find next non-whitespace character event
				_NextNonWhitespaceCharacterEvent = nextFinder(EventFlags.NonWhitespaceCharacter);

				//toggle value generation flag
				ValuesGenerated = true;
			}

			/// <summary>
			/// Tests whether all of target flags are set.
			/// </summary>
			/// <param name="targetFlags">Flag values.</param>
			/// <returns>True if all are set, false otherwise.</returns>
			public bool FlagTestAll(EventFlags targetFlags)
			{
				return (Flags & targetFlags) == targetFlags;
			}

			/// <summary>
			/// Tests whether any of target flags are set.
			/// </summary>
			/// <param name="targetFlags">Flag values.</param>
			/// <returns>True if any are set, false otherwise.</returns>
			public bool FlagTestAny(EventFlags targetFlags)
			{
				return (Flags & targetFlags) != EventFlags.NoEvents;
			}

			#endregion

			#region Constructors

			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="lineSpan">Span of the line for which the event is defined.</param>
			/// <param name="flags">Event flags.</param>
			/// <param name="previousEvent">Previous event in the line.</param>
			public LinearReadEvent(
				Range lineSpan,
				EventFlags flags,
				LinearReadEvent previousEvent)
			{
				//store property values
				LineSpan = lineSpan;
				Flags = flags;
				PreviousEvent = previousEvent;

				//attach event as next event for previous event
				if (PreviousEvent != null)
					PreviousEvent.NextEvent = this;
			}

			#endregion
		}

		#endregion

		#region Properties

		public ContentSection ParentNode { get; }

		/// <summary>
		/// Area encompassed by the section.
		/// </summary>
		public BoxCoords Area { get; }

		/// <summary>
		/// Type of section content.
		/// </summary>
		public ContentTypeFlags SectionContentType { get; }

		/// <summary>
		/// Content subsections within the section.
		/// </summary>
		public IReadOnlyList<ContentSection> Subsections { get; } = new List<ContentSection>();

		#endregion

		#region Constructors

		public List<Line> slices = new List<Line>();



		public Dictionary<float, List<LinearReadEvent>> AreaCharacterLineReadEventsHorizontal;
		public Dictionary<float, List<LinearReadEvent>> AreaCharacterLineReadEventsVertical;
		public List<Line> failedSlices = new List<Line>();
		public Dictionary<Line, float> failedSliceCorrectness = new Dictionary<Line, float>();
		public Dictionary<Line, float> failedSliceWeights = new Dictionary<Line, float>();
		public IReadOnlyList<float> horizontalSlices = new List<float>();
		public IReadOnlyList<float> verticalSlices = new List<float>();

		private ContentSection(
			ContentSection parentNode,
			BoxCoords area,
			HashSet<TextCharacter> areaCharacters,
			IReadOnlyList<float> areaCharacterLineCoordsHorizontal,
			IReadOnlyList<float> areaCharacterLineCoordsVertical,
			Dictionary<float, HashSet<TextCharacter>> areaCharacterLineSetsHorizontal,
			Dictionary<float, HashSet<TextCharacter>> areaCharacterLineSetsVertical,
			Dictionary<float, List<LinearReadEvent>> areaCharacterLineReadEventsHorizontal,
			Dictionary<float, List<LinearReadEvent>> areaCharacterLineReadEventsVertical,
			SliceTypes sliceTypes = SliceTypes.StartingSlice)
		{
			AreaCharacterLineReadEventsHorizontal = areaCharacterLineReadEventsHorizontal;
			AreaCharacterLineReadEventsVertical = areaCharacterLineReadEventsVertical;

			//Console.WriteLine($"<{ (area.LeftX.GetHashCode() ^ area.RightX.GetHashCode() ^ area.BottomY.GetHashCode() ^ area.TopY.GetHashCode()).ToString("X8") }>");
			//Console.WriteLine($"Area: leftX { area.LeftX }, rightX { area.RightX }, bottomY { area.BottomY }, topY { area.TopY }");
			//Console.Write($"Received X coords:");
			//foreach (var x in areaCharacterLineCoordsVertical)
			//	Console.Write($" [{x}]");
			//Console.WriteLine();
			//Console.Write($"Received Y coords:");
			//foreach (var y in areaCharacterLineCoordsHorizontal)
			//	Console.Write($" [{y}]");
			//Console.WriteLine();

			//store properties
			Area = area;
			ParentNode = parentNode;

			//generate potential slice coord lists
			List<float> potentialSlicesHorizontal = new List<float>();
			List<float> potentialSlicesVertical = new List<float>();
			{
				//generate initial values
				potentialSlicesHorizontal.Add(Area.BottomY);
				potentialSlicesVertical.Add(Area.LeftX);
				for (int index = 0; index < areaCharacterLineCoordsHorizontal.Count - 1; index++)
				{
					//get bottom edge
					float bottom = float.NegativeInfinity;
					foreach (var character in areaCharacterLineSetsHorizontal[areaCharacterLineCoordsHorizontal[index + 1]])
						bottom = Math.Max(bottom, character.BoundingBox.BottomY);

					//get top edge
					float top = float.PositiveInfinity;
					foreach (var character in areaCharacterLineSetsHorizontal[areaCharacterLineCoordsHorizontal[index]])
						top = Math.Min(top, character.BoundingBox.TopY);

					//check if either edge is invalid
					if (float.IsInfinity(bottom) ||
						float.IsInfinity(top))
						continue;

					//add average to list
					potentialSlicesHorizontal.Add((bottom + top) / 2);
				}
				for (int index = 0; index < areaCharacterLineCoordsVertical.Count - 1; index++)
				{
					//get left edge
					float left = float.NegativeInfinity;
					foreach (var character in areaCharacterLineSetsVertical[areaCharacterLineCoordsVertical[index]])
						left = Math.Max(left, character.BoundingBox.RightX);

					//get right edge
					float right = float.PositiveInfinity;
					foreach (var character in areaCharacterLineSetsVertical[areaCharacterLineCoordsVertical[index + 1]])
						right = Math.Min(right, character.BoundingBox.LeftX);

					//check if either edge is invalid
					if (float.IsInfinity(left) ||
						float.IsInfinity(right))
						continue;

					//add average to list
					potentialSlicesVertical.Add((left + right) / 2);
				}
				potentialSlicesHorizontal.Add(Area.TopY);
				potentialSlicesVertical.Add(Area.RightX);

				//add subdivisions
				int horizontalCount = potentialSlicesHorizontal.Count - 1;
				int verticalCount = potentialSlicesVertical.Count - 1;
				for (int index = 0; index < horizontalCount; index++)
					potentialSlicesHorizontal.Add((potentialSlicesHorizontal[index] + potentialSlicesHorizontal[index + 1]) / 2);
				for (int index = 0; index < verticalCount; index++)
					potentialSlicesVertical.Add((potentialSlicesVertical[index] + potentialSlicesVertical[index + 1]) / 2);

				//remove duplicates
				potentialSlicesHorizontal = new List<float>(potentialSlicesHorizontal.Distinct());
				potentialSlicesVertical = new List<float>(potentialSlicesVertical.Distinct());

				//sort lists
				potentialSlicesHorizontal.Sort();
				potentialSlicesVertical.Sort();
			}

			//Console.Write($"Filtered X coords:");
			//foreach (var x in potentialSlicesVertical)
			//	Console.Write($" [{x}]");
			//Console.WriteLine();
			//Console.Write($"Filtered Y coords:");
			//foreach (var y in potentialSlicesHorizontal)
			//	Console.Write($" [{y}]");
			//Console.WriteLine();

			//declare slice generator function
			(IReadOnlyList<float>, IReadOnlyList<float>) generateSlices(
				Func<LinearReadEvent, float, float> correctnessFuncHorizontal,
				Func<LinearReadEvent, float, float> correctnessFuncVertical,
				float minimumAvgCorrectnessHorizontal,
				float minimumAvgCorrectnessVertical,
				Func<IReadOnlyList<LinearReadEvent>, float, float> weightFuncHorizontal,
				Func<IReadOnlyList<LinearReadEvent>, float, float> weightFuncVertical)
			{
				//initialize output lists
				List<float> coordsHorizontal = new List<float>();
				List<float> coordsVertical = new List<float>();

				//declare slice value generation function
				(float, float) getSliceValues(
					float sliceLineCoord,
					Func<LinearReadEvent, float, float> correctnessFunc,
					Func<IReadOnlyList<LinearReadEvent>, float, float> weightFunc,
					IReadOnlyList<float> perpendicuarCoords,
					Dictionary<float, List<LinearReadEvent>> perpendicularReadEventLists)
				{
					//find overlapping events for each perpendicular coordinate
					List<LinearReadEvent> overlappingEvents = new List<LinearReadEvent>();
					foreach (float coord in perpendicuarCoords)
					{
						foreach (var readEvent in perpendicularReadEventLists[coord])
							if (readEvent.LineSpan.DoesIntersect(sliceLineCoord) != Range.IntersectData.NoIntersect)
							{
								overlappingEvents.Add(readEvent);
								break;
							}
					}

					//calculate correctness sum
					float correctnessSum = 0;
					foreach (var readEvent in overlappingEvents)
						correctnessSum += correctnessFunc(readEvent, sliceLineCoord);

					//calculate weight
					float weight = weightFunc(overlappingEvents, sliceLineCoord);

#warning REMOVE!
					//Console.WriteLine($"Line: [{ sliceLine.Start.X }:{ sliceLine.Start.Y }][{ sliceLine.End.X }:{ sliceLine.End.Y }] Strength: [{ correctnessSum / perpendicuarCoords.Count }] Sum: [{ correctnessSum }]");

					//failedSlices.Add(sliceLine);
					//failedSliceWeights[sliceLine] = weight;
					//failedSliceCorrectness[sliceLine] = correctnessSum / perpendicuarCoords.Count;

					return (correctnessSum / perpendicuarCoords.Count, weight);
				}

				//generate horizontal slices
				if (correctnessFuncHorizontal != null &&
					weightFuncHorizontal != null)
				{
					//initialize slice dictionary
					Dictionary<float, (float, float)> sliceValuesDictionary = new Dictionary<float, (float, float)>();

					//generate slice values
					foreach (float coord in potentialSlicesHorizontal)
						sliceValuesDictionary[coord] =
							getSliceValues(
								coord,
								correctnessFuncHorizontal,
								weightFuncHorizontal,
								areaCharacterLineCoordsVertical,
								areaCharacterLineReadEventsVertical);

					//generate acceptable slice correctness set
					HashSet<float> acceptedCorrectnessSet = new HashSet<float>();
					foreach (float coord in potentialSlicesHorizontal)
						if (sliceValuesDictionary[coord].Item1 >= minimumAvgCorrectnessHorizontal)
							acceptedCorrectnessSet.Add(coord);

					//generate local maximum weight set
					HashSet<float> localMaxWeightSet = new HashSet<float>();
					for (int coordIndex = 1; coordIndex < potentialSlicesHorizontal.Count - 1; coordIndex++)
					{
						float bottomCoord = potentialSlicesHorizontal[coordIndex - 1];
						float centerCoord = potentialSlicesHorizontal[coordIndex];
						float topCoord = potentialSlicesHorizontal[coordIndex + 1];
						if (!float.IsNegativeInfinity(sliceValuesDictionary[centerCoord].Item2) &&
							(!acceptedCorrectnessSet.Contains(bottomCoord) || sliceValuesDictionary[centerCoord].Item2 >= sliceValuesDictionary[bottomCoord].Item2) &&
							(!acceptedCorrectnessSet.Contains(topCoord) || sliceValuesDictionary[centerCoord].Item2 >= sliceValuesDictionary[topCoord].Item2))
							localMaxWeightSet.Add(potentialSlicesHorizontal[coordIndex]);
					}

					//Console.Write($"Y slice local maximums:");
					//foreach (var y in localMaxWeightSet)
					//	Console.Write($" [{y}]");
					//Console.WriteLine();
					//Console.Write($"Y slices with accepted correctness:");
					//foreach (var y in acceptedCorrectnessSet)
					//	Console.Write($" [{y}]");
					//Console.WriteLine();

					//generate and sort final coordinate list
					coordsHorizontal = new List<float>(localMaxWeightSet.Intersect(acceptedCorrectnessSet));
					coordsHorizontal.Sort((float left, float right) => { return right.CompareTo(left); });
				}

				//generate vertical slices
				if (correctnessFuncVertical != null &&
					weightFuncVertical != null)
				{
					//initialize slice dictionary
					Dictionary<float, (float, float)> sliceValuesDictionary = new Dictionary<float, (float, float)>();

					//generate slice values
					foreach (float coord in potentialSlicesVertical)
						sliceValuesDictionary[coord] =
							getSliceValues(
								coord,
								correctnessFuncVertical,
								weightFuncVertical,
								areaCharacterLineCoordsHorizontal,
								areaCharacterLineReadEventsHorizontal);

					//generate acceptable slice correctness set
					HashSet<float> acceptedCorrectnessSet = new HashSet<float>();
					foreach (float coord in potentialSlicesVertical)
						if (sliceValuesDictionary[coord].Item1 >= minimumAvgCorrectnessVertical)
							acceptedCorrectnessSet.Add(coord);

					//generate local maximum weight set
					HashSet<float> localMaxWeightSet = new HashSet<float>();
					for (int coordIndex = 1; coordIndex < potentialSlicesVertical.Count - 1; coordIndex++)
					{
						float leftCoord = potentialSlicesVertical[coordIndex - 1];
						float centerCoord = potentialSlicesVertical[coordIndex];
						float rightCoord = potentialSlicesVertical[coordIndex + 1];
						if (!float.IsNegativeInfinity(sliceValuesDictionary[centerCoord].Item2) &&
							(!acceptedCorrectnessSet.Contains(leftCoord) || sliceValuesDictionary[centerCoord].Item2 >= sliceValuesDictionary[leftCoord].Item2) &&
							(!acceptedCorrectnessSet.Contains(rightCoord) || sliceValuesDictionary[centerCoord].Item2 >= sliceValuesDictionary[rightCoord].Item2))
							localMaxWeightSet.Add(potentialSlicesVertical[coordIndex]);
					}

					//Console.Write($"X slice local maximums:");
					//foreach (var x in localMaxWeightSet)
					//	Console.Write($" [{x}]");
					//Console.WriteLine();
					//Console.Write($"X slices with accepted correctness:");
					//foreach (var x in acceptedCorrectnessSet)
					//	Console.Write($" [{x}]");
					//Console.WriteLine();

					//generate and sort final coordinate list
					coordsVertical = new List<float>(localMaxWeightSet.Intersect(acceptedCorrectnessSet));
					coordsVertical.Sort();
				}

				return (coordsHorizontal, coordsVertical);
			}

			//generate area slices
			Dictionary<SliceTypes, (IReadOnlyList<float>, IReadOnlyList<float>)> areaSlices = new Dictionary<SliceTypes, (IReadOnlyList<float>, IReadOnlyList<float>)>();
			{
#warning CONTENT GENERATION!

				//horizontal content sidebars at page top/bottom
				if (areaSlices.Count == 0 &&
					(sliceTypes & SliceTypes.PageWithSidebars) != 0)
				{
					//Console.WriteLine($"<{ SliceTypes.PageWithSidebars }>");
					var slices = generateSlices(
						(LinearReadEvent readEvent, float sliceLineCoord) =>
						{
							if (readEvent.FlagTestAll(LinearReadEvent.EventFlags.Character))
								return -10000;
								//if (readEvent.FlagTestAll(LinearReadEvent.EventFlags.AreaEdge))
								//{
								//	//if (!readEvent.FlagTestAll(LinearReadEvent.EventFlags.ReadStart | LinearReadEvent.EventFlags.ReadEnd))
								//	//	return 0.9f;
								//	if (readEvent.LineSection.EdgeLength > area.Height * 0.4 &&
								//		readEvent.FlagTestAll(LinearReadEvent.EventFlags.BitmapCollision))
								//		return 0;
								//	if (readEvent.FlagTestAll(LinearReadEvent.EventFlags.PathCollision))
								//		return 1;
								//	if (readEvent.FlagTestAll(LinearReadEvent.EventFlags.BitmapCollision))
								//		return 0.5f;
								//}
								if (readEvent.FlagTestAny(LinearReadEvent.EventFlags.FontHeightMismatch | LinearReadEvent.EventFlags.AreaEdge) ||
								readEvent.FlagTestAll(LinearReadEvent.EventFlags.UnexpectedSpacing | LinearReadEvent.EventFlags.FontStyleFamilyMismatch))
								return 1;
							return 0;
						},
						null,
						0.99f,
						0,
						(IReadOnlyList<LinearReadEvent> readEvents, float sliceLineCoord) =>
						{
							float weightSum = 0;
							foreach (var readEvent in readEvents)
							{
								float bottomDist = sliceLineCoord - readEvent.LineSpan.Lower;
								float topDist = readEvent.LineSpan.Upper - sliceLineCoord;
								float len = readEvent.LineSpan.Width;
								weightSum += (bottomDist * topDist) / (len * len * len);
							}
							return weightSum / readEvents.Count;
						},
						null);

					if (slices.Item1.Count > 0)
						areaSlices[SliceTypes.PageWithSidebars] = slices;
					//Console.WriteLine($"</{ SliceTypes.PageWithSidebars }>");
				}

				float tableCorrectnessHorizontal(LinearReadEvent readEvent, float sliceLineCoord)
				{
					if (readEvent.FlagTestAll(LinearReadEvent.EventFlags.Character))
						return 0;
					if (readEvent.FlagTestAll(LinearReadEvent.EventFlags.PathCollision))
						return 1;
					return 0;
				}
				float tableCorrectnessVertical(LinearReadEvent readEvent, float sliceLineCoord)
				{
					if (readEvent.FlagTestAny(LinearReadEvent.EventFlags.Character | LinearReadEvent.EventFlags.AreaEdge))
						return 0;
					//if ((readEvent.Flags & LinearReadEvent.EventFlags.PageEdge) != 0)
					//	return 0;
					if (readEvent.FlagTestAll(LinearReadEvent.EventFlags.UnexpectedSpacing))
						return 1;
					return 0;
				}
				float tableWeightHorizontal(IReadOnlyList<LinearReadEvent> readEvents, float sliceLineCoord)
				{
					float weightSum = 0;
					foreach (var readEvent in readEvents)
					{
						float bottomDist = sliceLineCoord - readEvent.LineSpan.Lower;
						float topDist = readEvent.LineSpan.Upper - sliceLineCoord;
						float len = readEvent.LineSpan.Width;
						weightSum += (bottomDist * topDist) / (len * len * len);
					}
					return weightSum / readEvents.Count;
				}
				float tableWeightVertical(IReadOnlyList<LinearReadEvent> readEvents, float sliceLineCoord)
				{
					//get spans
					List<(float, float)> spansNonCharacter = new List<(float, float)>();
					List<(float, float)> spansCharacter = new List<(float, float)>();
					foreach (var readEvent in readEvents)
					{
						//get end coordinates
						float left;
						float right;
						{
							if (readEvent.FlagTestAny(LinearReadEvent.EventFlags.Character))
							{
								//get left
								if (readEvent.PreviousNonCharacterEvent != null)
									left = readEvent.PreviousNonCharacterEvent.LineSpan.Upper;
								else
									left = readEvent.LineSpan.Lower;

								//get right
								if (readEvent.NextNonCharacterEvent != null)
									right = readEvent.NextNonCharacterEvent.LineSpan.Lower;
								else
									right = readEvent.LineSpan.Upper;
							}
							else
							{
								//get left
								if (readEvent.PreviousCharacterEvent != null)
									left = readEvent.PreviousCharacterEvent.LineSpan.Upper;
								else
									left = readEvent.LineSpan.Lower;

								//get right
								if (readEvent.NextCharacterEvent != null)
									right = readEvent.NextCharacterEvent.LineSpan.Lower;
								else
									right = readEvent.LineSpan.Upper;
							}
						}

						//create span
						var span = (left, right);

						//add span to appropriate list
						if (readEvent.FlagTestAny(LinearReadEvent.EventFlags.Character))
							spansCharacter.Add(span);
						else
							spansNonCharacter.Add(span);
					}

					//check if invalid line
					if (spansNonCharacter.Count == 0)
						return float.NegativeInfinity;

					//check if only whitespace spans are present
					if (spansCharacter.Count == 0)
					{
						//get left and right edges
						float left = float.NegativeInfinity;
						float right = float.PositiveInfinity;
						foreach (var span in spansNonCharacter)
						{
							left = Math.Max(left, span.Item1);
							right = Math.Min(right, span.Item2);
						}

						return 50000 - Math.Abs(((left + right) / 2) - sliceLineCoord);
					}

					//determine alignment direction
					bool directionPositive;
					{
						//get narrowest spans of each type
						float narrowestWidthCharacter = float.PositiveInfinity;
						float narrowestWidthNonCharacter = float.PositiveInfinity;
						(float, float) narrowestSpanCharacter = (0, 0);
						(float, float) narrowestSpanNonCharacter = (0, 0);
						foreach (var span in spansCharacter)
						{
							float width = span.Item2 - span.Item1;
							if (width < narrowestWidthCharacter)
							{
								narrowestSpanCharacter = span;
								narrowestWidthCharacter = width;
							}
						}
						foreach (var span in spansNonCharacter)
						{
							float width = span.Item2 - span.Item1;
							if (width < narrowestWidthNonCharacter)
							{
								narrowestSpanNonCharacter = span;
								narrowestWidthNonCharacter = width;
							}
						}

						//calculate direction
						directionPositive =
							((narrowestSpanNonCharacter.Item1 + narrowestSpanNonCharacter.Item2) -
							(narrowestSpanCharacter.Item1 + narrowestSpanCharacter.Item2))
							> 0;

					}

					//calculate weight sum
					float weightSum = 0;
					if (!directionPositive)
					{
						foreach (var span in spansCharacter)
							weightSum += (sliceLineCoord - span.Item2);// / (span.Item2 - span.Item1);
						foreach (var span in spansNonCharacter)
							weightSum += (span.Item2 - sliceLineCoord);// / (span.Item2 - span.Item1);
					}
					else
					{
						foreach (var span in spansCharacter)
							weightSum += (span.Item1 - sliceLineCoord);// / (span.Item2 - span.Item1);
						foreach (var span in spansNonCharacter)
							weightSum += (sliceLineCoord - span.Item1);// / (span.Item2 - span.Item1);
					}

					return weightSum / readEvents.Count;
				}

				//table body filling the entire content area
				if (areaSlices.Count == 0 &&
					(sliceTypes & SliceTypes.FillingTableBody) != 0)
				{
					//Console.WriteLine($"<{ SliceTypes.FillingTableBody }>");
					var slices = generateSlices(
					tableCorrectnessHorizontal,
					tableCorrectnessVertical,
					0.9f,
					0.65f,
					tableWeightHorizontal,
					tableWeightVertical);

					if (slices.Item1.Count > 3 &&
						slices.Item2.Count > 0)
						areaSlices[SliceTypes.FillingTableBody] = slices;
					//Console.WriteLine($"</{ SliceTypes.FillingTableBody }>");
				}

				//main content columns
				if (areaSlices.Count == 0 &&
					(sliceTypes & SliceTypes.ContentColumns) != 0)
				{
					//Console.WriteLine($"<{ SliceTypes.ContentColumns }>");
					var slices = generateSlices(
						null,
						(LinearReadEvent readEvent, float sliceLineCoord) =>
						{
							if (readEvent.FlagTestAll(LinearReadEvent.EventFlags.Character))
								return -10000;
							if (readEvent.FlagTestAll(LinearReadEvent.EventFlags.UnexpectedSpacing))
								return 1;
							return 0;
						},
						0,
						0.99f,
						null,
						(IReadOnlyList<LinearReadEvent> readEvents, float sliceLineCoord) =>
						{
							float weightSum = 0;
							foreach (var readEvent in readEvents)
							{
								float leftDist = sliceLineCoord - readEvent.LineSpan.Lower;
								float rightDist = readEvent.LineSpan.Upper - sliceLineCoord;
								weightSum += (leftDist * leftDist) + (rightDist * rightDist);
							}
							return weightSum / readEvents.Count;
						});

					if (slices.Item2.Count > 0)
						areaSlices[SliceTypes.ContentColumns] = slices;
					//Console.WriteLine($"</{ SliceTypes.ContentColumns }>");
				}

				//section separators within text
				if (areaSlices.Count == 0 &&
					(sliceTypes & SliceTypes.HorizontalSeparators) != 0)
				{
					//Console.WriteLine($"<{ SliceTypes.HorizontalSeparators }>");
					var slices = generateSlices(
						(LinearReadEvent readEvent, float sliceLineCoord) =>
						{
							if (readEvent.FlagTestAll(LinearReadEvent.EventFlags.Character))
								return -10000;
							if (readEvent.FlagTestAny(LinearReadEvent.EventFlags.UnexpectedSpacing | LinearReadEvent.EventFlags.FontHeightMismatch))
								return 1;
							return 0;
						},
						null,
						0.99f,
						0,
						(IReadOnlyList<LinearReadEvent> readEvents, float sliceLineCoord) =>
						{
							float weightSum = 0;
							foreach (var readEvent in readEvents)
							{
								float bottomDist = sliceLineCoord - readEvent.LineSpan.Lower;
								float topDist = readEvent.LineSpan.Upper - sliceLineCoord;
								float len = readEvent.LineSpan.Width;
								weightSum += (bottomDist * topDist) / (len * len * len);
							}
							return weightSum / readEvents.Count;
						},
						null);

					if (slices.Item1.Count > 0)
						areaSlices[SliceTypes.HorizontalSeparators] = slices;
					//Console.WriteLine($"</{ SliceTypes.HorizontalSeparators }>");
				}

				//inline table body
				if (areaSlices.Count == 0 &&
					(sliceTypes & SliceTypes.InlineTableBody) != 0)
				{
					//Console.WriteLine($"<{ SliceTypes.InlineTableBody }>");
					var slices = generateSlices(
					tableCorrectnessHorizontal,
					tableCorrectnessVertical,
					0.9f,
					0.65f,
					tableWeightHorizontal,
					tableWeightVertical);

					if (slices.Item1.Count > 2 &&
						slices.Item2.Count > 0)
						areaSlices[SliceTypes.InlineTableBody] = slices;
					//Console.WriteLine($"</{ SliceTypes.InlineTableBody }>");
				}
			}

			//foreach (var kv in areaSlices)
			//{
			//	Console.WriteLine($"Area slice [{ kv.Key }]:");
			//	Console.Write($"X coords:");
			//	foreach (var x in kv.Value.Item2)
			//		Console.Write($" [{x}]");
			//	Console.WriteLine();
			//	Console.Write($"Y coords:");
			//	foreach (var y in kv.Value.Item1)
			//		Console.Write($" [{y}]");
			//	Console.WriteLine();
			//}

			//find slices with highest priority
			SliceTypes bestSliceType = SliceTypes.NoSlices;
			(IReadOnlyList<float>, IReadOnlyList<float>) bestSlices = (new List<float>(), new List<float>());
			foreach (var kv in areaSlices)
				if (kv.Key > bestSliceType)
				{
					bestSliceType = kv.Key;
					bestSlices = kv.Value;
				}

			//Console.WriteLine($"Best slice: [{ bestSliceType }]");

			//generate content flags
			{
				//leaf/branch/root node
				if (ParentNode == null)
					SectionContentType |= ContentTypeFlags.RootNode;
				else if (
					bestSlices.Item1.Count > 0 ||
					bestSlices.Item2.Count > 0)
					SectionContentType |= ContentTypeFlags.BranchNode;
				else
					SectionContentType |= ContentTypeFlags.LeafNode;

				//content direction (horizontal/vertical/table)
				if ((SectionContentType & ContentTypeFlags.LeafNode) == ContentTypeFlags.NoFlags)
				{
					if (bestSlices.Item1.Count > 0 ^ bestSlices.Item2.Count > 0)
					{
						if (bestSlices.Item1.Count > 0)
							SectionContentType |= ContentTypeFlags.ContentIsVertical;
						else
							SectionContentType |= ContentTypeFlags.ContentIsHorizontal;
					}
					else
						SectionContentType |= ContentTypeFlags.ContentIsTable;
				}

				//parent content direction
				if ((SectionContentType & ContentTypeFlags.RootNode) == ContentTypeFlags.NoFlags)
					switch (ParentNode.SectionContentType & (ContentTypeFlags.ContentIsHorizontal | ContentTypeFlags.ContentIsVertical | ContentTypeFlags.ContentIsTable))
					{
						case ContentTypeFlags.ContentIsHorizontal:
							SectionContentType |= ContentTypeFlags.ParentIsHorizontal;
							break;
						case ContentTypeFlags.ContentIsVertical:
							SectionContentType |= ContentTypeFlags.ParentIsVertical;
							break;
						case ContentTypeFlags.ContentIsTable:
							SectionContentType |= ContentTypeFlags.ParentIsTable;
							break;
						default:
							Console.WriteLine($"CONTENT IS BORQUE!");
							throw new Exception();
							break;
					}
			}

			horizontalSlices = bestSlices.Item1;
			verticalSlices = bestSlices.Item2;


			//process sub-sections if any valid subsections exist
			if ((SectionContentType & ContentTypeFlags.LeafNode) == ContentTypeFlags.NoFlags)
			{
				foreach (float coord in bestSlices.Item1)
					slices.Add(
						new Line(
							new Point(
								Area.LeftX,
								coord),
							new Point(
								Area.RightX,
								coord)));
				foreach (float coord in bestSlices.Item2)
					slices.Add(
						new Line(
							new Point(
								coord,
								Area.BottomY),
							new Point(
								coord,
								Area.TopY)));


				//initialize sub-section list
				List<ContentSection> subsections = new List<ContentSection>();

				//generate horizontal coord list
				List<float> slicedAreaCoordsHorizontal = new List<float>();
				slicedAreaCoordsHorizontal.Add(Area.BottomY);
				slicedAreaCoordsHorizontal.AddRange(bestSlices.Item1);
				slicedAreaCoordsHorizontal.Add(Area.TopY);
				slicedAreaCoordsHorizontal.Sort();

				//generate vertical coord list
				List<float> slicedAreaCoordsVertical = new List<float>();
				slicedAreaCoordsVertical.Add(Area.LeftX);
				slicedAreaCoordsVertical.AddRange(bestSlices.Item2);
				slicedAreaCoordsVertical.Add(Area.RightX);
				slicedAreaCoordsHorizontal.Sort();

				//generate sub-area character line coordinate dictionaries
				Dictionary<int, List<float>> subAreaCharacterLineCoordsHorizontal = new Dictionary<int, List<float>>();
				{
					int coordIndex = 0;
					for (int rowIndex = 0; rowIndex < slicedAreaCoordsHorizontal.Count - 1; rowIndex++)
					{
						float cutoff = slicedAreaCoordsHorizontal[rowIndex + 1];
						List<float> coordList = new List<float>();
						for (; coordIndex < areaCharacterLineCoordsHorizontal.Count && areaCharacterLineCoordsHorizontal[coordIndex] < cutoff; coordIndex++)
							coordList.Add(areaCharacterLineCoordsHorizontal[coordIndex]);
						subAreaCharacterLineCoordsHorizontal[rowIndex] = coordList;
					}
				}
				Dictionary<int, List<float>> subAreaCharacterLineCoordsVertical = new Dictionary<int, List<float>>();
				{
					int coordIndex = 0;
					for (int columnIndex = 0; columnIndex < slicedAreaCoordsVertical.Count - 1; columnIndex++)
					{
						float cutoff = slicedAreaCoordsVertical[columnIndex + 1];
						List<float> coordList = new List<float>();
						for (; coordIndex < areaCharacterLineCoordsVertical.Count && areaCharacterLineCoordsVertical[coordIndex] < cutoff; coordIndex++)
							coordList.Add(areaCharacterLineCoordsVertical[coordIndex]);
						subAreaCharacterLineCoordsVertical[columnIndex] = coordList;
					}
				}

				//generate sub-area line read event dictionaries
				Dictionary<int, Dictionary<float, List<LinearReadEvent>>> subAreaCharacterLineReadEventsHorizontal = new Dictionary<int, Dictionary<float, List<LinearReadEvent>>>();
				{
					//check if no processing is necessary
					if (bestSlices.Item2.Count == 0)
						subAreaCharacterLineReadEventsHorizontal[0] = areaCharacterLineReadEventsHorizontal;
					else
					{
						//initialize dictionaries
						for (int columnIndex = 0; columnIndex <= bestSlices.Item2.Count; columnIndex++)
							subAreaCharacterLineReadEventsHorizontal[columnIndex] = new Dictionary<float, List<LinearReadEvent>>();

						//iterate over read event lines
						foreach (var kv in areaCharacterLineReadEventsHorizontal)
						{
							//get first event in line
							LinearReadEvent currentReadEvent = kv.Value.First();

							//declare span event generator function
							void generateSpanReadEvents(int columnIndex)
							{
								//get column bounds
								float columnLeft = slicedAreaCoordsVertical[columnIndex];
								float columnRight = slicedAreaCoordsVertical[columnIndex + 1];

								//initialize read event list
								List<LinearReadEvent> spanReadEvents = new List<LinearReadEvent>();

								//initialize buffers
								LinearReadEvent eventBuffer = null;
								LinearReadEvent.EventFlags flagBuffer = currentReadEvent.Flags;
								Range spanBuffer =
									new Range(
										columnLeft,
										currentReadEvent.LineSpan.Upper);

								//check if span's first event is a character event
								if ((currentReadEvent.Flags & LinearReadEvent.EventFlags.Character) != 0)
								{
									//add starting event
									eventBuffer = new LinearReadEvent(
										new Range(columnLeft, columnLeft),
										LinearReadEvent.EventFlags.ReadStart,
										null);
									spanReadEvents.Add(eventBuffer);
								}
								else
								{
									//adjust flags
									flagBuffer |= LinearReadEvent.EventFlags.ReadStart;
								}

								//add events until span end is reached
								while (
									spanBuffer.Upper < columnRight &&
									currentReadEvent.NextEvent != null)
								{
									//create and add event
									eventBuffer = new LinearReadEvent(
										spanBuffer,
										flagBuffer,
										eventBuffer);
									spanReadEvents.Add(eventBuffer);

									//get next event data
									currentReadEvent = currentReadEvent.NextEvent;
									spanBuffer = currentReadEvent.LineSpan;
									flagBuffer = currentReadEvent.Flags;
								}

								//adjust line buffer
								spanBuffer = 
									new Range(
										spanBuffer.Lower,
										columnRight);

								//check if last event would be a character event
								if ((flagBuffer & LinearReadEvent.EventFlags.Character) != 0)
								{
									//create and add current event
									eventBuffer = new LinearReadEvent(
										spanBuffer,
										flagBuffer,
										eventBuffer);
									spanReadEvents.Add(eventBuffer);

									//create last event values
									spanBuffer = new Range(columnRight, columnRight);
									flagBuffer = LinearReadEvent.EventFlags.NoEvents;
								}

								//add last event
								spanReadEvents.Add(
									new LinearReadEvent(
										spanBuffer,
										flagBuffer | LinearReadEvent.EventFlags.ReadEnd,
										eventBuffer));

								//skip over to next event if ended on edge
								if (currentReadEvent.LineSpan.Upper == columnRight)
									currentReadEvent = currentReadEvent.NextEvent;

								//add event list to appropriate dictionary
								subAreaCharacterLineReadEventsHorizontal[columnIndex][kv.Key] = spanReadEvents;
							}

							//iterate over spans
							for (int columnIndex = 0; columnIndex < slicedAreaCoordsVertical.Count - 1; columnIndex++)
								generateSpanReadEvents(columnIndex);
						}
					}
				}
				Dictionary<int, Dictionary<float, List<LinearReadEvent>>> subAreaCharacterLineReadEventsVertical = new Dictionary<int, Dictionary<float, List<LinearReadEvent>>>();
				{
					//check if no processing is necessary
					if (bestSlices.Item1.Count == 0)
						subAreaCharacterLineReadEventsVertical[0] = areaCharacterLineReadEventsVertical;
					else
					{
						//initialize dictionaries
						for (int rowIndex = 0; rowIndex <= bestSlices.Item1.Count; rowIndex++)
							subAreaCharacterLineReadEventsVertical[rowIndex] = new Dictionary<float, List<LinearReadEvent>>();

						//iterate over read event lines
						foreach (var kv in areaCharacterLineReadEventsVertical)
						{
							//get first event in line
							LinearReadEvent currentReadEvent = kv.Value.First();

							//declare span event generator function
							void generateSpanReadEvents(int rowIndex)
							{
								//get row bounds
								float rowBottom = slicedAreaCoordsHorizontal[rowIndex];
								float rowTop = slicedAreaCoordsHorizontal[rowIndex + 1];

								//initialize read event list
								List<LinearReadEvent> spanReadEvents = new List<LinearReadEvent>();

								//initialize buffers
								LinearReadEvent eventBuffer = null;
								LinearReadEvent.EventFlags flagBuffer = currentReadEvent.Flags;
								Range spanBuffer =
									new Range(
										rowBottom,
										currentReadEvent.LineSpan.Upper);

								//check if span's first event is a character event
								if ((currentReadEvent.Flags & LinearReadEvent.EventFlags.Character) != 0)
								{
									//add starting event
									eventBuffer = new LinearReadEvent(
										new Range(rowBottom, rowBottom),
										LinearReadEvent.EventFlags.ReadStart,
										null);
									spanReadEvents.Add(eventBuffer);
								}
								else
								{
									//adjust flags
									flagBuffer |= LinearReadEvent.EventFlags.ReadStart;
								}

								//add events until span end is reached
								while (
									spanBuffer.Upper < rowTop &&
									currentReadEvent.NextEvent != null)
								{
									//create and add event
									eventBuffer = new LinearReadEvent(
										spanBuffer,
										flagBuffer,
										eventBuffer);
									spanReadEvents.Add(eventBuffer);

									//get next event data
									currentReadEvent = currentReadEvent.NextEvent;
									spanBuffer = currentReadEvent.LineSpan;
									flagBuffer = currentReadEvent.Flags;
								}

								//adjust line buffer
								spanBuffer =
									new Range(
										spanBuffer.Lower,
										rowTop);

								//check if last event would be a character event
								if ((flagBuffer & LinearReadEvent.EventFlags.Character) != 0)
								{
									//create and add current event
									eventBuffer = new LinearReadEvent(
										spanBuffer,
										flagBuffer,
										eventBuffer);
									spanReadEvents.Add(eventBuffer);

									//create last event values
									spanBuffer = new Range(rowTop, rowTop);
									flagBuffer = LinearReadEvent.EventFlags.NoEvents;
								}

								//add last event
								spanReadEvents.Add(
									new LinearReadEvent(
										spanBuffer,
										flagBuffer | LinearReadEvent.EventFlags.ReadEnd,
										eventBuffer));

								//skip over to next event if ended on edge
								if (currentReadEvent.LineSpan.Upper == rowTop)
									currentReadEvent = currentReadEvent.NextEvent;

								//add event list to appropriate dictionary
								subAreaCharacterLineReadEventsVertical[rowIndex][kv.Key] = spanReadEvents;
							}

							//iterate over spans
							for (int rowIndex = 0; rowIndex < slicedAreaCoordsHorizontal.Count - 1; rowIndex++)
								generateSpanReadEvents(rowIndex);
						}
					}
				}

				//generate sub-area permitted slice types
				SliceTypes subAreaSliceTypes = sliceTypes;
				{
					if (bestSliceType == SliceTypes.PageWithSidebars)
					{
						subAreaSliceTypes &= ~SliceTypes.PageWithSidebars;
						subAreaSliceTypes |= SliceTypes.ContentColumns;
						subAreaSliceTypes |= SliceTypes.FillingTableBody;
					}
					if (bestSliceType == SliceTypes.FillingTableBody)
						subAreaSliceTypes = SliceTypes.NoSlices;
					if (bestSliceType == SliceTypes.ContentColumns)
						subAreaSliceTypes = SliceTypes.HorizontalSeparators | SliceTypes.InlineTableBody;
					if (bestSliceType == SliceTypes.HorizontalSeparators)
						subAreaSliceTypes = SliceTypes.InlineTableBody;
					if (bestSliceType == SliceTypes.InlineTableBody)
						subAreaSliceTypes = SliceTypes.NoSlices;
				}

				//iterate over sub-sections
				for (int y = 0; y < slicedAreaCoordsHorizontal.Count - 1; y++)
					for (int x = 0; x < slicedAreaCoordsVertical.Count - 1; x++)
					{
						//create sub-area rectangle
						BoxCoords subArea =
							new BoxCoords(
								slicedAreaCoordsVertical[x],
								slicedAreaCoordsVertical[x + 1],
								slicedAreaCoordsHorizontal[y],
								slicedAreaCoordsHorizontal[y + 1]);

						//get sub-area characters
						HashSet<TextCharacter> subAreaCharacters = new HashSet<TextCharacter>();
						foreach (var character in areaCharacters)
							if (character.BoundingBox.LeftX <= subArea.RightX &&
								character.BoundingBox.RightX >= subArea.LeftX &&
								character.BoundingBox.BottomY <= subArea.TopY &&
								character.BoundingBox.TopY >= subArea.BottomY)
								subAreaCharacters.Add(character);

						//check if no characters were found for the sub-area
						if (subAreaCharacters.Count == 0)
							continue;

						//check if sub-area and area character sets are equal
						if (areaCharacters.SetEquals(subAreaCharacters))
							continue;

						//get sub-area's character line coords
						List<float> coordsHorizontal = new List<float>(subAreaCharacterLineCoordsHorizontal[y]);
						List<float> coordsVertical = new List<float>(subAreaCharacterLineCoordsVertical[x]);

						//get sub area's line read events
						Dictionary<float, List<LinearReadEvent>> readEventsHorizontal = new Dictionary<float, List<LinearReadEvent>>();
						foreach (float coord in coordsHorizontal)
							readEventsHorizontal[coord] = subAreaCharacterLineReadEventsHorizontal[x][coord];
						Dictionary<float, List<LinearReadEvent>> readEventsVertical = new Dictionary<float, List<LinearReadEvent>>();
						foreach (var coord in coordsVertical)
							readEventsVertical[coord] = subAreaCharacterLineReadEventsVertical[y][coord];

						//generate sub-area character sets
						Dictionary<float, HashSet<TextCharacter>> subAreaCharacterLineSetsHorizontal = new Dictionary<float, HashSet<TextCharacter>>();
						{
							//initialize last set buffer
							HashSet<TextCharacter> lastSet = new HashSet<TextCharacter>();

							//iterate over coordinates
							for (int coordIndex = 0; coordIndex < coordsHorizontal.Count; coordIndex++)
							{
								//get subset
								HashSet<TextCharacter> subset = new HashSet<TextCharacter>(areaCharacterLineSetsHorizontal[coordsHorizontal[coordIndex]]);
								subset.IntersectWith(subAreaCharacters);

								//check if subset is invalid
								if (subset.Count == 0 || subset.SetEquals(lastSet))
								{
									//remove coordinate from list
									coordsHorizontal.RemoveAt(coordIndex);

									//decrement index
									coordIndex--;

									continue;
								}

								//add subset to dictionary
								subAreaCharacterLineSetsHorizontal[coordsHorizontal[coordIndex]] = subset;

								//replace last set
								lastSet = subset;
							}
						}
						Dictionary<float, HashSet<TextCharacter>> subAreaCharacterLineSetsVertical = new Dictionary<float, HashSet<TextCharacter>>();
						{
							//initialize last set buffer
							HashSet<TextCharacter> lastSet = new HashSet<TextCharacter>();

							//iterate over coordinates
							for (int coordIndex = 0; coordIndex < coordsVertical.Count; coordIndex++)
							{
								//get subset
								HashSet<TextCharacter> subset = new HashSet<TextCharacter>(areaCharacterLineSetsVertical[coordsVertical[coordIndex]]);
								subset.IntersectWith(subAreaCharacters);

								//check if subset is invalid
								if (subset.Count == 0 || subset.SetEquals(lastSet))
								{
									//remove coordinate from list
									coordsVertical.RemoveAt(coordIndex);

									//decrement index
									coordIndex--;

									continue;
								}

								//add subset to dictionary
								subAreaCharacterLineSetsVertical[coordsVertical[coordIndex]] = subset;

								//replace last set
								lastSet = subset;
							}
						}

						//add sub-area to subsection list
						subsections.Add(
							new ContentSection(
								this,
								subArea,
								subAreaCharacters,
								coordsHorizontal,
								coordsVertical,
								subAreaCharacterLineSetsHorizontal,
								subAreaCharacterLineSetsVertical,
								readEventsHorizontal,
								readEventsVertical,
								subAreaSliceTypes));
					}

				//store subsection list
				Subsections = subsections;
			}

			//Console.WriteLine($"</{ (area.LeftX.GetHashCode() ^ area.RightX.GetHashCode() ^ area.BottomY.GetHashCode() ^ area.TopY.GetHashCode()).ToString("X8") }>");
		}

		#region Factory methods

		internal static ContentSection GenerateForPage(
			DocumentPage page,
			BoxCoords contentArea,
			DocumentProject projectData)
		{
			//get page elements within content area
			List<TextCharacter> contentAreaCharacters = new List<TextCharacter>();
			List<DocumentPage.Path> contentAreaBitmaps = new List<DocumentPage.Path>();
			List<DocumentPage.Path> contentAreaPaths = new List<DocumentPage.Path>();
			foreach (var character in page.Characters)
				if (character.BoundingBox.LeftX <= contentArea.RightX &&
					character.BoundingBox.RightX >= contentArea.LeftX &&
					character.BoundingBox.BottomY <= contentArea.TopY &&
					character.BoundingBox.TopY >= contentArea.BottomY)
					contentAreaCharacters.Add(character);
			foreach (var bitmap in page.Bitmaps)
				if (bitmap.Outline.BoundingBox.LeftX <= contentArea.RightX &&
					bitmap.Outline.BoundingBox.RightX >= contentArea.LeftX &&
					bitmap.Outline.BoundingBox.BottomY <= contentArea.TopY &&
					bitmap.Outline.BoundingBox.TopY >= contentArea.BottomY &&
					bitmap.Outline.BoundingBox.Width < page.MediaBox.Width * 0.95 &&
					bitmap.Outline.BoundingBox.Height < page.MediaBox.Height * 0.95)
					contentAreaBitmaps.Add(bitmap.Outline);
			foreach (var path in page.Paths)
				if (path.BoundingBox.LeftX <= contentArea.RightX &&
					path.BoundingBox.RightX >= contentArea.LeftX &&
					path.BoundingBox.BottomY <= contentArea.TopY &&
					path.BoundingBox.TopY >= contentArea.BottomY)
					contentAreaPaths.Add(path);

			//generate graphics colliders
			Dictionary<DocumentPage.Path, IReadOnlyList<Line>> bitmapColliders = new Dictionary<DocumentPage.Path, IReadOnlyList<Line>>();
			Dictionary<DocumentPage.Path, IReadOnlyList<Line>> pathColliders = new Dictionary<DocumentPage.Path, IReadOnlyList<Line>>();
			foreach (var bitmap in contentAreaBitmaps)
			{
				List<Line> lines = new List<Line>();
				foreach (var pathSegment in bitmap.PathSegments)
					lines.Add(
						new Line(
							new Point(
								pathSegment.Start.X,
								pathSegment.Start.Y),
							new Point(
								pathSegment.End.X,
								pathSegment.End.Y)));
				bitmapColliders[bitmap] = lines;
			}
			foreach (var path in contentAreaPaths)
			{
				List<Line> lines = new List<Line>();
				foreach (var pathSegment in path.PathSegments)
					lines.Add(
						new Line(
							new Point(
								pathSegment.Start.X,
								pathSegment.Start.Y),
							new Point(
								pathSegment.End.X,
								pathSegment.End.Y)));
				bitmapColliders[path] = lines;
			}

			//get horizontal and vertical line coordinates for each character bounding box
			List<float> lineCoordsHorizontal;
			List<float> lineCoordsVertical;
			{
				//generate preliminary sets
				HashSet<float> lineCoordsPreliminaryHorizontal = new HashSet<float>();
				HashSet<float> lineCoordsPreliminaryVertical = new HashSet<float>();
				foreach (var character in page.Characters)
				{
					lineCoordsPreliminaryHorizontal.Add(character.BoundingBox.Center.Y);
					lineCoordsPreliminaryVertical.Add(character.BoundingBox.Center.X);
				}

				//generate final lists
				lineCoordsHorizontal = new List<float>(lineCoordsPreliminaryHorizontal);
				lineCoordsVertical = new List<float>(lineCoordsPreliminaryVertical);

				//sort lists
				lineCoordsHorizontal.Sort();
				lineCoordsVertical.Sort();
			}

			//generate content sets for each line coordinate
			Dictionary<float, HashSet<TextCharacter>> lineCharactersHorizontal = new Dictionary<float, HashSet<TextCharacter>>();
			Dictionary<float, HashSet<TextCharacter>> lineCharactersVertical = new Dictionary<float, HashSet<TextCharacter>>();
			Dictionary<float, HashSet<DocumentPage.Path>> lineBitmapsHorizontal = new Dictionary<float, HashSet<DocumentPage.Path>>();
			Dictionary<float, HashSet<DocumentPage.Path>> lineBitmapsVertical = new Dictionary<float, HashSet<DocumentPage.Path>>();
			Dictionary<float, HashSet<DocumentPage.Path>> linePathsHorizontal = new Dictionary<float, HashSet<DocumentPage.Path>>();
			Dictionary<float, HashSet<DocumentPage.Path>> linePathsVertical = new Dictionary<float, HashSet<DocumentPage.Path>>();
			foreach (float lineY in lineCoordsHorizontal)
			{
				//create content sets
				HashSet<TextCharacter> contentCharacters = new HashSet<TextCharacter>();
				HashSet<DocumentPage.Path> contentBitmaps = new HashSet<DocumentPage.Path>();
				HashSet<DocumentPage.Path> contentPaths = new HashSet<DocumentPage.Path>();

				//add overlaping characters
				foreach (var character in contentAreaCharacters)
					if (character.BoundingBox.BottomY <= lineY &&
						character.BoundingBox.TopY >= lineY)
						contentCharacters.Add(character);

				//add overlaping bitmaps
				foreach (var bitmap in contentAreaBitmaps)
					if (bitmap.BoundingBox.BottomY <= lineY &&
						bitmap.BoundingBox.TopY >= lineY)
						contentBitmaps.Add(bitmap);

				//add overlaping paths
				foreach (var path in contentAreaPaths)
					if (path.BoundingBox.BottomY <= lineY &&
						path.BoundingBox.TopY >= lineY)
						contentPaths.Add(path);

				//add sets to dictionaries
				lineCharactersHorizontal[lineY] = contentCharacters;
				lineBitmapsHorizontal[lineY] = contentBitmaps;
				linePathsHorizontal[lineY] = contentPaths;
			}
			foreach (float lineX in lineCoordsVertical)
			{
				//create content sets
				HashSet<TextCharacter> contentCharacters = new HashSet<TextCharacter>();
				HashSet<DocumentPage.Path> contentBitmaps = new HashSet<DocumentPage.Path>();
				HashSet<DocumentPage.Path> contentPaths = new HashSet<DocumentPage.Path>();

				//add overlaping characters
				foreach (var character in contentAreaCharacters)
					if (character.BoundingBox.LeftX <= lineX &&
						character.BoundingBox.RightX >= lineX)
						contentCharacters.Add(character);

				//add overlaping bitmaps
				foreach (var bitmap in contentAreaBitmaps)
					if (bitmap.BoundingBox.LeftX <= lineX &&
						bitmap.BoundingBox.RightX >= lineX)
						contentBitmaps.Add(bitmap);

				//add overlaping paths
				foreach (var path in contentAreaPaths)
					if (path.BoundingBox.LeftX <= lineX &&
						path.BoundingBox.RightX >= lineX)
						contentPaths.Add(path);

				//add sets to dictionaries
				lineCharactersVertical[lineX] = contentCharacters;
				lineBitmapsVertical[lineX] = contentBitmaps;
				linePathsVertical[lineX] = contentPaths;
			}

			//remove lines with duplicate content sets
			{
				//find horizontal duplicates
				{
					//initialize last set buffers
					HashSet<TextCharacter> lastCharacterSet = new HashSet<TextCharacter>();
					HashSet<DocumentPage.Path> lastBitmapSet = new HashSet<DocumentPage.Path>();
					HashSet<DocumentPage.Path> lastPathSet = new HashSet<DocumentPage.Path>();

					//iterate over coordinates
					for (int coordIndex = 0; coordIndex < lineCoordsHorizontal.Count; coordIndex++)
					{
						//get coordinate
						float coord = lineCoordsHorizontal[coordIndex];

						//get content sets
						var characterSet = lineCharactersHorizontal[coord];
						var bitmapSet = lineBitmapsHorizontal[coord];
						var pathSet = linePathsHorizontal[coord];

						//check if sets match fully to the previous values
						if (characterSet.Count == 0 ||
							(characterSet.SetEquals(lastCharacterSet) &&
							bitmapSet.SetEquals(lastBitmapSet) &&
							pathSet.SetEquals(lastPathSet)))
						{
							//full set overlap or empty set

							//remove coordinate and associated sets
							lineCoordsHorizontal.RemoveAt(coordIndex);
							lineCharactersHorizontal.Remove(coord);
							lineBitmapsHorizontal.Remove(coord);
							linePathsHorizontal.Remove(coord);

							//decrement index
							coordIndex--;
						}
						else
						{
							//partial or no set overlap

							//store set values
							lastCharacterSet = characterSet;
							lastBitmapSet = bitmapSet;
							lastPathSet = pathSet;
						}
					}
				}

				//find vertical duplicates
				{
					//initialize last set buffers
					HashSet<TextCharacter> lastCharacterSet = new HashSet<TextCharacter>();
					HashSet<DocumentPage.Path> lastBitmapSet = new HashSet<DocumentPage.Path>();
					HashSet<DocumentPage.Path> lastPathSet = new HashSet<DocumentPage.Path>();

					//iterate over coordinates
					for (int coordIndex = 0; coordIndex < lineCoordsVertical.Count; coordIndex++)
					{
						//get coordinate
						float coord = lineCoordsVertical[coordIndex];

						//get content sets
						var characterSet = lineCharactersVertical[coord];
						var bitmapSet = lineBitmapsVertical[coord];
						var pathSet = linePathsVertical[coord];

						//check if sets match fully to the previous values
						if (characterSet.Count == 0 ||
							(characterSet.SetEquals(lastCharacterSet) &&
							bitmapSet.SetEquals(lastBitmapSet) &&
							pathSet.SetEquals(lastPathSet)))
						{
							//full set overlap or empty set

							//remove coordinate and associated sets
							lineCoordsVertical.RemoveAt(coordIndex);
							lineCharactersVertical.Remove(coord);
							lineBitmapsVertical.Remove(coord);
							linePathsVertical.Remove(coord);

							//decrement index
							coordIndex--;
						}
						else
						{
							//partial or no set overlap

							//store set values
							lastCharacterSet = characterSet;
							lastBitmapSet = bitmapSet;
							lastPathSet = pathSet;
						}
					}
				}
			}

			//declare linear read event generation function
			List<LinearReadEvent> generateReadEvents(
				Line readingLine,
				IEnumerable<TextCharacter> readCharactersEnumerable,
				IEnumerable<DocumentPage.Path> readBitmapsEnumerable,
				IEnumerable<DocumentPage.Path> readPathsEnumerable,
				float characterSpacingTolerance,
				bool storeLineX)
			{
				//create output buffer
				List<LinearReadEvent> output = new List<LinearReadEvent>();

				//create content lists
				List<TextCharacter> readCharactersList = new List<TextCharacter>(readCharactersEnumerable);
				List<DocumentPage.Path> readBitmapsList = new List<DocumentPage.Path>(readBitmapsEnumerable);
				List<DocumentPage.Path> readPathsList = new List<DocumentPage.Path>(readPathsEnumerable);

				//generate line segments for each character
				Dictionary<TextCharacter, Line> readCharacterLines = new Dictionary<TextCharacter, Line>();
				for (int index = 0; index < readCharactersList.Count; index++)
				{
					var character = readCharactersList[index];
					Line line = 
						readingLine.Overlap(
							Rectangle.Containing(
								new List<Point>()
								{
									new Point(
										character.BoundingBox.LeftX, 
										character.BoundingBox.BottomY), 
									new Point(
										character.BoundingBox.RightX,
										character.BoundingBox.TopY) 
								}));
					if (line != null)
						readCharacterLines[character] = line;
					else
					{
						readCharactersList.RemoveAt(index);
						index--;
					}
				}

				//generate line start and end distance dictionaries
				Dictionary<TextCharacter, float> characterReadLineStartDistances = new Dictionary<TextCharacter, float>();
				Dictionary<TextCharacter, float> characterReadLineEndDistances = new Dictionary<TextCharacter, float>();
				foreach (var character in readCharactersList)
				{
					characterReadLineStartDistances[character] = readCharacterLines[character].Start.DistanceFrom(readingLine.Start);
					characterReadLineEndDistances[character] = readCharacterLines[character].End.DistanceFrom(readingLine.Start);
				}

				//sort characters by position within reading order
				{
					var comp = new Comparison<TextCharacter>((TextCharacter left, TextCharacter right) =>
					{
						return characterReadLineStartDistances[left].CompareTo(characterReadLineStartDistances[right]);
					});
					readCharactersList.Sort(comp);
				}

				//generate read data
				if (readCharactersList.Count > 0)
				{
					//initialize character buffers
					TextCharacter previousCharacter = readCharactersList.First();
					float previousCharacterEndDistance = 0;
					Point previousCharacterEndPoint = readingLine.Start;
					LinearReadEvent.EventFlags previousCharacterFlags = LinearReadEvent.EventFlags.NoEvents;

					//initialize whitespace buffers
					LinearReadEvent.EventFlags whitespaceSpanFlags = LinearReadEvent.EventFlags.ReadStart;
					bool whitespaceNotScanned = true;

					//initialize event buffer
					LinearReadEvent previousEvent = null;

					//iterate over characters
					for (int characterIndex = 0; characterIndex < readCharactersList.Count; characterIndex++)
					{
						//declare spacing width multiplier
						float maxSpacingWidthMultiplier = 1.25f;
#warning MULTIPLIER POSITION

						//get character
						var character = readCharactersList[characterIndex];

						//get character start and end points
						var characterLine = readCharacterLines[character];
						var characterStartPoint = characterLine.Start;
						var characterEndPoint = characterLine.End;

						//get character end distance
						float characterEndDistance = characterReadLineEndDistances[character];

						//scan current whitespace span if needed
						if (character.IsWhitespace && whitespaceNotScanned)
						{
							//initialize previous end distance buffer
							float previousEndDistance = previousCharacterEndDistance;

							//initialize whitespace span end point
							var whitespaceSpanEndPoint = readingLine.End;

							//iterate over characters
							for (int scanIndex = characterIndex; ; scanIndex++)
							{
								//check if line end was reached
								if (scanIndex >= readCharactersList.Count)
								{
									//add line end flag
									whitespaceSpanFlags |= LinearReadEvent.EventFlags.ReadEnd;

									break;
								}

								//get character
								var scanCharacter = readCharactersList[scanIndex];

								//check character spacing
								if (characterReadLineStartDistances[scanCharacter] - previousEndDistance > Math.Min(character.TextStyle.FontHeight, previousCharacter.TextStyle.FontHeight) * maxSpacingWidthMultiplier)
									whitespaceSpanFlags |= LinearReadEvent.EventFlags.UnexpectedSpacing;

								//check if a non-whitespace character was reached
								if (!scanCharacter.IsWhitespace)
								{
									//replace span end point
									whitespaceSpanEndPoint = readCharacterLines[scanCharacter].Start;

									break;
								}

								//check character value
								if (scanCharacter.CharacterValue == '\t' ||
									scanCharacter.CharacterValue == '\r' ||
									scanCharacter.CharacterValue == '\n')
									whitespaceSpanFlags |= LinearReadEvent.EventFlags.UnexpectedSpacing;

								//update end distance
								previousEndDistance = characterReadLineEndDistances[scanCharacter];
							}

							//create whitespace line
							Line whitespaceLine =
								new Line(
									previousCharacterEndPoint,
									whitespaceSpanEndPoint);

							//check for graphics collisions
							foreach (var bitmap in readBitmapsList)
							{
								bool found = false;
								foreach (var collisionLine in bitmapColliders[bitmap])
									if (collisionLine.Intersects(whitespaceLine) != Range.IntersectData.NoIntersect)
									{
										whitespaceSpanFlags |= LinearReadEvent.EventFlags.BitmapCollision;
										found = true;
										break;
									}
								if (found)
									break;
							}
							foreach (var path in readPathsList)
							{
								bool found = false;
								foreach (var collisionLine in bitmapColliders[path])
									if (collisionLine.Intersects(whitespaceLine) != Range.IntersectData.NoIntersect)
									{
										whitespaceSpanFlags |= LinearReadEvent.EventFlags.PathCollision;
										found = true;
										break;
									}
								if (found)
									break;
							}

							//disable scan flag
							whitespaceNotScanned = false;
						}

						//get character flags
						LinearReadEvent.EventFlags characterFlags = LinearReadEvent.EventFlags.NoEvents;
						{
							//check for whitespace
							if (character.IsWhitespace)
							{
								characterFlags |= LinearReadEvent.EventFlags.WhitespaceCharacter;
								characterFlags |= whitespaceSpanFlags;
							}
							else
								characterFlags |= LinearReadEvent.EventFlags.NonWhitespaceCharacter;
						}

						//calculate offset from previous character
						float characterOffset = characterReadLineStartDistances[character] - previousCharacterEndDistance;

						//check if character overlaps with previous character
						bool isCharacterOverlapingPrevious = characterOffset <= 0;

						//add spacing event
						{
							//initialize flag value
							LinearReadEvent.EventFlags eventFlags = whitespaceSpanFlags;

							//check spacing
							{
								//check character overlap
								if (isCharacterOverlapingPrevious)
								{
									eventFlags |= LinearReadEvent.EventFlags.CharacterOverlap;
									eventFlags |= characterFlags;
									eventFlags |= previousCharacterFlags;
								}
								else
								{
									//check unexpected spacing
									if (characterOffset > Math.Min(character.TextStyle.FontHeight, previousCharacter.TextStyle.FontHeight) * maxSpacingWidthMultiplier)
										eventFlags |= LinearReadEvent.EventFlags.UnexpectedSpacing;
								}
							}

							//check font height mismatch
							if (Math.Abs(previousCharacter.TextStyle.FontHeight - character.TextStyle.FontHeight) >
								Math.Min(previousCharacter.TextStyle.FontHeight, character.TextStyle.FontHeight) *
								0.075)
								eventFlags |= LinearReadEvent.EventFlags.FontHeightMismatch;

							//check font style family mismatch
							if (previousCharacter.TextStyle.StyleFamilyId != character.TextStyle.StyleFamilyId)
								eventFlags |= LinearReadEvent.EventFlags.FontStyleFamilyMismatch;

							//create event line
							Line eventLine;
							if (isCharacterOverlapingPrevious)
								eventLine =
									new Line(
										characterStartPoint,
										previousCharacterEndPoint);
							else
								eventLine =
									new Line(
										previousCharacterEndPoint,
										characterStartPoint);

							//check for graphics collisions
							foreach (var bitmap in readBitmapsList)
							{
								bool found = false;
								foreach (var collisionLine in bitmapColliders[bitmap])
									if (collisionLine.Intersects(eventLine) != Range.IntersectData.NoIntersect)
									{
										whitespaceSpanFlags |= LinearReadEvent.EventFlags.BitmapCollision;
										found = true;
										break;
									}
								if (found)
									break;
							}
							foreach (var path in readPathsList)
							{
								bool found = false;
								foreach (var collisionLine in bitmapColliders[path])
									if (collisionLine.Intersects(eventLine) != Range.IntersectData.NoIntersect)
									{
										whitespaceSpanFlags |= LinearReadEvent.EventFlags.PathCollision;
										found = true;
										break;
									}
								if (found)
									break;
							}

							//make character event if short enough
							if (characterOffset < Math.Min(character.TextStyle.FontHeight, previousCharacter.TextStyle.FontHeight) * characterSpacingTolerance)
								eventFlags |= LinearReadEvent.EventFlags.Character;

							//generate event value
							var readEvent = 
								new LinearReadEvent(
									new Range(
										(storeLineX ? eventLine.Start.X : eventLine.Start.Y),
										(storeLineX ? eventLine.End.X : eventLine.End.Y)), 
									eventFlags, 
									previousEvent);

							//add event to list
							output.Add(readEvent);

							//replace previous event value
							previousEvent = readEvent;
						}

						//add character event
						{
							//initialize flag value
							LinearReadEvent.EventFlags eventFlags = characterFlags;

							//create event line
							Line eventLine;
							{
								//get start point
								Point startPoint;
								if (isCharacterOverlapingPrevious)
									startPoint = previousCharacterEndPoint;
								else
									startPoint = characterStartPoint;

								//get end point
								Point endPoint;
								if (characterIndex >= readCharactersList.Count - 1 ||
									characterEndDistance < characterReadLineStartDistances[readCharactersList[characterIndex + 1]])
									endPoint = characterEndPoint;
								else
									endPoint = readCharacterLines[readCharactersList[characterIndex + 1]].Start; ;

								//generate line
								eventLine = new Line(startPoint, endPoint);
							}

							//check for graphics collisions
							foreach (var bitmap in readBitmapsList)
							{
								bool found = false;
								foreach (var collisionLine in bitmapColliders[bitmap])
									if (collisionLine.Intersects(eventLine) != Range.IntersectData.NoIntersect)
									{
										whitespaceSpanFlags |= LinearReadEvent.EventFlags.BitmapCollision;
										found = true;
										break;
									}
								if (found)
									break;
							}
							foreach (var path in readPathsList)
							{
								bool found = false;
								foreach (var collisionLine in bitmapColliders[path])
									if (collisionLine.Intersects(eventLine) != Range.IntersectData.NoIntersect)
									{
										whitespaceSpanFlags |= LinearReadEvent.EventFlags.PathCollision;
										found = true;
										break;
									}
								if (found)
									break;
							}

							//generate event value
							var readEvent = 
								new LinearReadEvent(
									new Range(
										(storeLineX ? eventLine.Start.X : eventLine.Start.Y),
										(storeLineX ? eventLine.End.X : eventLine.End.Y)), 
									eventFlags, 
									previousEvent);

							//add event to list
							output.Add(readEvent);

							//replace previous event value
							previousEvent = readEvent;
						}

						//reset whitespace flags if needed
						if (!character.IsWhitespace)
						{
							whitespaceSpanFlags = LinearReadEvent.EventFlags.NoEvents;
							whitespaceNotScanned = true;
						}

						//store character values
						previousCharacter = character;
						previousCharacterFlags = characterFlags;
						previousCharacterEndPoint = characterEndPoint;
						previousCharacterEndDistance = characterEndDistance;
					}

					//add ending event
					{
						//get event line
						Line eventLine = new Line(
							previousCharacterEndPoint,
							readingLine.End);

						//initialize flag buffer
						LinearReadEvent.EventFlags flags = whitespaceSpanFlags;

						//check for graphics collisions
						foreach (var bitmap in readBitmapsList)
						{
							bool found = false;
							foreach (var collisionLine in bitmapColliders[bitmap])
								if (collisionLine.Intersects(eventLine) != Range.IntersectData.NoIntersect)
								{
									whitespaceSpanFlags |= LinearReadEvent.EventFlags.BitmapCollision;
									found = true;
									break;
								}
							if (found)
								break;
						}
						foreach (var path in readPathsList)
						{
							bool found = false;
							foreach (var collisionLine in bitmapColliders[path])
								if (collisionLine.Intersects(eventLine) != Range.IntersectData.NoIntersect)
								{
									whitespaceSpanFlags |= LinearReadEvent.EventFlags.PathCollision;
									found = true;
									break;
								}
							if (found)
								break;
						}

						//add event
						output.Add(
							new LinearReadEvent(
								new Range(
									(storeLineX ? eventLine.Start.X : eventLine.Start.Y),
									(storeLineX ? eventLine.End.X : eventLine.End.Y)),
								flags | LinearReadEvent.EventFlags.ReadEnd,
								previousEvent));
					}
				}

				return output;
			}

			//generate read events
			Dictionary<float, List<LinearReadEvent>> readEventsHorizontal = new Dictionary<float, List<LinearReadEvent>>();
			foreach (float coord in lineCoordsHorizontal)
				readEventsHorizontal[coord] = generateReadEvents(
					new Line(
						new Point(
							page.MediaBox.LeftX,
							coord),
						new Point(
							page.MediaBox.RightX,
							coord)),
					lineCharactersHorizontal[coord],
					lineBitmapsHorizontal[coord],
					linePathsHorizontal[coord],
					0.75f,
					true);
			Dictionary<float, List<LinearReadEvent>> readEventsVertical = new Dictionary<float, List<LinearReadEvent>>();
			foreach (float coord in lineCoordsVertical)
				readEventsVertical[coord] = generateReadEvents(
					new Line(
						new Point(
							coord,
							page.MediaBox.BottomY),
						new Point(
							coord,
							page.MediaBox.TopY)),
					lineCharactersVertical[coord],
					lineBitmapsVertical[coord],
					linePathsVertical[coord],
					0.0f,
					false);

			//generate root node
			ContentSection root = new ContentSection(
				null,
				contentArea,
				new HashSet<TextCharacter>(contentAreaCharacters),
				lineCoordsHorizontal,
				lineCoordsVertical,
				lineCharactersHorizontal,
				lineCharactersVertical,
				readEventsHorizontal,
				readEventsVertical);

			return root;
		}

		#endregion

		#endregion
	}

	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("STARTING SLICER...");

			//check if argument was not provided
			if (args.Count() <= 0)
			{
				Console.WriteLine("NO PARAM!");
				return;
			}

			//get project data from file
			DocumentProject projectData = DocumentProject.LoadFromFile(args[0]);

			//print notifications
			Console.WriteLine($"[{ projectData.FileIdentifier }]: Loaded project file!");
			Console.Write($"[{ projectData.FileIdentifier }]: Loading { projectData.PageCount } page content files...");

			//load page files
			List<DocumentPage> pages = new List<DocumentPage>();
			for (int i = 0; i < projectData.PageCount; i++)
			{
				Console.Write($"{i}...");
				pages.Add(projectData.GetPageContent(i));
			}

			//print notifications
			Console.WriteLine($"done!");
			Console.Write($"[{ projectData.FileIdentifier }]: Generating text boxes for { projectData.PageCount } pages...");

			//generate text boxes for each page
			List<List<BoxCoords>> pageTextBoxes = new List<List<BoxCoords>>();
			for (int pageIndex = 0; pageIndex < projectData.PageCount; pageIndex++)
			{
				//print notification
				Console.Write($"{pageIndex}...");

				//get page data
				var pageData = pages[pageIndex];

				//generate overlaping text bounding boxes
				List<BoxCoords> textBoxes = new List<BoxCoords>();
				{
					//initialize character set
					HashSet<TextCharacter> characterSet = new HashSet<TextCharacter>(pageData.Characters);

					//iterate until set is exhausted
					while (characterSet.Count > 0)
					{
						//get first character's bounding box
						BoxCoords textBoundingBox = characterSet.First().BoundingBox;

						//generate set of characters whose bounding boxes may overlap with the first character's bounds
						HashSet<TextCharacter> potentialCharacterOverlaps = new HashSet<TextCharacter>();
						foreach (var character in characterSet)
							if ((textBoundingBox.LeftX <= character.BoundingBox.RightX && textBoundingBox.RightX >= character.BoundingBox.LeftX) ||
								(textBoundingBox.BottomY <= character.BoundingBox.TopY && textBoundingBox.TopY >= character.BoundingBox.BottomY))
								potentialCharacterOverlaps.Add(character);

						//iterate until additional characters stop being added to the bounding box
						while (true)
						{
							//initialize accepted character set
							HashSet<TextCharacter> acceptedCharacterSet = new HashSet<TextCharacter>();

							//iterate over characters in set
							foreach (var character in potentialCharacterOverlaps)
							{
								//check if character has overlap with the current bounding box
								float overlapXMin = character.BoundingBox.LeftX - (character.BoundingBox.Width / 4);
								float overlapXMax = character.BoundingBox.RightX + (character.BoundingBox.Width / 4);
								float overlapY = (character.BoundingBox.BottomY + character.BoundingBox.TopY) / 2;
								if (textBoundingBox.LeftX <= overlapXMax &&
									textBoundingBox.RightX >= overlapXMin &&
									textBoundingBox.BottomY <= overlapY &&
									textBoundingBox.TopY >= overlapY)
								{
									//replace current bounding box
									textBoundingBox =
										new BoxCoords(
											Math.Min(textBoundingBox.LeftX, character.BoundingBox.LeftX),
											Math.Max(textBoundingBox.RightX, character.BoundingBox.RightX),
											Math.Min(textBoundingBox.BottomY, character.BoundingBox.BottomY),
											Math.Max(textBoundingBox.TopY, character.BoundingBox.TopY));

									//add character to accepted set
									acceptedCharacterSet.Add(character);
								}
							}

							//check if no additional characters were added
							if (acceptedCharacterSet.Count == 0)
								break;

							//remove accepted characters from sets
							characterSet.ExceptWith(acceptedCharacterSet);
							potentialCharacterOverlaps.ExceptWith(acceptedCharacterSet);
						}

						//add bounding box to list
						textBoxes.Add(textBoundingBox);
					}
				}

				//add boxes to list
				pageTextBoxes.Add(textBoxes);
			}

			//print notifications
			Console.WriteLine($"done!");
			Console.Write($"[{ projectData.FileIdentifier }]: Generating starting page bounds for { projectData.PageCount } pages...");

			//generate page bounds
			List<BoxCoords> pageBounds = new List<BoxCoords>();
			for (int pageIndex = 0; pageIndex < projectData.PageCount; pageIndex++)
			{
				//print notification
				Console.Write($"{pageIndex}...");

				//get page data
				var pageData = pages[pageIndex];

				//find text bounds closest(ish) to the page center
				float distance = float.PositiveInfinity;
				PointCoords pageCenter = pageData.MediaBox.Center;
				BoxCoords startingRectangle = new BoxCoords(pageCenter.X, pageCenter.X, pageCenter.Y, pageCenter.Y);
				foreach (var box in pageTextBoxes[pageIndex])
					if (box.Center.DistanceFrom(pageCenter) < distance)
					{
						distance = box.Center.DistanceFrom(pageCenter);
						startingRectangle = box;
					}

				//get text bounds contained within the likely page bounds
				HashSet<BoxCoords> containedElements = new HashSet<BoxCoords>();
				{
					//create set copy of text bounds list
					HashSet<BoxCoords> textBoundsSet = new HashSet<BoxCoords>(pageTextBoxes[pageIndex]);

					//initialize page bounds
					float leftX = Math.Min(startingRectangle.LeftX, pageCenter.X);
					float rightX = Math.Max(startingRectangle.RightX, pageCenter.X);
					float bottomY = Math.Min(startingRectangle.BottomY, pageCenter.Y);
					float topY = Math.Max(startingRectangle.TopY, pageCenter.Y);

					//initialize rectangle weight function
					float rectangleWeight(BoxCoords rect)
					{
						return rect.Width;
					}

					//initialize average element height sums
					float avgHeightSum = 0;
					float avgHeightWeightSum = 0;

					//declare element capture function
					HashSet<BoxCoords> captureElements(
						float xMin,
						float xMax,
						float yMin,
						float yMax)
					{
						//initialize captured element set
						HashSet<BoxCoords> capturedSet = new HashSet<BoxCoords>();

						//iterate over bounds
						foreach (var box in textBoundsSet)
						{
							//check if box intersects the area
							if (box.LeftX <= xMax &&
								box.RightX >= xMin &&
								box.BottomY <= yMax &&
								box.TopY >= yMin)
							{
								//add box to captured element set
								capturedSet.Add(box);

								//increment average height sums
								float weight = rectangleWeight(box);
								avgHeightSum += box.Height * weight;
								avgHeightWeightSum += weight;
							}
						}

						//remove captured elements from bounds set
						textBoundsSet.ExceptWith(capturedSet);

						//add captured elements to content set
						containedElements.UnionWith(capturedSet);

						return capturedSet;
					}

					//capture elements within starting area
					captureElements(leftX, rightX, bottomY, topY);

					//iterate until expansions do not cause new elements to be captured
					bool areaExpanded = true;
					while (areaExpanded)
					{
						//reset expansion flag
						areaExpanded = false;

						//calculate expansion values
						float averageElementHeight = avgHeightSum / avgHeightWeightSum;
						float horizontalSpan = pageData.MediaBox.Width / 2;
						float verticalSpan = pageData.MediaBox.Height / 2;
						float distanceLeft = pageCenter.X - leftX;
						float distanceRight = rightX - pageCenter.X;
						float distanceBottom = pageCenter.Y - bottomY;
						float distanceTop = topY - pageCenter.Y;

						float leftExpansion = Math.Max(averageElementHeight * 2 * ((horizontalSpan - distanceLeft) / horizontalSpan), distanceRight - distanceLeft);
						float rightExpansion = Math.Max(averageElementHeight * 2 * ((horizontalSpan - distanceRight) / horizontalSpan), distanceLeft - distanceRight);
						float bottomExpansion = Math.Max(averageElementHeight * 2, distanceTop - distanceBottom);
						float topExpansion = Math.Max(averageElementHeight * 2, distanceBottom - distanceTop);

						//declare expansion function
						void expand(
							float xMin,
							float xMax,
							float yMin,
							float yMax)
						{
							//capture elements
							var capturedSet = captureElements(xMin, xMax, yMin, yMax);

							//set expansion flag if elements were captured
							if (capturedSet.Count > 0)
								areaExpanded = true;

							//expand area
							foreach (var box in capturedSet)
							{
								leftX = Math.Min(leftX, box.LeftX);
								rightX = Math.Max(rightX, box.RightX);
								bottomY = Math.Min(bottomY, box.BottomY);
								topY = Math.Max(topY, box.TopY);
							}
						}

						//expand left
						expand(
							leftX - leftExpansion,
							rightX,
							bottomY,
							topY);

						//expand right
						expand(
							leftX,
							rightX + rightExpansion,
							bottomY,
							topY);

						//expand bottom
						expand(
							leftX,
							rightX,
							bottomY - bottomExpansion,
							topY);

						//expand top
						expand(
							leftX,
							rightX,
							bottomY,
							topY + topExpansion);
					}

#warning AAA!

					pageBounds.Add(
						new BoxCoords(
							leftX,
							rightX,
							bottomY,
							topY));
				}

				//generate and add page bounds
				{
					float xMin = float.PositiveInfinity;
					float xMax = float.NegativeInfinity;
					float yMin = float.PositiveInfinity;
					float yMax = float.NegativeInfinity;
					foreach (var box in containedElements)
					{
						xMin = Math.Min(xMin, box.LeftX);
						xMax = Math.Max(xMax, box.RightX);
						yMin = Math.Min(yMin, box.BottomY);
						yMax = Math.Max(yMax, box.TopY);
					}
					//pageBounds.Add(
					//	new BoxCoords(
					//		new PointCoords(
					//			xMin,
					//			yMin),
					//		new PointCoords(
					//			xMax,
					//			yMax)));
				}
			}

			//print notifications
			Console.WriteLine($"done!");
			Console.Write($"[{ projectData.FileIdentifier }]: Calculating average page bounds...");

			//generate average page margins
			float averagePageMarginLeft;
			float averagePageMarginRight;
			float averagePageMarginTop;
			float averagePageMarginBottom;
			{
				//initialize value buffers
				decimal avgMarginLeftXSum = 0;
				decimal avgMarginLeftXWeightSum = 0;
				decimal avgMarginRightXSum = 0;
				decimal avgMarginRightXWeightSum = 0;
				decimal avgMarginBottomYSum = 0;
				decimal avgMarginBottomYWeightSum = 0;
				decimal avgMarginTopYSum = 0;
				decimal avgMarginTopYWeightSum = 0;

				//iterate over pages
				for (int pageIndex = 0; pageIndex < pages.Count; pageIndex++)
				{
					//get page properties
					var page = pages[pageIndex];
					var textBoxes = pageTextBoxes[pageIndex];
					var initialPageBounds = pageBounds[pageIndex];

					//calculate weights
					decimal weightPage = 0;
					foreach (var box in textBoxes)
						weightPage += (decimal)(box.Width * box.Height);
					weightPage /= (decimal)(page.MediaBox.Width * page.MediaBox.Height);
					weightPage *= weightPage;
					const float expectedMarginMultiplier = 0.2f;
					float expectedMarginHorizontal = page.MediaBox.Width * expectedMarginMultiplier;
					float expectedMarginVertical = page.MediaBox.Height * expectedMarginMultiplier;
					decimal weightLeft =
						weightPage *
						(decimal)(initialPageBounds.Width / page.MediaBox.Width);
					//(decimal)Math.Max(Math.Abs(expectedMarginHorizontal - (page.MostLikelyPageBounds.LeftX - page.MediaBox.LeftX)) / expectedMarginHorizontal, 0);
					decimal weightRight =
						weightPage *
						(decimal)(initialPageBounds.Width / page.MediaBox.Width);
					//(decimal)Math.Max(Math.Abs(expectedMarginHorizontal - (page.MediaBox.RightX - page.MostLikelyPageBounds.RightX)) / expectedMarginHorizontal, 0);
					decimal weightBottom =
						weightPage *
						(decimal)(initialPageBounds.Height / page.MediaBox.Height);
					//(decimal)Math.Max(Math.Abs(expectedMarginVertical - (page.MostLikelyPageBounds.BottomY - page.MediaBox.BottomY)) / expectedMarginVertical, 0);
					decimal weightTop =
						weightPage *
						(decimal)(initialPageBounds.Height / page.MediaBox.Height);
					//(decimal)Math.Max(Math.Abs(expectedMarginVertical - (page.MediaBox.TopY - page.MostLikelyPageBounds.TopY)) / expectedMarginVertical, 0);

					//increment sums
					avgMarginLeftXSum +=
						(decimal)(initialPageBounds.LeftX - page.MediaBox.LeftX) *
						weightLeft;
					avgMarginRightXSum +=
						(decimal)(page.MediaBox.RightX - initialPageBounds.RightX) *
						weightRight;
					avgMarginBottomYSum +=
						(decimal)(initialPageBounds.BottomY - page.MediaBox.BottomY) *
						weightBottom;
					avgMarginTopYSum +=
						(decimal)(page.MediaBox.TopY - initialPageBounds.TopY) *
						weightTop;
					avgMarginLeftXWeightSum += weightLeft;
					avgMarginRightXWeightSum += weightRight;
					avgMarginBottomYWeightSum += weightBottom;
					avgMarginTopYWeightSum += weightTop;
				}

				//calculate final values
				averagePageMarginLeft = (float)(avgMarginLeftXSum / avgMarginLeftXWeightSum);
				averagePageMarginRight = (float)(avgMarginRightXSum / avgMarginRightXWeightSum);
				averagePageMarginBottom = (float)(avgMarginBottomYSum / avgMarginBottomYWeightSum);
				averagePageMarginTop = (float)(avgMarginTopYSum / avgMarginTopYWeightSum);
			}

			//print notifications
			Console.SetBufferSize(Console.BufferWidth, 5000);
			Console.WriteLine($"done!");
			Console.WriteLine($"[{ projectData.FileIdentifier }]: Slicing { projectData.PageCount } pages:");

			//generate page slices
			int threadCounter = 0;
			int lastUnslicedPageIndex = 0;
			float progressCounter = 0;
			const float retrieveProgress = 1;
			const float sliceProgress = 4;
			const float convertProgress = 1;
			const float saveProgress = 1;
			const float pageProgress = retrieveProgress + sliceProgress + convertProgress + saveProgress;
			bool multithreadLock = false;
			void processPage()
			{
				try
				{
					int threadId = threadCounter++;
					Console.WriteLine($"[{ projectData.FileIdentifier }]: Thread {threadId} starting!");
					while (true)
					{
						//Console.ReadLine();

						//get page index
						while (multithreadLock) return;
						multithreadLock = true;
						int pageIndex = lastUnslicedPageIndex++;
						multithreadLock = false;
						if (pageIndex >= pages.Count)
						{
							Console.WriteLine($"[{ projectData.FileIdentifier }]: Thread {threadId} finished!");
							return;
						}

						//print notification
						Console.WriteLine($"[{ projectData.FileIdentifier }]: Retrieving page { pageIndex } data...");

						//get page data
						var page = pages[pageIndex];
						BoxCoords finalPageBounds =
							new BoxCoords(
								Math.Min(pageBounds[pageIndex].LeftX, page.MediaBox.LeftX + averagePageMarginLeft),
								Math.Max(pageBounds[pageIndex].RightX, page.MediaBox.RightX - averagePageMarginRight),
								Math.Min(pageBounds[pageIndex].BottomY, page.MediaBox.BottomY + averagePageMarginBottom),
								Math.Max(pageBounds[pageIndex].TopY, page.MediaBox.TopY - averagePageMarginTop));

						//update progress
						progressCounter += retrieveProgress;

						//print notifications
						Console.WriteLine($"[{ projectData.FileIdentifier }]: Slicing page { pageIndex }...");

						//slice page
						var pageSlice = ContentSection.GenerateForPage(page, finalPageBounds, projectData);

						//update progress
						progressCounter += sliceProgress;

						//print notifications
						Console.WriteLine($"[{ projectData.FileIdentifier }]: Converting page { pageIndex } slice data...");

						//convert slice data
						RootSection root;
						{
							//create root node
							root = new RootSection(
								pageSlice.Area);

							//declare conversion function
							void convert(
								PageSection section,
								ContentSection content)
							{
								//add column and row delims
								section.SetDelims(content.verticalSlices, content.horizontalSlices);

								//add subsections
								if (section.ColumnCount > 1 && section.RowCount == 1)
									for (int x = 0; x < section.ColumnCount && x < content.Subsections.Count; x++)
										convert(
											section.GetSubsection(x, 0),
											content.Subsections[x]);
								else if (section.ColumnCount == 1 && section.RowCount > 1)
									for (int y = 0; y < section.RowCount && y < content.Subsections.Count; y++)
										convert(
											section.GetSubsection(0, y),
											content.Subsections[y]);
							}

							//convert sections
							convert(root, pageSlice);

							//add text boxes
							//root.AddTextBoxes(pageTextBoxes[pageIndex]);
						}

						//update progress
						progressCounter += convertProgress;

						//print notifications
						Console.WriteLine($"[{ projectData.FileIdentifier }]: Saving page { pageIndex } slice data...");

						//serialize slice data to file
						root.SaveToFile(projectData, page.Index, false);

						//update progress
						progressCounter += saveProgress;

						//print notification
						Console.WriteLine($"[{ projectData.FileIdentifier }]: Page { pageIndex } done!");
					}
				}
				catch (Exception e)
				{
					Console.WriteLine($"[{ projectData.FileIdentifier }]: EXCEPTION [ { e.Message } ]");
					throw e;
				}
			}
			var Tasks = new List<Task>();
			for (int i = 0; i < Environment.ProcessorCount; i++)
			{
				var task = new Task(processPage, TaskCreationOptions.LongRunning);
				task.Start();
				Tasks.Add(task);
			}
			Thread.Sleep(15000);
			while (true)
			{
				Console.WriteLine($"[{ projectData.FileIdentifier }]: PROGRESS { (int)(100.0f * (progressCounter / (pageProgress * pages.Count))) }%");
				bool working = false;
				foreach (var task in Tasks)
					if (task.Status == TaskStatus.Running)
						working = true;
				if (!working) break;
				Thread.Sleep(5000);
			}
			processPage();

			//print notification
			Console.WriteLine($"[{ projectData.FileIdentifier }]: All pages sliced!");
		}
	}
}
