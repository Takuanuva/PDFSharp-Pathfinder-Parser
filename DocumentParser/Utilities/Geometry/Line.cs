using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentParser.Utilities.Geometry
{
	/// <summary>
	/// Represents a single line segment.
	/// </summary>
	public class Line : Polyline
	{
		#region Properties

		#region Ranges

		/// <summary>
		/// Horizontal range of the line.
		/// </summary>
		public Range HorizontalRange
		{
			get { return new Range(Start.X, End.X); }
		}

		/// <summary>
		/// Vertical range of the line.
		/// </summary>
		public Range VerticalRange
		{
			get { return new Range(Start.Y, End.Y); }
		}

		#endregion

		#endregion

		#region Methods

		/// <summary>
		/// Verifies whether the two lines intersect.
		/// </summary>
		/// <param name="line">Line to be checked.</param>
		/// <returns>Intersection data.</returns>
		public Range.IntersectData Intersects(Line line)
		{
			//check for degenerate lines (both points overlaping)
			{
				if (Start.X == End.X && Start.Y == End.Y)
				{
					if (line.Start.X == line.End.X && line.Start.Y == line.End.Y)
					{
						if (Start.X == line.Start.X &&
							Start.Y == line.Start.Y &&
							End.X == line.End.X &&
							End.Y == line.End.Y)
							return Range.IntersectData.BodyIntersect;
						return Range.IntersectData.NoIntersect;
					}
					else
					{
						float deltaAX = End.X - line.Start.X;
						float deltaAY = End.Y - line.Start.Y;
						float deltaBX = line.End.X - line.Start.X;
						float deltaBY = line.End.Y - line.Start.Y;
						if ((deltaAX * deltaBY) != (deltaBX - deltaAY))
							return Range.IntersectData.NoIntersect;
						return line.HorizontalRange.DoesIntersect(Start.X) & line.VerticalRange.DoesIntersect(Start.Y);
					}
				}
				else if (line.Start.X == line.End.X && line.Start.Y == line.End.Y)
				{
					float deltaAX = End.X - Start.X;
					float deltaAY = End.Y - Start.Y;
					float deltaBX = line.End.X - Start.X;
					float deltaBY = line.End.Y - Start.Y;
					if ((deltaAX * deltaBY) != (deltaBX - deltaAY))
						return Range.IntersectData.NoIntersect;
					return HorizontalRange.DoesIntersect(line.Start.X) & VerticalRange.DoesIntersect(line.Start.Y);
				}
			}

			//get initial intersect data
			Range.IntersectData boundsIntersect = 
				HorizontalRange.DoesIntersect(line.HorizontalRange) |
				VerticalRange.DoesIntersect(line.VerticalRange);

			//check if ranges disallow intersection
			if (boundsIntersect == Range.IntersectData.NoIntersect)
				return Range.IntersectData.NoIntersect;

			//get line properties
			float aX = Start.X;
			float aY = Start.Y;
			float aDeltaX = End.X - aX;
			float aDeltaY = End.Y - aY;
			float bX = line.Start.X;
			float bY = line.Start.Y;
			float bDeltaX = line.End.X - bX;
			float bDeltaY = line.End.Y - bY;

			//check if lines are parallel
			float lineMult = (aDeltaX * bDeltaY) - (aDeltaY * bDeltaX);
			if (lineMult == 0)
			{
				//check if lines overlap
				if ((aDeltaY * (bX - aX)) == (aDeltaX * (bY - aY)))
					return boundsIntersect;
				else
					return Range.IntersectData.NoIntersect;
			}

			//find intersect point


			//initialize output
			Range.IntersectData output = Range.IntersectData.BodyIntersect;

			//check if intersect point is within the A line
			{
				//calculate multiplier
				float multiplier =
					((bDeltaY * (bX - aX)) + (bDeltaX * (aY - bY))) / lineMult;

				//check if outside line
				if (multiplier < 0 ||
					multiplier > 1)
					return Range.IntersectData.NoIntersect;

				//check if at line end
				if (multiplier == 0 ||
					multiplier == 1)
					output &= Range.IntersectData.EdgeIntersect;
				else
					output &= Range.IntersectData.BodyIntersect;
			}

			//check if intersect point is within the B line
			{
				//calculate multiplier
				float multiplier =
					((aDeltaY * (bX - aX)) + (aDeltaX * (aY - bY))) / lineMult;

				//check if outside line
				if (multiplier < 0 ||
					multiplier > 1)
					return Range.IntersectData.NoIntersect;

				//check if at line end
				if (multiplier == 0 ||
					multiplier == 1)
					output &= Range.IntersectData.EdgeIntersect;
				else
					output &= Range.IntersectData.BodyIntersect;
			}

			return output;
		}

		/// <summary>
		/// Verifies whether the two line intersects any part of the polyline.
		/// </summary>
		/// <param name="line">Polyline to be checked.</param>
		/// <returns>Intersection data.</returns>
		public Range.IntersectData Intersects(Polyline polyline)
		{
			//initialize intersect data buffer
			Range.IntersectData dataBuffer = Range.IntersectData.NoIntersect;

			//check if line intersects any of the lines
			foreach (Line line in polyline.Lines)
			{
				dataBuffer |= Intersects(line);
				if (dataBuffer == Range.IntersectData.BodyIntersect) break;
			}

			return dataBuffer;
		}

		/// <summary>
		/// Generates line representing the overlap between the line and a specified area.
		/// </summary>
		/// <param name="area">The overlap area.</param>
		/// <returns>Overlapped line segment, or null if no overlap occurs.</returns>
		public Line Overlap(Rectangle area)
		{
			//check if line does not overlap the area
			if (area.Intersects(this) == Range.IntersectData.NoIntersect)
				return null;

			//get line properties
			float originX = Start.X;
			float originY = Start.Y;
			float deltaX = End.X - Start.X;
			float deltaY = End.Y - Start.Y;

			//check if degenerate line
			if (deltaX == 0 &&
				deltaY == 0)
				return new Line(Start, End);

			//initialize multiplier list
			List<float> multipliers = new List<float>();

			//add X coordinate multipliers
			if (deltaX != 0)
			{
				multipliers.Add((area.LeftX - originX) / deltaX);
				multipliers.Add((area.RightX - originX) / deltaX);
			}
			else
			{
				multipliers.Add(float.NegativeInfinity);
				multipliers.Add(float.PositiveInfinity);
			}

			//add Y coordinate multipliers
			if (deltaY != 0)
			{
				multipliers.Add((area.BottomY - originY) / deltaY);
				multipliers.Add((area.TopY - originY) / deltaY);
			}
			else
			{
				multipliers.Add(float.NegativeInfinity);
				multipliers.Add(float.PositiveInfinity);
			}

			//sort multiplier values
			multipliers.Sort();

			//get middle two values and normalize them
			float multiplierStart = Math.Max(Math.Min(multipliers[1], 1), 0);
			float multiplierEnd = Math.Max(Math.Min(multipliers[2], 1), 0);

			return new Line(
				new Point(
					originX + (deltaX * multiplierStart),
					originY + (deltaY * multiplierStart)),
				new Point(
					originX + (deltaX * multiplierEnd),
					originY + (deltaY * multiplierEnd)));
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="start">Start point of the segment.</param>
		/// <param name="end">End point of the segment.</param>
		public Line(
			Point start,
			Point end) :
			base(
				new List<Point>()
				{
					start,
					end
				})
		{ }

		#endregion
	}
}
