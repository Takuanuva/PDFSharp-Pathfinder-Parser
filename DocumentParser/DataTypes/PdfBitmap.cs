using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentParser.Utilities.Geometry;

namespace DocumentParser.DataTypes
{
	/// <summary>
	/// Represents a graphical page element consisting of a bitmap.
	/// </summary>
	public class PdfBitmap : PdfGraphicalElement
	{
		#region Sub-classes

		/// <summary>
		/// Represents the underlaying bitmap resource.
		/// </summary>
		public class Resource
		{
			#region Properties
			
			/// <summary>
			/// Bitmap data.
			/// </summary>
			public System.Drawing.Bitmap Data { get; }

			/// <summary>
			/// Polygon representing the outline of the bitmap in the [0,1] coordinate system.
			/// </summary>
			public Polygon Outline { get; }

			/// <summary>
			/// Bitmap bounding box.
			/// </summary>
			public Rectangle BoundingBox { get; }






			public string IdString
			{
				get { return GetHashCode().ToString("X8"); }
			}

			#endregion

			#region Methods

			/// <summary>
			/// Generates the bitmap outline.
			/// </summary>
			/// <param name="Data">Bitmap data.</param>
			/// <returns>Polygon representing the bitmap outline in the [0, 1] coordinate system.</returns>
			private Polygon generateOutline(System.Drawing.Bitmap Data)
			{
				//generate difference treshold arrays
				byte[] alphaTresholds = new byte[Data.Width];
				byte[] rgbTresholds = new byte[Data.Width];
				const byte alphaStartingTreshold = 0x10;
				const byte alphaEndingTreshold = 0x04;
				const byte rgbStartingTreshold = 0x28;
				const byte rgbEndingTreshold = 0x10;
				int endTresholdIndex = (Data.Width / 4) * 3;
				for (int i = 0; i < endTresholdIndex; i++)
				{
					alphaTresholds[i] = (byte)(alphaEndingTreshold + ((float)(i * (alphaStartingTreshold - alphaEndingTreshold)) / Data.Width));
					rgbTresholds[i] = (byte)(rgbEndingTreshold + ((float)(i * (rgbStartingTreshold - rgbEndingTreshold)) / Data.Width));
				}
				for (int i = endTresholdIndex; i < Data.Width; i++)
				{
					alphaTresholds[i] = alphaEndingTreshold;
					rgbTresholds[i] = rgbEndingTreshold;
				}

				//declare check function
				bool check(
					System.Drawing.Color previousColor,
					System.Drawing.Color currentColor,
					int tresholdIndex)
				{
					int a = previousColor.A;
					int b = currentColor.A;
					if (a == 0 ||
						b == 0)
						return false;

					return
						(previousColor.A > currentColor.A ? previousColor.A - currentColor.A : currentColor.A - previousColor.A) >= alphaTresholds[tresholdIndex] ||
						(previousColor.R > currentColor.R ? previousColor.R - currentColor.R : currentColor.R - previousColor.R) >= rgbTresholds[tresholdIndex] ||
						(previousColor.G > currentColor.G ? previousColor.G - currentColor.G : currentColor.G - previousColor.G) >= rgbTresholds[tresholdIndex] ||
						(previousColor.B > currentColor.B ? previousColor.B - currentColor.B : currentColor.B - previousColor.B) >= rgbTresholds[tresholdIndex];
				}
				
				//find first row with edge
				int firstRow = 0;
				for (; firstRow < Data.Height; firstRow++)
				{
					System.Drawing.Color previousColor = Data.GetPixel(0, firstRow);
					bool passed = false;
					for (int x = 1; x < Data.Width && !passed; x++)
					{
						System.Drawing.Color currentColor = Data.GetPixel(x, firstRow);
						passed = check(previousColor, currentColor, x);
						if (passed) break;
						previousColor = currentColor;
					}
					if (passed) break;
				}
				
				//check if no rows passed the check
				if (firstRow == Data.Height)
					return new Polygon(new List<Point>());

				//find last row with edge
				int lastRow = Data.Height - 1;
				for (; lastRow >= 0; lastRow--)
				{
					System.Drawing.Color previousColor = Data.GetPixel(0, lastRow);
					bool passed = false;
					for (int x = 1; x < Data.Width && !passed; x++)
					{
						System.Drawing.Color currentColor = Data.GetPixel(x, lastRow);
						passed = check(previousColor, currentColor, x);
						previousColor = currentColor;
					}
					if (passed) break;
				}

				//initialize point list
				List<Point> points = new List<Point>();

				//calculate coordinate steps
				float epsilonX = 1.0f / (Data.Width - 1);
				float epsilonY = 1.0f / (Data.Height - 1);
				
				//iterate over rows performing checks from left to right, bottom to top
				for (int y = firstRow; y <= lastRow; y++)
				{
					//find x coordinate
					System.Drawing.Color previousColor = Data.GetPixel(0, y);
					int x = 1;
					for (; x < Data.Width; x++)
					{
						System.Drawing.Color currentColor = Data.GetPixel(x, y);
						if (check(currentColor, previousColor, x)) break;
						previousColor = currentColor;
					}
					
					//add point to list
					points.Add(
						new Point(
							epsilonX * x,
							1 - (epsilonY * y)));
				}
				
				//iterate over rows performing checks from right to left, top to bottom
				for (int y = lastRow; y >= firstRow; y--)
				{
					//find x coordinate
					System.Drawing.Color previousColor = Data.GetPixel(Data.Width - 1, y);
					int x = Data.Width - 2;
					for (; x >= 0; x--)
					{
						System.Drawing.Color currentColor = Data.GetPixel(x, y);
						if (check(currentColor, previousColor, Data.Width - 1 - x)) break;
						previousColor = currentColor;
					}
					//add point to list
					points.Add(
						new Point(
							epsilonX * x,
							1 - (epsilonY * y)));
				}

				//perform anomaly smoothing
				for (int iter = 0; iter < 3; iter++)
				{
					//declare processing constants
					const float weightA = 1.0f;
					const float weightB = 1.0f / 2;
					const float weightC = 1.0f / 4;
					const float weightSum = weightA + (2 * weightB) + (2 * weightC);

					//declare point buffer
					List<Point> smoothedPoints = new List<Point>();

					//generate averaged points
					for (int i = 0; i < points.Count; i++)
						smoothedPoints.Add(
							new Point(
								((points[(i + points.Count - 2) % points.Count].X * weightC) + (points[(i + points.Count - 1) % points.Count].X * weightB) + (points[i].X * weightA) + (points[(i + 1) % points.Count].X * weightB) + (points[(i + 2) % points.Count].X * weightC)) / weightSum,
								points[i].Y));

					//update point list
					points = smoothedPoints;
				}

				return new Polygon(points);
			}

