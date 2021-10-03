using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentParser.DataTypes
{
	/// <summary>
	/// Allows the parsing of specified document(s) and access to the results thereof.
	/// </summary>
	public class PdfDocumentParser
	{
		#region Properties

		#region Private storage fields

		/// <summary>
		/// Private storage field for the Documents property.
		/// </summary>
		private List<PdfDocument> _Documents = new List<PdfDocument>();

		#endregion

		/// <summary>
		/// List of all attached documents.
		/// </summary>
		public IReadOnlyList<PdfDocument> Documents
		{
			get { return _Documents; }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Performs the parsing operation for all included documents.
		/// </summary>
		private void Parse()
		{
			//parse all documents
			foreach (var document in Documents)
				document.Parse();
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor, creates a parser for a single PDF file.
		/// </summary>
		/// <param name="path">File path.</param>
		public PdfDocumentParser(string path)
		{
			//add document to list
			_Documents.Add(new PdfDocument(this, path));

			//parse document
			Parse();
		}

		/// <summary>
		/// Constructor, creates a parser for multiple PDF files.
		/// </summary>
		/// <param name="paths">File path collection.</param>
		public PdfDocumentParser(IReadOnlyCollection<string> paths)
		{
			//add documents to list
			foreach (string path in paths)
				_Documents.Add(new PdfDocument(this, path));

			//parse documents
			Parse();
		}

		#endregion
	}
}
