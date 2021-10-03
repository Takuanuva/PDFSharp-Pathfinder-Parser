using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentParser.Utilities.Geometry
{
	/// <summary>
	/// Represents a set of points forming a polyline (closed or not) in 2D space.
	/// </summary>
	public class Polyline
	{
		#region Properties

		#region Private storage fields

		/// <summary>
		/// Private storage field for the Points property.
		/// </summary>
		private List<Point> _Points = new List<Point>();

		#endregion

		/// <summary>
		/// List of points defining the polyline.
		/// </summary>
		public IReadOnlyList<Point> Points
		{
			get { return _Points; }
		}

		/// <summary>
		/// List of line segments defined by the polyline.
		/// </summary>
		public IReadOnlyList<Line> Lines
		{
			get
			{
				//check point count
				if (Points.Count < 2) return new List<Line>();

				//initialize list
				List<Line> lines = new List<Line>();

				//get first point
				Point previousPoint = Points.First();

				//iterate over points
				for (int i = 1; i < Points.Count; i++)
				{
					//get next point
					Point nextPoint = Points[i];

					//add line
					lines.Add(new Line(previousPoint, nextPoint));

					//replace point
					previousPoint = nextPoint;
				}

				//add last line if required
				if (IsClosed)
					lines.Add(new Line(previousPoint, Points.First()));

				return lines;
			}
		}

		/// <summary>
		/// Start point of the line segment.
		/// </summary>
		public Point Start
		{
			get { return Points.First(); }
		}

		/// <summary>
		/// End point of the line segment.
		/// </summary>
		public Point End
		{
			get { return Points.Last(); }
		}

		/// <summary>
		/// Polyline closure flag, true if last point links back to the first, false otherwise.
		/// </summary>
		public bool IsClosed { get; }
		
		/// <summary>
		/// Length of the line edge.
		/// </summary>
		public float EdgeLength
		{
			get
			{
				//initialize counter
				float length = 0;

				//iterate over points
				for (int i = 0; i < Points.Count - 1; i++)
					//increment counter
					length += Points[i].DistanceFrom(Points[i + 1]);

				return length;
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="points">Ordered list of points defining the polyline.</param>
		/// <param name="isClosed">True if last point links back to the first, false otherwise.</param>
		public Polyline(
			IReadOnlyList<Point> points,
			bool isClosed = false)
		{
			//copy points
			foreach (Point point in points)
				_Points.Add(point);
		}

		#endregion
	}
}
