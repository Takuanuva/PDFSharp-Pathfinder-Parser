using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace PdfDocumentData
{
	public class StyleData : IXmlSerializable
	{
		#region Properties
		
		/// <summary>
		/// Style family ID.
		/// </summary>
		public string StyleFamilyId { get; private set; }

		/// <summary>
		/// Name of the font family.
		/// </summary>
		public string FontFamily { get; private set; }

		/// <summary>
		/// Font bolding flag, true if bolded font, false otherwise.
		/// </summary>
		public bool IsFontBolded { get; private set; }

		/// <summary>
		/// Font italicisation flag, true if italicised font, false otherwise.
		/// </summary>
		public bool IsFontItalicised { get; private set; }

		/// <summary>
		/// Font fill color.
		/// </summary>
		public int FillColor { get; private set; }

		/// <summary>
		/// Font stroke color.
		/// </summary>
		public int StrokeColor { get; private set; }

		/// <summary>
		/// Font stroke width.
		/// </summary>
		public float StrokeWidth { get; private set; }

		/// <summary>
		/// Defined font size.
		/// </summary>
		public float FontSize { get; private set; }

		/// <summary>
		/// Actual font height (measured as distance between starting points of baseline and ascent line). 
		/// </summary>
		public float FontHeight { get; private set; }

		/// <summary>
		/// Style ID string.
		/// </summary>
		public string IdString { get; private set; }

		/// <summary>
		/// Number of document characters using the style.
		/// </summary>
		public int CharacterCount { get; private set; }

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
			//write element values
			writer.WriteElementString(nameof(StyleFamilyId), StyleFamilyId.ToString());
			writer.WriteElementString(nameof(FontFamily), FontFamily.ToString());
			writer.WriteElementString(nameof(IsFontBolded), IsFontBolded.ToString().ToLower());
			writer.WriteElementString(nameof(IsFontItalicised), IsFontItalicised.ToString().ToLower());
			writer.WriteElementString(nameof(FillColor), FillColor.ToString());
			writer.WriteElementString(nameof(StrokeColor), StrokeColor.ToString());
			writer.WriteElementString(nameof(StrokeWidth), StrokeWidth.ToString());
			writer.WriteElementString(nameof(FontSize), FontSize.ToString());
			writer.WriteElementString(nameof(FontHeight), FontHeight.ToString());
			writer.WriteElementString(nameof(IdString), IdString.ToString());
			writer.WriteElementString(nameof(CharacterCount), CharacterCount.ToString());
		}

		/// <summary>
		/// XML deserialization function.
		/// </summary>
		/// <param name="reader">XML reader.</param>
		public void ReadXml(XmlReader reader)
		{
			//seek first element
			reader.ReadToFollowing(nameof(StyleFamilyId));

			//read element values
			StyleFamilyId = reader.ReadElementContentAsString(nameof(StyleFamilyId), "");
			FontFamily = reader.ReadElementContentAsString(nameof(FontFamily), "");
			IsFontBolded = reader.ReadElementContentAsBoolean(nameof(IsFontBolded), "");
			IsFontItalicised = reader.ReadElementContentAsBoolean(nameof(IsFontItalicised), "");
			FillColor = reader.ReadElementContentAsInt(nameof(FillColor), "");
			StrokeColor = reader.ReadElementContentAsInt(nameof(StrokeColor), "");
			StrokeWidth = reader.ReadElementContentAsFloat(nameof(StrokeWidth), "");
			FontSize = reader.ReadElementContentAsFloat(nameof(FontSize), "");
			FontHeight = reader.ReadElementContentAsFloat(nameof(FontHeight), "");
			IdString = reader.ReadElementContentAsString(nameof(IdString), "");
			CharacterCount = reader.ReadElementContentAsInt(nameof(CharacterCount), "");
		}

		#endregion

		#endregion

		#region Constructors

		public StyleData(
			string styleFamilyId,
			string fontFamily,
			bool isFontBolded,
			bool isFontItalicised,
			int fillColor,
			int strokeColor,
			float strokeWidth,
			float fontSize,
			float fontHeight,
			string idString,
			int characterCount)
		{
			StyleFamilyId = styleFamilyId;
			FontFamily = fontFamily;
			IsFontBolded = isFontBolded;
			IsFontItalicised = isFontItalicised;
			FillColor = fillColor;
			StrokeColor = strokeColor;
			StrokeWidth = strokeWidth;
			FontSize = fontSize;
			FontHeight = fontHeight;
			IdString = idString;
			CharacterCount = characterCount;
		}

		/// <summary>
		/// Default constructor for serialization.
		/// </summary>
		public StyleData()
		{ }
		
		#endregion
	}
}
