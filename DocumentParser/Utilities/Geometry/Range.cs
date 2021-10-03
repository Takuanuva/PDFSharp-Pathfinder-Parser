using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentParser.Utilities.Geometry
{
	/// <summary>
	/// Represents a one-dimensional range between two points.
	/// </summary>
	public class Range
	{
		#region Sub-types

		/// <summary>
		/// Possible intersection scenarios.
		/// </summary>
		[Flags]
		public enum IntersectData
		{
			NoIntersect		= 0b00,
			EdgeIntersect	= 0b01,
			BodyIntersect	= 0b11
		}

		#endregion

		#region Properties

		/// <summary>
		/// Lower bounds of the range.
		/// </summary>
		public float Lower { get; }

		/// <summary>
		/// Upper bounds of the range
		/// </summary>
		public float Upper { get; }

		public float Width
		{
			get
			{
				return Upper - Lower;
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Verifies intersection between the range and a point.
		/// </summary>
		/// <param name="point">Point to be checked.</param>
		/// <returns>Intersection data.</returns>
		public IntersectData DoesIntersect(float point)
		{
			//check if point is outside the range
			if (Lower > point ||
				Upper < point)
				return IntersectData.NoIntersect;
			
			return IntersectData.BodyIntersect;
		}

		/// <summary>
		/// Verifies intersection between the two ranges.
		/// </summary>
		/// <param name="point">Range to be checked.</param>
		/// <returns>Intersection data.</returns>
		public IntersectData DoesIntersect(Range range)
		{
			//check if ranges have no intersection
			if (Lower > range.Upper ||
				Upper < range.Lower)
				return IntersectData.NoIntersect;

			//check if ranges intersect on the ends
			if (Lower == range.Upper ||
				Upper == range.Lower)
				return IntersectData.EdgeIntersect;

			return IntersectData.BodyIntersect;
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="a">First range endpoint.</param>
		/// <param name="b">Second range endpoint.</param>
		public Range(float a, float b)
		{
			//store values
			Lower = Math.Min(a, b);
			Upper = Math.Max(a, b);
		}

		#endregion
	}
}
