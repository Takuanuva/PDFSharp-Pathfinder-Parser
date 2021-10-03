using PdfDocumentData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace SliceAdjuster
{
	public partial class Form1 : Form
	{
		#region Properties

		#region Private storage fields

		/// <summary>
		/// Private storage field for the Viewport property.
		/// </summary>
		private BoxCoords _Viewport = null;

		/// <summary>
		/// Root section control.
		/// </summary>
		private SectionControl RootSectionControl = null;

		#region File data

		/// <summary>
		/// Project's data.
		/// </summary>
		private DocumentProject ProjectData { get; }

		/// <summary>
		/// Project style dictionary.
		/// </summary>
		private IReadOnlyDictionary<string, StyleData> StyleDictionary { get; }
		/// <summary>
		/// Currently displayed page's index.
		/// </summary>
		private int CurrentPageIndex = 0;

		/// <summary>
		/// Current page contents.
		/// </summary>
		private DocumentPage CurrentPageContentData;

		/// <summary>
		/// Current page slice data.
		/// </summary>
		private RootSection CurrentPageSliceData;

		private List<DocumentPage> ContentData;

		private List<RootSection> SliceData;

		#endregion

		#region Render bitmaps

		/// <summary>
		/// Page character render component.
		/// </summary>
		private Bitmap PageRender_Characters;

		/// <summary>
		/// Page character box render component.
		/// </summary>
		private Bitmap PageRender_CharacterBoxes;

		/// <summary>
		/// Page bitmap render component.
		/// </summary>
		private Bitmap PageRender_Bitmaps;

		/// <summary>
		/// Page path render component.
		/// </summary>
		private Bitmap PageRender_Paths;

		#endregion

		#region Section adjustment

		/// <summary>
		/// Function to be used to adjust the target.
		/// </summary>
		private Action<PointF, MouseButtons> AdjustmentFunction = null;

		/// <summary>
		/// Adjustment highlight lines.
		/// </summary>
		private List<(PointF, PointF, Color, DashStyle, float)> AdjustmentHighlights = new List<(PointF, PointF, Color, DashStyle, float)>();

		#endregion

		#endregion

		#region Rendering

		/// <summary>
		/// Current page viewport.
		/// </summary>
		public BoxCoords Viewport
		{
			get { return _Viewport; }
			set
			{
				//store new viewport
				_Viewport = value;

				//regenerate coordinate translation properties
				GenerateCoordTranslationProperties();

				//invalidate display panel
				DisplayPanel.Invalidate();
			}
		}

		#endregion

		#endregion

		#region Methods

		/// <summary>
		/// Loads the page at provided index.
		/// </summary>
		/// <param name="pageIndex">Page index.</param>
		private void LoadPage(int pageIndex)
		{
			//check if same index as current
			if (pageIndex == CurrentPageIndex)
				return;

			//set current index value
			CurrentPageIndex = pageIndex;

			//load content data
			CurrentPageContentData = ContentData[pageIndex];

			//load slice data
			CurrentPageSliceData = SliceData[pageIndex];
			
			//reset viewport
			Viewport = CurrentPageContentData.MediaBox;
			
			//refresh section controls
			SectionControlsPanel.Controls.Clear();
			RootSectionControl = new SectionControl(this, CurrentPageSliceData);
			RootSectionControl.Dock = DockStyle.Top;
			SectionControlsPanel.Controls.Add(RootSectionControl);
		}

		public void StartEdit(
			Action<PointF, MouseButtons> editFunciton, 
			List<(PointF, PointF, Color, DashStyle, float)> editHighlights)
		{
			//store function
			AdjustmentFunction = editFunciton;

			//translate and store lines
			var lines = new List<(PointF, PointF, Color, DashStyle, float)>();
			foreach (var line in editHighlights)
				lines.Add((
					TranslateCoords(line.Item1), 
					TranslateCoords(line.Item2), 
					line.Item3, 
					line.Item4, 
					line.Item5));
			AdjustmentHighlights = lines;

			//refresh display panel
			DisplayPanel.Invalidate();
		}

		/// <summary>
		/// Validates section controls to 
		/// </summary>
		public void ValidateSectionControls()
		{
			//validate root control
			RootSectionControl.ValidateProperties();

			//redraw display panel
			DisplayPanel.Invalidate();
		}

		#region Coordinate translation

		private float TranslationMultiplier;
		private float TranslationOffsetXA;
		private float TranslationOffsetXB;
		private float TranslationOffsetYA;
		private float TranslationOffsetYB;

		/// <summary>
		/// Generates translation properties.
		/// </summary>
		private void GenerateCoordTranslationProperties()
		{
			//calculate multiplier
			TranslationMultiplier = Math.Min(
				DisplayPanel.Width / Viewport.Width,
				DisplayPanel.Height / Viewport.Height);

			//calculate offsets
			TranslationOffsetXA = -Viewport.LeftX;
			TranslationOffsetXB = (DisplayPanel.Width - (Viewport.Width * TranslationMultiplier)) / 2;
			TranslationOffsetYA = -Viewport.BottomY;
			TranslationOffsetYB = DisplayPanel.Height - ((DisplayPanel.Height - (Viewport.Height * TranslationMultiplier)) / 2);
			
			//regenerate render bitmaps
			GenerateRenderBitmaps();
		}

		public float TranslateXCoord(float x)
		{
			return TranslationOffsetXB + ((x + TranslationOffsetXA) * TranslationMultiplier);
		}

		public float TranslateYCoord(float y)
		{
			return TranslationOffsetYB - ((y + TranslationOffsetYA) * TranslationMultiplier);
		}

		public PointF TranslateCoords(float x, float y)
		{
			return new PointF(TranslateXCoord(x), TranslateYCoord(y));
		}

		public PointF TranslateCoords(PointF point)
		{
			return new PointF(TranslateXCoord(point.X), TranslateYCoord(point.Y));
		}

		public float ReverseTranslateXCoord(float x)
		{
			return ((x - TranslationOffsetXB) / TranslationMultiplier) - TranslationOffsetXA;
		}

		public float ReverseTranslateYCoord(float y)
		{
			return TranslationOffsetYA - ((y - TranslationOffsetYB) / TranslationMultiplier);
		}

		public PointF ReverseTranslateCoords(float x, float y)
		{
			return new PointF(ReverseTranslateXCoord(x), ReverseTranslateYCoord(y));
		}

		public PointF ReverseTranslateCoords(PointF point)
		{
			return new PointF(ReverseTranslateXCoord(point.X), ReverseTranslateYCoord(point.Y));
		}

		#endregion

		#region Bitmap rendering

		/// <summary>
		/// Re-generates all render bitmaps.
		/// </summary>
		private void GenerateRenderBitmaps()
		{
			//generate renders
			GenerateRenderBitmap_Bitmaps();
			GenerateRenderBitmap_Characters();
			GenerateRenderBitmap_CharacterBoxes();
			GenerateRenderBitmap_Paths();
		}

		/// <summary>
		/// Generates the characters render.
		/// </summary>
		private void GenerateRenderBitmap_Characters()
		{
			//generate character render
			if (PageRender_Characters != null)
				PageRender_Characters.Dispose();
			PageRender_Characters = new Bitmap(DisplayPanel.Width, DisplayPanel.Height);
			{
				//initialize drawing objects
				Graphics graphics = Graphics.FromImage(PageRender_Characters);
				Pen characterOutlinePen = new Pen(Color.FromArgb(0x5FFFFFFF), 4);
				Brush characterInfillBrush = new SolidBrush(Color.Black);

				//set aliasing
				graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

				//clear bitmap
				graphics.Clear(Color.Transparent);

				//draw character paths
				foreach (var character in CurrentPageContentData.Characters)
				{
					//get character style
					var characterStyle = StyleDictionary[character.TextStyleId];

					//draw character path
					var p = new System.Drawing.Drawing2D.GraphicsPath();
					p.AddString(
						"" + character.CharacterValue,
						FontFamily.GenericSansSerif,
						(int)(
							FontStyle.Regular |
							(characterStyle.IsFontBolded ? FontStyle.Bold : FontStyle.Regular) |
							(characterStyle.IsFontItalicised ? FontStyle.Italic : FontStyle.Regular)),
						characterStyle.FontHeight * (graphics.DpiY / 72) * 0.9f * TranslationMultiplier,
						TranslateCoords(character.BoundingBox.LeftX, character.BoundingBox.TopY),
						new StringFormat());
					graphics.DrawPath(characterOutlinePen, p);
					graphics.FillPath(characterInfillBrush, p);
				}
			}
		}

		/// <summary>
		/// Generate character box render.
		/// </summary>
		private void GenerateRenderBitmap_CharacterBoxes()
		{
			//generate character box render
			if (PageRender_CharacterBoxes != null)
				PageRender_CharacterBoxes.Dispose();
			PageRender_CharacterBoxes = new Bitmap(DisplayPanel.Width, DisplayPanel.Height);
			{
				//initialize drawing objects
				Graphics graphics = Graphics.FromImage(PageRender_CharacterBoxes);
				Pen characterBoxPenP = new Pen(Color.Orange, 2);
				Pen characterBoxPenQ = new Pen(Color.DodgerBlue, 2);

				//set aliasing
				graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

				//clear bitmap
				graphics.Clear(Color.Transparent);

				//render character box paths
				foreach (var character in CurrentPageContentData.Characters)
				{
					//render character box paths
					var p = new System.Drawing.Drawing2D.GraphicsPath();
					var q = new System.Drawing.Drawing2D.GraphicsPath();
					p.AddLine(
						TranslateCoords(
							character.BoundingBox.LeftX,
							character.BoundingBox.TopY),
						TranslateCoords(
							character.BoundingBox.RightX,
							character.BoundingBox.BottomY));
					q.AddLine(
						TranslateCoords(
							character.BoundingBox.RightX,
							character.BoundingBox.TopY),
						TranslateCoords(
							character.BoundingBox.LeftX,
							character.BoundingBox.BottomY));
					//subP.AddLine(
					//	new PointF(
					//		character.BoundingBox.RightX,
					//		character.BoundingBox.TopY),
					//	new PointF(
					//		character.BoundingBox.LeftX,
					//		character.BoundingBox.BottomY));
					graphics.DrawPath(characterBoxPenP, p);
					graphics.DrawPath(characterBoxPenQ, q);
				}
			}
		}

		/// <summary>
		/// Generate bitmap render.
		/// </summary>
		private void GenerateRenderBitmap_Bitmaps()
		{
			//generate bitmap renders
			if (PageRender_Bitmaps != null)
				PageRender_Bitmaps.Dispose();
			PageRender_Bitmaps = new Bitmap(DisplayPanel.Width, DisplayPanel.Height);
			{
				//initialize drawing objects
				Graphics graphics = Graphics.FromImage(PageRender_Bitmaps);
				Pen bitmapOutlinePen = new Pen(Color.DarkGreen, 2);

				//set aliasing
				graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

				//clear bitmap
				graphics.Clear(Color.Transparent);

				//draw bitmap paths
				foreach (var bitmap in CurrentPageContentData.Bitmaps)
				{
					//draw bitmap outline segments
					foreach (var pathSegment in bitmap.Outline.PathSegments)
					{
						var p = new System.Drawing.Drawing2D.GraphicsPath();
						p.AddLine(
							TranslateCoords(
								pathSegment.Start.X,
								pathSegment.Start.Y),
							TranslateCoords(
								pathSegment.End.X,
								pathSegment.End.Y));
						graphics.DrawPath(bitmapOutlinePen, p);
					}
				}
			}
		}

		/// <summary>
		/// Generate path render.
		/// </summary>
		private void GenerateRenderBitmap_Paths()
		{
			//generate path renders
			if (PageRender_Paths != null)
				PageRender_Paths.Dispose();
			PageRender_Paths = new Bitmap(DisplayPanel.Width, DisplayPanel.Height);
			{
				//initialize drawing objects
				Graphics graphics = Graphics.FromImage(PageRender_Paths);
				Pen pathOutlinePen = new Pen(Color.DarkBlue, 2);

				//set aliasing
				graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

				//clear bitmap
				graphics.Clear(Color.Transparent);

				//draw paths
				foreach (var path in CurrentPageContentData.Paths)
				{
					//draw path segments
					foreach (var pathSegment in path.PathSegments)
					{
						var p = new System.Drawing.Drawing2D.GraphicsPath();
						p.AddLine(
							TranslateCoords(
								pathSegment.Start.X,
								pathSegment.Start.Y),
							TranslateCoords(
								pathSegment.End.X,
								pathSegment.End.Y));
						graphics.DrawPath(pathOutlinePen, p);
					}
				}
			}
		}

		#endregion

		#region Events

		private void Form1_Load(object sender, EventArgs e)
		{
			//load files
			ContentData = new List<DocumentPage>();
			SliceData = new List<RootSection>();
			for (int i = 0; i < ProjectData.PageCount; i++)
			{
				//load content data
				using (TextReader reader = new StreamReader(ProjectData.PageContentFilePath(i)))
				{
					XmlSerializer serializer = new XmlSerializer(typeof(DocumentPage));
					var pageData = serializer.Deserialize(reader) as DocumentPage;
					pageData.Document = ProjectData;
					foreach (var character in pageData.Characters)
						character.ParentPage = pageData;
					ContentData.Add(pageData);
				}

				//load slice data
				using (TextReader reader = new StreamReader(ProjectData.InitialPageSliceFilePath(i)))
				{
					XmlSerializer serializer = new XmlSerializer(typeof(RootSection));
					var data = serializer.Deserialize(reader) as RootSection;
					data.PageContent = ContentData[i];
					SliceData.Add(data);
				}
			}

			//load first page
			CurrentPageIndex = -1;
			numericUpDown1.Minimum = 0;
			numericUpDown1.Maximum = ProjectData.PageCount - 1;
			numericUpDown1.Value = 0;
			LoadPage(0);
		}

		private void DisplayPanel_Paint(object sender, PaintEventArgs e)
		{
			//recalculate scaling properties
			GenerateCoordTranslationProperties();

			//clear panel
			e.Graphics.Clear(Color.White);

			//set aliasing
			e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

			//render bitmaps
			if (PageRender_Bitmaps != null)
				e.Graphics.DrawImage(
					PageRender_Bitmaps,
					0,
					0,
					DisplayPanel.Width,
					DisplayPanel.Height);
			if (PageRender_Paths != null)
				e.Graphics.DrawImage(
					PageRender_Paths,
					0,
					0,
					DisplayPanel.Width,
					DisplayPanel.Height);
			if (PageRender_CharacterBoxes != null)
				e.Graphics.DrawImage(
					PageRender_CharacterBoxes,
					0,
					0,
					DisplayPanel.Width,
					DisplayPanel.Height);
			if (PageRender_Characters != null)
				e.Graphics.DrawImage(
					PageRender_Characters,
					0,
					0,
					DisplayPanel.Width,
					DisplayPanel.Height);
			
			//declare section rendering function
			Pen sectionDividerPen = new Pen(Color.Magenta, 2);
			void renderSection(PageSection section)
			{
				//check if end node
				if (section.SubsectionCount <= 1)
				{
					//render infill
					e.Graphics.FillRectangle(
						new SolidBrush(Color.FromArgb(0x3F, Color.FromArgb(section.InnerColor))),
						TranslateXCoord(section.BoundingBox.LeftX),
						TranslateYCoord(section.BoundingBox.TopY),
						section.BoundingBox.Width * TranslationMultiplier,
						section.BoundingBox.Height * TranslationMultiplier);
				}
				else
				{
					//render subsections
					foreach (var subsection in section.Subsections)
						renderSection(subsection);

					//render delims
					Pen delimPen = new Pen(Color.FromArgb(section.OuterColor), 2);
					foreach (var columnDelim in section.ColumnDelims)
						e.Graphics.DrawLine(
							delimPen,
							TranslateCoords(columnDelim, section.BoundingBox.BottomY),
							TranslateCoords(columnDelim, section.BoundingBox.TopY));
					foreach (var rowDelim in section.RowDelims)
						e.Graphics.DrawLine(
							delimPen,
							TranslateCoords(section.BoundingBox.LeftX, rowDelim),
							TranslateCoords(section.BoundingBox.RightX, rowDelim));
				}
			}

			//render sections
			renderSection(CurrentPageSliceData);

			//render outline
			e.Graphics.DrawRectangle(
				new Pen(Color.FromArgb(CurrentPageSliceData.OuterColor), 3),
				TranslateXCoord(CurrentPageSliceData.BoundingBox.LeftX),
				TranslateYCoord(CurrentPageSliceData.BoundingBox.TopY),
				CurrentPageSliceData.BoundingBox.Width * TranslationMultiplier,
				CurrentPageSliceData.BoundingBox.Height * TranslationMultiplier);

			//e.Graphics.DrawRectangle(
			//	new Pen(Color.DarkCyan, 3),
			//	TranslateXCoord(CurrentPageContentData.MediaBox.LeftX),
			//	TranslateYCoord(CurrentPageContentData.MediaBox.TopY),
			//	CurrentPageContentData.MediaBox.Width * TranslationMultiplier,
			//	CurrentPageContentData.MediaBox.Height * TranslationMultiplier);
			//if (CurrentPageContentData.ArtBox != null)
			//	e.Graphics.DrawRectangle(
			//		new Pen(Color.DarkGoldenrod, 3),
			//		TranslateXCoord(CurrentPageContentData.ArtBox.LeftX),
			//		TranslateYCoord(CurrentPageContentData.ArtBox.TopY),
			//		CurrentPageContentData.ArtBox.Width * TranslationMultiplier,
			//		CurrentPageContentData.ArtBox.Height * TranslationMultiplier);
			//if (CurrentPageContentData.CropBox != null)
			//	e.Graphics.DrawRectangle(
			//		new Pen(Color.DarkKhaki, 3),
			//		TranslateXCoord(CurrentPageContentData.CropBox.LeftX),
			//		TranslateYCoord(CurrentPageContentData.CropBox.TopY),
			//		CurrentPageContentData.CropBox.Width * TranslationMultiplier,
			//		CurrentPageContentData.CropBox.Height * TranslationMultiplier);
			//if (CurrentPageContentData.TrimBox != null)
			//	e.Graphics.DrawRectangle(
			//		new Pen(Color.DarkMagenta, 3),
			//		TranslateXCoord(CurrentPageContentData.TrimBox.LeftX),
			//		TranslateYCoord(CurrentPageContentData.TrimBox.TopY),
			//		CurrentPageContentData.TrimBox.Width * TranslationMultiplier,
			//		CurrentPageContentData.TrimBox.Height * TranslationMultiplier);
			//if (CurrentPageContentData.BleedBox != null)
			//	e.Graphics.DrawRectangle(
			//		new Pen(Color.DarkRed, 3),
			//		TranslateXCoord(CurrentPageContentData.BleedBox.LeftX),
			//		TranslateYCoord(CurrentPageContentData.BleedBox.TopY),
			//		CurrentPageContentData.BleedBox.Width * TranslationMultiplier,
			//		CurrentPageContentData.BleedBox.Height * TranslationMultiplier);
			
			//render adjustment highlights
			foreach (var line in AdjustmentHighlights)
			{
				var pen = new Pen(line.Item3, line.Item5);
				pen.DashStyle = line.Item4;
				e.Graphics.DrawLine(
					pen,
					line.Item1,
					line.Item2);
			}
		}

		private void DisplayPanel_MouseClick(object sender, MouseEventArgs e)
		{
			//check if a section is being adjusted
			if (AdjustmentFunction != null)
			{
				//store adjustment function
				var function = AdjustmentFunction;

				//clear adjustment properties
				AdjustmentFunction = null;
				AdjustmentHighlights.Clear();

				//run adjustment function
				function(ReverseTranslateCoords(e.X, e.Y), e.Button);

				//update section controls
				RootSectionControl.ValidateProperties();

				//invalidate panel
				DisplayPanel.Invalidate();
			}
		}

		private void PageSelectorUpDown_SelectedItemChanged(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}

		private void DisplayPanel_SizeChanged(object sender, EventArgs e)
		{
			DisplayPanel.Invalidate();
		}
		
		private void numericUpDown1_ValueChanged(object sender, EventArgs e)
		{
			LoadPage((int)numericUpDown1.Value);
		}

		#endregion

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="projectData">Document project data.</param>
		public Form1(DocumentProject projectData)
		{
			//store project data
			ProjectData = projectData;

			//generate style dictionary
			Dictionary<string, StyleData> styleDictionary = new Dictionary<string, StyleData>();
			foreach (var kv in ProjectData.Styles)
				styleDictionary[kv.Value.IdString] = kv.Value;

			//store style dictionary
			StyleDictionary = styleDictionary;
			
			//initialize form
			InitializeComponent();
		}

		#endregion
	}
}
