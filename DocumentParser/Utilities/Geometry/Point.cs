using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentParser.Utilities.Geometry
{
	/// <summary>
	/// Represents a point in 2D space.
	/// </summary>
	public class Point
	{
		#region Properties

		/// <summary>
		/// X coordinate of the point.
		/// </summary>
		public float X { get; private set; }

		/// <summary>
		/// Y coordinate of the point.
		/// </summary>
		public float Y { get; private set; }

		#endregion

		#region Methods

		/// <summary>
		/// Distance between the original point and the target.
		/// </summary>
		/// <returns />
		public float DistanceFrom(Point target)
		{
			//calculate x and y distances
			float xDist = X - target.X;
			float yDist = Y - target.Y;

			return (float)Math.Sqrt(
				(xDist * xDist) + 
				(yDist * yDist));
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="x">X coordinate of the point.</param>
		/// <param name="y">Y coordinate of the point.</param>
		public Point(float x, float y)
		{
			//store property values
			X = x;
			Y = y;
		}

		#endregion
	}
}
