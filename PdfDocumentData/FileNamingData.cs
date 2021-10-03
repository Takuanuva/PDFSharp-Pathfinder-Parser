using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PdfDocumentData
{
	/// <summary>
	/// File naming data.
	/// </summary>
	public partial class DocumentProject
	{
		private const string _ProjectSuperDirectory = @"C:\PROJECTS\PDF_PARSE-DATA\PROJECTS\";
		private static bool ProjectSuperDirectoryChecked = false;
		public static string ProjectSuperDirectory
		{
			get
			{
				if (!ProjectSuperDirectoryChecked)
				{
					if (!Directory.Exists(_ProjectSuperDirectory))
						Directory.CreateDirectory(_ProjectSuperDirectory);
					ProjectSuperDirectoryChecked = true;
				}
				return _ProjectSuperDirectory;
			}
		}

		private const string ProjectFilePrefix = @"PROJ_";
		private const string ProjectFileSufix = @".proj";
		public string ProjectFilePath()
		{
			return ProjectSuperDirectory + ProjectFilePrefix + FileIdentifier + ProjectFileSufix;
		}

		private const string FailFilePrefix = @"_FAIL_";
		private const string FailFileSufix = @".fail";
		public static string FailFilePath(string fileId)
		{
			return ProjectSuperDirectory + FailFilePrefix + fileId + FailFileSufix;
		}

		private const string ProjectDirectoryPrefix = @"PROJ_";
		private const string ProjectDirectorySufix = @"_DIR\";
		public string ProjectDirectoryPath()
		{
			string dir = ProjectSuperDirectory + ProjectDirectoryPrefix + FileIdentifier + ProjectDirectorySufix;
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			return dir;
		}

		//private const string StyleDictionaryFilePrefix = @"STYLE_";
		//private const string StyleDictionaryFileSufix = @".xml";
		//public static string StyleDictionaryFilePath(string fileId)
		//{
		//	return ProjectDirectoryPath(fileId) + StyleDictionaryFilePrefix + fileId + StyleDictionaryFileSufix;
		//}

		private const string ExtractedBitmapSubDirectory = @"BITMAPS\";
		private const string ExtractedBitmapFilePrefix = @"BITMAP_";
		private const string ExtractedBitmapFileSufix = @".png";
		public string ExtractedBitmapFilePath(string bitmapId)
		{
			string dir = ProjectDirectoryPath() + ExtractedBitmapSubDirectory;
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			return dir + ExtractedBitmapFilePrefix + bitmapId + ExtractedBitmapFileSufix;
		}

		private const string PageContentSubDirectory = @"PAGE_CONTENTS\";
		private const string PageContentFilePrefix = @"PAGE_";
		private const string PageContentFileSufix = @"_CONTENT.xml";
		public string PageContentFilePath(int pageIndex)
		{
			string dir = ProjectDirectoryPath() + PageContentSubDirectory;
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			return dir + PageContentFilePrefix + pageIndex.ToString() + PageContentFileSufix;
		}

		private const string PageRenderSubDirectory = @"PAGE_RENDERS\";
		private const string PageRenderFilePrefix = @"PAGE_";
		private const string PageRenderFileSufix = @"_RENDER.png";
		public string PageRenderFilePath(int pageIndex)
		{

			string dir = ProjectDirectoryPath() + PageRenderSubDirectory;
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			return dir + PageRenderFilePrefix + pageIndex.ToString() + PageRenderFileSufix;
		}

		private const string PageSliceSubDirectory = @"PAGE_SLICES\";
		private const string InitialPageSliceFilePrefix = @"PAGE_";
		private const string InitialPageSliceFileSufix = @"_SLICE_INITIAL.xml";
		public string InitialPageSliceFilePath(int pageIndex)
		{

			string dir = ProjectDirectoryPath() + PageSliceSubDirectory;
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			return dir + InitialPageSliceFilePrefix + pageIndex.ToString() + InitialPageSliceFileSufix;
		}
		private const string FinalPageSliceFilePrefix = @"PAGE_";
		private const string FinalPageSliceFileSufix = @"_SLICE_FINAL.xml";
		public string FinalPageSliceFilePath(int pageIndex)
		{

			string dir = ProjectDirectoryPath() + PageSliceSubDirectory;
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
			return dir + FinalPageSliceFilePrefix + pageIndex.ToString() + FinalPageSliceFileSufix;
		}
	}
}
