using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace PdfDocumentData
{
	/// <summary>
	/// Represents a single text character within a PDF file.
	/// </summary>
	public class TextCharacter : IXmlSerializable
	{
		#region Properties

		#region Private storage fields

		#region Regular expressions

		/// <summary>
		/// Determines whether the character is whitespace or not.
		/// </summary>
		private static Regex IsWhitespaceRegex = new Regex(@"\s", RegexOptions.Compiled);

		#endregion
		
		#endregion

		/// <summary>
		/// Page containing the character.
		/// </summary>
		public DocumentPage ParentPage { get; set; }

		/// <summary>
		/// Represented character value.
		/// </summary>
		public char CharacterValue { get; private set; }

		/// <summary>
		/// Character's bounding box.
		/// </summary>
		public BoxCoords BoundingBox { get; private set; }

		/// <summary>
		/// Text style ID.
		/// </summary>
		public string TextStyleId { get; private set; }

		/// <summary>
		/// Text style data associated with the character.
		/// </summary>
		public StyleData TextStyle
		{
			get { return ParentPage.Document.Styles[TextStyleId]; }
		}

		/// <summary>
		/// Whitespace flag.
		/// </summary>
		public bool IsWhitespace
		{
			get { return IsWhitespaceRegex.IsMatch(CharacterValue.ToString()); }
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
			//write element values
			writer.WriteElementString(nameof(CharacterValue), ((int)CharacterValue).ToString());
			writer.WriteStartElement(nameof(BoundingBox));
			BoundingBox.WriteXml(writer);
			writer.WriteEndElement();
			writer.WriteElementString(nameof(TextStyleId), TextStyleId);
		}

		/// <summary>
		/// XML deserialization function.
		/// </summary>
		/// <param name="reader">XML reader.</param>
		public void ReadXml(XmlReader reader)
		{
			//seek first element
			reader.ReadToFollowing(nameof(CharacterValue));

			//read element values
			CharacterValue = (char)reader.ReadElementContentAsInt(nameof(CharacterValue), "");
			BoundingBox = new BoxCoords();
			BoundingBox.ReadXml(reader.ReadSubtree());
			reader.ReadToNextSibling(nameof(TextStyleId));
			TextStyleId = reader.ReadElementContentAsString(nameof(TextStyleId), "");
		}

		#endregion

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="characterValue" />
		/// <param name="boundingBox" />
		/// <param name="textStyleId" />
		public TextCharacter(
			char characterValue,
			BoxCoords boundingBox,
			string textStyleId)
		{
			//store property values
			ParentPage = null;
			CharacterValue = characterValue;
			BoundingBox = boundingBox;
			TextStyleId = textStyleId;
		}

		#endregion

		/// <summary>
		/// Default constructor for serialization.
		/// </summary>
		public TextCharacter()
		{ }

	}
}
