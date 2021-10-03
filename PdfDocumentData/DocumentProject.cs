using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace PdfDocumentData
{
	/// <summary>
	/// Stores general document data.
	/// </summary>
	public partial class DocumentProject : IXmlSerializable
	{
		#region Properties

		/// <summary>
		/// Project ID string.
		/// </summary>
		public string FileIdentifier { get; set; }

		/// <summary>
		/// Number of pages within the document.
		/// </summary>
		public int PageCount { get; private set; }

		/// <summary>
		/// Document style dictionary.
		/// </summary>
		public IReadOnlyDictionary<string, StyleData> Styles { get; private set; }
		
		#endregion

		#region Methods

		#region XML serialization

		/// <summary>
		/// Schema generator.
		/// </summary>
		/// <returns>Null.</returns>
		public XmlSchema GetSchema()
		{
			return null;
		}

		/// <summary>
		/// XML serialization function.
		/// </summary>
		/// <param name="writer">XML writer.</param>
		public void WriteXml(XmlWriter writer)
		{
			//write file ID
			writer.WriteElementString(nameof(FileIdentifier), FileIdentifier);

			//write page count
			writer.WriteElementString(nameof(PageCount), PageCount.ToString());

			//write styles
			writer.WriteStartElement(nameof(Styles));
			foreach (var kv in Styles)
			{
				writer.WriteStartElement(nameof(StyleData));
				kv.Value.WriteXml(writer);
				writer.WriteEndElement();
			}
			writer.WriteEndElement();

		}

		/// <summary>
		/// XML deserialization function.
		/// </summary>
		/// <param name="reader">XML reader.</param>
		public void ReadXml(XmlReader reader)
		{
			//read start tag
			reader.ReadStartElement();

			//read file ID
			FileIdentifier = reader.ReadElementContentAsString(nameof(FileIdentifier), "");

			//read page count
			PageCount = reader.ReadElementContentAsInt(nameof(PageCount), "");

			//read styles
			var stylesDict = new Dictionary<string, StyleData>();
			if (reader.ReadToDescendant(nameof(StyleData)))
			{
				do
				{
					StyleData style = new StyleData();
					style.ReadXml(reader.ReadSubtree());
					stylesDict[style.IdString] = style;
				}
				while (reader.ReadToNextSibling(nameof(StyleData)));
			}
			Styles = stylesDict;
		}

		#endregion

		#region Sub-object accessors

		/// <summary>
		/// Retrieves document page content.
		/// </summary>
		/// <param name="index">Page index.</param>
		/// <returns>Page contents.</returns>
		public DocumentPage GetPageContent(int index)
		{
			//get data from file
			DocumentPage data;
			using (TextReader reader = new StreamReader(new FileStream(PageContentFilePath(index), FileMode.Open)))
			{
				data = new XmlSerializer(typeof(DocumentPage)).Deserialize(reader) as DocumentPage;
			}

			//add document reference
			data.Document = this;

			return data;
		}

		/// <summary>
		/// Retrieves document page initial slice data.
		/// </summary>
		/// <param name="index">Page index.</param>
		/// <returns>Page initial slice data.</returns>
		public RootSection GetPageInitialSlice(int index)
		{
			//get data from file
			RootSection data;
			using (TextReader reader = new StreamReader(new FileStream(InitialPageSliceFilePath(index), FileMode.Open)))
			{
				data = new XmlSerializer(typeof(RootSection)).Deserialize(reader) as RootSection;
			}

			//add document project reference
			//data.Project = this;

			return data;
		}

		/// <summary>
		/// Retrieves document page final slice data.
		/// </summary>
		/// <param name="index">Page index.</param>
		/// <returns>Page final slice data.</returns>
		public RootSection GetPageFinalSlice(int index)
		{
			//get data from file
			RootSection data;
			using (TextReader reader = new StreamReader(new FileStream(FinalPageSliceFilePath(index), FileMode.Open)))
			{
				data = new XmlSerializer(typeof(RootSection)).Deserialize(reader) as RootSection;
			}

			//add document project reference
			//data.Project = this;

			return data;
		}

		/// <summary>
		/// Saves project data to file.
		/// </summary>
		public void SaveToFile()
		{
			XmlSerializer ser = new XmlSerializer(typeof(DocumentProject));
			using (FileStream stream = new FileStream(ProjectFilePath(), FileMode.OpenOrCreate))
			{
				using (TextWriter writer = new StreamWriter(stream))
				{
					ser.Serialize(writer, this);
				}
			}
		}

		#endregion

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="fileIdentifier">File ID string.</param>
		/// <param name="pageCount">Number of document pages.</param>
		/// <param name="styles">Style list.</param>
		public DocumentProject(
			string fileIdentifier,
			int pageCount,
			IEnumerable<StyleData> styles)
		{
			//store properties
			FileIdentifier = fileIdentifier;
			PageCount = pageCount;

			//generate style dictionary
			var styleDict = new Dictionary<string, StyleData>();
			foreach (var style in styles)
				styleDict[style.IdString] = style;
			Styles = styleDict;
		}

		/// <summary>
		/// Empty constructor for initialization.
		/// </summary>
		public DocumentProject()
		{ }

		#region Factory methods

		/// <summary>
		/// Loads document project from file.
		/// </summary>
		/// <param name="filePath">File path.</param>
		/// <returns>Document project data.</returns>
		public static DocumentProject LoadFromFile(string filePath)
		{
			//get data from file
			DocumentProject data;
			using (TextReader reader = new StreamReader(new FileStream(filePath, FileMode.Open)))
			{
				data = new XmlSerializer(typeof(DocumentProject)).Deserialize(reader) as DocumentProject;
			}

			return data;
		}

		#endregion

		#endregion
	}
}
