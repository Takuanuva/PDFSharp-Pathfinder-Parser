using DocumentParser.Utilities.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentParser.DataTypes
{
	/// <summary>
	/// Represents a PDF graphical element consisting of an SVG path.
	/// </summary>
	public class PdfPath : PdfGraphicalElement
	{
		#region Properties

		#region Private storage fields

		/// <summary>
		/// Private storage field for the LineSegments property.
		/// </summary>
		private List<Line> _LineSegments = new List<Line>();

		#endregion

		/// <summary>
		/// Line segments forming the path.
		/// </summary>
		public IReadOnlyList<Line> LineSegments
		{
			get { return _LineSegments; }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Override method, should not need to be called.
		/// </summary>
		protected override void GeneratePropertyValues()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Verifies whether the line is intersected by any of the path's line segments.
		/// </summary>
		/// <param name="line">Line to be tested.</param>
		/// <returns>True if an intersection occurs, false otherwise.</returns>
		public override bool IsLineIntersectingEdge(Line line)
		{
			//check if line intersects any of the path's line segments
			foreach (Line lineSegment in LineSegments)
				if (line.Intersects(lineSegment) != Range.IntersectData.NoIntersect)
					return true;

			return false;
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="page">Page containing the path.</param>
		/// <param name="lines">Collection of path's lines.</param>
		/// <param name="transformMatrix">Point transformation matrix.</param>
		public PdfPath(
			PdfPage page,
			IEnumerable<Line> lines, 
			TransformMatrix transformMatrix) :
			base(page)
		{
			//generate line list
			foreach (Line line in lines)
				_LineSegments.Add(transformMatrix.Transform(line));

			//generate bounding box
			List<Point> points = new List<Point>();
			foreach (Line line in LineSegments)
				points.AddRange(line.Points);
			_BoundingBox = Rectangle.Containing(points);

		}

		#endregion
	}
}
