using DocumentParser.Utilities;
using DocumentParser.Utilities.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentParser.DataTypes
{
	/// <summary>
	/// Abstract class, represents an element which occurs within a page of a PDF document.
	/// </summary>
	public abstract class PdfPageElement
	{
		#region Properties

		#region Private storage fields

		/// <summary>
		/// Private storage field for the BoundingBox property.
		/// </summary>
		protected Rectangle _BoundingBox = null;

		#endregion
		
		/// <summary>
		/// Page on which the element is placed.
		/// </summary>
		public PdfPage Page { get; }

		/// <summary>
		/// Element's placement within the rendering order.
		/// </summary>
		public ulong RenderingOrder { get; }

		/// <summary>
		/// The element's bounding rectangle.
		/// </summary>
		public Rectangle BoundingBox
		{
			get
			{
				//check if null
				if (_BoundingBox == null)
					//generate property values
					GeneratePropertyValues();

				return _BoundingBox;
			}
		}

		#endregion

		#region Methods
		
		/// <summary>
		/// Property value generation method.
		/// </summary>
		protected abstract void GeneratePropertyValues();
		
		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="page">Page containing the element.</param>
		public PdfPageElement(
			PdfPage page)
		{
			//check if valid data
			if (page == null) throw new ArgumentNullException(nameof(page));
			
			//store property values
			Page = page;
			RenderingOrder = page.GetNextRenderingOrder();
		}

		#endregion
	}
}