			#endregion

			#region Constructors

			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="data">Image bitmap.</param>
			internal Resource(System.Drawing.Bitmap data)
			{
				//Data.Save($"{Data.GetHashCode().ToString("X8")}.png");

				//store data
				Data = data;

				//generate outline
				Outline = generateOutline(data);

				//generate bounding box
				BoundingBox = new Rectangle(
					new Point(
						0,
						0),
					new Point(
						data.Width,
						data.Height));

				//dispose of bitmap data
				//data.Dispose();
			}

			#endregion
		}

		#endregion

		#region Properties

		/// <summary>
		/// The image resource associated with the bitmap.
		/// </summary>
		public Resource ImageResource { get; }
		
		/// <summary>
		/// Image anchor point.
		/// </summary>
		public Point Anchor { get; }

		/// <summary>
		/// Image transformation matrix.
		/// </summary>
		public TransformMatrix Transformation { get; }

		/// <summary>
		/// Image transformation matrix adjusted for anchor point.
		/// </summary>
		public TransformMatrix AdjustedTransformation { get; }

		/// <summary>
		/// Transformed image outline.
		/// </summary>
		public Polygon ImageOutline { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Override method, should never need to be called.
		/// </summary>
		protected override void GeneratePropertyValues()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Determines whether the provided line intersects what could be considered an edge of the bitmap.
		/// </summary>
		/// <param name="line">Line to be checked.</param>
		/// <returns>True if line intersects an edge, false otherwise.</returns>
		public override bool IsLineIntersectingEdge(Line line)
		{
			return line.Intersects(ImageOutline) == Range.IntersectData.BodyIntersect;
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="page">Page containing the bitmap.</param>
		/// <param name="imageResource">The image resource associated with the bitmap.</param>
		/// <param name="anchor">Image anchor point.</param>
		/// <param name="transformation">Image transformation matrix.</param>
		public PdfBitmap(
			PdfPage page,
			Resource imageResource,
			Point anchor,
			TransformMatrix transformation) :
			base(page)
		{
			//store property values
			ImageResource = imageResource;
			Anchor = anchor;
			Transformation = transformation;

			//generate adjusted transformation matrix
			AdjustedTransformation = TransformMatrix.Translation(Anchor.X, Anchor.Y).ApplyToMatrix(Transformation);

			//generate image outline
			//ImageOutline = AdjustedTransformation.Transform(
			//	new Rectangle(
			//		new Point(0, 0),
			//		new Point(1, 1)));
			ImageOutline = (Transformation.Transform(//TransformMatrix.Translation(-Anchor.X, -anchor.Y).Transform(Transformation.Transform(
				ImageResource.Outline));
				//new Rectangle(
				//	new Point(0, 0),
				//	new Point(1, 1))));

			//generate bounding box
			_BoundingBox = Rectangle.Containing(Transformation.Transform(ImageResource.BoundingBox).Points);
		}

		#endregion
	}
}
