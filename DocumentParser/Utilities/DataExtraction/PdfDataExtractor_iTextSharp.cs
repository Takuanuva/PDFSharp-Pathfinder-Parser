using DocumentParser.DataTypes;
using DocumentParser.Utilities.Geometry;
using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DocumentParser.Utilities.DataExtraction
{
	/// <summary>
	/// Extracts PDF data using the iTextSharp library.
	/// </summary>
	internal class PdfDataExtractor :
		ITextExtractionStrategy
	{
		#region Properties

		#region Private storage fields

		/// <summary>
		/// Document instance into which the data is to be extracted.
		/// </summary>
		private DataTypes.PdfDocument DocumentInstance;

		/// <summary>
		/// Currently processed page.
		/// </summary>
		private DataTypes.PdfPage CurrentPage = null;

		/// <summary>
		/// Current text line.
		/// </summary>
		private DataTypes.PdfTextLine CurrentLine = null;

		/// <summary>
		/// Current text segment.
		/// </summary>
		private DataTypes.PdfTextSegment CurrentSegment = null;
		
		#endregion

		/// <summary>
		/// Path to PDF file 
		/// </summary>
		public string FilePath { get; }
		
		#endregion

		#region Methods

		#region ITextExtractionStrategy implementation

		public void EventOccurred(IEventData data, EventType type)
		{
			//check event type
			switch (type)
			{
				case EventType.BEGIN_TEXT:
					//end current line
					CurrentLine = null;

					//finalize last text group if one was present
					//if (CurrentGroup != null)
					//	CurrentGroup.FinalizeConstruction();

					//begin text group
					//CurrentGroup = new TextGroup(CurrentPage);

					break;

				case EventType.RENDER_TEXT:
					//get text data
					var textData = data as TextRenderInfo;

					//declare color parser function
					int getArgbColor(Color color)
					{
						//check if null
						if (color == null) return 0;

						//declare color channel array
						float[] colorChannels = new float[4];
						colorChannels[0] = 1;
						colorChannels[1] = 1;
						colorChannels[2] = 1;
						colorChannels[3] = 1;

						//get channels
						switch (color.GetNumberOfComponents())
						{
							case 0:
								colorChannels[0] = 0;
								colorChannels[1] = 0;
								colorChannels[2] = 0;
								colorChannels[3] = 1;
								break;

							case 1:
								colorChannels[1] = color.GetColorValue()[0];
								colorChannels[2] = color.GetColorValue()[0];
								colorChannels[3] = color.GetColorValue()[0];
								break;

							case 3:
								colorChannels[1] = color.GetColorValue()[0];
								colorChannels[2] = color.GetColorValue()[1];
								colorChannels[3] = color.GetColorValue()[2];
								break;

							case 4:
								colorChannels[0] = color.GetColorValue()[0];
								colorChannels[1] = color.GetColorValue()[1];
								colorChannels[2] = color.GetColorValue()[2];
								colorChannels[3] = color.GetColorValue()[3];
								break;

							default:
								throw new NotImplementedException($"No case implemented for handling color values with {color.GetNumberOfComponents()} components.");
						}

						//parse color value
						uint colorVal =
							((uint)Math.Round((float)0xFF * colorChannels[0]) << 24) |
							((uint)Math.Round((float)0xFF * colorChannels[1]) << 16) |
							((uint)Math.Round((float)0xFF * colorChannels[2]) << 8) |
							((uint)Math.Round((float)0xFF * colorChannels[3]));

						return unchecked((int)colorVal);
					}

					//declare line parser function
					Line getLine(iText.Kernel.Geom.LineSegment line)
					{
						return new Line(
							new Point(
								line.GetStartPoint().Get(iText.Kernel.Geom.Vector.I1),
								line.GetStartPoint().Get(iText.Kernel.Geom.Vector.I2)),
							new Point(
								line.GetEndPoint().Get(iText.Kernel.Geom.Vector.I1),
								line.GetEndPoint().Get(iText.Kernel.Geom.Vector.I2)));
					}

					//get characters
					var charInfos = textData.GetCharacterRenderInfos();

					//get text style
					var pointA = charInfos.First().GetAscentLine().GetStartPoint();
					var pointB = charInfos.First().GetBaseline().GetStartPoint();
					float xOffset = pointA.Get(iText.Kernel.Geom.Vector.I1) - pointB.Get(iText.Kernel.Geom.Vector.I1);
					float yOffset = pointA.Get(iText.Kernel.Geom.Vector.I2) - pointB.Get(iText.Kernel.Geom.Vector.I2);
					float fontHeight = (float)Math.Sqrt((xOffset * xOffset) + (yOffset * yOffset));
					int textRenderMode = textData.GetTextRenderMode();

					PdfTextCharacter.Style style = PdfTextCharacter.Style.FromStyleData(
						DocumentInstance.Parent,
						textData.GetFont().GetFontProgram().GetFontNames().GetFontName(),
						fontHeight,//textData.GetFont().GetFontProgram().GetWidth(' '),
						(textRenderMode == 0 || textRenderMode == 2 || textRenderMode == 4 || textRenderMode == 6) ? getArgbColor(textData.GetFillColor()) : getArgbColor(textData.GetFillColor()) & 0x00FFFFFF,
						(textRenderMode == 1 || textRenderMode == 2 || textRenderMode == 5 || textRenderMode == 6) ? getArgbColor(textData.GetStrokeColor()) : getArgbColor(textData.GetStrokeColor()) & 0x00FFFFFF,
						textData.GetGraphicsState().GetLineWidth());

					//check if style matches current segment
					if (CurrentSegment != null &&
						CurrentSegment.CharacterStyle != style)
						CurrentSegment = null;

					//iterate over characters
					for (int i = 0; i < charInfos.Count; i++)
					{
						//get character info
						var charInfo = charInfos[i];

						//create character
						PdfTextCharacter character = new PdfTextCharacter(
							CurrentPage,
							charInfo.GetText()[0],
							getLine(charInfo.GetBaseline()),
							getLine(charInfo.GetAscentLine()),
							getLine(charInfo.GetDescentLine()),
							style);

						//check if character is within acceptable line boundaries for current line
						if (CurrentLine != null &&
							IsCharacterWithinLine(CurrentLine, character))
						{
							//check if segment is ready
							if (CurrentSegment == null)
							{
								//initialize new segment
								CurrentSegment = new PdfTextSegment(CurrentPage, style);

								//add segment to line
								CurrentLine.AddSegment(CurrentSegment);
							}

							//add character to segment
							CurrentSegment.AddCharacter(character);
						}
						else
						{
							//initialize new line and segment
							CurrentLine = new PdfTextLine(CurrentPage);
							CurrentSegment = new PdfTextSegment(CurrentPage, style);

							//add line to page
							CurrentPage.AddLine(CurrentLine);

							//add segment to line
							CurrentLine.AddSegment(CurrentSegment);

							//add character to segment
							CurrentSegment.AddCharacter(character);
						}

						//check if character value breaks grouping
						char c = charInfo.GetText()[0];
						if (c == '\n' ||
							c == '\r' ||
							c == '\t')
						{
							//end segment and line
							CurrentSegment = null;
							CurrentLine = null;
						}
					}

					break;

				case EventType.RENDER_IMAGE:
					//get image data
					var imageData = data as ImageRenderInfo;

					//get image
					var image = imageData.GetImage();

					//generate bitmap signature
					string imageSignature = $"[{image.GetWidth()}x{image.GetHeight()}][{imageData.GetImageResourceName()}]";

					//check if bitmap resource does not exist
					if (!DocumentInstance.BitmapResources.ContainsKey(imageSignature))
					{
						//generate bitmap
						var bytes = image.GetImageBytes(true);
						System.Drawing.Bitmap bitmap;
						try
						{
							bitmap = new System.Drawing.Bitmap(new MemoryStream(bytes));
						}
						catch (ArgumentException)
						{
							bitmap = new System.Drawing.Bitmap((int)image.GetWidth(), (int)image.GetHeight());
						}
						bitmap.MakeTransparent();

						//add bitmap resource to document
						DocumentInstance.AddBitmapResource(imageSignature, new PdfBitmap.Resource(bitmap));
					}

					//get image transform matrix
					var imageCtm = imageData.GetImageCtm();
					//Console.WriteLine(imageCtm.ToString());
					//Console.WriteLine($"0: {imageCtm.Get(0)} 1: {imageCtm.Get(1)} 2: {imageCtm.Get(2)} 3: {imageCtm.Get(3)} 4: {imageCtm.Get(4)} 5: {imageCtm.Get(5)} 6: {imageCtm.Get(6)} 7: {imageCtm.Get(7)} 8: {imageCtm.Get(8)}");
					var transformMatrix = new TransformMatrix(new float[] {
						imageCtm.Get(0),
						imageCtm.Get(3),
						imageCtm.Get(6),
						imageCtm.Get(1),
						imageCtm.Get(4),
						imageCtm.Get(7) });

#warning TODO: MAKE LESS ARCANE!

					//add image
					CurrentPage.AddGraphic(
						new PdfBitmap(
							CurrentPage,
							DocumentInstance.BitmapResources[imageSignature],
							new Point(
								imageData.GetStartPoint().Get(iText.Kernel.Geom.Vector.I1),
								imageData.GetStartPoint().Get(iText.Kernel.Geom.Vector.I2)),
							transformMatrix));

					break;

				case EventType.RENDER_PATH:
					//if (false)
					{
						//get path data
						var pathData = data as PathRenderInfo;

						//check if path is renderable
						if (pathData.GetOperation() != PathRenderInfo.NO_OP)
						{
							//initialize line segment list
							List<Line> lines = new List<Line>();

							//get path transform matrix
							var pathCtm = pathData.GetGraphicsState().GetCtm();
							var pathTransformMatrix = new TransformMatrix(new float[] {
							pathCtm.Get(0),
							pathCtm.Get(3),
							pathCtm.Get(6),
							pathCtm.Get(1),
							pathCtm.Get(4),
							pathCtm.Get(7) });

							//iterate over path segments
							foreach (var subpath in pathData.GetPath().GetSubpaths())
								if (subpath.GetSegments().Count > 0)
								{
									//create subpath line list
									List<Line> subpathLines = new List<Line>();

									//iterate over segments
									foreach (var segment in subpath.GetSegments())
									{
										//get segment base point collection
										var basePoints = segment.GetBasePoints();

										//check segment type
										switch (basePoints.Count)
										{
											case 2: //straight line segment
													//add segment to list
												subpathLines.Add(
													new Line(
														new Point(
															(float)basePoints[0].GetX(),
															(float)basePoints[0].GetY()),
														new Point(
															(float)basePoints[1].GetX(),
															(float)basePoints[1].GetY())));

												break;

											case 4: //cubic bezier segment
													//get control points
												float ax = (float)basePoints[0].GetX();
												float ay = (float)basePoints[0].GetY();
												float bx = (float)basePoints[1].GetX();
												float by = (float)basePoints[1].GetY();
												float cx = (float)basePoints[2].GetX();
												float cy = (float)basePoints[2].GetY();
												float dx = (float)basePoints[3].GetX();
												float dy = (float)basePoints[3].GetY();

												//declare point generation function
												Point bezier(float t)
												{
													//calculate weights
													float aWeight = t * t * t;
													float bWeight = 3 * t * t * (t - 1);
													float cWeight = 3 * t * (t - 1) * (t - 1);
													float dWeight = (t - 1) * (t - 1) * (t - 1);

													return new Point(
														(ax * aWeight) + (bx * bWeight) + (cx * cWeight) + (dx * dWeight),
														(ay * aWeight) + (by * bWeight) + (cy * cWeight) + (dy * dWeight));
												}

												//declare interpolation count
												const int interpolations = 5;

												//declare interpolation step
												float interpolationStep = 1.0f / (1 + interpolations);

												//get first point
												Point previousPoint = new Point(ax, ay);

												//generate interpolated segments
												float position = 0;
												for (int i = 0; i < interpolations; i++)
												{
													//increase position
													position += interpolationStep;

													//get next point
													Point nextPoint = bezier(position);

													//add line to list
													subpathLines.Add(new Line(previousPoint, nextPoint));

													//update previous point
													previousPoint = nextPoint;
												}

												//add last line
												subpathLines.Add(new Line(previousPoint, new Point(dx, dy)));

												break;

											default: //unknown
												throw new InvalidDataException($"Unknown segment type with {basePoints.Count} element(s) encountered.");
										}
									}

									//add closing segment if necessary
									if (subpath.IsClosed())
										subpathLines.Add(
											new Line(
												subpathLines.Last().End,
												subpathLines.First().Start));

									//add subpath lines to list
									lines.AddRange(subpathLines);
								}

							//string colorConvert(float[] array)
							//{
							//	string output = "";
							//	foreach (float val in array)
							//		output += $"[{val}]";
							//	return output;
							//}
							//Console.WriteLine($"First point [[{lines.First().Start.X}][{lines.First().Start.Y}]] Last point [[{lines.Last().Start.X}][{lines.Last().Start.Y}]] Count [{lines.Count}] Fill color [{colorConvert(pathData.GetFillColor().GetColorValue())}] Stroke color [{colorConvert(pathData.GetStrokeColor().GetColorValue())}] Rule [{pathData.GetRule()}] Operation [{pathData.GetOperation()}]");

							//add path if any lines were created
							if (lines.Count > 0)
								CurrentPage.AddGraphic(
									new PdfPath(
										CurrentPage,
										lines,
										pathTransformMatrix));
						}
					}

					break;
			}
		}

		/// <summary>
		/// Dummy method, implements abstract method required by the <seealso cref="ITextExtractionStrategy" /> interface. 
		/// </summary>
		/// <returns>An empty string.</returns>
		public string GetResultantText()
		{
			return "";
		}

		/// <summary>
		/// Returns array containing all supported event types.
		/// </summary>
		/// <returns />
		public ICollection<EventType> GetSupportedEvents()
		{
			return new EventType[] {
				EventType.BEGIN_TEXT,
				EventType.RENDER_TEXT,
				EventType.RENDER_PATH,
				EventType.RENDER_IMAGE,
				};
		}

		#endregion

		/// <summary>
		/// Extracts the PDF document data into the document.
		/// </summary>
		public void ExtractDocument()
		{
			//open file
			using (PdfReader reader = new PdfReader(FilePath))
			{
				//open document
				using (iText.Kernel.Pdf.PdfDocument document = new iText.Kernel.Pdf.PdfDocument(reader))
				{
#warning DEBUG TOGGLE!
					//DEBUG: if > 0, only gets specified page!
					int DEBUG_pageNumber = 0;//17;//13;
					//51, 65, 84, 88, 90, 94, 95, 97, 104, 108, 115, 116, 117, 118, 128, 130, 141, 143, 144, 152, 154, 156, 159, 160, 163, 173, 175, 176, 184, 196, 208, 217, 220, 240, 241, 243!

					//parse pages
					for (int i = 1; i <= document.GetNumberOfPages(); i++)
					{
						//DEBUG: set index
						if (DEBUG_pageNumber > 0) i = DEBUG_pageNumber;

						Console.Write($"{i - 1}...");

						//get page
						iText.Kernel.Pdf.PdfPage page = document.GetPage(i);

						//declare rectangles for page boxes
						Rectangle mediaBox = null;
						Rectangle cropBox = null;
						Rectangle trimBox = null;
						Rectangle artBox = null;
						Rectangle bleedBox = null;

						//get media box
						var mediaRect = page.GetMediaBox();
						if (mediaRect != null)
							mediaBox = new Rectangle(
								new Point(
									mediaRect.GetLeft(),
									mediaRect.GetBottom()),
								new Point(
									mediaRect.GetRight(),
									mediaRect.GetTop()));

						//get crop box
						var cropRect = page.GetCropBox();
						if (cropRect != null)
							cropBox = new Rectangle(
								new Point(
									cropRect.GetLeft(),
									cropRect.GetBottom()),
								new Point(
									cropRect.GetRight(),
									cropRect.GetTop()));

						//get trim box
						var trimRect = page.GetTrimBox();
						if (trimRect != null)
							trimBox = new Rectangle(
								new Point(
									trimRect.GetLeft(),
									trimRect.GetBottom()),
								new Point(
									trimRect.GetRight(),
									trimRect.GetTop()));

						//get art box
						var artRect = page.GetArtBox();
						if (artRect != null)
							artBox = new Rectangle(
								new Point(
									artRect.GetLeft(),
									artRect.GetBottom()),
								new Point(
									artRect.GetRight(),
									artRect.GetTop()));

						//get bleed box
						var bleedRect = page.GetBleedBox();
						if (bleedRect != null)
							bleedBox = new Rectangle(
								new Point(
									bleedRect.GetLeft(),
									bleedRect.GetBottom()),
								new Point(
									bleedRect.GetRight(),
									bleedRect.GetTop()));

						//create page
						CurrentPage = new DataTypes.PdfPage(
							DocumentInstance,
							i.ToString(),//document.GetPageLabels()[i],
#warning TODO: GET ACTUAL LABELS!
							mediaBox,
							cropBox,
							trimBox,
							artBox,
							bleedBox);

						//add page to document
						DocumentInstance.AddPage(CurrentPage);

						//parse page elements
						PdfTextExtractor.GetTextFromPage(page, this);

						//DEBUG: end loop.
						if (DEBUG_pageNumber > 0) break;
					}		
				}
			}
		}

		/// <summary>
		/// Verifies whether the provided character conforms to the predicted boundaries of the provided text line.
		/// </summary>
		/// <param name="currentLine">Line which is to be continued.</param>
		/// <param name="nextCharacter">Continuing character.</param>
		/// <returns></returns>
		private bool IsCharacterWithinLine(
			PdfTextLine currentLine,
			PdfTextCharacter nextCharacter)
		{
			//check if empty line
			if (currentLine.Segments.Count == 0 ||
				currentLine.Segments.Last().Characters.Count == 0)
				return true;

			//get last character of the line
			var lastCharacter = currentLine.Segments.Last().Characters.Last();

			//check vertical ranges
			if ((nextCharacter.BoundingBox.VerticalRange.DoesIntersect(lastCharacter.BoundingBox.Center.Y) |
				lastCharacter.BoundingBox.VerticalRange.DoesIntersect(nextCharacter.BoundingBox.Center.Y)) == Range.IntersectData.BodyIntersect)
				return true;

			return false;
			
#warning TODO: REEVALUATE USEFULNESS!

			//get last character of line
			//PdfTextCharacter lastCharacter = currentLine.Segments.Last().Characters.Last();

			//calculate reference points
			Point lastCharacterReference = new Point(
				(lastCharacter.AscentLine.End.X + lastCharacter.DescentLine.End.X) / 2,
				(lastCharacter.AscentLine.End.Y + lastCharacter.DescentLine.End.Y) / 2);
			Point nextCharacterReference = new Point(
				(nextCharacter.AscentLine.Start.X + nextCharacter.DescentLine.Start.X) / 2,
				(nextCharacter.AscentLine.Start.Y + nextCharacter.DescentLine.Start.Y) / 2);

			//calculate max distance
			float maxDistance = 1.1f * Math.Max(
				lastCharacter.AscentLine.End.DistanceFrom(lastCharacter.DescentLine.End),
				nextCharacter.AscentLine.Start.DistanceFrom(nextCharacter.DescentLine.Start));
			//if (lastCharacter.Text.HasNonWhitespaceCharacters ^ nextCharacter.Text.HasNonWhitespaceCharacters)
			//	maxDistance *= 2;

			return lastCharacterReference.DistanceFrom(nextCharacterReference) <= maxDistance;
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="documentInstance">Document instance into which the data is to be extracted.</param>
		/// <param name="filePath">PDF file path.</param>
		public PdfDataExtractor(
			DataTypes.PdfDocument documentInstance,
			string filePath)
		{
			//try to open file
			using (var file = File.OpenRead(filePath))
			{
				//store file path
				FilePath = filePath;
			}

			//store document instance
			DocumentInstance = documentInstance;
		}

		#endregion
	}
}
