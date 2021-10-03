using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentParser.Utilities.Geometry
{
	/// <summary>
	/// Represents a closed 2D polyline.
	/// </summary>
	public class Polygon : Polyline
	{
		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="points">Ordered list of points defining the polygon.</param>
		public Polygon(IReadOnlyList<Point> points) :
			base(points, true)
		{ }

		#endregion
	}
}
