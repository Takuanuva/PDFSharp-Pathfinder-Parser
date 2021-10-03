using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace PdfDocumentData
{
	public class DocumentPage : IXmlSerializable
	{
		#region Sub-types

		/// <summary>
		/// Represents a bitmap occuring within the page.
		/// </summary>
		public class PageBitmap : IXmlSerializable
		{
			#region Properties
			
			/// <summary>
			/// Bitmap file ID.
			/// </summary>
			public string BitmapId { get; private set; }

			/// <summary>
			/// Bitmap's outline path.
			/// </summary>
			public Path Outline { get; private set; }

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
				//write bimap ID
				writer.WriteElementString(nameof(BitmapId), BitmapId.ToString());

				//write bitmap outline
				writer.WriteStartElement(nameof(Outline));
				Outline.WriteXml(writer);
				writer.WriteEndElement();
			}

			/// <summary>
			/// XML deserialization function.
			/// </summary>
			/// <param name="reader">XML reader.</param>
			public void ReadXml(XmlReader reader)
			{
				//seek first element
				reader.ReadToFollowing(nameof(BitmapId));

				//read bitmap id
				BitmapId = reader.ReadElementContentAsString(nameof(BitmapId), "");

				//read bitmap outline
				Outline = new Path();
				Outline.ReadXml(reader.ReadSubtree());
			}

			#endregion

			#endregion

			#region Constructors

			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="bitmapId">Bitmap file ID.</param>
			/// <param name="outline">Bitmap's outline path.</param>
			public PageBitmap(
				string bitmapId,
				Path outline)
			{
				//store properties
				BitmapId = bitmapId;
				Outline = outline;
			}

			/// <summary>
			/// Default constructor for serialization.
			/// </summary>
			public PageBitmap()
			{ }

			#endregion
		}

		/// <summary>
		/// Represents a vector path.
		/// </summary>
		public class Path : IXmlSerializable
		{
			#region Sub-types

			/// <summary>
			/// Represents a simple line segment defined by a pair of points.
			/// </summary>
			public class LineSegment : IXmlSerializable
			{
				#region Properties
				
				/// <summary>
				/// Starting point of the segment.
				/// </summary>
				public PointCoords Start { get; private set; }

				/// <summary>
				/// Ending point of the segment.
				/// </summary>
				public PointCoords End { get; private set; }

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
					//write points
					writer.WriteStartElement(nameof(Start));
					Start.WriteXml(writer);
					writer.WriteEndElement();
					writer.WriteStartElement(nameof(End));
					End.WriteXml(writer);
					writer.WriteEndElement();
				}

				/// <summary>
				/// XML deserialization function.
				/// </summary>
				/// <param name="reader">XML reader.</param>
				public void ReadXml(XmlReader reader)
				{
					//read points
					reader.ReadToFollowing(nameof(Start));
					Start = new PointCoords();
					Start.ReadXml(reader.ReadSubtree());
					reader.ReadToFollowing(nameof(End));
					End = new PointCoords();
					End.ReadXml(reader.ReadSubtree());
				}

				#endregion

				#endregion

				#region Constructors

				/// <summary>
				/// Constructor.
				/// </summary>
				/// <param name="start">Starting point of the segment.</param>
				/// <param name="end">Ending point of the segment.</param>
				public LineSegment(
					PointCoords start,
					PointCoords end)
				{
					//store property values
					Start = start;
					End = end;
				}

				/// <summary>
				/// Default constructor for serialization.
				/// </summary>
				public LineSegment()
				{ }

				#endregion
			}

			#endregion

			#region Properties

			#region Private storage fields
			
			/// <summary>
			/// Private storage field for the BoundingBox property.
			/// </summary>
			private BoxCoords _BoundingBox = null;

			#endregion

			/// <summary>
			/// List of path segments.
			/// </summary>
			public IReadOnlyList<LineSegment> PathSegments { get; private set; }

			/// <summary>
			/// Path bounding box.
			/// </summary>
			[XmlIgnore]
			public BoxCoords BoundingBox
			{
				get
				{
					//generate value if required
					if (_BoundingBox == null)
					{
						float leftX = float.PositiveInfinity;
						float rightX = float.NegativeInfinity;
						float topY = float.PositiveInfinity;
						float bottomY = float.NegativeInfinity;
						foreach (var segment in PathSegments)
						{
							leftX = Math.Min(leftX, Math.Min(segment.Start.X, segment.End.X));
							rightX = Math.Max(rightX, Math.Max(segment.Start.X, segment.End.X));
							topY = Math.Min(topY, Math.Min(segment.Start.Y, segment.End.Y));
							bottomY = Math.Max(bottomY, Math.Max(segment.Start.Y, segment.End.Y));
						}
						_BoundingBox =
							new BoxCoords(
								leftX,
								rightX,
								topY,
								bottomY);
					}

					return _BoundingBox;
				}
			}

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
				//write path segments
				writer.WriteStartElement(nameof(PathSegments));
				foreach (var segment in PathSegments)
				{
					writer.WriteStartElement(nameof(LineSegment));
					segment.WriteXml(writer);
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
				try
				{
					//read segments
					reader.ReadToFollowing(nameof(PathSegments));
					PathSegments = new List<LineSegment>();
					if (reader.ReadToDescendant(nameof(LineSegment)))
					{
						do
						{
							LineSegment obj = new LineSegment();
							obj.ReadXml(reader.ReadSubtree());
							(PathSegments as List<LineSegment>).Add(obj);
						}
						while (reader.ReadToNextSibling(nameof(LineSegment)));
					}
				}
				catch
				{
					int i = 0;
				}
			}

			#endregion

			#endregion

			#region Constructors

			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="pathSegments">Path segment enumerable.</param>
			public Path(
				IEnumerable<LineSegment> pathSegments)
			{
				//store property value
				PathSegments = new List<LineSegment>(pathSegments);
			}

			/// <summary>
			/// Default constructor for serialization.
			/// </summary>
			public Path()
			{ }

			#endregion
		}

		#endregion

		#region Properties

		/// <summary>
		/// Page's source document.
		/// </summary>
		[XmlIgnore]
		public DocumentProject Document { get; set; }

		/// <summary>
		/// Page index.
		/// </summary>
		public int Index { get; private set; }

		/// <summary>
		/// List of characters defined for the page.
		/// </summary>
		public IReadOnlyList<TextCharacter> Characters { get; private set; }

		/// <summary>
		/// List of characters defined for the page.
		/// </summary>
		public IReadOnlyList<PageBitmap> Bitmaps { get; private set; }

		/// <summary>
		/// List of characters defined for the page.
		/// </summary>
		public IReadOnlyList<Path> Paths { get; private set; }

		/// <summary>
		/// Page's media box.
		/// </summary>
		public BoxCoords MediaBox { get; private set; }

		/// <summary>
		/// Page's crop box.
		/// </summary>
		public BoxCoords CropBox { get; private set; } = null;

		/// <summary>
		/// Page's trim box.
		/// </summary>
		public BoxCoords TrimBox { get; private set; } = null;

		/// <summary>
		/// Page's art box.
		/// </summary>
		public BoxCoords ArtBox { get; private set; } = null;

		/// <summary>
		/// Page's bleed box.
		/// </summary>
		public BoxCoords BleedBox { get; private set; } = null;

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
			//write page index
			writer.WriteElementString(nameof(Index), Index.ToString());

			//write page boxes
			writer.WriteStartElement(nameof(MediaBox));
			MediaBox.WriteXml(writer);
			writer.WriteEndElement();
			writer.WriteStartElement(nameof(CropBox));
			if (CropBox != null)
				CropBox.WriteXml(writer);
			writer.WriteEndElement();
			writer.WriteStartElement(nameof(TrimBox));
			if (TrimBox != null)
				TrimBox.WriteXml(writer);
			writer.WriteEndElement();
			writer.WriteStartElement(nameof(ArtBox));
			if (ArtBox != null)
				ArtBox.WriteXml(writer);
			writer.WriteEndElement();
			writer.WriteStartElement(nameof(BleedBox));
			if (BleedBox != null)
				BleedBox.WriteXml(writer);
			writer.WriteEndElement();

			//write characters
			writer.WriteStartElement(nameof(Characters));
			foreach (var character in Characters)
			{
				writer.WriteStartElement(nameof(TextCharacter));
				character.WriteXml(writer);
				writer.WriteEndElement();
			}
			writer.WriteEndElement();

			//write bitmaps
			writer.WriteStartElement(nameof(Bitmaps));
			foreach (var bitmap in Bitmaps)
			{
				writer.WriteStartElement(nameof(PageBitmap));
				bitmap.WriteXml(writer);
				writer.WriteEndElement();
			}
			writer.WriteEndElement();

			//write paths
			writer.WriteStartElement(nameof(Paths));
			foreach (var path in Paths)
			{
				writer.WriteStartElement(nameof(Path));
				path.WriteXml(writer);
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
			//seek first element
			reader.ReadToFollowing(nameof(Index));

			//read page index
			Index = reader.ReadElementContentAsInt(nameof(Index), "");

			//read page boxes
			MediaBox = new BoxCoords();
			MediaBox.ReadXml(reader.ReadSubtree());
			if (reader.ReadToNextSibling(nameof(CropBox)))
			{
				CropBox = new BoxCoords();
				CropBox.ReadXml(reader.ReadSubtree());
			}
			if (reader.ReadToNextSibling(nameof(TrimBox)))
			{
				TrimBox = new BoxCoords();
				TrimBox.ReadXml(reader.ReadSubtree());
			}
			if (reader.ReadToNextSibling(nameof(ArtBox)))
			{
				ArtBox = new BoxCoords();
				ArtBox.ReadXml(reader.ReadSubtree());
			}
			if (reader.ReadToNextSibling(nameof(BleedBox)))
			{
				BleedBox = new BoxCoords();
				BleedBox.ReadXml(reader.ReadSubtree());
			}

			//read characters
			Characters = new List<TextCharacter>();
			reader.ReadToNextSibling(nameof(Characters));
			if (reader.ReadToDescendant(nameof(TextCharacter)))
			{
				do
				{
					TextCharacter obj = new TextCharacter();
					obj.ReadXml(reader.ReadSubtree());
					(Characters as List<TextCharacter>).Add(obj);
				}
				while (reader.ReadToNextSibling(nameof(TextCharacter)));
			}
			
			//add page reference to characters
			foreach (var character in Characters)
				character.ParentPage = this;

			//read bitmaps
			Bitmaps = new List<PageBitmap>();
			reader.ReadToNextSibling(nameof(Bitmaps));
			if (reader.ReadToDescendant(nameof(PageBitmap)))
			{
				do
				{
					PageBitmap obj = new PageBitmap();
					obj.ReadXml(reader.ReadSubtree());
					(Bitmaps as List<PageBitmap>).Add(obj);
				}
				while (reader.ReadToNextSibling(nameof(PageBitmap)));
			}

			//read paths
			Paths = new List<Path>();
			reader.ReadToNextSibling(nameof(Paths));
			if (reader.ReadToDescendant(nameof(Path)))
			{
				do
				{
					Path obj = new Path();
					obj.ReadXml(reader.ReadSubtree());
					(Paths as List<Path>).Add(obj);
				}
				while (reader.ReadToNextSibling(nameof(Path)));
			}
		}

		#endregion

		/// <summary>
		/// Saves page data to file.
		/// </summary>
		public void SaveToFile()
		{
			XmlSerializer ser = new XmlSerializer(typeof(DocumentPage));
			using (FileStream stream = new FileStream(Document.PageContentFilePath(Index), FileMode.OpenOrCreate))
			{
				using (TextWriter writer = new StreamWriter(stream))
				{
					ser.Serialize(writer, this);
				}
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="document">Source document.</param>
		/// <param name="index">Page index.</param>
		/// <param name="mediaBox">Page media box.</param>
		/// <param name="cropBox">Page crop box (can be null).</param>
		/// <param name="trimBox">Page trim box (can be null).</param>
		/// <param name="artBox">Page art box (can be null).</param>
		/// <param name="bleedBox">Page bleed box (can be null).</param>
		/// <param name="characters">Page character list.</param>
		/// <param name="bitmaps">Page bitmap list.</param>
		/// <param name="paths">Page path list.</param>
		public DocumentPage(
			DocumentProject document,
			int index,
			BoxCoords mediaBox,
			BoxCoords cropBox,
			BoxCoords trimBox,
			BoxCoords artBox,
			BoxCoords bleedBox,
			IReadOnlyList<TextCharacter> characters,
			IReadOnlyList<PageBitmap> bitmaps,
			IReadOnlyList<Path> paths)
		{
			//store property values
			Document = document;
			Index = index;
			MediaBox = mediaBox;
			CropBox = cropBox;
			TrimBox = trimBox;
			ArtBox = artBox;
			BleedBox = bleedBox;

			//generate lists
			Characters = new List<TextCharacter>(characters);
			Bitmaps = new List<PageBitmap>(bitmaps);
			Paths = new List<Path>(paths);
		}

		/// <summary>
		/// Default constructor for serialization.
		/// </summary>
		public DocumentPage()
		{ }

		#endregion
	}
}
