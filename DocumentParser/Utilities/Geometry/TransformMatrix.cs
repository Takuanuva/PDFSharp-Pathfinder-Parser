using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentParser.Utilities.Geometry
{
	/// <summary>
	/// Represents a 2D coordinate transformation matrix.
	/// </summary>
	public class TransformMatrix
	{
		#region Properties

		#region Private storage fields

		//data values
		private float XX { get; }
		private float XY { get; }
		private float XZ { get; }
		private float YX { get; }
		private float YY { get; }
		private float YZ { get; }

#warning TODO: IMPLEMENT BETTER!

		#endregion

		#endregion

		#region Methods

		/// <summary>
		/// Transforms a point using the matrix.
		/// </summary>
		/// <param name="point">Point to be transformed.</param>
		/// <returns>Transformed point.</returns>
		public Point Transform(Point point)
		{
			return new Point(
				(XX * point.X) + (XY * point.Y) + XZ,
				(YX * point.X) + (YY * point.Y) + YZ);
#warning TODO: MAKE LESS ARCANE!
		}

		/// <summary>
		/// Transforms a line using the matrix.
		/// </summary>
		/// <param name="line">Line to be transformed.</param>
		/// <returns>Transformed line.</returns>
		public Line Transform(Line line)
		{
			return new Line(
				Transform(line.Start),
				Transform(line.End));
		}

		/// <summary>
		/// Transforms a polyline using the matrix.
		/// </summary>
		/// <param name="polyline">Polyline to be transformed.</param>
		/// <returns>Transformed polyline.</returns>
		public Polyline Transform(Polyline polyline)
		{
			//generate point list
			List<Point> points = new List<Point>();
			foreach (Point point in polyline.Points)
				points.Add(Transform(point));

			return new Polyline(points);
		}

		/// <summary>
		/// Transforms a polygon using the matrix.
		/// </summary>
		/// <param name="polygon">Polygon to be transformed.</param>
		/// <returns>Transformed polygon.</returns>
		public Polygon Transform(Polygon polygon)
		{
			//generate point list
			List<Point> points = new List<Point>();
			foreach (Point point in polygon.Points)
				points.Add(Transform(point));

			return new Polygon(points);
		}

		/// <summary>
		/// Applies transformation matrix to the provided matrix.
		/// </summary>
		/// <param name="matrix">Matrix to be adjusted.</param>
		/// <returns>Adjusted matrix.</returns>
		public TransformMatrix ApplyToMatrix(TransformMatrix matrix)
		{
			return new TransformMatrix(new float[]
			{
				(XX * matrix.XX) + (XY * matrix.YX),    (XX * matrix.XY) + (XY * matrix.YY),    (XX * matrix.XZ) + (XY * matrix.YZ) + XZ,
				(YX * matrix.XX) + (YY * matrix.YX),    (YX * matrix.XY) + (YY * matrix.YY),    (YX * matrix.XZ) + (YY * matrix.YZ) + YZ
			});
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="values">Collection of matrix values.</param>
		public TransformMatrix(IEnumerable<float> values)
		{
			//check if collection is invalid
			if (values.Count() != 6) throw new ArgumentException("Constructor must be provided a collection of exactly 6 values!");

			//store property values
			XX = values.ElementAt(0);
			XY = values.ElementAt(1);
			XZ = values.ElementAt(2);
			YX = values.ElementAt(3);
			YY = values.ElementAt(4);
			YZ = values.ElementAt(5);
		}

		#region Factory methods

		/// <summary>
		/// Generates a transform matrix for a translation operation.
		/// </summary>
		/// <param name="x">X translation.</param>
		/// <param name="y">Y translation.</param>
		/// <returns>Translation matrix.</returns>
		public static TransformMatrix Translation(float x, float y)
		{
			return new TransformMatrix(new float[]
			{
				1, 0, x,
				0, 1, y
			});
		}

		#endregion

		#endregion
	}
}
