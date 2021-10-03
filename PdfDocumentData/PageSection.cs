using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace PdfDocumentData
{
	/// <summary>
	/// Represents a distinct section of a page, optionally subdivided into smaller sub-sections.
	/// </summary>
	public class PageSection : IXmlSerializable
	{
		#region Properties

		#region Private storage fields

		/// <summary>
		/// Dictionary storing all child subsections indexed by their position identifiers.
		/// </summary>
		private Dictionary<ulong, PageSection> SubsectionDict = new Dictionary<ulong, PageSection>();

#warning TODO: MAKE PRIVATE AGAIN!
		/// <summary>
		/// Collection of text characters associated with the section.
		/// </summary>
		public HashSet<TextCharacter> SectionCharacters = null;

		/// <summary>
		/// Private storage field for the SectionText property.
		/// </summary>
		private string _SectionText = null;

		#region Color space

		/// <summary>
		/// Section's slice of the color space (for rendering purposes).
		/// </summary>
		private ColorSpaceContainer ColorSpace;

		/// <summary>
		/// Represents the color space assigned to a section.
		/// </summary>
		private class ColorSpaceContainer
		{
			#region Properties

			#region Private storage fields

			#region Constants
			
			private const float OuterColorContrast = 0.2f;

			private const float ParentReservedBrightestSpan = 0.1f;

			#endregion
			
			private Func<int, int, Tuple<float, float>> SpanR { get; }
			private Func<int, int, Tuple<float, float>> SpanG { get; }
			private Func<int, int, Tuple<float, float>> SpanB { get; }

			#endregion

			public int OuterColor { get; }

			public int InnerColor { get; }

			#endregion

			#region Constructors

			private ColorSpaceContainer(
				PageSection section,
				float minR,
				float maxR,
				float minG,
				float maxG,
				float minB,
				float maxB)
			{
				//calculate colors
				int ConvertToColor(float r, float g, float b)
				{
					return (unchecked((int)0xff000000)) | ((int)(255.0f * r) << 16) | ((int)(255.0f * g) << 8) | ((int)(255.0f * b) << 0);
				}
				InnerColor = ConvertToColor(
					maxR, 
					maxG, 
					maxB);
				OuterColor = ConvertToColor(
					maxR - OuterColorContrast,
					maxG - OuterColorContrast,
					maxB - OuterColorContrast);

				//calculate color span properties
				float spanMinR = minR;
				float spanMinG = minG;
				float spanMinB = minB;
				float spanMaxR = maxR - ((maxR - minR) * ParentReservedBrightestSpan);
				float spanMaxG = maxG - ((maxG - minG) * ParentReservedBrightestSpan);
				float spanMaxB = maxB - ((maxB - minB) * ParentReservedBrightestSpan);
				float spanWidthR = spanMaxR - spanMinR;
				float spanWidthG = spanMaxG - spanMinG;
				float spanWidthB = spanMaxB - spanMinB;
				
				//set color span functions
				{
					//declare span function generators
					Func<int, int, Tuple<float, float>> ColumnSpanFunction(float min, float max)
					{
						float mid = min + max / 2;
						return (int column, int row) =>
						{
							if (column % 2 == 0)
								return new Tuple<float, float>(min, mid);
							else
								return new Tuple<float, float>(mid, max);
						};
					}
					Func<int, int, Tuple<float, float>> RowSpanFunction(float min, float max)
					{
						float mid = min + max / 2;
						return (int column, int row) =>
						{
							if (row % 2 == 0)
								return new Tuple<float, float>(min, mid);
							else
								return new Tuple<float, float>(mid, max);
						};
					}
					Func<int, int, Tuple<float, float>> InvariableSpanFunction(float min, float max)
					{
						float mid = min + max / 2;
						return (int column, int row) =>
						{
							return new Tuple<float, float>(min, max);
						};
					}

					//determine required function types
					Func<float, float, Func<int, int, Tuple<float, float>>> WidestSpanFunction = InvariableSpanFunction;
					Func<float, float, Func<int, int, Tuple<float, float>>> MiddleSpanFunction = InvariableSpanFunction;
					Func<float, float, Func<int, int, Tuple<float, float>>> NarrowestSpanFunction = InvariableSpanFunction;
					if (section.ColumnCount > 1 && section.RowCount > 1)
					{
						if (section.BoundingBox.Width > section.BoundingBox.Height)
						{
							WidestSpanFunction = ColumnSpanFunction;
							MiddleSpanFunction = RowSpanFunction;
						}
						else
						{
							WidestSpanFunction = RowSpanFunction;
							MiddleSpanFunction = ColumnSpanFunction;
						}
					}
					else if (section.ColumnCount > 1)
					{
						WidestSpanFunction = ColumnSpanFunction;
					}
					else if (section.RowCount > 1)
					{
						WidestSpanFunction = RowSpanFunction;
					}

					//assign functions to color components
					if (spanWidthR >= spanWidthG && spanWidthR >= spanWidthB)
					{
						SpanR = WidestSpanFunction(spanMinR, spanMaxR);
						if (spanWidthG >= spanWidthB)
						{
							SpanG = MiddleSpanFunction(spanMinG, spanMaxG);
							SpanB = NarrowestSpanFunction(spanMinB, spanMaxB);
						}
						else
						{
							SpanB = MiddleSpanFunction(spanMinB, spanMaxB);
							SpanG = NarrowestSpanFunction(spanMinG, spanMaxG);
						}
					}
					else if (spanWidthG >= spanWidthR && spanWidthG >= spanWidthB)
					{
						SpanG = WidestSpanFunction(spanMinG, spanMaxG);
						if (spanWidthR >= spanWidthB)
						{
							SpanR = MiddleSpanFunction(spanMinR, spanMaxR);
							SpanB = NarrowestSpanFunction(spanMinB, spanMaxB);
						}
						else
						{
							SpanB = MiddleSpanFunction(spanMinB, spanMaxB);
							SpanR = NarrowestSpanFunction(spanMinR, spanMaxR);
						}
					}
					else
					{
						SpanB = WidestSpanFunction(spanMinB, spanMaxB);
						if (spanWidthR >= spanWidthG)
						{
							SpanR = MiddleSpanFunction(spanMinR, spanMaxR);
							SpanG = NarrowestSpanFunction(spanMinG, spanMaxG);
						}
						else
						{
							SpanG = MiddleSpanFunction(spanMinG, spanMaxG);
							SpanR = NarrowestSpanFunction(spanMinR, spanMaxR);
						}
					}
				}
			}

			public ColorSpaceContainer(RootSection root) : 
				this(
					root,
					OuterColorContrast,
					1.0f,
					OuterColorContrast,
					1.0f,
					OuterColorContrast,
					1.0f)
			{ }



			#region Factory methods

			/// <summary>
			/// Generates color space for given subsection.
			/// </summary>
			/// <param name="section" />
			/// <returns />
			public ColorSpaceContainer GenerateForSubsection(PageSection section)
			{
				//get properties
				int column = section.ParentColumnIndex;
				int row = section.ParentRowIndex;
				var spanR = SpanR(column, row);
				var spanG = SpanG(column, row);
				var spanB = SpanB(column, row);

				return new ColorSpaceContainer(
					section,
					spanR.Item1,
					spanR.Item2,
					spanG.Item1,
					spanG.Item2,
					spanB.Item1,
					spanB.Item2);
			}

			#endregion

			#endregion
				
		}

		#endregion

		#endregion

		/// <summary>
		/// Section ID.
		/// </summary>
		public long Id { get; private set; } = -1;

		/// <summary>
		/// Section's outer bounding box.
		/// </summary>
		public BoxCoords BoundingBox { get; protected set; }

		/// <summary>
		/// Outer color for section rendering.
		/// </summary>
		public int OuterColor { get { return ColorSpace.OuterColor; } }

		/// <summary>
		/// Inner color for section rendering.
		/// </summary>
		public int InnerColor { get { return ColorSpace.InnerColor; } }

		/// <summary>
		/// Section's resulting text.
		/// </summary>
		public string SectionText
		{
			get
			{
				if (_SectionText == null)
					_SectionText = GenerateSectionText();

				return _SectionText;
			}
			private set { _SectionText = value; }
		}

		#region Supersection data

		/// <summary>
		/// Parent supersection of which the object is a subsection of.
		/// </summary>
		public PageSection Parent { get; private set; }

		/// <summary>
		/// Root supersection reference.
		/// </summary>
		public virtual RootSection Root
		{
			get { return Parent.Root; }
		}

		/// <summary>
		/// Node depth within hierarchy, with 0 being the root node and every child relation incrementing the value by 1.
		/// </summary>
		public virtual int HierarchyDepth
		{
			get { return Parent.HierarchyDepth + 1; }
		}

		/// <summary>
		/// Index of the column occupied by the section within the parent.
		/// </summary>
		public int ParentColumnIndex { get; private set; }

		/// <summary>
		/// Index of the row occupied by the section within the parent.
		/// </summary>
		public int ParentRowIndex { get; private set; }

		#endregion

		#region Subsection data

		/// <summary>
		/// List of all subsections within the section.
		/// </summary>
		public IReadOnlyList<PageSection> Subsections
		{
			get
			{
				//create list
				List<PageSection> list = new List<PageSection>();
				if (RowCount > 1 || ColumnCount > 1)
					for (int row = 0; row < RowCount; row++)
						for (int column = 0; column < ColumnCount; column++)
							list.Add(SubsectionDict[ConvertToPositionId(column, row)]);

				return list;
			}
		}

		/// <summary>
		/// Column separation delimiters.
		/// </summary>
		public IReadOnlyList<float> ColumnDelims { get; private set; } = new List<float>();

		/// <summary>
		/// Row separation delimiters.
		/// </summary>
		public IReadOnlyList<float> RowDelims { get; private set; } = new List<float>();

		/// <summary>
		/// Number of columns the section is subdivided into.
		/// </summary>
		public int ColumnCount
		{
			get { return ColumnDelims.Count + 1; }
		}

		/// <summary>
		/// Number of rows the section is subdivided into.
		/// </summary>
		public int RowCount
		{
			get { return RowDelims.Count + 1; }
		}

		/// <summary>
		/// Number of subsections contained within the section.
		/// </summary>
		public int SubsectionCount
		{
			get
			{
				int count = RowCount * ColumnCount;
				if (count == 1)
					return 0;
				return count;
			}
		}

		#endregion

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
		public virtual void WriteXml(XmlWriter writer)
		{
			//write section ID
			writer.WriteElementString(nameof(Id), Id.ToString());

			//write row index
			writer.WriteElementString(nameof(ParentRowIndex), ParentRowIndex.ToString());

			//write column index
			writer.WriteElementString(nameof(ParentColumnIndex), ParentColumnIndex.ToString());

			//write row delims
			writer.WriteStartElement(nameof(RowDelims));
			foreach (var delim in RowDelims)
				writer.WriteElementString("VAL", delim.ToString());
			writer.WriteEndElement();

			//write column delims
			writer.WriteStartElement(nameof(ColumnDelims));
			foreach (var delim in ColumnDelims)
				writer.WriteElementString("VAL", delim.ToString());
			writer.WriteEndElement();

			//write subsection list
			writer.WriteStartElement(nameof(SubsectionDict));
			foreach (var kv in SubsectionDict)
			{
				writer.WriteStartElement(nameof(PageSection));
				kv.Value.WriteXml(writer);
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}

		/// <summary>
		/// XML deserialization function.
		/// </summary>
		/// <param name="reader">XML reader.</param>
		public virtual void ReadXml(XmlReader reader)
		{
			//read section ID
			if (!reader.IsStartElement(nameof(Id)))
				reader.ReadToFollowing(nameof(Id));
			Id = reader.ReadElementContentAsLong(nameof(Id), "");

			//read row index
			if (!reader.IsStartElement(nameof(ParentRowIndex)))
				reader.ReadToFollowing(nameof(ParentRowIndex));
			ParentRowIndex = reader.ReadElementContentAsInt(nameof(ParentRowIndex), "");

			//read column index
			if (!reader.IsStartElement(nameof(ParentColumnIndex)))
				reader.ReadToFollowing(nameof(ParentColumnIndex));
			ParentColumnIndex = reader.ReadElementContentAsInt(nameof(ParentColumnIndex), "");

			//read delims
			RowDelims = new List<float>();
			ColumnDelims = new List<float>();
			if (reader.ReadToDescendant("VAL"))
			{
				try
				{
					while (true)
						(RowDelims as List<float>).Add(reader.ReadElementContentAsFloat("VAL", ""));
				}
				catch (XmlException) { }
			}
			reader.ReadToNextSibling(nameof(ColumnDelims));
			if (reader.ReadToDescendant("VAL"))
			{
				try
				{
					while (true)
						(ColumnDelims as List<float>).Add(reader.ReadElementContentAsFloat("VAL", ""));
				}
				catch (XmlException) { }
			}
			
			//read subsections
			var subsections = new List<PageSection>();
			reader.ReadToNextSibling(nameof(SubsectionDict));
			if (!reader.IsEmptyElement)
			{
				reader.ReadToDescendant(nameof(PageSection));
				while (true)
				{
					PageSection section = new PageSection();
					section.ReadXml(reader.ReadSubtree());
					subsections.Add(section);
					reader.ReadEndElement();
					if (!reader.IsStartElement(nameof(PageSection))) break;
				}
				reader.ReadEndElement();
			}
			else
			{
				reader.ReadStartElement(nameof(SubsectionDict));
			}

			//process subsections
			SubsectionDict = new Dictionary<ulong, PageSection>();
			foreach (var subsection in subsections)
			{
				//add parent reference
				subsection.Parent = this;

				//add to child dictionary
				SubsectionDict[ConvertToPositionId(subsection.ParentColumnIndex, subsection.ParentRowIndex)] = subsection;
			}
		}

		#endregion

		#region Data integrity

		/// <summary>
		/// Performs the property validation process.
		/// </summary>
		protected void ValidateProperties()
		{
			//validate bounding box
			if (Parent != null)
				BoundingBox = Parent.GetSubsectionBoundingBox(ParentColumnIndex, ParentRowIndex);

			//store delim index values
			int columnDelimIndexMin = 0;
			int columnDelimIndexMax = ColumnDelims.Count - 1;
			int rowDelimIndexMin = 0;
			int rowDelimIndexMax = RowDelims.Count - 1;

			//remove column delims below permitted range
			for (; columnDelimIndexMin <= columnDelimIndexMax; columnDelimIndexMin++)
				if (ColumnDelims[columnDelimIndexMin] > BoundingBox.LeftX)
					break;

			//remove column delims above permitted range
			for (; columnDelimIndexMax >= columnDelimIndexMin; columnDelimIndexMax--)
				if (ColumnDelims[columnDelimIndexMax] < BoundingBox.RightX)
					break;

			//remove row delims below permitted range
			for (; rowDelimIndexMin <= rowDelimIndexMax; rowDelimIndexMin++)
				if (RowDelims[rowDelimIndexMin] > BoundingBox.BottomY)
					break;

			//remove row delims above permitted range
			for (; rowDelimIndexMax >= rowDelimIndexMin; rowDelimIndexMax--)
				if (RowDelims[rowDelimIndexMax] < BoundingBox.TopY)
					break;

			//check if dictionary regen is necessary
			if (columnDelimIndexMin != 0 ||
				columnDelimIndexMax != ColumnDelims.Count - 1 ||
				rowDelimIndexMin != 0 ||
				rowDelimIndexMax != RowDelims.Count - 1)
			{
				//generate new column delims
				List<float> newColumnDelims = new List<float>();
				for (int i = columnDelimIndexMin; i <= columnDelimIndexMax; i++)
					newColumnDelims.Add(ColumnDelims[i]);
				List<float> newRowDelims = new List<float>();
				for (int i = rowDelimIndexMin; i <= rowDelimIndexMax; i++)
					newRowDelims.Add(RowDelims[i]);

				//generate new subsection dictionary
				var newSubsectionDict = new Dictionary<ulong, PageSection>();
				for (int columnIndex = columnDelimIndexMin; columnIndex <= columnDelimIndexMax + 1; columnIndex++)
					for (int rowIndex = rowDelimIndexMin; rowIndex <= rowDelimIndexMax + 1; rowIndex++)
					{
						//get subsection
						var subsection = SubsectionDict[ConvertToPositionId(columnIndex, rowIndex)];

						//update subsection coords
						subsection.ParentColumnIndex = columnIndex - columnDelimIndexMin;
						subsection.ParentRowIndex = rowIndex - rowDelimIndexMin;

						//store subsection in new dictionary
						newSubsectionDict[ConvertToPositionId(subsection.ParentColumnIndex, subsection.ParentRowIndex)] = subsection;
					}

				//replace collections
				ColumnDelims = newColumnDelims;
				RowDelims = newRowDelims;
				SubsectionDict = newSubsectionDict;
			}

			//prevent degenerate node scenario
			PreventDegenerateNodes();

			//clean subsection dictionary if necessary
			if (SubsectionDict.Count > SubsectionCount)
			{
				var cleanedDict = new Dictionary<ulong, PageSection>();
				for (int columnIndex = columnDelimIndexMin; columnIndex <= columnDelimIndexMax + 1; columnIndex++)
					for (int rowIndex = rowDelimIndexMin; rowIndex <= rowDelimIndexMax + 1; rowIndex++)
					{
						ulong id = ConvertToPositionId(columnIndex, rowIndex);
						cleanedDict[id] = SubsectionDict[id];
					}
				SubsectionDict = cleanedDict;
			}

			//validate color space
			if (Parent == null)
				ColorSpace = new ColorSpaceContainer(this as RootSection);
			else
				ColorSpace = Parent.GetSubsectionColorSpace(this);

			//validate character collection
			var newCharacters = GenerateSectionCharacters();
			bool sectionCharactersChanged = SectionCharacters != newCharacters;
			SectionCharacters = newCharacters;

			//validate subsections
			foreach (var kv in SubsectionDict)
				kv.Value.ValidateProperties();

			//invalidate resulting text
			if (sectionCharactersChanged)
				InvalidateText();
		}

		/// <summary>
		/// Invlidates section text.
		/// </summary>
		private void InvalidateText()
		{
			//invalidate parent text
			if (_SectionText != null &&
				Parent != null)
				Parent.InvalidateText();

			//invalidate own text
			SectionText = null;
		}

		/// <summary>
		/// Should be called after every operation which would result in reduction of the number of rows and/or columns. Prevents degenerate nodes from occurring, I.E.: sections containing only one child element encompassing the entirety of the section's area.
		/// </summary>
		private void PreventDegenerateNodes()
		{
			//check if degenerate node scenario did not occur
			if (ColumnCount > 1 ||
				RowCount > 1 ||
				SubsectionDict.Count == 0)
				return;

			//get subsection
			PageSection subsection = GetSubsection(0, 0);
		
			//copy subsection's delims
			ColumnDelims = subsection.ColumnDelims;
			RowDelims = subsection.RowDelims;

			//clear subsection dictionary
			SubsectionDict.Clear();

			//claim subsection's child nodes
			foreach (var kv in subsection.SubsectionDict)
			{
				//get child
				PageSection subsectionChild = kv.Value;

				//replace parent
				subsectionChild.Parent = this;

				//add child to subsection dictionary
				SubsectionDict[
					ConvertToPositionId(
						subsectionChild.ParentColumnIndex,
						subsectionChild.ParentRowIndex)] = subsectionChild;
			}

			//clear subsection's child dictionary
			subsection.SubsectionDict.Clear();

			//remove subsection
			subsection.Remove();

			//validate properties
			ValidateProperties();
		}
		
		/// <summary>
		/// Erases node and all of it's child nodes.
		/// </summary>
		private void Remove()
		{
			//remove from parent if not done yet
			if (Parent != null)
			{
				//remove parent reference
				Parent = null;
			}

			//erase children
			foreach (var kv in SubsectionDict)
			{
				//remove parent reference
				kv.Value.Parent = null;

				//erase node
				kv.Value.Remove();
			}

			//clear subsection list
			SubsectionDict.Clear();
		}

		#endregion

		#region Subsection methods

		/// <summary>
		/// Converts coordinate set into subsection position identifier.
		/// </summary>
		/// <param name="columnIndex">Column index.</param>
		/// <param name="rowIndex">Row index.</param>
		/// <returns>Subsection position identifier.</returns>
		private ulong ConvertToPositionId(int columnIndex, int rowIndex)
		{
			//check if index values are out of bounds
			if (rowIndex < 0)
				throw new IndexOutOfRangeException($"Row index below zero (value {rowIndex})!");
			if (columnIndex < 0)
				throw new IndexOutOfRangeException($"Column index below zero (value {columnIndex})!");
			if (rowIndex >= RowCount)
				throw new IndexOutOfRangeException($"Row index above maximum (value {rowIndex}, max index {RowCount - 1})!");
			if (columnIndex >= ColumnCount)
				throw new IndexOutOfRangeException($"Column index above maximum (value {columnIndex}, max index {ColumnCount - 1})!");

			return unchecked(((ulong)((uint)rowIndex) << 32) | (ulong)((uint)columnIndex));
		}

		/// <summary>
		/// Retrieves subsection based on row and column index.
		/// </summary>
		/// <param name="columnIndex">Column index.</param>
		/// <param name="rowIndex">Row index.</param>
		/// <returns>Subsection.</returns>
		public PageSection GetSubsection(int columnIndex, int rowIndex)
		{
			return SubsectionDict[ConvertToPositionId(columnIndex, rowIndex)];
		}

		/// <summary>
		/// Generates bounding box for node at specified coords.
		/// </summary>
		/// <param name="columnIndex">Column index.</param>
		/// <param name="rowIndex">Row index.</param>
		/// <returns>Subsection bounding box.</returns>
		private BoxCoords GetSubsectionBoundingBox(int columnIndex, int rowIndex)
		{
			return new BoxCoords(
				(columnIndex == 0) ? BoundingBox.LeftX : ColumnDelims[columnIndex - 1],
				(columnIndex == ColumnDelims.Count) ? BoundingBox.RightX : ColumnDelims[columnIndex],
				(rowIndex == RowDelims.Count) ? BoundingBox.TopY : RowDelims[rowIndex],
				(rowIndex == 0) ? BoundingBox.BottomY : RowDelims[rowIndex - 1]);
		}

		/// <summary>
		/// Generates color space for given subsection.
		/// </summary>
		/// <param name="subsection" />
		/// <returns />
		private ColorSpaceContainer GetSubsectionColorSpace(PageSection subsection)
		{
			return ColorSpace.GenerateForSubsection(subsection);
		}

		#region Delim modification

		/// <summary>
		/// Possible sidings for operations which add/remove delims.
		/// </summary>
		public enum SubdivisionSiding
		{
			LowerSide,
			HigherSide
		}

		/// <summary>
		/// Sets all of the section delims. Throws <seealso cref="ArgumentException"/> if any existing delims are present.
		/// </summary>
		/// <param name="columnDelims" />
		/// <param name="rowDelims" />
		public void SetDelims(
			IReadOnlyCollection<float> columnDelims,
			IReadOnlyCollection<float> rowDelims)
		{
			//store delims
			ColumnDelims = new List<float>(columnDelims);
			RowDelims = new List<float>(rowDelims);

			//sort delims
			(ColumnDelims as List<float>).Sort();
			(RowDelims as List<float>).Sort();

			//remove existing subsections
			if (SubsectionDict == null)
				SubsectionDict = new Dictionary<ulong, PageSection>();
			else
			{
				foreach (var kv in SubsectionDict)
					if (kv.Value != null)
						kv.Value.Remove();
				SubsectionDict.Clear();
			}

			//generate subsections
			if (ColumnDelims.Count > 0 || RowDelims.Count > 0)
				for (int column = 0; column < ColumnCount; column++)
					for (int row = 0; row < RowCount; row++)
						SubsectionDict[ConvertToPositionId(column, row)] = new PageSection(this, column, row);
		}

		/// <summary>
		/// Adds a new column delim.
		/// </summary>
		/// <param name="newDelim">New delim position.</param>
		/// <param name="existingColumnSiding">Side of the new delim where the existing column should be placed.</param>
		public void AddColumnDelim(float newDelim, SubdivisionSiding existingColumnSiding)
		{
			//check if out of bounds
			if (newDelim <= BoundingBox.LeftX || newDelim >= BoundingBox.RightX)
				return;

			//find which column the delim is bisecting
			int bisectedColumnIndex = 0;
			for (; bisectedColumnIndex < ColumnDelims.Count; bisectedColumnIndex++)
				if (newDelim < ColumnDelims[bisectedColumnIndex])
					break;

			//add subsection to bisect if none are present
			if (SubsectionCount == 0)
				SubsectionDict[ConvertToPositionId(0, 0)] = new PageSection(this, 0, 0);

			//add delim value
			(ColumnDelims as List<float>).Add(newDelim);
			(ColumnDelims as List<float>).Sort();

			//adjust columns to the right of the bisection
			for (int columnIndex = ColumnCount - 2; columnIndex > bisectedColumnIndex; columnIndex--)
				for (int rowIndex = 0; rowIndex < RowCount; rowIndex++)
				{
					PageSection subsection = GetSubsection(columnIndex, rowIndex);
					subsection.ParentColumnIndex = columnIndex + 1;
					SubsectionDict[ConvertToPositionId(subsection.ParentColumnIndex, subsection.ParentRowIndex)] = subsection;
				}

			//adjust bisected column and add the new one
			switch (existingColumnSiding)
			{
				case SubdivisionSiding.LowerSide:
					//add new column
					for (int rowIndex = 0; rowIndex < RowCount; rowIndex++)
						SubsectionDict[ConvertToPositionId(bisectedColumnIndex + 1, rowIndex)] = new PageSection(this, bisectedColumnIndex + 1, rowIndex);

					break;

				case SubdivisionSiding.HigherSide:
					//adjust bisected column
					for (int rowIndex = 0; rowIndex < RowCount; rowIndex++)
					{
						PageSection subsection = GetSubsection(bisectedColumnIndex, rowIndex);
						subsection.ParentColumnIndex = bisectedColumnIndex + 1;
						SubsectionDict[ConvertToPositionId(subsection.ParentColumnIndex, subsection.ParentRowIndex)] = subsection;
					}

					//add new column
					for (int rowIndex = 0; rowIndex < RowCount; rowIndex++)
						SubsectionDict[ConvertToPositionId(bisectedColumnIndex, rowIndex)] = new PageSection(this, bisectedColumnIndex, rowIndex);

					break;

				default:
					throw new NotImplementedException();
			}

			//validate properties
			ValidateProperties();
		}

		/// <summary>
		/// Adds a new row delim.
		/// </summary>
		/// <param name="newDelim">New delim position.</param>
		/// <param name="existingRowSiding">Side of the new delim where the existing row should be placed.</param>
		public void AddRowDelim(float newDelim, SubdivisionSiding existingRowSiding)
		{
			//check if out of bounds
			if (newDelim <= BoundingBox.BottomY || newDelim >= BoundingBox.TopY)
				return;

			//find which row the delim is bisecting
			int bisectedRowIndex = 0;
			for (; bisectedRowIndex < RowDelims.Count; bisectedRowIndex++)
				if (newDelim < RowDelims[bisectedRowIndex])
					break;

			//add subsection to bisect if none are present
			if (SubsectionCount == 0)
				SubsectionDict[ConvertToPositionId(0, 0)] = new PageSection(this, 0, 0);

			//add delim value
			(RowDelims as List<float>).Add(newDelim);
			(RowDelims as List<float>).Sort();

			//adjust rows above the bisection
			for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
				for (int rowIndex = RowCount - 2; rowIndex > bisectedRowIndex; rowIndex--)
				{
					PageSection subsection = GetSubsection(columnIndex, rowIndex);
					subsection.ParentRowIndex = rowIndex + 1;
					SubsectionDict[ConvertToPositionId(subsection.ParentColumnIndex, subsection.ParentRowIndex)] = subsection;
				}

			//adjust bisected row and add the new one
			switch (existingRowSiding)
			{
				case SubdivisionSiding.LowerSide:
					//add new row
					for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
						SubsectionDict[ConvertToPositionId(columnIndex, bisectedRowIndex + 1)] = new PageSection(this, columnIndex, bisectedRowIndex + 1);

					break;

				case SubdivisionSiding.HigherSide:
					//adjust bisected row
					for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
					{
						PageSection subsection = GetSubsection(columnIndex, bisectedRowIndex);
						subsection.ParentRowIndex = bisectedRowIndex + 1;
						SubsectionDict[ConvertToPositionId(subsection.ParentColumnIndex, subsection.ParentRowIndex)] = subsection;
					}

					//add new column
					for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
						SubsectionDict[ConvertToPositionId(columnIndex, bisectedRowIndex)] = new PageSection(this, columnIndex, bisectedRowIndex);

					break;

				default:
					throw new NotImplementedException();
			}

			//validate properties
			ValidateProperties();
		}

		/// <summary>
		/// Attempts to modify a single column delim. Performs no changes if operation fails.
		/// </summary>
		/// <param name="delimIndex">Delim index.</param>
		/// <param name="newValue">New delim value.</param>
		public void ModifyColumnDelim(int delimIndex, float newValue)
		{
			//get available span
			float xMin;
			if (delimIndex == 0)
				xMin = BoundingBox.LeftX;
			else
				xMin = ColumnDelims[delimIndex - 1];
			float xMax;
			if (delimIndex == ColumnDelims.Count - 1)
				xMax = BoundingBox.RightX;
			else
				xMax = ColumnDelims[delimIndex + 1];

			//check if outside of span
			if (newValue <= xMin ||
				newValue >= xMax)
				return;

			//adjust delim
			(ColumnDelims as List<float>)[delimIndex] = newValue;

			//update sections bordering on adjusted delim
			for (int rowIndex = 0; rowIndex < RowCount; rowIndex++)
			{
				GetSubsection(delimIndex, rowIndex).ValidateProperties();
				GetSubsection(delimIndex + 1, rowIndex).ValidateProperties();
			}
		}

		/// <summary>
		/// Attempts to modify a single row delim. Performs no changes if operation fails.
		/// </summary>
		/// <param name="delimIndex">Delim index.</param>
		/// <param name="newValue">New delim value.</param>
		public void ModifyRowDelim(int delimIndex, float newValue)
		{
			//get available span
			float yMin;
			if (delimIndex == 0)
				yMin = BoundingBox.BottomY;
			else
				yMin = RowDelims[delimIndex - 1];
			float yMax;
			if (delimIndex == RowDelims.Count - 1)
				yMax = BoundingBox.TopY;
			else
				yMax = RowDelims[delimIndex + 1];

			//check if outside of span
			if (newValue <= yMin ||
				newValue >= yMax)
				return;

			//adjust delim
			(RowDelims as List<float>)[delimIndex] = newValue;

			//update sections bordering on adjusted delim
			for (int columnDelim = 0; columnDelim < ColumnCount; columnDelim++)
			{
				GetSubsection(columnDelim, delimIndex).ValidateProperties();
				GetSubsection(columnDelim, delimIndex + 1).ValidateProperties();
			}
		}

		/// <summary>
		/// Removes an existing column delim.
		/// </summary>
		/// <param name="delimIndex">Index of delim to be removed.</param>
		/// <param name="remainingColumnSiding">Column which should remain after delim removal.</param>
		public void RemoveColumnDelim(int delimIndex, SubdivisionSiding remainingColumnSiding)
		{
			//set starting column index
			int columnIndex;
			switch (remainingColumnSiding)
			{
				case SubdivisionSiding.LowerSide:
					columnIndex = delimIndex + 2;
					break;

				case SubdivisionSiding.HigherSide:
					columnIndex = delimIndex + 1;
					break;

				default:
					throw new NotImplementedException();
			}

			//adjust subsections
			for (; columnIndex < ColumnCount; columnIndex++)
				for (int rowIndex = 0; rowIndex < RowCount; rowIndex++)
				{
					PageSection subsection = GetSubsection(columnIndex, rowIndex);
					subsection.ParentColumnIndex = columnIndex - 1;
					SubsectionDict[ConvertToPositionId(subsection.ParentColumnIndex, subsection.ParentRowIndex)] = subsection;
				}

			//remove delim
			(ColumnDelims as List<float>).RemoveAt(delimIndex);

			//validate properties
			ValidateProperties();
		}

		/// <summary>
		/// Removes an existing row delim.
		/// </summary>
		/// <param name="delimIndex">Index of delim to be removed.</param>
		/// <param name="remainingRowSiding">Row which should remain after delim removal.</param>
		public void RemoveRowDelim(int delimIndex, SubdivisionSiding remainingRowSiding)
		{
			//set starting row index
			int rowIndex;
			switch (remainingRowSiding)
			{
				case SubdivisionSiding.LowerSide:
					rowIndex = delimIndex + 2;
					break;

				case SubdivisionSiding.HigherSide:
					rowIndex = delimIndex + 1;
					break;

				default:
					throw new NotImplementedException();
			}

			//adjust subsections
			for (; rowIndex < RowCount; rowIndex++)
				for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
				{
					PageSection subsection = GetSubsection(columnIndex, rowIndex);
					subsection.ParentRowIndex = rowIndex - 1;
					SubsectionDict[ConvertToPositionId(subsection.ParentColumnIndex, subsection.ParentRowIndex)] = subsection;
				}

			//remove delim
			(RowDelims as List<float>).RemoveAt(delimIndex);

			//validate properties
			ValidateProperties();
		}

		#endregion

		#endregion

		/// <summary>
		/// Locates deepest level subsection which a given point is a part of. 
		/// </summary>
		/// <param name="coords">Point coordinates.</param>
		/// <returns>Deepest page section that the point is a part of, or null if point lays outside the section bounding box.</returns>
		public PageSection FindSectionAtCoords(PointCoords coords)
		{
			//check if out of bounds
			if (coords.X < BoundingBox.LeftX ||
				coords.X > BoundingBox.RightX ||
				coords.Y < BoundingBox.TopY ||
				coords.Y > BoundingBox.BottomY)
				return null;

			//check if end node
			if (ColumnCount == 1 &&
				RowCount == 1)
				return this;

			//find subdivision which the point is a part of
			int column = 0;
			int row = 0;
			for (; column < ColumnDelims.Count; column++)
				if (ColumnDelims[column] > coords.X)
					break;
			for (; row < RowDelims.Count; row++)
				if (RowDelims[row] > coords.Y)
					break;

			return SubsectionDict[ConvertToPositionId(column, row)].FindSectionAtCoords(coords);
		}

		/// <summary>
		/// Generates section character collection.
		/// </summary>
		/// <returns />
		protected virtual HashSet<TextCharacter> GenerateSectionCharacters()
		{
			//generate character collection
			HashSet<TextCharacter> characters = new HashSet<TextCharacter>();
			foreach (var character in Parent.SectionCharacters)
				if (character.BoundingBox.Center.X > BoundingBox.LeftX &&
					character.BoundingBox.Center.X < BoundingBox.RightX &&
					character.BoundingBox.Center.Y > BoundingBox.BottomY &&
					character.BoundingBox.Center.Y < BoundingBox.TopY)
					characters.Add(character);
			return characters;
		}

		/// <summary>
		/// Generates section text.
		/// </summary>
		/// <returns />
		protected virtual string GenerateSectionText()
		{
			//check if empty
			if (SectionCharacters.Count == 0)
				return "";

			//initialize output string
			string output = "\t";//"<s>";

			//check if divided into subsections
			if (SubsectionCount > 1)
			{
				//check if table
				if (ColumnCount > 1 && RowCount > 1)
				{
					//generate rows
					for (int rowIndex = RowCount - 1; rowIndex >= 0; rowIndex--)
					{
						//add row start
						output += "<row>";

						//add first cell
						output += GetSubsection(0, rowIndex).SectionText;

						//add remaining cells
						for (int columnIndex = 1; columnIndex < ColumnCount; columnIndex++)
							output += "<|>" + GetSubsection(columnIndex, rowIndex).SectionText;

						//add row end
						output += "</row>";
					}
				}
				else
				{
					//generate string
					for (int rowIndex = RowCount - 1; rowIndex >= 0; rowIndex--)
						for (int columnIndex = 0; columnIndex < ColumnCount; columnIndex++)
							output += GetSubsection(columnIndex, rowIndex).SectionText;
				}
			}
			else
			{
				//sort characters
				List<TextCharacter> characters = new List<TextCharacter>(SectionCharacters);
				{
					//declare sort function
					bool swapCharacters(TextCharacter earlier, TextCharacter later)
					{
						//check for vertical overlap
						if (earlier.BoundingBox.BottomY < later.BoundingBox.TopY &&
							earlier.BoundingBox.TopY > later.BoundingBox.BottomY)
							return later.BoundingBox.LeftX < earlier.BoundingBox.LeftX;

						return later.BoundingBox.TopY > earlier.BoundingBox.TopY;
					}

					//sort list
					while (true)
					{
						//initialize flag
						bool swapped = false;

						//iterate over characters
						for (int index = 0; index < characters.Count - 1; index++)
							if (swapCharacters(characters[index], characters[index + 1]))
							{
								swapped = true;
								var buffer = characters[index];
								characters[index] = characters[index + 1];
								characters[index + 1] = buffer;
							}

						//check if swap was performed
						if (!swapped)
							break;
					}
				}

				//generate string
				string lastTextStyleId = "";
				string lastTextStyleCloseTag = "";
				foreach (var character in characters)
				{
					//check if style changed
					if (lastTextStyleId != character.TextStyleId)
					{
						//update style ID
						lastTextStyleId = character.TextStyleId;

						//add previous style's close tag
						output += lastTextStyleCloseTag;

						//add style tag
						//output += $"[{character.TextStyle.FontFamily}|{character.TextStyle.IsFontBolded}|{character.TextStyle.IsFontItalicised}|{character.TextStyle.FontSize}|{character.TextStyle.FillColor.ToString("X8")}|{character.TextStyle.StrokeColor.ToString("X8")}|{character.TextStyle.StrokeWidth}]";
						output += $"<h_{character.TextStyle.FontHeight}>" + (character.TextStyle.IsFontBolded ? "<b>" : "") + (character.TextStyle.IsFontItalicised ? "<i>" : "");

						//generate style close tag
						lastTextStyleCloseTag = (character.TextStyle.IsFontItalicised ? "</i>" : "") + (character.TextStyle.IsFontBolded ? "</b>" : "") + $"</h_{character.TextStyle.FontHeight}>";
					}

					//add character value
					output += character.CharacterValue;
				}

				//add close tag
				output += lastTextStyleCloseTag;
			}

			//parse for paragraph delims
			output = Regex.Replace(
				output,
				@"([\.\?\!](?:<\/[^\s\>]+>)*)((?:<[^\s\>\/]+>)*[^\s\d\<])",
				@"$1" + Environment.NewLine + @"$2");
			
			//add end tag
			output += Environment.NewLine;//"</s>";

			return output;
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Default constructor for serialization purposes.
		/// </summary>
		public PageSection()
		{
			//initialize collections
			ColumnDelims = new List<float>();
			RowDelims = new List<float>();
			SubsectionDict = new Dictionary<ulong, PageSection>();
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent">Parent supersection.</param>
		/// <param name="parentColumnIndex">Column index of section within parent.</param>
		/// <param name="parentRowIndex">Row index of section within parent.</param>
		protected PageSection(
			PageSection parent,
			int parentColumnIndex,
			int parentRowIndex)
		{
			//initialize collections
			ColumnDelims = new List<float>();
			RowDelims = new List<float>();
			SubsectionDict = new Dictionary<ulong, PageSection>();
			
			//store prooperty values
			Parent = parent;
			ParentColumnIndex = parentColumnIndex;
			ParentRowIndex = parentRowIndex;

			//get ID
			try { Id = Root.AssignId(this); }
			catch (NullReferenceException) { }
		}

		#endregion
	}

	/// <summary>
	/// Represents the root section of the page sectioning.
	/// </summary>
	public class RootSection : PageSection
	{
		#region Properties

		#region Private storage fields

		/// <summary>
		/// ID number counter.
		/// </summary>
		private long IdCounter = 0;

		/// <summary>
		/// Private storage field for the PageContent property.
		/// </summary>
		private DocumentPage _PageContent = null;

		#endregion
		
		/// <summary>
		/// Root section reference.
		/// </summary>
		public override RootSection Root
		{
			get { return this; }
		}

		/// <summary>
		/// Node depth within hierarchy, with 0 being the root node and every child relation incrementing the value by 1.
		/// </summary>
		public override int HierarchyDepth
		{
			get { return 0; }
		}

		/// <summary>
		/// Page content area.
		/// </summary>
		public BoxCoords ContentArea
		{
			get { return BoundingBox; }
			set
			{
				//update bounding box
				BoundingBox = value;

				//validate properties
				ValidateProperties();
			}
		}

		/// <summary>
		/// Contents of the page being sliced.
		/// </summary>
		public DocumentPage PageContent
		{
			get { return _PageContent; }
			set
			{
				_PageContent = value;
				ValidateProperties();
			}
		}

		#endregion

		#region Methods

		#region XML serialization
		
		/// <summary>
		/// XML serialization function.
		/// </summary>
		/// <param name="writer">XML writer.</param>
		public override void WriteXml(XmlWriter writer)
		{
			//write ID counter
			writer.WriteElementString(nameof(IdCounter), IdCounter.ToString());

			//write content area
			writer.WriteStartElement(nameof(ContentArea));
			ContentArea.WriteXml(writer);
			writer.WriteEndElement();

			//write base properties
			base.WriteXml(writer);
		}

		/// <summary>
		/// XML deserialization function.
		/// </summary>
		/// <param name="reader">XML reader.</param>
		public override void ReadXml(XmlReader reader)
		{
			//seek first element
			reader.ReadToFollowing(nameof(IdCounter));

			//read ID counter
			IdCounter = reader.ReadElementContentAsLong(nameof(IdCounter), "");

			//read content area
			BoundingBox = new BoxCoords();
			BoundingBox.ReadXml(reader.ReadSubtree());

			//read base properties
			base.ReadXml(reader);

			//perform the property validation process
			ValidateProperties();
		}

		#endregion

		/// <summary>
		/// Generates section character collection.
		/// </summary>
		/// <returns />
		protected override HashSet<TextCharacter> GenerateSectionCharacters()
		{
			//check if no content is associated
			if (PageContent == null)
				return new HashSet<TextCharacter>();

			//generate character collection
			HashSet<TextCharacter> characters = new HashSet<TextCharacter>();
			foreach (var character in PageContent.Characters)
				if (character.BoundingBox.Center.X > BoundingBox.LeftX &&
					character.BoundingBox.Center.X < BoundingBox.RightX &&
					character.BoundingBox.Center.Y > BoundingBox.BottomY &&
					character.BoundingBox.Center.Y < BoundingBox.TopY)
					characters.Add(character);
			return characters;
		}

		/// <summary>
		/// Generates section text.
		/// </summary>
		/// <returns />
		protected override string GenerateSectionText()
		{
			if (PageContent == null)
				return "PAGE CONTENT INVALID!";
			return $"<page={ PageContent.Index }>{ base.GenerateSectionText() }</page={ PageContent.Index }>";
		}

		/// <summary>
		/// Assigns ID number to given section.
		/// </summary>
		/// <param name="section">Section which requires an ID value.</param>
		/// <returns>ID number.</returns>
		internal long AssignId(PageSection section)
		{
			//check if ID is not required
			if (section.Id != -1) return -1;

			return IdCounter++;
		}
		
		/// <summary>
		/// Saves section data to file.
		/// </summary>
		public void SaveToFile(DocumentProject project, int pageIndex, bool isFinal)
		{
			XmlSerializer ser = new XmlSerializer(typeof(RootSection));
			using (FileStream stream = new FileStream((isFinal ? project.FinalPageSliceFilePath(pageIndex) : project.InitialPageSliceFilePath(pageIndex)), FileMode.Create))
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
		/// Default constructor for serialization.
		/// </summary>
		public RootSection() { }
		
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="contentArea">Page content area.</param>
		public RootSection(
			BoxCoords contentArea) :
			base(
				null,
				0,
				0)
		{
			//store properties
			BoundingBox = contentArea;
		}

		#endregion
	}

}