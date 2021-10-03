using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentParser.Utilities.Geometry
{
	/// <summary>
	/// Represents a rectangle.
	/// </summary>
	public class Rectangle : Polygon
	{
		#region Properties

		#region Corners

		/// <summary>
		/// Lower left corner.
		/// </summary>
		public Point LowerLeft
		{
			get { return Points[0]; }
		}
		
		/// <summary>
		/// Upper left corner.
		/// </summary>
		public Point UpperLeft
		{
			get { return Points[1]; }
		}
		
		/// <summary>
		/// Upper right corner.
		/// </summary>
		public Point UpperRight
		{
			get { return Points[2]; }
		}
		
		/// <summary>
		/// Lower right corner.
		/// </summary>
		public Point LowerRight
		{
			get { return Points[3]; }
		}

		/// <summary>
		/// Center of the rectangle.
		/// </summary>
		public Point Center
		{
			get
			{
				return new Point(
					(LeftX + RightX) / 2,
					(BottomY + TopY) / 2);
			}
		}

		#endregion

		#region Coordinates

		/// <summary>
		/// Left X coordinate.
		/// </summary>
		public float LeftX
		{
			get { return LowerLeft.X; }
		}

		/// <summary>
		/// Right X coordinate.
		/// </summary>
		public float RightX
		{
			get { return UpperRight.X; }
		}

		/// <summary>
		/// Bottom Y coordinate.
		/// </summary>
		public float BottomY
		{
			get { return LowerLeft.Y; }
		}

		/// <summary>
		/// Top Y coordinate.
		/// </summary>
		public float TopY
		{
			get { return UpperRight.Y; }
		}

		#endregion

		#region Ranges

		/// <summary>
		/// Horizontal range of the rectangle.
		/// </summary>
		public Range HorizontalRange
		{
			get { return new Range(LeftX, RightX); }
		}

		/// <summary>
		/// Vertical range of the rectangle.
		/// </summary>
		public Range VerticalRange
		{
			get { return new Range(BottomY, TopY); }
		}

		#endregion

		/// <summary>
		/// Width of rectangle.
		/// </summary>
		public float Width
		{
			get { return UpperRight.X - LowerLeft.X; }
		}

		/// <summary>
		/// Height of rectangle.
		/// </summary>
		public float Height
		{
			get { return UpperRight.Y - LowerLeft.Y; }
		}

		/// <summary>
		/// Surface area of rectangle.
		/// </summary>
		public float SurfaceArea
		{
			get { return Width * Height; }
		}

		#endregion

		#region Methods

		/// <summary>
		/// Checks if rectangle contains a point.
		/// </summary>
		/// <param name="point">Point to be checked.</param>
		/// <returns>Point intersection data.</returns>
		public Range.IntersectData Contains(Point point)
		{
			return
				HorizontalRange.DoesIntersect(point.X) &
				VerticalRange.DoesIntersect(point.Y);
		}

		/// <summary>
		/// Checks if a line intersects the rectangle.
		/// </summary>
		/// <param name="line">Line to be checked with the object.</param>
		/// <returns>Line intersection data.</returns>
		public Range.IntersectData Intersects(Line line)
		{
			//check line type
			if (line.Start == line.End) //degenerate line (endpoints overlap)
			{
				return Contains(line.Start);
			}
			else if (line.Start.Y == line.End.Y) //horizontal line
			{
				return
					HorizontalRange.DoesIntersect(line.HorizontalRange) &
					VerticalRange.DoesIntersect(line.Start.Y);
			}
			else if (line.Start.X == line.End.X) //vertical line
			{
				return
					HorizontalRange.DoesIntersect(line.Start.X) &
					VerticalRange.DoesIntersect(line.VerticalRange);
			}
			else //any other line type
			{
				//check if line position allows for intersection
				if ((HorizontalRange.DoesIntersect(line.HorizontalRange) & VerticalRange.DoesIntersect(line.VerticalRange)) == Range.IntersectData.NoIntersect)
					return Range.IntersectData.NoIntersect;

				//calculate line's delta vector
				float deltaX = line.End.X - line.Start.X;
				float deltaY = line.End.Y - line.Start.Y;

				return
					HorizontalRange.DoesIntersect(
						new Range(
							line.Start.X + (deltaX * ((TopY - line.Start.Y) / deltaY)),
							line.Start.X + (deltaX * ((BottomY - line.Start.Y) / deltaY)))) &
					HorizontalRange.DoesIntersect(
						new Range(
							line.Start.Y + (deltaY * ((LeftX - line.Start.X) / deltaX)),
							line.Start.Y + (deltaY * ((RightX - line.Start.X) / deltaX))));

#warning TODO: MAKE LESS ARCANE!
			}
		}

		/// <summary>
		/// Checks if the two rectangles intersect.
		/// </summary>
		/// <param name="rectangle">Rectangle to be checked with the object.</param>
		/// <returns>Rectangle intersection data.</returns>
		public Range.IntersectData Intersects(Rectangle rectangle)
		{
			return
				HorizontalRange.DoesIntersect(rectangle.HorizontalRange) &
				VerticalRange.DoesIntersect(rectangle.VerticalRange);
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="lowerLeft">Lower left corner.</param>
		/// <param name="upperRight">Upper right corner.</param>
		public Rectangle(
			Point lowerLeft,
			Point upperRight) :
			base(new List<Point>() {
				lowerLeft,
				new Point(lowerLeft.X, upperRight.Y),
				upperRight,
				new Point(upperRight.X, lowerLeft.Y)
			})
		{ }

		#region Factory methods

		/// <summary>
		/// Creates a rectangle containing all points within the list.
		/// </summary>
		/// <param name="points">List of points.</param>
		/// <returns>Smallest rectangle containing all points.</returns>
		public static Rectangle Containing(IEnumerable<Point> points)
		{
			//get lowest and highest values
			float xMin = float.PositiveInfinity;
			float xMax = float.NegativeInfinity;
			float yMin = float.PositiveInfinity;
			float yMax = float.NegativeInfinity;
			foreach (Point point in points)
			{
				if (point.X < xMin) xMin = point.X;
				if (point.X > xMax) xMax = point.X;
				if (point.Y < yMin) yMin = point.Y;
				if (point.Y > yMax) yMax = point.Y;
			}

			return new Rectangle(
				new Point(
					xMin,
					yMin),
				new Point(
					xMax,
					yMax));
		}

		/// <summary>
		/// Creates a rectangle representing the intersect of all provided rectangles.
		/// </summary>
		/// <param name="rectangles">Collection of rectangles to intersect.</param>
		/// <returns>Rectangle representing the intersect (or null if no intersect exists).</returns>
		public static Rectangle FromIntersect(IEnumerable<Rectangle> rectangles)
		{
			//check if valid collection
			if (rectangles == null) throw new ArgumentNullException(nameof(rectangles));
			
			//generate coordinates
			float leftMax = float.NegativeInfinity;
			float rightMin = float.PositiveInfinity;
			float bottomMax = float.NegativeInfinity;
			float topMin = float.PositiveInfinity;
			foreach (Rectangle rectangle in rectangles)
			{
				if (leftMax < rectangle.LeftX) leftMax = rectangle.LeftX;
				if (rightMin > rectangle.RightX) rightMin = rectangle.RightX;
				if (bottomMax < rectangle.BottomY) bottomMax = rectangle.BottomY;
				if (topMin > rectangle.TopY) topMin = rectangle.TopY;
			}

			//check if no valid intersect rectangle exists
			if (leftMax >= rightMin ||
				bottomMax >= topMin)
				return null;

			return new Rectangle(
				new Point(
					leftMax,
					bottomMax),
				new Point(
					rightMin,
					topMin));
		}

		#endregion

		#endregion
	}
}
