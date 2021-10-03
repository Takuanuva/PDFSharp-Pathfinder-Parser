using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentParser.Utilities.DataExtraction;
using DocumentParser.Utilities.Geometry;

namespace DocumentParser.DataTypes
{
	/// <summary>
	/// Represents the contents of a single PDF file.
	/// </summary>
	public class PdfDocument
	{
		#region Properties

		#region Private storage fields

		/// <summary>
		/// Private storage field for the Pages property.
		/// </summary>
		private List<PdfPage> _Pages = new List<PdfPage>();

		/// <summary>
		/// Private storage field for the BitmapResources property.
		/// </summary>
		private Dictionary<string, PdfBitmap.Resource> _BitmapResources = new Dictionary<string, PdfBitmap.Resource>();

		#endregion

		/// <summary>
		/// Parser which created the document instance.
		/// </summary>
		public PdfDocumentParser Parent { get; }

		/// <summary>
		/// List of all pages within the document.
		/// </summary>
		public IReadOnlyList<PdfPage> Pages
		{
			get { return _Pages; }
		}

		/// <summary>
		/// Bitmap resources within the document indexed by identifier strings.
		/// </summary>
		public IReadOnlyDictionary<string, PdfBitmap.Resource> BitmapResources
		{
			get { return _BitmapResources; }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Adds page to document.
		/// </summary>
		/// <param name="page">Page to be added.</param>
		internal void AddPage(PdfPage page)
		{
			//add page to dictionary
			_Pages.Add(page);
		}

		/// <summary>
		/// Adds a bitmap resource to the resource dictionary.
		/// </summary>
		/// <param name="resourceIdentifier">Resource identifier.</param>
		/// <param name="resource">Bitmap resource.</param>
		internal void AddBitmapResource(
			string resourceIdentifier,
			PdfBitmap.Resource resource)
		{
			//add resource to dictionary
			_BitmapResources.Add(resourceIdentifier, resource);
		}





		public Rectangle AveragePageBounds = null;
		public float AveragePageMarginLeft = 0;
		public float AveragePageMarginRight = 0;
		public float AveragePageMarginBottom = 0;
		public float AveragePageMarginTop = 0;




		/// <summary>
		/// Parses the document contents.
		/// </summary>
		internal void Parse()
		{
			foreach (var page in Pages)
				page.PrimaryParse();

			return;

			//declare mutli-threaded processing function
			void multiThreadedProcessing(Action<PdfPage> pageProcessingFunction, bool debug = false)
			{
				//initialize multi-process variables
				int currentPage = 0;
				bool currentPageLock = false;

				//declare page parsing function
				void parsePage()
				{
					//perform loops continuously
					while (true)
					{
						//wait for the page counter to be unlocked
						while (currentPageLock)
						{ }

						//lock page counter
						currentPageLock = true;

						//get current page index
						int pageIndex = currentPage++;

						//unlock page counter
						currentPageLock = false;

						//check if index is out of bounds
						if (pageIndex >= Pages.Count)
							return;

						Console.WriteLine($"Starting processing on page index {pageIndex}.");

						//perform processing on the page
						pageProcessingFunction(Pages[pageIndex]);

						Console.WriteLine($"Ending processing on page index {pageIndex}.");
					}
				}
				
				//check if debug mode
				if (debug)
				{
					//debug: single-thread
					foreach (var page in Pages)
					{
						Console.WriteLine($"Starting single-threaded processing on page {page.Title}.");
						pageProcessingFunction(page);
						Console.WriteLine($"Ending single-threaded processing on page {page.Title}.");
					}
				}
				else
				{
					//create thread tasks
					List<Task> tasks = new List<Task>();
					for (int i = 0; i < Environment.ProcessorCount * 2; i++)
						tasks.Add(new Task(parsePage, TaskCreationOptions.LongRunning));

					//start tasks
					foreach (var task in tasks)
						task.Start();

					//wait until all tasks are over
					while (true)
					{
						bool allCompleted = true;
						foreach (var task in tasks)
							allCompleted &= task.IsCompleted;
						if (allCompleted)
							break;
					}
				}
			}

			//perform primary page parsing
			multiThreadedProcessing((PdfPage page) => { page.PrimaryParse(); }, false);

			//calculate average page content bounds properties
			{
				//initialize average sum buffers
				decimal avgLeftXSum = 0;
				decimal avgLeftXWeightSum = 0;
				decimal avgRightXSum = 0;
				decimal avgRightXWeightSum = 0;
				decimal avgBottomYSum = 0;
				decimal avgBottomYWeightSum = 0;
				decimal avgTopYSum = 0;
				decimal avgTopYWeightSum = 0;
				decimal avgMarginLeftXSum = 0;
				decimal avgMarginLeftXWeightSum = 0;
				decimal avgMarginRightXSum = 0;
				decimal avgMarginRightXWeightSum = 0;
				decimal avgMarginBottomYSum = 0;
				decimal avgMarginBottomYWeightSum = 0;
				decimal avgMarginTopYSum = 0;
				decimal avgMarginTopYWeightSum = 0;

				//iterate over pages
				foreach (var page in Pages)
				{
					//calculate weights
					decimal weightPage = 0;
					foreach (var box in page.ContinuousTextBounds)
						weightPage += (decimal)box.SurfaceArea;
					weightPage /= (decimal)page.MediaBox.SurfaceArea;
					weightPage *= weightPage;
					const float expectedMarginMultiplier = 0.2f;
					float expectedMarginHorizontal = page.MediaBox.Width * expectedMarginMultiplier;
					float expectedMarginVertical = page.MediaBox.Height * expectedMarginMultiplier;
					decimal weightLeft =
						weightPage *
						(decimal)(page.MostLikelyPageBounds.Width / page.MediaBox.Width);
						//(decimal)Math.Max(Math.Abs(expectedMarginHorizontal - (page.MostLikelyPageBounds.LeftX - page.MediaBox.LeftX)) / expectedMarginHorizontal, 0);
					decimal weightRight =
						weightPage *
						(decimal)(page.MostLikelyPageBounds.Width / page.MediaBox.Width);
					//(decimal)Math.Max(Math.Abs(expectedMarginHorizontal - (page.MediaBox.RightX - page.MostLikelyPageBounds.RightX)) / expectedMarginHorizontal, 0);
					decimal weightBottom = 
						weightPage *
						(decimal)(page.MostLikelyPageBounds.Height / page.MediaBox.Height);
					//(decimal)Math.Max(Math.Abs(expectedMarginVertical - (page.MostLikelyPageBounds.BottomY - page.MediaBox.BottomY)) / expectedMarginVertical, 0);
					decimal weightTop = 
						weightPage *
						(decimal)(page.MostLikelyPageBounds.Height / page.MediaBox.Height);
					//(decimal)Math.Max(Math.Abs(expectedMarginVertical - (page.MediaBox.TopY - page.MostLikelyPageBounds.TopY)) / expectedMarginVertical, 0);

					//increment sums
					avgLeftXSum +=
						(decimal)page.MostLikelyPageBounds.LeftX *
						weightLeft;
					avgRightXSum +=
						(decimal)page.MostLikelyPageBounds.RightX *
						weightRight;
					avgBottomYSum +=
						(decimal)page.MostLikelyPageBounds.BottomY *
						weightBottom;
					avgTopYSum +=
						(decimal)page.MostLikelyPageBounds.TopY *
						weightTop;
					avgMarginLeftXSum +=
						(decimal)(page.MostLikelyPageBounds.LeftX - page.MediaBox.LeftX) *
						weightLeft;
					avgMarginRightXSum +=
						(decimal)(page.MediaBox.RightX - page.MostLikelyPageBounds.RightX) *
						weightRight;
					avgMarginBottomYSum +=
						(decimal)(page.MostLikelyPageBounds.BottomY - page.MediaBox.BottomY) *
						weightBottom;
					avgMarginTopYSum +=
						(decimal)(page.MediaBox.TopY - page.MostLikelyPageBounds.TopY) *
						weightTop;
					avgLeftXWeightSum += weightLeft;
					avgRightXWeightSum += weightRight;
					avgBottomYWeightSum += weightBottom;
					avgTopYWeightSum += weightTop;
					avgMarginLeftXWeightSum += weightLeft;
					avgMarginRightXWeightSum += weightRight;
					avgMarginBottomYWeightSum += weightBottom;
					avgMarginTopYWeightSum += weightTop;
				}

				//calculate final values
				AveragePageBounds =
					new Rectangle(
						new Point(
							(float)(avgLeftXSum / avgLeftXWeightSum),
							(float)(avgBottomYSum / avgBottomYWeightSum)),
						new Point(
							(float)(avgRightXSum / avgRightXWeightSum),
							(float)(avgTopYSum / avgTopYWeightSum)));
				AveragePageMarginLeft = (float)(avgMarginLeftXSum / avgMarginLeftXWeightSum);
				AveragePageMarginRight = (float)(avgMarginRightXSum / avgMarginRightXWeightSum);
				AveragePageMarginBottom = (float)(avgMarginBottomYSum / avgMarginBottomYWeightSum);
				AveragePageMarginTop = (float)(avgMarginTopYSum / avgMarginTopYWeightSum);
			}

			//perform secondary page parsing
			multiThreadedProcessing((PdfPage page) =>
			{
				page.SecondaryParse();
				//if (false)
				{
					System.Drawing.Bitmap bmp = new System.Drawing.Bitmap((int)page.MediaBox.Width * 2, (int)page.MediaBox.Height * 2);
					System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bmp);
					graphics.Clear(System.Drawing.Color.White);
					int margin = 10;
					Utilities.DataRendering.PageRenderer renderer = new Utilities.DataRendering.PageRenderer(
							page,
							new System.Drawing.Rectangle(
								0,
								0,
								bmp.Size.Width,
								bmp.Size.Height),
						graphics);
					renderer.RenderPage();
					bmp.Save($"{page.Title}.png");


					string generateJson(PdfPage.ContentSection section)
					{
						string output = "";
						output += "{";
						output += $"\"left\":{section.Area.LeftX},";
						output += $"\"right\":{section.Area.RightX},";
						output += $"\"bottom\":{section.Area.BottomY},";
						output += $"\"top\":{section.Area.TopY},";
						output += "\"slicesHorizontal\":[";
						foreach (float slice in section.horizontalSlices)
							output += $"{slice},";
						if (section.horizontalSlices.Count > 0)
							output = output.Remove(output.Length - 1);
						output += "],";
						output += "\"slicesVertical\":[";
						foreach (float slice in section.verticalSlices)
							output += $"{slice},";
						if (section.verticalSlices.Count > 0)
							output = output.Remove(output.Length - 1);
						output += "],";
						output += $"\"children\":[";
						foreach (var subSection in section.Subsections)
							output += $"{generateJson(subSection)},";
						if (section.Subsections.Count > 0)
							output = output.Remove(output.Length - 1);
						output += "]";
						output += "}";
						return output;
					}
					System.IO.File.WriteAllText($"{page.Title}.json", $"{{\"left\":{page.MediaBox.LeftX},\"right\":{page.MediaBox.RightX},\"bottom\":{page.MediaBox.BottomY},\"top\":{page.MediaBox.TopY},\"content\":{generateJson(page.RootSection)}}}");


#warning delete
					page.RootSection = null;
				}
			}, false);
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor, creates new document instance from specified file.
		/// </summary>
		/// <param name="parent">Document parser which initialized the instance.</param>
		/// <param name="filePath">Path to file to be loaded.</param>
		internal PdfDocument(
			PdfDocumentParser parent,
			string filePath)
		{
			//store parent reference
			Parent = parent;

			//extract document data from file
			PdfDataExtractor extractor = new PdfDataExtractor(this, filePath);
			extractor.ExtractDocument();
		}

		#endregion
	}
}
