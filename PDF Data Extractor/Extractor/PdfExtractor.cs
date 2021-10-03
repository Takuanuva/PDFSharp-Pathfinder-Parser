using DocumentParser.DataTypes;
using PdfDocumentData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PDF_Data_Extractor.Extractor
{
	/// <summary>
	/// Data extractor class.
	/// </summary>
	internal class PdfExtractor
	{
		#region Properties

		#region Private storage fields
		
		/// <summary>
		/// File's project identifier string.
		/// </summary>
		public string _FileProjectId { get; }

		#endregion

		/// <summary>
		/// Extracted PDF document data.
		/// </summary>
		public PdfDocument Document { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Extracts and saves the data of page with the provided index.
		/// </summary>
		/// <param name="documentProject">Source document project.</param>
		/// <param name="pageIndex">Page index.</param>
		private void ExtractPageDataToFiles(DocumentProject documentProject, int pageIndex)
		{
			//get page
			var page = Document.Pages[pageIndex];

			//declare coordinate translation function
			float offsetX;
			float offsetY;
			{
				float xMin = page.MediaBox.LeftX;
				float yMax = page.MediaBox.TopY;
				if (page.CropBox != null)
				{
					xMin = Math.Min(xMin, page.CropBox.LeftX);
					yMax = Math.Max(yMax, page.CropBox.TopY);
				}
				if (page.ArtBox != null)
				{
					xMin = Math.Min(xMin, page.ArtBox.LeftX);
					yMax = Math.Max(yMax, page.ArtBox.TopY);
				}
				if (page.TrimBox != null)
				{
					xMin = Math.Min(xMin, page.TrimBox.LeftX);
					yMax = Math.Max(yMax, page.TrimBox.TopY);
				}
				if (page.BleedBox != null)
				{
					xMin = Math.Min(xMin, page.BleedBox.LeftX);
					yMax = Math.Max(yMax, page.BleedBox.TopY);
				}
				offsetX = -xMin;
				offsetY = yMax;
			}
			PointCoords translatePoint(DocumentParser.Utilities.Geometry.Point point)
			{
				return
					new PointCoords(
						point.X,
						point.Y);
				return
					new PointCoords(
						offsetX + point.X,
						offsetY - point.Y);
			}

			//declare rectangle translation function
			BoxCoords translateRectangle(DocumentParser.Utilities.Geometry.Rectangle rectangle)
			{
				return
					new BoxCoords(
						translatePoint(rectangle.UpperLeft).X,
						translatePoint(rectangle.LowerRight).X,
						translatePoint(rectangle.UpperLeft).Y,
						translatePoint(rectangle.LowerRight).Y);
			}

			//declare path translation function
			DocumentPage.Path translatePath(IReadOnlyList<DocumentParser.Utilities.Geometry.Line> lineSegments)
			{
				List<DocumentPage.Path.LineSegment> segments = new List<DocumentPage.Path.LineSegment>();
				foreach (var line in lineSegments)
					segments.Add(
						new DocumentPage.Path.LineSegment(
							translatePoint(
								line.Start),
							translatePoint(
								line.End)));

				return new DocumentPage.Path(segments);
			}

			//generate page boxes
			BoxCoords mediaBox = translateRectangle(page.MediaBox);
			BoxCoords cropBox = null;
			if (page.CropBox != null)
				cropBox = translateRectangle(page.CropBox);
			BoxCoords trimBox = null;
			if (page.TrimBox != null)
				trimBox = translateRectangle(page.TrimBox);
			BoxCoords artBox = null;
			if (page.ArtBox != null)
				artBox = translateRectangle(page.ArtBox);
			BoxCoords bleedBox = null;
			if (page.BleedBox != null)
				bleedBox = translateRectangle(page.BleedBox);

			//generate character list
			var characters = new List<TextCharacter>();
			foreach (var character in page.Characters)
				characters.Add(
					new TextCharacter(
						character.CharacterValue,
						translateRectangle(character.BoundingBox),
						character.CharacterStyle.IdString));

			//generate bitmap and path lists
			var bitmaps = new List<DocumentPage.PageBitmap>();
			var paths = new List<DocumentPage.Path>();
			foreach (var graphic in page.Graphics)
			{
				if (graphic is PdfBitmap)
					bitmaps.Add(
						new DocumentPage.PageBitmap(
							(graphic as PdfBitmap).ImageResource.IdString,
							translatePath((graphic as PdfBitmap).ImageOutline.Lines)));
				if (graphic is PdfPath)
					paths.Add(translatePath((graphic as PdfPath).LineSegments));
			}

			//generate page data object
			var pageData = new DocumentPage(
				documentProject,
				pageIndex,
				mediaBox,
				cropBox,
				trimBox,
				artBox,
				bleedBox,
				characters,
				bitmaps,
				paths);

			//serialize page data to file
			pageData.SaveToFile();
		}
		
		/// <summary>
		/// Extracts PDF data into appropriate files.
		/// </summary>
		internal void ExtractDataToFiles()
		{
			try
			{
				//generate project file data
				DocumentProject documentProject;
				{
					//create style data objects
					List<StyleData> styleDataList = new List<StyleData>();
					foreach (var style in PdfTextCharacter.Style.GetParserStyleList(Document.Parent))
						styleDataList.Add(
							new StyleData(
								style.FontFamily.GetHashCode().ToString("X8"),
								style.FontFamily,
								style.IsFontBolded,
								style.IsFontItalicised,
								style.FillColor,
								style.StrokeColor,
								style.StrokeWidth,
								style.FontSize,
								style.FontHeight,
								style.IdString,
								style.AssociatedCharacters.Count));

					//create document project data object
					documentProject =
						new DocumentProject(
							_FileProjectId,
							Document.Pages.Count,
							styleDataList);
				}


				//print notification
				Console.WriteLine($"[{ _FileProjectId }]: Exporting { Document.Pages.Count } pages:");

				//extract page data
				for (int i = 0; i < Document.Pages.Count; i++)
				{
					Console.Write($"{i}...");
					ExtractPageDataToFiles(documentProject, i);
				}

				//print notification
				Console.WriteLine();
				Console.WriteLine($"[{ _FileProjectId }]: Exporting { Document.BitmapResources.Count } bitmaps:");

				//save extracted bitmaps
				var kvList = Document.BitmapResources.ToList();
				for (int i = 0; i < kvList.Count; i++)
				{
					Console.Write($"{i}...");
					var resource = kvList[i].Value;
					resource.Data.Save(
						documentProject.ExtractedBitmapFilePath(resource.IdString));
				}

				//print notification
				Console.WriteLine();
				Console.Write($"[{ _FileProjectId }]: Exporting project file...");

				//save project file
				documentProject.SaveToFile();

				//print notification
				Console.WriteLine($"done!");
				Console.WriteLine($"[{ _FileProjectId }]: All exporting done!");
			}
			catch (Exception e)
			{
				//create fail file
				File.WriteAllText(
					DocumentProject.FailFilePath(_FileProjectId),
					$"WRITE:{Environment.NewLine}[{e.Message}]{e.StackTrace}");

				throw e;
			}

		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="filePath">Path to PDF file to be extracted.</param>
		internal PdfExtractor(
			string filePath)
		{
			//generate ID string
			_FileProjectId = Regex.Match(filePath, @"\\([^\\]+).pdf$").Groups[1].Value;
			//_FileProjectId = filePath.GetHashCode().ToString("X8");
			
			//print notification
			Console.Write($"[{ _FileProjectId }]: Loading...");

			try
			{
				//get document data
				var documentParser = new PdfDocumentParser(filePath);
				Document = documentParser.Documents.First();
			}
			catch (Exception e)
			{
				//create fail file
				File.WriteAllText(
					DocumentProject.FailFilePath(_FileProjectId),
					$"READ:{Environment.NewLine}[{e.Message}]{e.StackTrace}");
			
				throw e;
			}

			//print notification
			Console.WriteLine($"done!");
		}

		#endregion
	}
}
