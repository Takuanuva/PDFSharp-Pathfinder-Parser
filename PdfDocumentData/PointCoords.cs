using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace PdfDocumentData
{
	/// <summary>
	/// Stores a pair of coordinaes defining a point.
	/// </summary>
	public class PointCoords : IXmlSerializable
	{
		#region Properties
		
		/// <summary>
		/// X coordinate.
		/// </summary>
		public float X { get; private set; }

		/// <summary>
		/// Y coordinate.
		/// </summary>
		public float Y { get; private set; }

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
			writer.WriteElementString(nameof(X), X.ToString());
			writer.WriteElementString(nameof(Y), Y.ToString());
		}

		/// <summary>
		/// XML deserialization function.
		/// </summary>
		/// <param name="reader">XML reader.</param>
		public void ReadXml(XmlReader reader)
		{
			//seek to first element
			reader.ReadToFollowing(nameof(X));

			//read element values
			X = reader.ReadElementContentAsFloat(nameof(X), "");
			Y = reader.ReadElementContentAsFloat(nameof(Y), "");
		}

		#endregion

		/// <summary>
		/// Calculates distance between the point and target.
		/// </summary>
		/// <param name="target">Target point.</param>
		/// <returns>Distance between points.</returns>
		public float DistanceFrom(PointCoords target)
		{
			float a = X - target.X;
			float b = Y - target.Y;
			return (float)Math.Sqrt((a * a) + (b * b));
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="x">X coordinate.</param>
		/// <param name="y">Y coordinate.</param>
		public PointCoords(
			float x,
			float y)
		{
			//store property values
			X = x;
			Y = y;
		}

		public PointCoords()
		{ }

		#endregion
	}
}
