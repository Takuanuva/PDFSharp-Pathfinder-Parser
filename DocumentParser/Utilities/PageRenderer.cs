using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentParser.DataTypes;
using DocumentParser.Utilities.Geometry;

namespace DocumentParser.Utilities.DataRendering
{
	/// <summary>
	/// Page rendering adapter, draws specified page to the provided graphics adapter. 
	/// </summary>
	public class PageRenderer
	{
		#region Properties

		#region Private storage fields

		/// <summary>
		/// Page to be rendered.
		/// </summary>
		private PdfPage Page;

		/// <summary>
		/// Permitted output area.
		/// </summary>
		private System.Drawing.Rectangle OutputRectangle;

		/// <summary>
		/// Output graphics adapter.
		/// </summary>
		private System.Drawing.Graphics OutputGraphics;

		/// <summary>
		/// Offset value for X component of coordinates.
		/// </summary>
		private float OffsetX;

		/// <summary>
		/// Offset value for Y component of coordinates.
		/// </summary>
		private float OffsetY;

		/// <summary>
		/// Length scaling multiplier.
		/// </summary>
		private float ScaleMultiplier;

		#endregion

		#endregion

		#region Methods

		#region Coordinate conversion

		/// <summary>
		/// Converts the X component of a page coordinate.
		/// </summary>
		/// <param name="x" />
		/// <returns />
		private float ConvertXCoord(float x)
		{
			return OffsetX + (x * ScaleMultiplier);
		}

		/// <summary>
		/// Converts the Y component of a page coordinate.
		/// </summary>
		/// <param name="y" />
		/// <returns />
		private float ConvertYCoord(float y)
		{
			return OffsetY - (y * ScaleMultiplier);
		}

		/// <summary>
		/// Converts the length within the page coordinate system.
		/// </summary>
		/// <param name="l" />
		/// <returns />
		private float ConvertLength(float l)
		{
			return l * ScaleMultiplier;
		}

		#region COPIED OVER HSV CODE

		/// <summary>
		/// Converts HSV to RGB color.
		/// </summary>
		/// <param name="h"></param>
		/// <param name="s"></param>
		/// <param name="v"></param>
		/// <returns></returns>
		public static System.Drawing.Color HsvaToArgb(double h, double s, double v, double a = 1)
		{
			int hi = (int)Math.Floor(h / 60.0) % 6;
			double f = (h / 60.0) - Math.Floor(h / 60.0);

			double p = v * (1.0 - s);
			double q = v * (1.0 - (f * s));
			double t = v * (1.0 - ((1.0 - f) * s));

			System.Drawing.Color ret;

			switch (hi)
			{
				case 0:
					ret = GetArgb(a, v, t, p);
					break;
				case 1:
					ret = GetArgb(a, q, v, p);
					break;
				case 2:
					ret = GetArgb(a, p, v, t);
					break;
				case 3:
					ret = GetArgb(a, p, q, v);
					break;
				case 4:
					ret = GetArgb(a, t, p, v);
					break;
				case 5:
					ret = GetArgb(a, v, p, q);
					break;
				default:
					ret = System.Drawing.Color.FromArgb(0xFF, 0x00, 0x00, 0x00);
					break;
			}
			return ret;
		}

		public static System.Drawing.Color GetArgb(double a, double r, double g, double b)
		{
			return System.Drawing.Color.FromArgb((byte)(a * 255.0), (byte)(r * 255.0), (byte)(g * 255.0), (byte)(b * 255.0));
		}

		#endregion

		#endregion

