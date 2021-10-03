using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace PdfDocumentData
{
	/// <summary>
	/// Represents a rectangular box defined by a pair of corners.
	/// </summary>
	public class BoxCoords : IXmlSerializable
	{
		#region Properties
		
		/// <summary>
		/// Left edge of the box.
		/// </summary>
		public float LeftX { get; private set; }

		/// <summary>
		/// Right edge of the box.
		/// </summary>
		public float RightX { get; private set; }

		/// <summary>
		/// Bottom edge of the box.
		/// </summary>
		public float BottomY { get; private set; }

		/// <summary>
		/// Top edge of the box.
		/// </summary>
		public float TopY { get; private set; }

		/// <summary>
		/// Center point of the box.
		/// </summary>
		[XmlIgnore]
		public PointCoords Center
		{
			get
			{
				return new PointCoords(
					(LeftX + RightX) / 2, 
					(BottomY + TopY) / 2);
			}
		}

		/// <summary>
		/// Box width.
		/// </summary>
		[XmlIgnore]
		public float Width
		{
			get { return RightX - LeftX; }
		}

		/// <summary>
		/// Box height.
		/// </summary>
		[XmlIgnore]
		public float Height
		{
			get { return TopY - BottomY; }
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
			writer.WriteElementString(nameof(LeftX), LeftX.ToString());
			writer.WriteElementString(nameof(RightX), RightX.ToString());
			writer.WriteElementString(nameof(BottomY), BottomY.ToString());
			writer.WriteElementString(nameof(TopY), TopY.ToString());
		}

		/// <summary>
		/// XML deserialization function.
		/// </summary>
		/// <param name="reader">XML reader.</param>
		public void ReadXml(XmlReader reader)
		{
			//seek first element
			reader.ReadToFollowing(nameof(LeftX));

			//read element values
			LeftX = reader.ReadElementContentAsFloat(nameof(LeftX), "");
			RightX = reader.ReadElementContentAsFloat(nameof(RightX), "");
			BottomY = reader.ReadElementContentAsFloat(nameof(BottomY), "");
			TopY = reader.ReadElementContentAsFloat(nameof(TopY), "");
			
			//swap pairs if needed
			if (RightX < LeftX)
			{
				var swap = RightX;
				RightX = LeftX;
				LeftX = swap;
			}
			if (TopY < BottomY)
			{
				var swap = TopY;
				TopY = BottomY;
				BottomY = swap;
			}
		}

		#endregion

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="leftX">Left edge of the box.</param>
		/// <param name="rightX">Right edge of the box.</param>
		/// <param name="bottomY">Bottom edge of the box.</param>
		/// <param name="topY">Top edge of the box.</param>
		public BoxCoords(
			float leftX,
			float rightX,
			float bottomY,
			float topY)
		{
			//store property values
			LeftX = leftX;
			RightX = rightX;
			BottomY = topY;
			TopY = bottomY;

			//swap pairs if needed
			if (RightX < LeftX)
			{
				var swap = RightX;
				RightX = LeftX;
				LeftX = swap;
			}
			if (TopY < BottomY)
			{
				var swap = TopY;
				TopY = BottomY;
				BottomY = swap;
			}
		}

		/// <summary>
		/// Default constructor for serialization.
		/// </summary>
		public BoxCoords()
		{ }

		#endregion
	}
}
