using DocumentParser.Utilities.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentParser.DataTypes
{
	/// <summary>
	/// Abstract base class, represents a text element consisting of one or more sub-elements.
	/// </summary>
	/// <typeparam name="SubType">Sub-element type.</typeparam>
	public abstract class PdfTextMultiElement<SubType> : PdfTextElement
		where SubType : PdfTextElement, PdfTextMultiElement<SubType>.ISubElement
	{
		#region Sub-types

		/// <summary>
		/// Sub-element interface.
		/// </summary>
		public interface ISubElement
		{
			#region Properties

			/// <summary>
			/// Element container.
			/// </summary>
			PdfTextMultiElement<SubType> Container { get; set; }

			#endregion

			#region Methods

			/// <summary>
			/// Container invalidation method.
			/// </summary>
			void InvalidateContainer();

			#endregion
		}

		#endregion

		#region Properties

		#region Private storage fields

		/// <summary>
		/// Private storage field for the SubElements property.
		/// </summary>
		private List<SubType> _SubElements = new List<SubType>();

		#endregion

		/// <summary>
		/// List of sub-elements contained within the element.
		/// </summary>
		protected IReadOnlyList<SubType> SubElements
		{
			get { return _SubElements; }
		}
		
		#endregion

		#region Methods

		/// <summary>
		/// Attempts to add a sub-element to the element.
		/// </summary>
		/// <param name="subElement">Sub-element to be added.</param>
		/// <returns>True if successful, false otherwise.</returns>
		protected bool AddSubElement(SubType subElement)
		{
			//check if valid addition
			if (!IsValidSubElement(subElement))
				return false;

			//add sub-element to list
			SortElementIntoList(_SubElements, subElement);

			//add container to sub-element
			subElement.Container = this;

			//invalidate container
			Invalidate();

			return true;
		}

		/// <summary>
		/// Method to be used when inserting element into the element list. Default behavior is appending the element to the end with no sorting.
		/// </summary>
		/// <param name="list">List of elements.</param>
		/// <param name="element">Element to be added into the list.</param>
		protected virtual void SortElementIntoList(
			List<SubType> list,
			SubType element)
		{
			//append element to list
			list.Add(element);
		}

		/// <summary>
		/// Element verification method, checks if sub-element can be a part of the element.
		/// </summary>
		/// <param name="subElement">Potential sub-element to be checked.</param>
		/// <returns>True if sub-element can be a part of the element, false otherwise.</returns>
		protected abstract bool IsValidSubElement(SubType subElement);

		#region Property data validation

		/// <summary>
		/// Invalidates property values.
		/// </summary>
		internal override void Invalidate()
		{
			//invalidate properties
			InvalidatePropertyValues();
		}

		/// <summary>
		/// Invalidates the values of all relevant properties.
		/// </summary>
		private void InvalidatePropertyValues()
		{
			//invalidate properties
			_BoundingBox = null;
			_TextString = null;
			_IsWhitespace = null;
			_Baseline = null;
			_AscentLine = null;
			_DescentLine = null;
			_CharacterStyleFamilies = null;
		}

		/// <summary>
		/// Generates values for all invalidated properties.
		/// </summary>
		protected override void GeneratePropertyValues()
		{
			//invalidate property values
			InvalidatePropertyValues();
			
			//generate bounding box
			{
				List<Point> points = new List<Point>();
				foreach (SubType subElement in SubElements)
				{
					points.Add(subElement.BoundingBox.LowerLeft);
					points.Add(subElement.BoundingBox.UpperRight);
				}
				_BoundingBox = Rectangle.Containing(points);
			}

			//generate text string
			{
				_TextString = "";
				foreach (SubType subElement in SubElements)
					_TextString += subElement.TextString;
			}

			//generate whitespace flag
			{
				bool isWhitespace = true;
				foreach (SubType subElement in SubElements)
					if (!subElement.IsWhitespace)
					{
						isWhitespace = false;
						break;
					}
				_IsWhitespace = isWhitespace;
			}

			//generate baseline
			{
				List<Point> points = new List<Point>();
				foreach (SubType subElement in SubElements)
					points.AddRange(subElement.Baseline.Points);
				_Baseline = new Polyline(points);
			}

			//generate ascent line
			{
				List<Point> points = new List<Point>();
				foreach (SubType subElement in SubElements)
					points.AddRange(subElement.AscentLine.Points);
				_AscentLine = new Polyline(points);
			}

			//generate descent line
			{
				List<Point> points = new List<Point>();
				foreach (SubType subElement in SubElements)
					points.AddRange(subElement.DescentLine.Points);
				_DescentLine = new Polyline(points);
			}

			//generate character style family set
			{
				_CharacterStyleFamilies = new HashSet<PdfTextCharacter.Style.Family>();
				foreach (SubType subElement in SubElements)
					_CharacterStyleFamilies.UnionWith(subElement.CharacterStyleFamilies);
			}
		}

		#endregion

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="page">Source page.</param>
		public PdfTextMultiElement(PdfPage page) : base(page)
		{
			//initialize property values
			_BoundingBox = new Rectangle(
				new Point(
					float.NegativeInfinity,
					float.NegativeInfinity),
				new Point(
					float.NegativeInfinity,
					float.NegativeInfinity));
			_TextString = "";
			_Baseline = new Line(
				new Point(
					float.NegativeInfinity,
					float.NegativeInfinity),
				new Point(
					float.NegativeInfinity,
					float.NegativeInfinity));
			_AscentLine = new Line(
				new Point(
					float.NegativeInfinity,
					float.NegativeInfinity),
				new Point(
					float.NegativeInfinity,
					float.NegativeInfinity));
			_DescentLine = new Line(
				new Point(
					float.NegativeInfinity,
					float.NegativeInfinity),
				new Point(
					float.NegativeInfinity,
					float.NegativeInfinity));
		}

		#endregion
	}
}
