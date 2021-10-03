using DocumentParser.Utilities.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentParser.DataTypes
{
	/// <summary>
	/// Represents a single page of a PDF document.
	/// </summary>
	public class PdfPage
	{
		#region Sub-types
		
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
				NoSlices				= 0,
				PageWithSidebars		= (ulong)1 << (63 - 0),
				FillingTableBody		= (ulong)1 << (63 - 1),
				ContentColumns			= (ulong)1 << (63 - 2),
				HorizontalSeparators	= (ulong)1 << (63 - 3),
				InlineTableBody			= (ulong)1 << (63 - 4),
				StartingSlice			= PageWithSidebars | ContentColumns
			}

			/// <summary>
			/// Possible section content type flags.
			/// </summary>
			[Flags]
			public enum ContentTypeFlags
			{
				NoFlags				= 0,
				LeafNode			= 1 << 0,
				BranchNode			= 1 << 1,
				RootNode			= (1 << 2) | BranchNode,
				NoSubsections		= LeafNode,
				ContentIsHorizontal	= 1 << 3,
				ContentIsVertical	= 1 << 4,
				ContentIsTable		= 1 << 5,
				ParentIsHorizontal	= 1 << 6,
				ParentIsVertical	= 1 << 7,
				ParentIsTable		= 1 << 8,




				horizontalSidebar	= (1 << 3) | BranchNode,
				textBody			= (1 << 4) | BranchNode,
				textColumn			= (1 << 5) | BranchNode,
				textSection			= (1 << 6) | LeafNode,
				tableBody			= (1 << 7) | LeafNode,
				titleHeader			= (1 << 8) | LeafNode
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
				/// Section of the line for which the event is defined.
				/// </summary>
				public Line LineSection { get; }

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
				/// <param name="lineSection">Section of the line for which the event is defined.</param>
				/// <param name="flags">Event flags.</param>
				/// <param name="previousEvent">Previous event in the line.</param>
				public LinearReadEvent(
					Line lineSection,
					EventFlags flags,
					LinearReadEvent previousEvent)
				{
					//store property values
					LineSection = lineSection;
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
			public Rectangle Area { get; }

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
				Rectangle area,
				HashSet<PdfTextCharacter> areaCharacters,
				IReadOnlyList<float> areaCharacterLineCoordsHorizontal,
				IReadOnlyList<float> areaCharacterLineCoordsVertical,
				Dictionary<float, HashSet<PdfTextCharacter>> areaCharacterLineSetsHorizontal,
				Dictionary<float, HashSet<PdfTextCharacter>> areaCharacterLineSetsVertical,
				Dictionary<float, List<LinearReadEvent>> areaCharacterLineReadEventsHorizontal,
				Dictionary<float, List<LinearReadEvent>> areaCharacterLineReadEventsVertical,
				SliceTypes sliceTypes = SliceTypes.StartingSlice)
			{
				AreaCharacterLineReadEventsHorizontal = areaCharacterLineReadEventsHorizontal;
				AreaCharacterLineReadEventsVertical = areaCharacterLineReadEventsVertical;



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
							bottom = Math.Max(bottom, character.BoundingBox.TopY);

						//get top edge
						float top = float.PositiveInfinity;
						foreach (var character in areaCharacterLineSetsHorizontal[areaCharacterLineCoordsHorizontal[index]])
							top = Math.Min(top, character.BoundingBox.BottomY);

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

				//declare slice generator function
				(IReadOnlyList<float>, IReadOnlyList<float>) generateSlices(
					Func<LinearReadEvent, Line, float> correctnessFuncHorizontal,
					Func<LinearReadEvent, Line, float> correctnessFuncVertical,
					float minimumAvgCorrectnessHorizontal,
					float minimumAvgCorrectnessVertical,
					Func<IReadOnlyList<LinearReadEvent>, Line, float> weightFuncHorizontal,
					Func<IReadOnlyList<LinearReadEvent>, Line, float> weightFuncVertical)
				{
					//initialize output lists
					List<float> coordsHorizontal = new List<float>();
					List<float> coordsVertical = new List<float>();

					//declare slice value generation function
					(float, float) getSliceValues(
						Line sliceLine,
						Func<LinearReadEvent, Line, float> correctnessFunc,
						Func<IReadOnlyList<LinearReadEvent>, Line, float> weightFunc,
						IReadOnlyList<float> perpendicuarCoords,
						Dictionary<float, List<LinearReadEvent>> perpendicularReadEventLists)
					{
						//find overlapping events for each perpendicular coordinate
						List<LinearReadEvent> overlappingEvents = new List<LinearReadEvent>();
						foreach (float coord in perpendicuarCoords)
						{
							foreach (var readEvent in perpendicularReadEventLists[coord])
								if (readEvent.LineSection.Intersects(sliceLine) != Range.IntersectData.NoIntersect)
								{
									overlappingEvents.Add(readEvent);
									break;
								}
						}
						//calculate correctness sum
						float correctnessSum = 0;
						foreach (var readEvent in overlappingEvents)
							correctnessSum += correctnessFunc(readEvent, sliceLine);

						//calculate weight
						float weight = weightFunc(overlappingEvents, sliceLine);

#warning REMOVE!
						//Console.WriteLine($"Line: [{ sliceLine.Start.X }:{ sliceLine.Start.Y }][{ sliceLine.End.X }:{ sliceLine.End.Y }] Strength: [{ correctnessSum / perpendicuarCoords.Count }] Sum: [{ correctnessSum }]");

						failedSlices.Add(sliceLine);
						failedSliceWeights[sliceLine] = weight;
						failedSliceCorrectness[sliceLine] = correctnessSum / perpendicuarCoords.Count;

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
						{
							Line sliceLine =
								new Line(
									new Point(
										Area.LeftX,
										coord),
									new Point(
										Area.RightX,
										coord));
							sliceValuesDictionary[coord] =
								getSliceValues(
									sliceLine,
									correctnessFuncHorizontal,
									weightFuncHorizontal,
									areaCharacterLineCoordsVertical,
									areaCharacterLineReadEventsVertical);
						}

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
						{
							Line sliceLine =
								new Line(
									new Point(
										coord,
										Area.BottomY),
									new Point(
										coord,
										Area.TopY));
							sliceValuesDictionary[coord] =
								getSliceValues(
									sliceLine,
									correctnessFuncVertical,
									weightFuncVertical,
									areaCharacterLineCoordsHorizontal,
									areaCharacterLineReadEventsHorizontal);
						}

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
						var slices = generateSlices(
							(LinearReadEvent readEvent, Line sliceLine) =>
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
							(IReadOnlyList<LinearReadEvent> readEvents, Line sliceLine) =>
							{
								float weightSum = 0;
								foreach (var readEvent in readEvents)
								{
									float bottomDist = sliceLine.Start.Y - readEvent.LineSection.Start.Y;
									float topDist = readEvent.LineSection.End.Y - sliceLine.End.Y;
									float len = readEvent.LineSection.EdgeLength;
									weightSum += (bottomDist * topDist) / (len * len * len);
								}
								return weightSum / readEvents.Count;
							},
							null);

						if (slices.Item1.Count > 0)
							areaSlices[SliceTypes.PageWithSidebars] = slices;
					}
					
					float tableCorrectnessHorizontal(LinearReadEvent readEvent, Line sliceLine)
					{
						if (readEvent.FlagTestAll(LinearReadEvent.EventFlags.Character))
							return 0;
						if (readEvent.FlagTestAll(LinearReadEvent.EventFlags.PathCollision))
							return 1;
						return 0;
					}
					float tableCorrectnessVertical(LinearReadEvent readEvent, Line sliceLine)
					{
						if (readEvent.FlagTestAny(LinearReadEvent.EventFlags.Character | LinearReadEvent.EventFlags.AreaEdge))
							return 0;
						//if ((readEvent.Flags & LinearReadEvent.EventFlags.PageEdge) != 0)
						//	return 0;
						if (readEvent.FlagTestAll(LinearReadEvent.EventFlags.UnexpectedSpacing))
							return 1;
						return 0;
					}
					float tableWeightHorizontal(IReadOnlyList<LinearReadEvent> readEvents, Line sliceLine) 
					{
						float weightSum = 0;
						foreach (var readEvent in readEvents)
						{
							float bottomDist = sliceLine.Start.Y - readEvent.LineSection.Start.Y;
							float topDist = readEvent.LineSection.End.Y - sliceLine.End.Y;
							float len = readEvent.LineSection.EdgeLength;
							weightSum += (bottomDist * topDist) / (len * len * len);
						}
						return weightSum / readEvents.Count;
					}
					float tableWeightVertical(IReadOnlyList<LinearReadEvent> readEvents, Line sliceLine)
					{
						//get line coord
						float lineCoord = sliceLine.Start.X;

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
										left = readEvent.PreviousNonCharacterEvent.LineSection.End.X;
									else
										left = readEvent.LineSection.Start.X;

									//get right
									if (readEvent.NextNonCharacterEvent != null)
										right = readEvent.NextNonCharacterEvent.LineSection.Start.X;
									else
										right = readEvent.LineSection.End.X;
								}
								else
								{
									//get left
									if (readEvent.PreviousCharacterEvent != null)
										left = readEvent.PreviousCharacterEvent.LineSection.End.X;
									else
										left = readEvent.LineSection.Start.X;

									//get right
									if (readEvent.NextCharacterEvent != null)
										right = readEvent.NextCharacterEvent.LineSection.Start.X;
									else
										right = readEvent.LineSection.End.X;
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

							return 50000 - Math.Abs(((left + right) / 2) - lineCoord);
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
								weightSum += (lineCoord - span.Item2);// / (span.Item2 - span.Item1);
							foreach (var span in spansNonCharacter)
								weightSum += (span.Item2 - lineCoord);// / (span.Item2 - span.Item1);
						}
						else
						{
							foreach (var span in spansCharacter)
								weightSum += (span.Item1 - lineCoord);// / (span.Item2 - span.Item1);
							foreach (var span in spansNonCharacter)
								weightSum += (lineCoord - span.Item1);// / (span.Item2 - span.Item1);
						}
						
						return weightSum / readEvents.Count;
					}
					
					//table body filling the entire content area
					if (areaSlices.Count == 0 &&
						(sliceTypes & SliceTypes.FillingTableBody) != 0)
					{
						var slices = generateSlices(
						tableCorrectnessHorizontal,
						tableCorrectnessVertical,
						0.9f,
						0.65f,
						tableWeightHorizontal,
						tableWeightVertical);

						if (slices.Item1.Count > 3 &&
							slices.Item2.Count > 0 )
							areaSlices[SliceTypes.FillingTableBody] = slices;
					}

					//main content columns
					if (areaSlices.Count == 0 &&
						(sliceTypes & SliceTypes.ContentColumns) != 0)
					{
						var slices = generateSlices(
							null,
							(LinearReadEvent readEvent, Line sliceLine) =>
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
							(IReadOnlyList<LinearReadEvent> readEvents, Line sliceLine) =>
							{
								float weightSum = 0;
								foreach (var readEvent in readEvents)
								{
									float leftDist = sliceLine.Start.X - readEvent.LineSection.Start.X;
									float rightDist = readEvent.LineSection.End.X - sliceLine.End.X;
									weightSum += (leftDist * leftDist) + (rightDist * rightDist);
								}
								return weightSum / readEvents.Count;
							});

						if (slices.Item2.Count > 0)
							areaSlices[SliceTypes.ContentColumns] = slices;
					}

					//section separators within text
					if (areaSlices.Count == 0 &&
						(sliceTypes & SliceTypes.HorizontalSeparators) != 0)
					{
						var slices = generateSlices(
							(LinearReadEvent readEvent, Line sliceLine) =>
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
							(IReadOnlyList<LinearReadEvent> readEvents, Line sliceLine) =>
							{
								float weightSum = 0;
								foreach (var readEvent in readEvents)
								{
									float bottomDist = sliceLine.Start.Y - readEvent.LineSection.Start.Y;
									float topDist = readEvent.LineSection.End.Y - sliceLine.End.Y;
									float len = readEvent.LineSection.EdgeLength;
									weightSum += (bottomDist * topDist) / (len * len * len);
								}
								return weightSum / readEvents.Count;
							},
							null);

						if (slices.Item1.Count > 0)
							areaSlices[SliceTypes.HorizontalSeparators] = slices;
					}

					//inline table body
					if (areaSlices.Count == 0 &&
						(sliceTypes & SliceTypes.InlineTableBody) != 0)
					{
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
					}
				}

				//find slices with highest priority
				SliceTypes bestSliceType = SliceTypes.NoSlices;
				(IReadOnlyList<float>, IReadOnlyList<float>) bestSlices = (new List<float>(), new List<float>());
				foreach (var kv in areaSlices)
					if (kv.Key > bestSliceType)
					{
						bestSliceType = kv.Key;
						bestSlices = kv.Value;
					}
				
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
									Area.TopY),
								new Point(
									coord,
									Area.BottomY)));


					//initialize sub-section list
					List<ContentSection> subsections = new List<ContentSection>();
					
					//generate horizontal coord list
					List<float> slicedAreaCoordsHorizontal = new List<float>();
					slicedAreaCoordsHorizontal.Add(Area.TopY);
					slicedAreaCoordsHorizontal.AddRange(bestSlices.Item1);
					slicedAreaCoordsHorizontal.Add(Area.BottomY);

					//generate vertical coord list
					List<float> slicedAreaCoordsVertical = new List<float>();
					slicedAreaCoordsVertical.Add(Area.LeftX);
					slicedAreaCoordsVertical.AddRange(bestSlices.Item2);
					slicedAreaCoordsVertical.Add(Area.RightX);

					//generate sub-area character line coordinate dictionaries
					Dictionary<int, List<float>> subAreaCharacterLineCoordsHorizontal = new Dictionary<int, List<float>>();
					{
						int coordIndex = 0;
						for (int rowIndex = 0; rowIndex < slicedAreaCoordsHorizontal.Count - 1; rowIndex++)
						{
							float cutoff = slicedAreaCoordsHorizontal[rowIndex + 1];
							List<float> coordList = new List<float>();
							for (; coordIndex < areaCharacterLineCoordsHorizontal.Count && areaCharacterLineCoordsHorizontal[coordIndex] > cutoff; coordIndex++)
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
									Line lineBuffer =
										new Line(
											new Point(
												columnLeft,
												currentReadEvent.LineSection.Start.Y),
											currentReadEvent.LineSection.End);

									//check if span's first event is a character event
									if ((currentReadEvent.Flags & LinearReadEvent.EventFlags.Character) != 0)
									{
										//add starting event
										eventBuffer = new LinearReadEvent(
											new Line(lineBuffer.Start, lineBuffer.Start),
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
										lineBuffer.End.X < columnRight &&
										currentReadEvent.NextEvent != null)
									{
										//create and add event
										eventBuffer = new LinearReadEvent(
											lineBuffer,
											flagBuffer,
											eventBuffer);
										spanReadEvents.Add(eventBuffer);

										//get next event data
										currentReadEvent = currentReadEvent.NextEvent;
										lineBuffer = currentReadEvent.LineSection;
										flagBuffer = currentReadEvent.Flags;
									}

									//get end point
									Point endPoint =
										new Point(
											columnRight,
											lineBuffer.End.Y);

									//adjust line buffer
									lineBuffer =
										new Line(
											lineBuffer.Start,
											endPoint);

									//check if last event would be a character event
									if ((flagBuffer & LinearReadEvent.EventFlags.Character) != 0)
									{
										//create and add current event
										eventBuffer = new LinearReadEvent(
											lineBuffer,
											flagBuffer,
											eventBuffer);
										spanReadEvents.Add(eventBuffer);

										//create last event values
										lineBuffer = new Line(endPoint, endPoint);
										flagBuffer = LinearReadEvent.EventFlags.NoEvents;
									}

									//add last event
									spanReadEvents.Add(
										new LinearReadEvent(
											lineBuffer,
											flagBuffer | LinearReadEvent.EventFlags.ReadEnd,
											eventBuffer));

									//skip over to next event if ended on edge
									if (currentReadEvent.LineSection.End.X == columnRight)
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
									float rowTop = slicedAreaCoordsHorizontal[rowIndex];
									float rowBottom = slicedAreaCoordsHorizontal[rowIndex + 1];

									//initialize read event list
									List<LinearReadEvent> spanReadEvents = new List<LinearReadEvent>();

									//initialize buffers
									LinearReadEvent eventBuffer = null;
									LinearReadEvent.EventFlags flagBuffer = currentReadEvent.Flags;
									Line lineBuffer =
										new Line(
											new Point(
												currentReadEvent.LineSection.Start.X,
												rowTop),
											currentReadEvent.LineSection.End);

									//check if span's first event is a character event
									if ((currentReadEvent.Flags & LinearReadEvent.EventFlags.Character) != 0)
									{
										//add starting event
										eventBuffer = new LinearReadEvent(
											new Line(lineBuffer.Start, lineBuffer.Start),
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
										lineBuffer.End.Y > rowBottom &&
										currentReadEvent.NextEvent != null)
									{
										//create and add event
										eventBuffer = new LinearReadEvent(
											lineBuffer,
											flagBuffer,
											eventBuffer);
										spanReadEvents.Add(eventBuffer);

										//get next event data
										currentReadEvent = currentReadEvent.NextEvent;
										lineBuffer = currentReadEvent.LineSection;
										flagBuffer = currentReadEvent.Flags;
									}

									//get end point
									Point endPoint =
										new Point(
											lineBuffer.End.X,
											rowBottom);

									//adjust line buffer
									lineBuffer =
										new Line(
											lineBuffer.Start,
											endPoint);

									//check if last event would be a character event
									if ((flagBuffer & LinearReadEvent.EventFlags.Character) != 0)
									{
										//create and add current event
										eventBuffer = new LinearReadEvent(
											lineBuffer,
											flagBuffer,
											eventBuffer);
										spanReadEvents.Add(eventBuffer);

										//create last event values
										lineBuffer = new Line(endPoint, endPoint);
										flagBuffer = LinearReadEvent.EventFlags.NoEvents;
									}

									//add last event
									spanReadEvents.Add(
										new LinearReadEvent(
											lineBuffer,
											flagBuffer | LinearReadEvent.EventFlags.ReadEnd,
											eventBuffer));

									//skip over to next event if ended on edge
									if (currentReadEvent.LineSection.End.X == rowBottom)
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
					for (int x = 0; x < slicedAreaCoordsVertical.Count - 1; x++)
						for (int y = 0; y < slicedAreaCoordsHorizontal.Count - 1; y++)
						{
							//create sub-area rectangle
							Rectangle subArea =
								new Rectangle(
									new Point(
										slicedAreaCoordsVertical[x],
										slicedAreaCoordsHorizontal[y + 1]),
									new Point(
										slicedAreaCoordsVertical[x + 1],
										slicedAreaCoordsHorizontal[y]));

							//get sub-area characters
							HashSet<PdfTextCharacter> subAreaCharacters = new HashSet<PdfTextCharacter>();
							foreach (var character in areaCharacters)
								if (character.BoundingBox.Intersects(subArea) == Range.IntersectData.BodyIntersect)
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
							Dictionary<float, List<LinearReadEvent>> readEventsHorizontal = subAreaCharacterLineReadEventsHorizontal[x];
							Dictionary<float, List<LinearReadEvent>> readEventsVertical = subAreaCharacterLineReadEventsVertical[y];

							//generate sub-area character sets
							Dictionary<float, HashSet<PdfTextCharacter>> subAreaCharacterLineSetsHorizontal = new Dictionary<float, HashSet<PdfTextCharacter>>();
							{
								//initialize last set buffer
								HashSet<PdfTextCharacter> lastSet = new HashSet<PdfTextCharacter>();

								//iterate over coordinates
								for (int coordIndex = 0; coordIndex < coordsHorizontal.Count; coordIndex++)
								{
									//get subset
									HashSet<PdfTextCharacter> subset = new HashSet<PdfTextCharacter>(areaCharacterLineSetsHorizontal[coordsHorizontal[coordIndex]]);
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
							Dictionary<float, HashSet<PdfTextCharacter>> subAreaCharacterLineSetsVertical = new Dictionary<float, HashSet<PdfTextCharacter>>();
							{
								//initialize last set buffer
								HashSet<PdfTextCharacter> lastSet = new HashSet<PdfTextCharacter>();

								//iterate over coordinates
								for (int coordIndex = 0; coordIndex < coordsVertical.Count; coordIndex++)
								{
									//get subset
									HashSet<PdfTextCharacter> subset = new HashSet<PdfTextCharacter>(areaCharacterLineSetsVertical[coordsVertical[coordIndex]]);
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
			}

			#region Factory methods

			/// <summary>
			/// Generates content section data for the provided page.
			/// </summary>
			/// <param name="page">Page to be parsed.</param>
			/// <returns>Root node of a ContentSection tree.</returns>
			internal static ContentSection GenerateForPage(PdfPage page)
			{
				//create content area rectangle
				Rectangle contentArea = Rectangle.Containing(new List<Point>
				{
					page.MostLikelyPageBounds.LowerLeft,
					page.MostLikelyPageBounds.UpperRight,
					page.SourceDocument.AveragePageBounds.LowerLeft,
					page.SourceDocument.AveragePageBounds.UpperRight
				});

				//get page elements within content area
				List<PdfTextCharacter> contentAreaCharacters = new List<PdfTextCharacter>();
				List<PdfGraphicalElement> contentAreaGraphics = new List<PdfGraphicalElement>();
				foreach (var character in page.Characters)
					if (character.BoundingBox.Intersects(contentArea) != Range.IntersectData.NoIntersect)
						contentAreaCharacters.Add(character);
				foreach (var graphic in page.Graphics)
					if (graphic.BoundingBox.Intersects(contentArea) != Range.IntersectData.NoIntersect &&
						graphic.BoundingBox.Width < page.MediaBox.Width * 0.95 &&
						graphic.BoundingBox.Height < page.MediaBox.Height * 0.95)
						contentAreaGraphics.Add(graphic);

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
					lineCoordsHorizontal.Sort((float left, float right) => { return right.CompareTo(left); });
					lineCoordsVertical.Sort();
				}

				//generate content sets for each line coordinate
				Dictionary<float, HashSet<PdfTextCharacter>> lineCharactersHorizontal = new Dictionary<float, HashSet<PdfTextCharacter>>();
				Dictionary<float, HashSet<PdfTextCharacter>> lineCharactersVertical = new Dictionary<float, HashSet<PdfTextCharacter>>();
				Dictionary<float, HashSet<PdfGraphicalElement>> lineGraphicsHorizontal = new Dictionary<float, HashSet<PdfGraphicalElement>>();
				Dictionary<float, HashSet<PdfGraphicalElement>> lineGraphicsVertical = new Dictionary<float, HashSet<PdfGraphicalElement>>();
				foreach (float lineY in lineCoordsHorizontal)
				{
					//create content sets
					HashSet<PdfTextCharacter> contentCharacters = new HashSet<PdfTextCharacter>();
					HashSet<PdfGraphicalElement> contentGraphics = new HashSet<PdfGraphicalElement>();

					//add overlaping characters
					foreach (var character in contentAreaCharacters)
						if (character.BoundingBox.VerticalRange.DoesIntersect(lineY) != Range.IntersectData.NoIntersect)
							contentCharacters.Add(character);

					//add overlaping graphics
					foreach (var graphic in contentAreaGraphics)
						if (graphic.BoundingBox.VerticalRange.DoesIntersect(lineY) != Range.IntersectData.NoIntersect)
							contentGraphics.Add(graphic);

					//add sets to dictionaries
					lineCharactersHorizontal[lineY] = contentCharacters;
					lineGraphicsHorizontal[lineY] = contentGraphics;
				}
				foreach (float lineX in lineCoordsVertical)
				{
					//create content sets
					HashSet<PdfTextCharacter> contentCharacters = new HashSet<PdfTextCharacter>();
					HashSet<PdfGraphicalElement> contentGraphics = new HashSet<PdfGraphicalElement>();

					//add overlaping characters
					foreach (var character in contentAreaCharacters)
						if (character.BoundingBox.HorizontalRange.DoesIntersect(lineX) != Range.IntersectData.NoIntersect)
							contentCharacters.Add(character);

					//add overlaping graphics
					foreach (var graphic in contentAreaGraphics)
						if (graphic.BoundingBox.HorizontalRange.DoesIntersect(lineX) != Range.IntersectData.NoIntersect)
							contentGraphics.Add(graphic);

					//add sets to dictionaries
					lineCharactersVertical[lineX] = contentCharacters;
					lineGraphicsVertical[lineX] = contentGraphics;
				}

				//remove lines with duplicate content sets
				{
					//find horizontal duplicates
					{
						//initialize last set buffers
						HashSet<PdfTextCharacter> lastCharacterSet = new HashSet<PdfTextCharacter>();
						HashSet<PdfGraphicalElement> lastGraphicSet = new HashSet<PdfGraphicalElement>();

						//iterate over coordinates
						for (int coordIndex = 0; coordIndex < lineCoordsHorizontal.Count; coordIndex++)
						{
							//get coordinate
							float coord = lineCoordsHorizontal[coordIndex];

							//get content sets
							var characterSet = lineCharactersHorizontal[coord];
							var graphicSet = lineGraphicsHorizontal[coord];

							//check if sets match fully to the previous values
							if (characterSet.Count == 0 ||
								(characterSet.SetEquals(lastCharacterSet) &&
								graphicSet.SetEquals(lastGraphicSet)))
							{
								//full set overlap or empty set

								//remove coordinate and associated sets
								lineCoordsHorizontal.RemoveAt(coordIndex);
								lineCharactersHorizontal.Remove(coord);
								lineGraphicsHorizontal.Remove(coord);

								//decrement index
								coordIndex--;
							}
							else
							{
								//partial or no set overlap

								//store set values
								lastCharacterSet = characterSet;
								lastGraphicSet = graphicSet;
							}
						}
					}

					//find vertical duplicates
					{
						//initialize last set buffers
						HashSet<PdfTextCharacter> lastCharacterSet = new HashSet<PdfTextCharacter>();
						HashSet<PdfGraphicalElement> lastGraphicSet = new HashSet<PdfGraphicalElement>();

						//iterate over coordinates
						for (int coordIndex = 0; coordIndex < lineCoordsVertical.Count; coordIndex++)
						{
							//get coordinate
							float coord = lineCoordsVertical[coordIndex];

							//get content sets
							var characterSet = lineCharactersVertical[coord];
							var graphicSet = lineGraphicsVertical[coord];

							//check if sets match fully to the previous values
							if (characterSet.Count == 0 ||
								(characterSet.SetEquals(lastCharacterSet) &&
								graphicSet.SetEquals(lastGraphicSet)))
							{
								//full set overlap or empty set

								//remove coordinate and associated sets
								lineCoordsVertical.RemoveAt(coordIndex);
								lineCharactersVertical.Remove(coord);
								lineGraphicsVertical.Remove(coord);

								//decrement index
								coordIndex--;
							}
							else
							{
								//partial or no set overlap

								//store set values
								lastCharacterSet = characterSet;
								lastGraphicSet = graphicSet;
							}
						}
					}
				}

				//declare linear read event generation function
				List<LinearReadEvent> generateReadEvents(
					Line readingLine,
					IEnumerable<PdfTextCharacter> readCharactersEnumerable,
					IEnumerable<PdfGraphicalElement> readGraphicsEnumerable,
					float characterSpacingTolerance)
				{
					//create output buffer
					List<LinearReadEvent> output = new List<LinearReadEvent>();

					//create content lists
					List<PdfTextCharacter> readCharactersList = new List<PdfTextCharacter>(readCharactersEnumerable);
					List<PdfGraphicalElement> readGraphicsList = new List<PdfGraphicalElement>(readGraphicsEnumerable);
					List<PdfPageElement> readElementsList = new List<PdfPageElement>();
					readElementsList.AddRange(readCharactersList);
					readElementsList.AddRange(readGraphicsList);

					//generate line segments for each content element
					Dictionary<PdfPageElement, Line> readElementLines = new Dictionary<PdfPageElement, Line>();
					for (int index = 0; index < readElementsList.Count; index++)
					{
						var element = readElementsList[index];
						Line line = readingLine.Overlap(element.BoundingBox);
						if (line != null)
							readElementLines[element] = line;
						else
						{
							readElementsList.RemoveAt(index);
							if (element is PdfTextCharacter)
								readCharactersList.Remove(element as PdfTextCharacter);
							else if (element is PdfGraphicalElement)
								readGraphicsList.Remove(element as PdfGraphicalElement);
							index--;
						}
					}

					//generate line start and end distance dictionaries
					Dictionary<PdfPageElement, float> readLineStartDistances = new Dictionary<PdfPageElement, float>();
					Dictionary<PdfPageElement, float> readLineEndDistances = new Dictionary<PdfPageElement, float>();
					foreach (var element in readElementsList)
					{
						readLineStartDistances[element] = readElementLines[element].Start.DistanceFrom(readingLine.Start);
						readLineEndDistances[element] = readElementLines[element].End.DistanceFrom(readingLine.Start);
					}

					//sort elements by position within reading order
					{
						var comp = new Comparison<PdfPageElement>((PdfPageElement left, PdfPageElement right) =>
						{
							return readLineStartDistances[left].CompareTo(readLineStartDistances[right]);
						});
						readElementsList.Sort(comp);
						readCharactersList.Sort(comp);
						readGraphicsList.Sort(comp);
					}

					//generate read data
					{
						//initialize character buffers
						PdfTextCharacter previousCharacter = readCharactersList.First();
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
							var characterLine = readElementLines[character];
							var characterStartPoint = characterLine.Start;
							var characterEndPoint = characterLine.End;

							//get character end distance
							float characterEndDistance = readLineEndDistances[character];

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
									if (readLineStartDistances[scanCharacter] - previousEndDistance > Math.Min(character.CharacterStyle.FontHeight, previousCharacter.CharacterStyle.FontHeight) * maxSpacingWidthMultiplier)
										whitespaceSpanFlags |= LinearReadEvent.EventFlags.UnexpectedSpacing;

									//check if a non-whitespace character was reached
									if (!scanCharacter.IsWhitespace)
									{
										//replace span end point
										whitespaceSpanEndPoint = readElementLines[scanCharacter].Start;

										break;
									}

									//check character value
									if (scanCharacter.CharacterValue == '\t' ||
										scanCharacter.CharacterValue == '\r' ||
										scanCharacter.CharacterValue == '\n')
										whitespaceSpanFlags |= LinearReadEvent.EventFlags.UnexpectedSpacing;

									//update end distance
									previousEndDistance = readLineEndDistances[scanCharacter];
								}

								//create whitespace line
								Line whitespaceLine =
									new Line(
										previousCharacterEndPoint,
										whitespaceSpanEndPoint);

								//check for graphics collisions
								foreach (var graphic in readGraphicsList)
									if (graphic.IsLineIntersectingEdge(whitespaceLine))
									{
										if (graphic.GetType() == typeof(PdfBitmap))
											whitespaceSpanFlags |= LinearReadEvent.EventFlags.BitmapCollision;
										else if (graphic.GetType() == typeof(PdfPath))
											whitespaceSpanFlags |= LinearReadEvent.EventFlags.PathCollision;
										else
											throw new Exception("Unknown graphic type!");

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
							float characterOffset = readLineStartDistances[character] - previousCharacterEndDistance;

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
										if (characterOffset > Math.Min(character.CharacterStyle.FontHeight, previousCharacter.CharacterStyle.FontHeight) * maxSpacingWidthMultiplier)
											eventFlags |= LinearReadEvent.EventFlags.UnexpectedSpacing;
									}
								}

								//check font height mismatch
								if (Math.Abs(previousCharacter.CharacterStyle.FontHeight - character.CharacterStyle.FontHeight) >
									Math.Min(previousCharacter.CharacterStyle.FontHeight, character.CharacterStyle.FontHeight) *
									0.075)
									eventFlags |= LinearReadEvent.EventFlags.FontHeightMismatch;

								//check font style family mismatch
								if (previousCharacter.CharacterStyle.StyleFamily != character.CharacterStyle.StyleFamily)
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
								foreach (var graphic in readGraphicsList)
									if (graphic.IsLineIntersectingEdge(eventLine))
									{
										if (graphic.GetType() == typeof(PdfBitmap))
											eventFlags |= LinearReadEvent.EventFlags.BitmapCollision;
										else if (graphic.GetType() == typeof(PdfPath))
											eventFlags |= LinearReadEvent.EventFlags.PathCollision;
										else
											throw new Exception("Unknown graphic type!");

										break;
									}

								//make character event if short enough
								if (characterOffset < Math.Min(previousCharacter.CharacterHeight, character.CharacterHeight) * characterSpacingTolerance)
									eventFlags |= LinearReadEvent.EventFlags.Character;

								//generate event value
								var readEvent = new LinearReadEvent(eventLine, eventFlags, previousEvent);

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
										characterEndDistance < readLineStartDistances[readCharactersList[characterIndex + 1]])
										endPoint = characterEndPoint;
									else
										endPoint = readElementLines[readCharactersList[characterIndex + 1]].Start; ;

									//generate line
									eventLine = new Line(startPoint, endPoint);
								}
								
								//check for graphics collisions
								foreach (var graphic in readGraphicsList)
									if (graphic.IsLineIntersectingEdge(eventLine))
									{
										if (graphic.GetType() == typeof(PdfBitmap))
											eventFlags |= LinearReadEvent.EventFlags.BitmapCollision;
										else if (graphic.GetType() == typeof(PdfPath))
											eventFlags |= LinearReadEvent.EventFlags.PathCollision;
										else
											throw new Exception("Unknown graphic type!");

										break;
									}
								
								//generate event value
								var readEvent = new LinearReadEvent(eventLine, eventFlags, previousEvent);

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
							foreach (var graphic in readGraphicsList)
								if (graphic.IsLineIntersectingEdge(eventLine))
								{
									if (graphic.GetType() == typeof(PdfBitmap))
										flags |= LinearReadEvent.EventFlags.BitmapCollision;
									else if (graphic.GetType() == typeof(PdfPath))
										flags |= LinearReadEvent.EventFlags.PathCollision;
									else
										throw new Exception("Unknown graphic type!");

									break;
								}

							//add event
							output.Add(
								new LinearReadEvent(
									eventLine,
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
						lineGraphicsHorizontal[coord],
						0.75f);
				Dictionary<float, List<LinearReadEvent>> readEventsVertical = new Dictionary<float, List<LinearReadEvent>>();
				foreach (float coord in lineCoordsVertical)
					readEventsVertical[coord] = generateReadEvents(
						new Line(
							new Point(
								coord,
								page.MediaBox.TopY),
							new Point(
								coord,
								page.MediaBox.BottomY)),
						lineCharactersVertical[coord],
						lineGraphicsVertical[coord],
						0.0f);
			
				//generate root node
				ContentSection root = new ContentSection(
					null,
					contentArea,
					new HashSet<PdfTextCharacter>(contentAreaCharacters),
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

		#endregion

		#region Properties

		#region Private storage fields

		/// <summary>
		/// Page's next rendering order value.
		/// </summary>
		private ulong NextRenderingOrderValue = 0;

		/// <summary>
		/// Private storage field for the Characters property.
		/// </summary>
		private List<PdfTextCharacter> _Characters = new List<PdfTextCharacter>();

		/// <summary>
		/// Private storage field for the Lines property.
		/// </summary>
		private List<PdfTextLine> _Lines = new List<PdfTextLine>();

		/// <summary>
		/// Private storage field for the ContinuousTextBounds property.
		/// </summary>
		private List<Rectangle> _ContinuousTextBounds = new List<Rectangle>();

		/// <summary>
		/// Private storage field for the Graphics property.
		/// </summary>
		private List<PdfGraphicalElement> _Graphics = new List<PdfGraphicalElement>();

		/// <summary>
		/// List of all text blocks formed during initial parsing.
		/// </summary>
		public List<PdfTextBlock> InitialBlocks = new List<PdfTextBlock>();
#warning UNDO!

		/// <summary>
		/// Private storage field for the Graphics property.
		/// </summary>
		private List<PdfTextParagraph> _Paragraphs = new List<PdfTextParagraph>();

		/// <summary>
		/// Private storage field for the Graphics property.
		/// </summary>
		private List<PdfTextTable> _Tables = new List<PdfTextTable>();

		#endregion

		/// <summary>
		/// Document which contains the page.
		/// </summary>
		public PdfDocument SourceDocument { get; }

		/// <summary>
		/// Page title.
		/// </summary>
		public string Title { get; }

		/// <summary>
		/// Page's media box.
		/// </summary>
		public Rectangle MediaBox { get; }

		/// <summary>
		/// Page's crop box (can be null).
		/// </summary>
		public Rectangle CropBox { get; }

		/// <summary>
		/// Page's trim box (can be null).
		/// </summary>
		public Rectangle TrimBox { get; }

		/// <summary>
		/// Page's art box (can be null).
		/// </summary>
		public Rectangle ArtBox { get; }

		/// <summary>
		/// Page's bleed box (can be null).
		/// </summary>
		public Rectangle BleedBox { get; }

		/// <summary>
		/// List of all text characters defined for the page.
		/// </summary>
		public IReadOnlyList<PdfTextCharacter> Characters
		{
			get { return _Characters; }
		}

		/// <summary>
		/// List of all text lines defined for the page.
		/// </summary>
		public IReadOnlyList<PdfTextLine> Lines
		{
			get { return _Lines; }
		}

		#region Preliminary page elements

		/// <summary>
		/// List of bounding boxes of continuous spans of text.
		/// </summary>
		public IReadOnlyList<Rectangle> ContinuousTextBounds
		{
			get { return _ContinuousTextBounds; }
		}

		/// <summary>
		/// Rectangle which is most likely to contain the page's actual content.
		/// </summary>
		internal Rectangle MostLikelyPageBounds { get; private set; } = null;

		#endregion

		#region Final page elements

		/// <summary>
		/// List of all graphics elements defined for the page.
		/// </summary>
		public IReadOnlyList<PdfGraphicalElement> Graphics
		{
			get { return _Graphics; }
		}

		/// <summary>
		/// List of all text paragraphs defined for the page.
		/// </summary>
		public IReadOnlyList<PdfTextParagraph> Paragraphs
		{
			get { return _Paragraphs; }
		}

		/// <summary>
		/// List of all text tables defined for the page.
		/// </summary>
		public IReadOnlyList<PdfTextTable> Tables
		{
			get { return _Tables; }
		}

		#endregion

		#endregion

		#region Methods

		/// <summary>
		/// Retrieves the page's current rendering order value.
		/// </summary>
		/// <returns />
		internal ulong GetNextRenderingOrder()
		{
			return NextRenderingOrderValue++;
		}

		/// <summary>
		/// Adds line to page.
		/// </summary>
		/// <param name="line">Line to be added.</param>
		internal void AddLine(PdfTextLine line)
		{
			//add line to list
			_Lines.Add(line);
		}

		/// <summary>
		/// Removes line from page.
		/// </summary>
		/// <param name="line">Line to be removed.</param>
		internal void RemoveLine(PdfTextLine line)
		{
			//remove line from list
			_Lines.Remove(line);
		}

		/// <summary>
		/// Adds a graphic to page.
		/// </summary>
		/// <param name="graphic">Graphic to be added.</param>
		internal void AddGraphic(PdfGraphicalElement graphic)
		{
			//add graphic to list
			_Graphics.Add(graphic);
		}

		/// <summary>
		/// Performs the primary parsing of page contents.
		/// </summary>
		internal void PrimaryParse()
		{
			//store list of all characters
			foreach (var line in Lines)
				foreach (var segment in line.Segments)
					foreach (var character in segment.Characters)
						_Characters.Add(character);

			return;

			//generate overlaping text bounding boxes
			{
				//initialize character set
				HashSet<PdfTextCharacter> characterSet = new HashSet<PdfTextCharacter>(Characters);

				//iterate until set is exhausted
				while (characterSet.Count > 0)
				{
					//get first character's bounding box
					Rectangle textBoundingBox = characterSet.First().BoundingBox;

					//generate set of characters whose bounding boxes may overlap with the first character's bounds
					HashSet<PdfTextCharacter> potentialCharacterOverlaps = new HashSet<PdfTextCharacter>();
					foreach (var character in characterSet)
						if (textBoundingBox.VerticalRange.DoesIntersect(character.BoundingBox.Center.Y) != Range.IntersectData.NoIntersect ||
							character.BoundingBox.VerticalRange.DoesIntersect(textBoundingBox.Center.Y) != Range.IntersectData.NoIntersect)
							potentialCharacterOverlaps.Add(character);

					//iterate until additional characters stop being added to the bounding box
					while (true)
					{
						//initialize accepted character set
						HashSet<PdfTextCharacter> acceptedCharacterSet = new HashSet<PdfTextCharacter>();

						//iterate over characters in set
						foreach (var character in potentialCharacterOverlaps)
						{
							//check if character has overlap with the current bounding box
							if (textBoundingBox.Intersects(
								new Line(
									new Point(
										character.BoundingBox.LeftX - (character.BoundingBox.Width / 4),
										character.BoundingBox.Center.Y),
									new Point(
										character.BoundingBox.RightX + (character.BoundingBox.Width / 4),
										character.BoundingBox.Center.Y)))
									!= Range.IntersectData.NoIntersect)
							{
								//replace current bounding box
								textBoundingBox = Rectangle.Containing(new List<Point>()
								{
									textBoundingBox.LowerLeft,
									textBoundingBox.UpperRight,
									character.BoundingBox.LowerLeft,
									character.BoundingBox.UpperRight
								});

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
					_ContinuousTextBounds.Add(textBoundingBox);
				}
			}

			//detect likely page bounds
			{
				//find text bounds closest(ish) to the page center
				Rectangle startingRectangle = null;
				float distance = float.PositiveInfinity;
				Point pageCenter = MediaBox.Center;
				foreach (var box in ContinuousTextBounds)
					if (box.Center.DistanceFrom(pageCenter) < distance)
					{
						distance = box.Center.DistanceFrom(pageCenter);
						startingRectangle = box;
					}

				//get text bounds contained within the likely page bounds
				HashSet<Rectangle> containedElements = new HashSet<Rectangle>();
				{
					//create set copy of text bounds list
					HashSet<Rectangle> textBoundsSet = new HashSet<Rectangle>(ContinuousTextBounds);

					//initialize page bounds
					float leftX = Math.Min(startingRectangle.LeftX, pageCenter.X);
					float rightX = Math.Max(startingRectangle.RightX, pageCenter.X);
					float bottomY = Math.Min(startingRectangle.BottomY, pageCenter.Y);
					float topY = Math.Max(startingRectangle.TopY, pageCenter.Y);

					//initialize rectangle weight function
					float rectangleWeight(Rectangle rect)
					{
						return rect.Width;
					}

					//initialize average element height sums
					float avgHeightSum = 0;
					float avgHeightWeightSum = 0;

					//declare element capture function
					HashSet<Rectangle> captureElements(
						float xMin,
						float xMax,
						float yMin,
						float yMax)
					{
						//generate area rectangle
						Rectangle area =
							new Rectangle(
								new Point(
									xMin,
									yMin),
								new Point(
									xMax,
									yMax));

						//initialize captured element set
						HashSet<Rectangle> capturedSet = new HashSet<Rectangle>();

						//iterate over bounds
						foreach (var box in textBoundsSet)
						{
							//check if box intersects the area
							if (box.Intersects(area) != Range.IntersectData.NoIntersect)
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
						float horizontalSpan = MediaBox.Width / 2;
						float verticalSpan = MediaBox.Height / 2;
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
				}

				//generate point list
				List<Point> contentPointList = new List<Point>();
				foreach (var box in containedElements)
				{
					contentPointList.Add(box.LowerLeft);
					contentPointList.Add(box.UpperRight);
				}

				//generate page bounds rectangle
				MostLikelyPageBounds = Rectangle.Containing(contentPointList);
			}
		}


		public ContentSection RootSection;

		/// <summary>
		/// Performs the secondary parsing of page contents.
		/// </summary>
		internal void SecondaryParse()
		{
			//generate page content sections
			ContentSection rootSection = ContentSection.GenerateForPage(this);

			RootSection = rootSection;


#warning TODO!
		}






		/// <summary>
		/// Performs the primary parsing of page contents.
		/// </summary>
		internal void PrimaryParse_OLD()
		{

			//sort lines by top edge of bounding box
			_Lines.Sort(new Comparison<PdfTextLine>((PdfTextLine left, PdfTextLine right) =>
			{
				return -left.BoundingBox.TopY.CompareTo(right.BoundingBox.TopY);
			}));

			//assign lines to initial blocks
			{
				//iterate over lines
				for (int lineIndex = 0; lineIndex < Lines.Count; lineIndex++)
				{
					//create block for the line
					PdfTextBlock block = new PdfTextBlock(Lines[lineIndex]);

					//iterate downwards
					for (int i = lineIndex - 1; i >= 0; i--)
					{
						//check if line fails to add into the block
						if (!block.AddLine(Lines[i]))
						{
							//check if line horizontal position overlaps the block's
							if (block.BoundingBox.HorizontalRange.DoesIntersect(Lines[i].BoundingBox.HorizontalRange) == Range.IntersectData.BodyIntersect)
								break;
						}
					}

					//iterate upwards
					for (int i = lineIndex + 1; i < Lines.Count; i++)
					{
						//check if line fails to add into the block
						if (!block.AddLine(Lines[i]))
						{
							//check if line horizontal position overlaps the block's
							if (block.BoundingBox.HorizontalRange.DoesIntersect(Lines[i].BoundingBox.HorizontalRange) == Range.IntersectData.BodyIntersect)
								break;
						}
					}

					//add block to list
					InitialBlocks.Add(block);
				}
			}

			//merge overlapping blocks
			bool blocksMerged = true;
			while (blocksMerged)
			{
				//reset flag
				blocksMerged = false;

				//iterate over blocks
				for (int primaryBlockIndex = 0; primaryBlockIndex < InitialBlocks.Count; primaryBlockIndex++)
				{
					//get primary block
					var primaryBlock = InitialBlocks[primaryBlockIndex];

					//iterate over secondary blocks
					for (int secondaryBlockIndex = primaryBlockIndex + 1; secondaryBlockIndex < InitialBlocks.Count; secondaryBlockIndex++)
					{
						//attempt to merge secondary block into primary block
						if (InitialBlocks[secondaryBlockIndex].TryMergeInto(primaryBlock))
						{
							//set merge flag
							blocksMerged = true;

							//remove secondary block from list
							InitialBlocks.RemoveAt(secondaryBlockIndex);

							//decrement index
							secondaryBlockIndex--;
						}
					}
				}

				int i = 0;
			}
		}

		/// <summary>
		/// Performs the secondary parsing of page contents.
		/// </summary>
		internal void SecondaryParse_OLD()
		{
			//detect tables
			{
				//create copies of block list
				var blockListLeftToRight = new List<PdfTextBlock>(InitialBlocks);
				var blockListBottomToTop = new List<PdfTextBlock>(InitialBlocks);

				//sort block lists
				blockListLeftToRight.Sort(new Comparison<PdfTextBlock>(
					(PdfTextBlock left, PdfTextBlock right) =>
					{
						return left.BoundingBox.LeftX.CompareTo(right.BoundingBox.LeftX);
					}));
				blockListBottomToTop.Sort(new Comparison<PdfTextBlock>(
					(PdfTextBlock left, PdfTextBlock right) =>
					{
						return left.BoundingBox.BottomY.CompareTo(right.BoundingBox.BottomY);
					}));

				//initialize already accepted block set
				var acceptedBlockSet = new HashSet<PdfTextBlock>();
				
				//iterate over blocks in list
				for (int index = 0; index < blockListLeftToRight.Count; index++)
				{
					//get block
					var block = blockListLeftToRight[index];

					//check if block has already been accepted into a table
					if (acceptedBlockSet.Contains(block))
						continue;

					//check if block has justified alignment
					if (block.Alignment == PdfTextBlock.TextAlignment.Justified)
						continue;

					//check if block is not multiline
					if (!block.IsMultiline)
						continue;

					//initialize potential column list
					List<PdfTextBlock> potentialColumns = new List<PdfTextBlock>();

					//add first block to list
					potentialColumns.Add(block);

					//declare table body bounds buffers
					float bodyLeftmostLeftX = block.BoundingBox.LeftX;
					float bodyLeftmostRightX = block.BoundingBox.RightX;
					float bodyRightmostLeftX = block.BoundingBox.LeftX;
					float bodyRightmostRightX = block.BoundingBox.RightX;
					float bodyBottomY = block.BoundingBox.BottomY;
					float bodyTopY = block.BoundingBox.TopY;

					//attempt to get left neighbors 
					for (int leftIndex = index - 1; leftIndex >= 0; leftIndex--)
					{
						//get block
						var leftBlock = blockListLeftToRight[leftIndex];

						//check if block has no vertical overlap with the current bounding box
						if (leftBlock.BoundingBox.BottomY >= bodyTopY ||
							leftBlock.BoundingBox.TopY <= bodyBottomY)
							continue;

						//check if block has no style family overlap with the first block
						if (!leftBlock.CharacterStyleFamilies.Overlaps(block.CharacterStyleFamilies))
							break;

						//add block to list
						potentialColumns.Add(leftBlock);

						//adjust bounds
						bodyLeftmostLeftX = leftBlock.BoundingBox.LeftX;
						bodyLeftmostRightX = leftBlock.BoundingBox.RightX;
						bodyBottomY = Math.Min(bodyBottomY, leftBlock.BoundingBox.BottomY);
						bodyTopY = Math.Max(bodyTopY, leftBlock.BoundingBox.TopY);
					}

					//attempt to get right neighbors 
					for (int rightIndex = index + 1; rightIndex < blockListLeftToRight.Count; rightIndex++)
					{
						//get block
						var rightBlock = blockListLeftToRight[rightIndex];

						//check if block has no vertical overlap with the current bounding box
						if (rightBlock.BoundingBox.BottomY >= bodyTopY ||
							rightBlock.BoundingBox.TopY <= bodyBottomY)
							continue;

						//check if block has no style family overlap with the first block
						if (!rightBlock.CharacterStyleFamilies.Overlaps(block.CharacterStyleFamilies))
							break;

						//add block to list
						potentialColumns.Add(rightBlock);

						//adjust bounds
						bodyRightmostLeftX = rightBlock.BoundingBox.LeftX;
						bodyRightmostRightX = rightBlock.BoundingBox.RightX;
						bodyBottomY = Math.Min(bodyBottomY, rightBlock.BoundingBox.BottomY);
						bodyTopY = Math.Max(bodyTopY, rightBlock.BoundingBox.TopY);
					}

					//check if no additional potential columns were found
					if (potentialColumns.Count <= 1)
						continue;

					//find index of first block above table body
					int headerIndex = 0;
					for (; headerIndex < blockListBottomToTop.Count; headerIndex++)
						if (blockListBottomToTop[headerIndex].BoundingBox.BottomY >= bodyTopY)
							break;

					//calculate table body character height
					PdfTextCharacter bodyChar = block.Lines.First().Segments.First().Characters.First();
					float bodyCharHeight = bodyChar.Baseline.Start.DistanceFrom(bodyChar.AscentLine.Start);
					
					//find first header block
					PdfTextBlock firstHeaderBlock = null;
					for (; headerIndex < blockListBottomToTop.Count; headerIndex++)
					{
						//get block
						var headerBlock = blockListBottomToTop[headerIndex];

						//check if block has no horizontal overlap with the table body bounding box
						if (headerBlock.BoundingBox.LeftX >= bodyRightmostRightX ||
							headerBlock.BoundingBox.RightX <= bodyLeftmostLeftX)
							continue;
						
						//get first character of the block
						PdfTextCharacter headerChar = headerBlock.Lines.First().Segments.First().Characters.First();

						//check if block does not have similar character height to the table body
						float headerCharHeight = headerChar.Baseline.Start.DistanceFrom(headerChar.AscentLine.Start);
						if (Math.Abs(headerCharHeight - bodyCharHeight) > Math.Min(headerCharHeight, bodyCharHeight) / 6)
							break;

						//check if block character is not bolded
						if (!headerChar.CharacterStyle.IsFontBolded)
							break;

						//store block reference
						firstHeaderBlock = headerBlock;

						break;
					}

					//check if no header block was found
					if (firstHeaderBlock == null)
						continue;

					//declare table header bounds buffers
					float headerLeftX = firstHeaderBlock.BoundingBox.LeftX;
					float headerRightX = firstHeaderBlock.BoundingBox.RightX;
					float headerBottomY = firstHeaderBlock.BoundingBox.BottomY;
					float headerTopY = firstHeaderBlock.BoundingBox.TopY;

					//detect header bounds
					HashSet<PdfTextCharacter.Style.Family> headerStyleFamilies = new HashSet<PdfTextCharacter.Style.Family>(firstHeaderBlock.CharacterStyleFamilies);
					for (headerIndex += 1; headerIndex < blockListBottomToTop.Count; headerIndex++)
					{
						//get block
						var headerBlock = blockListBottomToTop[headerIndex];

						//check if block has no horizontal overlap with the table body bounding box
						if (headerBlock.BoundingBox.LeftX >= bodyRightmostRightX ||
							headerBlock.BoundingBox.RightX <= bodyLeftmostLeftX)
							continue;

						//check if block is above current header top bounds
						if (headerBlock.BoundingBox.BottomY >= headerTopY)
						{
							//check if block does not have a valid header style
							var blockCharacter = headerBlock.Lines.First().Segments.First().Characters.First();
							float blockCharacterHeight = blockCharacter.Baseline.Start.DistanceFrom(blockCharacter.AscentLine.Start);

							if (((Math.Abs(blockCharacterHeight - bodyCharHeight) > Math.Min(blockCharacterHeight, bodyCharHeight) / 6) ||
								!blockCharacter.CharacterStyle.IsFontBolded) &&
								!headerStyleFamilies.Overlaps(headerBlock.CharacterStyleFamilies))
								break;
						}

						//adjust header bounds
						headerLeftX = Math.Min(headerLeftX, headerBlock.BoundingBox.LeftX);
						headerRightX = Math.Max(headerRightX, headerBlock.BoundingBox.RightX);
						headerBottomY = Math.Min(headerBottomY, headerBlock.BoundingBox.BottomY);
						headerTopY = Math.Max(headerTopY, headerBlock.BoundingBox.TopY);

						//add block style families to set
						headerStyleFamilies.UnionWith(headerBlock.CharacterStyleFamilies);
					}

					//check if header does not cover all columns of the table body
					//if (headerLeftX >= bodyLeftmostRightX ||
					//	headerRightX <= bodyRightmostLeftX)
					//	continue;

					//initialize potential header and body block lists
					List<PdfTextBlock> potentialHeader = new List<PdfTextBlock>();
					List<PdfTextBlock> potentialBody = new List<PdfTextBlock>();

					//declare table bounds
					float tableLeftX = Math.Min(headerLeftX, bodyLeftmostLeftX);
					float tableRightX = Math.Max(headerRightX, bodyRightmostRightX);

					//generate header and body lists
					var headerRect =
						new Rectangle(
							new Point(
								tableLeftX,
								headerBottomY),
							new Point(
								tableRightX,
								headerTopY));
					var bodyRect =
						new Rectangle(
							new Point(
								tableLeftX,
								bodyBottomY),
							new Point(
								tableRightX,
								bodyTopY));
					foreach (var pageBlock in InitialBlocks)
					{
						if (headerRect.Contains(pageBlock.BoundingBox.Center) == Range.IntersectData.BodyIntersect)
							potentialHeader.Add(pageBlock);
						if (bodyRect.Contains(pageBlock.BoundingBox.Center) == Range.IntersectData.BodyIntersect)
							potentialBody.Add(pageBlock);
					}
					
					//attempt to get table title
					PdfTextBlock titleBlock = null;
					for (int i = 0; i < blockListBottomToTop.Count; i++)
					{
						//get block
						var potentialTitleBlock = blockListBottomToTop[i];

						//check if block is not above header
						if (potentialTitleBlock.BoundingBox.BottomY < headerTopY)
							continue;

						//check if block has no horizontal overlap with the table bounding box
						if (potentialTitleBlock.BoundingBox.LeftX >= tableRightX ||
							potentialTitleBlock.BoundingBox.RightX <= tableLeftX)
							continue;

						//check if block font is not large enough
						PdfTextCharacter titleChar = potentialTitleBlock.Lines.First().Segments.First().Characters.First();
						float titleCharHeight = titleChar.Baseline.Start.DistanceFrom(titleChar.AscentLine.Start);
						if (titleCharHeight < bodyCharHeight * 1.5)
							break;

						//store block reference
						titleBlock = potentialTitleBlock;

						break;
					}

					//attempt to get table footer
					PdfTextBlock footerBlock = null;
					for (int i = blockListBottomToTop.Count - 1; i >= 0 ; i--)
					{
						//get block
						var potentialFooterBlock = blockListBottomToTop[i];

						//check if block is not below body
						if (potentialFooterBlock.BoundingBox.BottomY > bodyBottomY)
							continue;

						//check if block has no horizontal overlap with the table bounding box
						if (potentialFooterBlock.BoundingBox.LeftX >= tableRightX ||
							potentialFooterBlock.BoundingBox.RightX <= tableLeftX)
							continue;

						//check if block font is not small enough
						PdfTextCharacter footerChar = potentialFooterBlock.Lines.First().Segments.First().Characters.First();
						float footerCharHeight = footerChar.Baseline.Start.DistanceFrom(footerChar.AscentLine.Start);
						if (footerCharHeight > bodyCharHeight * 0.9)
							break;

						//store block reference
						footerBlock = potentialFooterBlock;

						break;
					}

					//create table
					PdfTextTable table =
						new PdfTextTable(
							titleBlock,
							potentialHeader,
							potentialBody,
							footerBlock);
					//check if table has cells
					if (table.Cells != null &&
						table.Cells.Count > 0)
					{
						//add table to list
						_Tables.Add(table);
						
						//add all blocks involved in table creation to the accepted block set
						foreach (var blk in potentialHeader)
							acceptedBlockSet.Add(blk);
						foreach (var blk in potentialBody)
							acceptedBlockSet.Add(blk);
						if (titleBlock != null)
							acceptedBlockSet.Add(titleBlock);
						if (footerBlock != null)
							acceptedBlockSet.Add(footerBlock);
					}
				}

				//remove accepted blocks from initial blocks
				//foreach (var block in acceptedBlockSet)
				//	InitialBlocks.Remove(block);
			}

			//convert remaining blocks into paragraphs
			{
#warning TODO!
			}

			//sort paragraphs

#warning TODO!
			//_PageElements.Sort(new Comparison<PdfTextBlock>((PdfTextBlock left, PdfTextBlock right) =>
			//{
			//	//check if blocks are on separate sides of the page
			//	float pageCenter = MediaBox.Center.X;
			//	if ((left.BoundingBox.RightX < pageCenter && right.BoundingBox.LeftX > pageCenter) ||
			//		(right.BoundingBox.RightX < pageCenter && left.BoundingBox.LeftX > pageCenter))
			//		return left.BoundingBox.Center.X.CompareTo(right.BoundingBox.Center.X);
			//
			//	return -left.BoundingBox.TopY.CompareTo(right.BoundingBox.TopY);
			//}));
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="sourceDocument">Document containing the page.</param>
		/// <param name="title">Page title.</param>
		/// <param name="mediaBox">Page's media box.</param>
		/// <param name="cropBox">Page's crop box.</param>
		/// <param name="trimBox">Page's trim box.</param>
		/// <param name="artBox">Page's art box.</param>
		/// <param name="bleedBox">Page's bleed box.</param>
		internal PdfPage(
			PdfDocument sourceDocument,
			string title,
			Rectangle mediaBox,
			Rectangle cropBox = null,
			Rectangle trimBox = null,
			Rectangle artBox = null,
			Rectangle bleedBox = null)
		{
			//check properties
			if (sourceDocument == null) throw new ArgumentNullException(nameof(sourceDocument));
			if (title == null) throw new ArgumentNullException(nameof(title));
			if (mediaBox == null) throw new ArgumentNullException(nameof(mediaBox));

			//store property values
			SourceDocument = sourceDocument;
			Title = title;
			MediaBox = mediaBox;
			CropBox = cropBox;
			TrimBox = trimBox;
			ArtBox = artBox;
			BleedBox = bleedBox;
		}

		#endregion
	}
}