		/// <summary>
		/// Renders the contents of the page to the output graphics adapter.
		/// </summary>
		public void RenderPage()
		{
			//clear output
			OutputGraphics.Clear(System.Drawing.Color.White);

			//get page render box
			Rectangle pageRenderBox = Page.MediaBox;
#warning TODO: EXPAND?

			//check if valid box
			if (float.IsInfinity(pageRenderBox.Width) ||
				float.IsInfinity(pageRenderBox.Height) ||
				float.IsNaN(pageRenderBox.Width) ||
				float.IsNaN(pageRenderBox.Height))
				throw new ArgumentException("Provided render box does not define a finite rectangle!", nameof(pageRenderBox));

			//calculate scale multiplier
			ScaleMultiplier =
				Math.Min(
					OutputRectangle.Width / pageRenderBox.Width,
					OutputRectangle.Height / pageRenderBox.Height);

			//calculate offsets
			OffsetX = OutputRectangle.Left + ((OutputRectangle.Width - (pageRenderBox.Width * ScaleMultiplier)) / 2) - (pageRenderBox.LeftX * ScaleMultiplier);
			OffsetY = OutputRectangle.Top + ((OutputRectangle.Height + (pageRenderBox.Height * ScaleMultiplier)) / 2) - (pageRenderBox.BottomY * ScaleMultiplier);

			//create pens
			System.Drawing.Brush textBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(unchecked((int)0x8F7F00FF)));
			System.Drawing.Brush characterBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(unchecked((int)0x36FF0000)));
			System.Drawing.Brush characterWhitespaceBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(unchecked((int)0x3600FFFF)));
			System.Drawing.Pen characterPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(unchecked((int)0x16FF0000)), 1);
			Dictionary<PdfTextCharacter.Style, System.Drawing.Pen> segmentBoundingBoxPens = new Dictionary<PdfTextCharacter.Style, System.Drawing.Pen>();
			Dictionary<PdfTextCharacter.Style, System.Drawing.Pen> segmentBaselinePens = new Dictionary<PdfTextCharacter.Style, System.Drawing.Pen>();
			System.Drawing.Pen linePen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(unchecked((int)0x1F0000FF)), 3);
			System.Drawing.Pen pagePen = new System.Drawing.Pen(System.Drawing.Color.Orange);
			/*
			var textTypeColors = new Dictionary<TextElement.TextType.Designation, System.Drawing.Color>();
			{
				textTypeColors[TextElement.TextType.Designation.Unknown]				= HsvaToArgb(300, 1, 1, 0.5);
				textTypeColors[TextElement.TextType.Designation.MainText]				= HsvaToArgb(0, 1, 1, 0.5);
				textTypeColors[TextElement.TextType.Designation.NonJustifiedMainText]	= HsvaToArgb(0, 0.5, 1, 0.5);
				textTypeColors[TextElement.TextType.Designation.Title]					= HsvaToArgb(60, 1, 0.85, 0.5);
				textTypeColors[TextElement.TextType.Designation.Sidebar]				= HsvaToArgb(120, 1, 1, 0.5);
				textTypeColors[TextElement.TextType.Designation.TableHeader]			= HsvaToArgb(240, 0.85, 0.5, 0.5);
				textTypeColors[TextElement.TextType.Designation.TableBody]				= HsvaToArgb(240, 1, 1, 0.5);
				textTypeColors[TextElement.TextType.Designation.Utility]				= HsvaToArgb(0, 0, 0.65, 0.5);
			}
			foreach (var kv in Page.PdfTextCharacter.StyleOccurences)
			{
				//create bounding box pen
				System.Drawing.Pen boundingBoxPen = new System.Drawing.Pen(
					//TextStyle.knownStyleDict.ContainsKey(kv.Key.GetHashCode()) ? System.Drawing.Color.LightGray : System.Drawing.Color.OrangeRed,
					System.Drawing.Color.Black,//textTypeColors[kv.Key.HighestConfidenceType],
					2);
				boundingBoxPen.Alignment = System.Drawing.Drawing2D.PenAlignment.Center;

				//create baseline pen
				System.Drawing.Pen baselinePen = new System.Drawing.Pen(
					System.Drawing.Color.FromArgb(kv.Key.FillColor),
					kv.Key.IsBold ? 4 : 2);
				if (kv.Key.IsItalic) baselinePen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
				baselinePen.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset;

				//add pens to dictionary
				segmentBoundingBoxPens.Add(kv.Key, boundingBoxPen);
				segmentBaselinePens.Add(kv.Key, baselinePen);
			}
			*/

			//set graphics crop rectangle
			var oldClip = OutputGraphics.Clip;
			//OutputGraphics.Clip = new System.Drawing.Region(new System.Drawing.RectangleF(
			//	ConvertXCoord(pageRenderBox.LeftX),
			//	ConvertYCoord(pageRenderBox.TopY),
			//	ConvertLength(pageRenderBox.Width),
			//	ConvertLength(pageRenderBox.Height)));

			//define alignment line pens
			System.Drawing.Pen leftAlignmentPen = new System.Drawing.Pen(System.Drawing.Color.DarkCyan, 1);
			System.Drawing.Pen centerAlignmentPen = new System.Drawing.Pen(System.Drawing.Color.DarkMagenta, 5);
			System.Drawing.Pen rightAlignmentPen = new System.Drawing.Pen(System.Drawing.Color.GreenYellow, 5);
			/*
			void drawAlignments(TextBlock block)
			{
				void drawAlignment(
					IReadOnlyList<Tuple<float, float>> alignments,
					System.Drawing.Pen renderPen)
				{
					float minWeight = float.PositiveInfinity;
					float maxWeight = float.NegativeInfinity;
					foreach (var alignment in alignments)
					{
						if (alignment.Item2 < minWeight)
							minWeight = alignment.Item2;
						if (alignment.Item2 > maxWeight)
							maxWeight = alignment.Item2;
					}
					float delta = maxWeight - minWeight;
					foreach (var alignment in alignments)
					{
						RenderLine(
							renderPen,
							new Line(
								new Point(
									alignment.Item1,
									block.BoundingBox.BottomY + (block.BoundingBox.Height * (alignment.Item2 / maxWeight))),
								new Point(
									alignment.Item1,
									block.BoundingBox.BottomY)));
					}
				}
				//drawAlignment(block.LeftEdgeAlignments, leftAlignmentPen);
				//drawAlignment(block.CenterAlignments, centerAlignmentPen);
				//drawAlignment(block.RightEdgeAlignments, rightAlignmentPen);
			}

			*/
			
			/*

			//render main text markers
			System.Drawing.SolidBrush mainTextBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0x7FFF0000));
			//foreach (TextElement element in Page.MainText.Elements)
			//	RenderFilledPolygon(mainTextBrush, element.BoundingBox);
			drawAlignments(Page.MainText);

			//render sidebar markers
			System.Drawing.SolidBrush sidebarBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0x7F00FF00));
			foreach (TextBlock sidebar in Page.Sidebars)
			{
				//foreach (TextElement element in sidebar.Elements)
				//	RenderFilledPolygon(sidebarBrush, element.BoundingBox);
				drawAlignments(sidebar);
			}

			*/

			//render table markers
			//System.Drawing.SolidBrush tableBrush = new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0x7F0000FF));
			//foreach (TextTable table in Page.Tables)
			//{
			//	if (table.Header != null)
			//		RenderFilledPolygon(tableBrush, table.Header.BoundingBox);
			//	foreach (TextTable.Row row in table.Rows)
			//		foreach (TextTable.Cell cell in row.Cells)
			//			if (cell.Contents.Text.HasNonWhitespaceCharacters)
			//				RenderFilledPolygon(tableBrush, cell.Contents.BoundingBox);
			//	if (table.Footer != null)
			//		RenderFilledPolygon(tableBrush, table.Footer.BoundingBox);
			//}

			//define table rendering constants
			const float StartSaturation = 1.0f;
			const float EndSaturation = 0.3f;
			const float SaturationChange = EndSaturation - StartSaturation;
			const float StartLightness = 1.0f;
			const float EndLightness = 0.3f;
			const float LightnessChange = EndLightness - StartLightness;

			/*

			//calculate hue multiplier
			float hueMultiplier = 360.0f / Page.Tables.Count;
			
			//render tables
			for (int tableIndex = 0; tableIndex < Page.Tables.Count; tableIndex++)
			{
				//get table
				TextTable table = Page.Tables[tableIndex];

				//generate hue
				float hue = tableIndex * hueMultiplier;

				//calculate saturation multiplier
				//float saturationMultiplier = SaturationChange / Math.Max(1, table.Rows.Count - 1);
				//
				////iterate over rows
				//for (int rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
				//{
				//	//get row
				//	TextTable.Row row = table.Rows[rowIndex];
				//
				//	//generate saturation
				//	float saturation = StartSaturation + (rowIndex * saturationMultiplier);
				//
				//	//calculate lightness multiplier
				//	float lightnessMultiplier = LightnessChange / Math.Max(1, row.Cells.Count - 1);
				//
				//	//iterate over cells
				//	for (int cellIndex = 0; cellIndex < row.Cells.Count; cellIndex++)
				//	{
				//		//generate lightness
				//		float lightness = StartLightness + (cellIndex * lightnessMultiplier);
				//
				//		//generate pen
				//		System.Drawing.Pen pen = new System.Drawing.Pen(
				//			HsvToRgb(hue, saturation, lightness),
				//			3);
				//		pen.Alignment = System.Drawing.Drawing2D.PenAlignment.Inset;
				//
				//		//draw cell bounding box
				//		RenderPolygon(
				//			pen,
				//			row.Cells[cellIndex].BoundingBox);
				//	}
				//}
			}

			*/

			//iterate over lines in page
			//foreach (PdfTextLine line in Page.Lines)
			//{
			//	RenderPolygon(
			//		new System.Drawing.Pen(
			//			System.Drawing.Color.Orange,//textTypeColors[line.TypeProbabilities.HighestProbabilityType],
			//			2),
			//		line.BoundingBox);
			//
			//	//iterate over segments in group
			//	foreach (PdfTextSegment segment in line.Segments)
			//	{
			//		//render segment
			//		//if (segment.Style.IntersectsPageCenter)
			//		//	RenderFilledPolygon(
			//		//	segmentBoundingBoxPens[segment.Style].Brush,
			//		//	segment.BoundingBox);
			//		//else
			//
			//		//RenderPolygon(
			//		//	leftAlignmentPen,
			//		//	segment.BoundingBox);
			//		//RenderPolygon(
			//		//	segmentBoundingBoxPens[segment.Style],
			//		//	segment.BoundingBox);
			//		//RenderPolyline(
			//		//	segmentBaselinePens[segment.Style],
			//		//	segment.Baseline);
			//
			//		//get segment pen
			//		var segmentPen = new System.Drawing.Pen(
			//			System.Drawing.Color.BlueViolet,//textTypeColors[segment.TypeProbabilities.HighestProbabilityType],
			//			2);
			//
			//		//iterate over characters in segment
			//		foreach (PdfTextCharacter character in segment.Characters)
			//		{
			//			//render character
			//			RenderText(
			//				segmentPen.Brush,//segmentBoundingBoxPens[segment.Style].Brush, //textBrush,
			//				null,
			//				character.TextString,
			//				character.AscentLine.Start,
			//				character.AscentLine.Start.DistanceFrom(character.Baseline.Start) * 0.75f);
			//		}
			//
			//		//render style ID
			//		//if (!TextStyle.knownStyleDict.ContainsKey(segment.Style.GetHashCode()))
			//		{
			//			//RenderFilledPolygon(
			//			//	new System.Drawing.SolidBrush(System.Drawing.Color.Black),
			//			//	segment.BoundingBox);
			//			//RenderText(
			//			//	new System.Drawing.SolidBrush(System.Drawing.Color.Black),
			//			//	new System.Drawing.Pen(System.Drawing.Color.White, 5),
			//			//	segment.CharacterStyle.GetHashCode().ToString("X8"),
			//			//	segment.BoundingBox.UpperLeft,
			//			//	15);
			//		}
			//	}
			//}

			//draw line relations
			//foreach (var relation in Page.LineNeighboringRelations)
			//{
			//	if (relation.CanBeContinuous())
			//		RenderLine(
			//			new System.Drawing.Pen(System.Drawing.Color.FromArgb(0x1fff00ff), 2),
			//			new Line(
			//				relation.LineA.BoundingBox.Center,
			//				relation.LineB.BoundingBox.Center));
			//}

			//draw page characters
			{
				//declare character pens/brushes
				System.Drawing.Pen charBoxPen = new System.Drawing.Pen(System.Drawing.Color.Black, 1);
				System.Drawing.Pen charStrokePen = new System.Drawing.Pen(System.Drawing.Color.LightSlateGray, 2);
				System.Drawing.Brush charFillBrush = new System.Drawing.SolidBrush(System.Drawing.Color.DarkSlateGray);

				//render characters
				foreach (var character in Page.Characters)
				{
					//render bounding box
					RenderLine(
						charBoxPen,
						new Line(
							character.BoundingBox.UpperLeft,
							character.BoundingBox.LowerRight));

					//render character
					RenderText(
						charFillBrush,
						charStrokePen,
						character.TextString,
						character.AscentLine.Start,
						character.AscentLine.Start.DistanceFrom(character.Baseline.Start) * 0.75f);
				}
			}

			//draw page overlaping text rectangles
			//foreach (Rectangle rect in Page.ContinuousTextBounds)
			//	RenderPolygon(
			//		new System.Drawing.Pen(
			//			System.Drawing.Color.Aqua,
			//			2),
			//		rect);
			
			
			
			//RenderPolygon(
			//	new System.Drawing.Pen(
			//		System.Drawing.Color.CadetBlue,
			//		5),
			//	Page.MostLikelyPageBounds);
			//RenderPolygon(
			//	new System.Drawing.Pen(
			//		System.Drawing.Color.DarkGoldenrod,
			//		5),
			//	Page.SourceDocument.AveragePageBounds);


			//draw read events
			System.Drawing.Color generateReadEventColor(PdfPage.ContentSection.LinearReadEvent.EventFlags flags)
			{
				uint color = 0xFFFFFFFF;
				if ((flags & PdfPage.ContentSection.LinearReadEvent.EventFlags.Character) == 0)
					color = 0xFFFF00FF;
				else
					color = 0;
				//if ((flags & PdfPage.ContentSection.LinearReadEvent.EventFlags.WhitespaceCharacter) != PdfPage.ContentSection.LinearReadEvent.EventFlags.NoEvents)
				//	color &= ~((uint)1 << (7 + 16));
				//if ((flags & PdfPage.ContentSection.LinearReadEvent.EventFlags.NonWhitespaceCharacter) != PdfPage.ContentSection.LinearReadEvent.EventFlags.NoEvents)
				//	color &= ~((uint)1 << (6 + 16));
				//if ((flags & PdfPage.ContentSection.LinearReadEvent.EventFlags.CharacterOverlap) != PdfPage.ContentSection.LinearReadEvent.EventFlags.NoEvents)
				//	color &= ~((uint)1 << (5 + 16));
				//if ((flags & PdfPage.ContentSection.LinearReadEvent.EventFlags.UnexpectedSpacing) != PdfPage.ContentSection.LinearReadEvent.EventFlags.NoEvents)
				//	color &= ~((uint)1 << (7 + 8));
				//if ((flags & PdfPage.ContentSection.LinearReadEvent.EventFlags.AreaEdge) != PdfPage.ContentSection.LinearReadEvent.EventFlags.NoEvents)
				//	color &= ~((uint)1 << (6 + 8));
				////if ((flags & PdfPage.ContentSection.LinearReadEvent.EventFlags.) != PdfPage.ContentSection.LinearReadEvent.EventFlags.NoEvents)
				////	color &= ~((uint)1 << (5 + 8));
				//if ((flags & PdfPage.ContentSection.LinearReadEvent.EventFlags.FontHeightMismatch) != PdfPage.ContentSection.LinearReadEvent.EventFlags.NoEvents)
				//	color &= ~((uint)1 << (7 + 0));
				//if ((flags & PdfPage.ContentSection.LinearReadEvent.EventFlags.FontStyleFamilyMismatch) != PdfPage.ContentSection.LinearReadEvent.EventFlags.NoEvents)
				//	color &= ~((uint)1 << (6 + 0));
				//if ((flags & PdfPage.ContentSection.LinearReadEvent.EventFlags.GraphicsCollision) != PdfPage.ContentSection.LinearReadEvent.EventFlags.NoEvents)
				//	color &= ~((uint)1 << (5 + 0));

				return System.Drawing.Color.FromArgb(unchecked((int)color));
			}
			//foreach (var kv in Page.RootSection.AreaCharacterLineReadEventsVertical)
			//	foreach (var readEvent in kv.Value)
			//		RenderLine(
			//			new System.Drawing.Pen(
			//				generateReadEventColor(readEvent.Flags),
			//				1),
			//			readEvent.LineSection);
			//foreach (var subsection in Page.RootSection.Subsections)
			//	foreach (var kv in subsection.AreaCharacterLineReadEventsVertical)
			//		foreach (var readEvent in kv.Value)
			//			RenderLine(
			//				new System.Drawing.Pen(
			//					generateReadEventColor(readEvent.Flags),
			//					1),
			//				readEvent.LineSection);

			//foreach (var line in Page.potentialSlices)
			//RenderLine(
			//	new System.Drawing.Pen(System.Drawing.Color.Black, 3),//.FromArgb(0x3F000000), 1),
			//	line);



			void renderContentSection(PdfPage.ContentSection section)
			{
				System.Drawing.Color color = System.Drawing.Color.Black;
				if ((section.SectionContentType & PdfPage.ContentSection.ContentTypeFlags.ContentIsHorizontal) != PdfPage.ContentSection.ContentTypeFlags.NoFlags)
					color = System.Drawing.Color.Red;
				else if ((section.SectionContentType & PdfPage.ContentSection.ContentTypeFlags.ContentIsVertical) != PdfPage.ContentSection.ContentTypeFlags.NoFlags)
					color = System.Drawing.Color.Green;
				else if ((section.SectionContentType & PdfPage.ContentSection.ContentTypeFlags.ContentIsTable) != PdfPage.ContentSection.ContentTypeFlags.NoFlags)
					color = System.Drawing.Color.Blue;
				System.Drawing.Pen sectionPen = new System.Drawing.Pen(color, 3);
				//foreach (var line in section.failedSlices)
				//{
				//	RenderLine(new System.Drawing.Pen(System.Drawing.Color.Black, 2), line);
				//	RenderText(
				//		new System.Drawing.SolidBrush(System.Drawing.Color.Black),
				//		new System.Drawing.Pen(System.Drawing.Color.Beige, 3),
				//		$"[{ section.failedSliceWeights[line] }]",
				//		line.Start,
				//		10);
				//}
				foreach (var subsection in section.Subsections)
					renderContentSection(subsection);
				foreach (var line in section.slices)
					RenderLine(sectionPen, line);
				//if (section.slices.Count > 5)
				//{
				//	foreach (var line in section.slices)
				//	{
				//		//RenderLine(new System.Drawing.Pen(System.Drawing.Color.Black, 3), line);
				//		float mult =
				//			((((line.Start.X + line.End.X) / 2) - section.Area.LeftX) + (((line.Start.Y + line.End.Y) / 2) - section.Area.BottomY)) /
				//			(section.Area.Width + section.Area.Height);
				//		if (section.failedSliceWeights.ContainsKey(line))
				//			RenderText(
				//				new System.Drawing.SolidBrush(System.Drawing.Color.Black),
				//				new System.Drawing.Pen(System.Drawing.Color.Beige, 3),
				//				$"[{ section.failedSliceWeights[line] }]",
				//				new Point(
				//					line.Start.X + ((line.End.X - line.Start.X) * mult),
				//					line.Start.Y + ((line.End.Y - line.Start.Y) * mult)),
				//				1f);
				//	}
				//}
			}
			//renderContentSection(Page.RootSection);



			//foreach (var line in Page.horizontalSlices)
			//	RenderLine(
			//		new System.Drawing.Pen(System.Drawing.Color.DarkCyan, 7),
			//		line);
			//foreach (var line in Page.verticalSlices)
			//	RenderLine(
			//		new System.Drawing.Pen(System.Drawing.Color.DarkMagenta, 7),
			//		line);
			//foreach (var line in Page.tableSlices)
			//	RenderLine(
			//		new System.Drawing.Pen(System.Drawing.Color.DarkKhaki, 7),
			//		line);

			//foreach (var line in Page.RootSection.failedSlices)
			//{
			//	RenderLine(new System.Drawing.Pen(System.Drawing.Color.Black, 2), line);
			//	RenderText(
			//		new System.Drawing.SolidBrush(System.Drawing.Color.Black),
			//		new System.Drawing.Pen(System.Drawing.Color.Beige, 3),
			//		$"[{ Page.RootSection.failedSliceCorrectness[line] }]",
			//		line.Start,
			//		3);
			//}

			//foreach (var kv in Page.RootSection.AreaCharacterLineReadEventsHorizontal)
			//	foreach (var readEvent in kv.Value)
			//		RenderLine(
			//			new System.Drawing.Pen(
			//				generateReadEventColor(readEvent.Flags),
			//				7),
			//			new Line(
			//				new Point(
			//					readEvent.LineSection.Start.X - 1,
			//					readEvent.LineSection.Start.Y),
			//				new Point(
			//					readEvent.LineSection.End.X + 1,
			//					readEvent.LineSection.End.Y)));



			//declare rendering constants
			const float LineLightnessMin = 0.7f;
			const float LineLightnessMax = 1.0f;
			const int LineLightnessSteps = 4;
			const float SegmentSaturationMin = 0.5f;
			const float SegmentSaturationMax = 1.0f;
			const int SegmentSaturationSteps = 4;

			//draw initial blocks
			foreach (var block in Page.InitialBlocks)
			{
				//get block's hue
				float blockHue = block.GetHashCode() % 360;

				//iterate over lines in block
				for (int lineIndex = 0; lineIndex < block.Lines.Count; lineIndex++)
				{
					//get line
					var line = block.Lines[lineIndex];

					//calculate line lightness
					float lineLightness = LineLightnessMin + ((LineLightnessMax - LineLightnessMin) * ((float)(lineIndex % (LineLightnessSteps - 1)) / LineLightnessSteps));

					//iterate over segments in line
					for (int segmentIndex = 0; segmentIndex < line.Segments.Count; segmentIndex++)
					{
						//get segment
						var segment = line.Segments[segmentIndex];

						//calculate segment saturation
						float segmentSaturation = SegmentSaturationMin + ((SegmentSaturationMax - SegmentSaturationMin) * ((float)(segmentIndex % (SegmentSaturationSteps - 1)) / SegmentSaturationSteps));

						//draw segment bounding box
						RenderFilledPolygon(
							new System.Drawing.SolidBrush(
								HsvaToArgb(
									blockHue,
									segmentSaturation,
									lineLightness)),
							segment.BoundingBox);


						//iterate over characters in segment
						foreach (var character in segment.Characters)
						{
							//render character
							RenderText(
								new System.Drawing.SolidBrush(System.Drawing.Color.Black),
								null,
								character.TextString,
								character.AscentLine.Start,
								character.AscentLine.Start.DistanceFrom(character.Baseline.Start) * 0.75f);
						}
					}

					//draw line bounding box
					System.Drawing.Pen lineBoxPen =
						new System.Drawing.Pen(
							HsvaToArgb(
								blockHue,
								SegmentSaturationMin / 2,
								lineLightness),
							1);
					RenderPolygon(
							lineBoxPen,
							line.BoundingBox);

					RenderPolygon(
							lineBoxPen,
							new Polygon(new List<Point>()
							{
								line.BoundingBox.UpperLeft,
								line.BoundingBox.LowerRight,
								line.BoundingBox.LowerLeft,
								line.BoundingBox.UpperRight
							}));
				}

				//render block bounding box
				RenderPolygon(
					new System.Drawing.Pen(
						HsvaToArgb(
							blockHue,
							1,
							1),
						2),
					block.BoundingBox);

				//render block baseline
				RenderPolyline(
					new System.Drawing.Pen(System.Drawing.Color.Black, 2),
					block.Baseline);

				//render block alignments
				var alignmentPen =
					new System.Drawing.Pen(System.Drawing.Color.Black, 3);
				if ((block.Alignment & PdfTextBlock.TextAlignment.LeftAligned) != PdfTextBlock.TextAlignment.NoAlignment)
					RenderLine(alignmentPen, new Line(
						block.BoundingBox.UpperLeft,
						block.BoundingBox.LowerLeft));
				if ((block.Alignment & PdfTextBlock.TextAlignment.CenterAligned) != PdfTextBlock.TextAlignment.NoAlignment)
					RenderLine(alignmentPen, new Line(
						new Point(
							block.BoundingBox.Center.X,
							block.BoundingBox.TopY),
						new Point(
							block.BoundingBox.Center.X,
							block.BoundingBox.BottomY)));
				if ((block.Alignment & PdfTextBlock.TextAlignment.RightAligned) != PdfTextBlock.TextAlignment.NoAlignment)
					RenderLine(alignmentPen, new Line(
						block.BoundingBox.UpperRight,
						block.BoundingBox.LowerRight));
				if ((block.Alignment & PdfTextBlock.TextAlignment.Justified) != PdfTextBlock.TextAlignment.NoAlignment)
					RenderLine(alignmentPen, new Line(
						block.BoundingBox.UpperLeft,
						block.BoundingBox.LowerRight));
				
				//render block ID
				//RenderText(
				//	new System.Drawing.SolidBrush(System.Drawing.Color.Black),
				//	new System.Drawing.Pen(System.Drawing.Color.White, 2),
				//	block.GetHashCode().ToString("X8"),
				//	block.Baseline.Start,
				//	10);
			}

			//draw tables
			foreach (var table in Page.Tables)
			{
				//draw cell bounding boxes
				foreach (var cell in table.Cells)
				{
					RenderPolygon(
						new System.Drawing.Pen(System.Drawing.Color.DarkMagenta, 2),
						cell.BoundingBox);
					if (cell.Contents != null)
					{
						RenderFilledPolygon(
							new System.Drawing.SolidBrush(System.Drawing.Color.SlateGray),
							cell.BoundingBox);
						RenderPolygon(
							new System.Drawing.Pen(System.Drawing.Color.BlueViolet, 1),
							cell.Contents.BoundingBox);

						foreach (var line in cell.Contents.Lines)
							foreach (var segment in line.Segments)
							{
								//RenderPolygon(
								//	new System.Drawing.Pen(System.Drawing.Color.Lime, 1),
								//	segment.BoundingBox);

								foreach (var character in segment.Characters)
								{
									RenderPolygon(
										new System.Drawing.Pen(System.Drawing.Color.Azure, 1),
										character.BoundingBox);

									//render character
									RenderText(
										new System.Drawing.SolidBrush(System.Drawing.Color.Black),
										new System.Drawing.Pen(System.Drawing.Color.Cyan, 2),
										character.TextString,
										character.AscentLine.Start,
										character.AscentLine.Start.DistanceFrom(character.Baseline.Start) * 0.75f);
								}
							}
					}
				}

				//draw row/column bounds
				foreach (float val in table.ColumnBounds)
					RenderLine(
						new System.Drawing.Pen(System.Drawing.Color.Black, 1),
						new Line(
							new Point(
								val,
								table.RowBounds.First()),
							new Point(
								val,
								table.RowBounds.Last())));
				foreach (float val in table.RowBounds)
					RenderLine(
						new System.Drawing.Pen(System.Drawing.Color.Black, 1),
						new Line(
							new Point(
								table.ColumnBounds.First(),
								val),
							new Point(
								table.ColumnBounds.Last(),
								val)));

				//draw title bounding box
				if (table.HasTitle)
					RenderPolygon(
						new System.Drawing.Pen(System.Drawing.Color.Crimson, 2),
						table.TitleBlock.BoundingBox);

				//draw footer bounding box
				if (table.HasFooter)
					RenderPolygon(
						new System.Drawing.Pen(System.Drawing.Color.DarkCyan, 2),
						table.FooterBlock.BoundingBox);

				if (false)
				{
					for (int rowIndex = 0; rowIndex < table.RowCount; rowIndex++)
					{
						float topY = table.RowBounds[rowIndex];
						float bottomY = table.RowBounds[rowIndex + 1];
						foreach (var span in table.segmentSpans[rowIndex])
						{
							float leftX = span.Item1;
							float rightX = span.Item2;
							Point upperLeft = new Point(leftX, topY);
							Point lowerLeft = new Point(leftX, bottomY);
							Point upperRight = new Point(rightX, topY);
							Point lowerRight = new Point(rightX, bottomY);
							RenderPolygon(
								new System.Drawing.Pen(System.Drawing.Color.DarkKhaki, 3),
								new Polygon(new List<Point>()
								{
									upperLeft,
									lowerLeft,
									upperRight,
									lowerRight
								}));
						}
					}
				}

				//if (false)
				{
					for (int index = 0; index < table.alignments.Count; index++)
					{
						var set = table.alignments[index];
						float topY = table.RowBounds.First() - (((table.RowBounds.First() - table.RowBounds.Last()) / table.alignments.Count) * index);
						float bottomY = topY - ((table.RowBounds.First() - table.RowBounds.Last()) / table.alignments.Count);

						const float wideLine = 5;
						const float narrowLine = 1;
						RenderLine(
							new System.Drawing.Pen(
								System.Drawing.Color.Red,
								(set.Item1 == PdfTextBlock.TextAlignment.LeftAligned) ? wideLine : narrowLine),
							new Line(
								new Point(
									set.Item2.Item1,
									topY),
								new Point(
									set.Item2.Item1,
									bottomY)));
						RenderLine(
							new System.Drawing.Pen(
								System.Drawing.Color.Green,
								(set.Item1 == PdfTextBlock.TextAlignment.CenterAligned) ? wideLine : narrowLine),
							new Line(
								new Point(
									set.Item2.Item2,
									topY),
								new Point(
									set.Item2.Item2,
									bottomY)));
						RenderLine(
							new System.Drawing.Pen(
								System.Drawing.Color.Blue,
								(set.Item1 == PdfTextBlock.TextAlignment.RightAligned) ? wideLine : narrowLine),
							new Line(
								new Point(
									set.Item2.Item3,
									topY),
								new Point(
									set.Item2.Item3,
									bottomY)));
					}
				}
			}

			//draw graphics bounding boxes
			Random rand = new Random();
			if (Page.Graphics.Count < 50)
				foreach (PdfGraphicalElement graphics in Page.Graphics)
				{
					//generate color
					System.Drawing.Color color = System.Drawing.Color.FromArgb((rand.Next() & 0x00ffffff) | 0x7f000000);

					//check graphic type
					if (graphics.GetType() == typeof(PdfBitmap))
					{
						RenderPolygon(
							new System.Drawing.Pen(color, 2),
							(graphics as PdfBitmap).ImageOutline);
						RenderPolygon(
							new System.Drawing.Pen(color, 2),
							(graphics as PdfBitmap).BoundingBox);
					}
					if (graphics.GetType() == typeof(PdfPath))
					{
						int count = 0;
						foreach (Line line in (graphics as PdfPath).LineSegments)
						{
							RenderLine(
								new System.Drawing.Pen(color, 2),
								line);
							if (count++ > 50) break;
						}
					}

					//RenderText(
					//	new System.Drawing.SolidBrush(System.Drawing.Color.Black),
					//	null,
					//	$"C: [{graphics.BoundingBox.LowerLeft.X}, {graphics.BoundingBox.LowerLeft.Y}] A: [{(graphics as PdfBitmap).Anchor.X}, {(graphics as PdfBitmap).Anchor.Y}]",
					//	graphics.BoundingBox.LowerLeft,
					//	10.0f);
				}

			//DEBUG: draw page baseline
			//RenderPolyline(new System.Drawing.Pen(System.Drawing.Color.Black, 1), Page.LineBuffer.Baseline);

			//render page boxes
			//if (Page.HasMediaBox) RenderPolygon(pagePen, Page.MediaBox);
			//if (Page.HasArtBox) RenderPolygon(pagePen, Page.ArtBox);
			//if (Page.HasBleedBox) RenderPolygon(pagePen, Page.BleedBox);
			//if (Page.HasCropBox) RenderPolygon(pagePen, Page.CropBox);
			//if (Page.HasTrimBox) RenderPolygon(pagePen, Page.TrimBox);

			//unset graphics crop rectangle
			OutputGraphics.Clip = oldClip;
		}

		private void RenderText(
			System.Drawing.Brush fillBrush,
			System.Drawing.Pen strokePen,
			string text,
			Point point,
			float height)
		{
			var p = new System.Drawing.Drawing2D.GraphicsPath();
			p.AddString(
				text,
				System.Drawing.FontFamily.GenericSansSerif,
				(int)System.Drawing.FontStyle.Regular,
				ConvertLength(height) * (OutputGraphics.DpiY / 72),
				new System.Drawing.PointF(
					ConvertXCoord(point.X),
					ConvertYCoord(point.Y)),
				new System.Drawing.StringFormat());
			if (strokePen != null)
				OutputGraphics.DrawPath(strokePen, p);
			if (fillBrush != null)
				OutputGraphics.FillPath(fillBrush, p);
			
			return;

			//create text font
			System.Drawing.Font textFont = new System.Drawing.Font("Arial", ConvertLength(height));

			//draw character
			OutputGraphics.DrawString(
				text,
				textFont,
				fillBrush,
				ConvertXCoord(point.X),
				ConvertYCoord(point.Y));
		}

		/// <summary>
		/// Renders a point to the output graphics adapter,
		/// </summary>
		/// <param name="point">Point to be rendered.</param>
		private void RenderPoint(Point point)
		{
#warning TODO!
		}

		/// <summary>
		/// Renders a line to the output graphics adapter,
		/// </summary>
		/// <param name="pen">Pen to draw the line with.</param>
		/// <param name="line">Line to be rendered.</param>
		private void RenderLine(
			System.Drawing.Pen pen,
			Line line)
		{
			//draw line
			OutputGraphics.DrawLine(
				pen,
				ConvertXCoord(line.Start.X),
				ConvertYCoord(line.Start.Y),
				ConvertXCoord(line.End.X),
				ConvertYCoord(line.End.Y));
		}

		/// <summary>
		/// Renders a polygon to the output graphics adapter.
		/// </summary>
		/// <param name="pen">Shape drawing pen.</param>
		/// <param name="polygon">Polygon to be rendered.</param>
		private void RenderPolygon(
			System.Drawing.Pen pen,
			Polygon polygon)
		{
			//check if polygon exists
			if (polygon == null) return;

			//generate point list
			List<System.Drawing.PointF> points = new List<System.Drawing.PointF>();
			foreach (Point point in polygon.Points)
				points.Add(new System.Drawing.PointF(
					ConvertXCoord(point.X),
					ConvertYCoord(point.Y)));

			//draw polygon
			if (points.Count > 0)
				OutputGraphics.DrawPolygon(
					pen,
					points.ToArray());
		}

		/// <summary>
		/// Renders a filled polygon to the output graphics adapter.
		/// </summary>
		/// <param name="pen">Shape filling brush.</param>
		/// <param name="polygon">Polygon to be rendered.</param>
		private void RenderFilledPolygon(
			System.Drawing.Brush brush,
			Polygon polygon)
		{
			//check if polygon exists
			if (polygon == null) return;

			//generate point list
			List<System.Drawing.PointF> points = new List<System.Drawing.PointF>();
			foreach (Point point in polygon.Points)
			{
				points.Add(new System.Drawing.PointF(
					ConvertXCoord(point.X),
					ConvertYCoord(point.Y)));
			}
			
			//draw polygon
			OutputGraphics.FillPolygon(
				brush,
				points.ToArray());
		}

		/// <summary>
		/// Renders a polyline to the output graphics adapter.
		/// </summary>
		/// <param name="pen">Shape drawing pen.</param>
		/// <param name="polyline">Polyline to be rendered.</param>
		private void RenderPolyline(
			System.Drawing.Pen pen,
			Polyline polygon)
		{
			//check if polyline exists
			if (polygon == null) return;

			//generate point list
			List<System.Drawing.PointF> points = new List<System.Drawing.PointF>();
			foreach (Point point in polygon.Points)
				points.Add(new System.Drawing.PointF(
					ConvertXCoord(point.X),
					ConvertYCoord(point.Y)));

			//draw polyline
			OutputGraphics.DrawLines(
				pen,
				points.ToArray());
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="page">Page to be rendered.</param>
		/// <param name="outputRectangle">Rectangle defining the maximum permitted render area.</param>
		/// <param name="outputGraphics">Graphics adapter the page is to be rendered to.</param>
		public PageRenderer(
			PdfPage page,
			System.Drawing.Rectangle outputRectangle,
			System.Drawing.Graphics outputGraphics)
		{
			//store page
			Page = page;

			//store output rectangle
			OutputRectangle = outputRectangle;

			//store graphics adapter
			OutputGraphics = outputGraphics;

			//set aliasing
			OutputGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
		}

		#endregion
	}
}
