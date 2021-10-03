using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentParser.Utilities
{
	/// <summary>
	/// Represents a field for which the value can be invalidated and re-generated as needed.
	/// </summary>
	/// <typeparam name=""></typeparam>
	internal class VolatileField<FieldType>
	{
		#region Sub-types

		/// <summary>
		/// Value generation delegate.
		/// </summary>
		/// <returns>Generated value.</returns>
		public delegate FieldType ValueGenerationDelegate();

		#endregion

		#region Properties

		#region Private storage fields

		/// <summary>
		/// Value validity flag.
		/// </summary>
		private bool Valid = false;

		/// <summary>
		/// Private storage field for the Value property.
		/// </summary>
		private FieldType _Value;

		/// <summary>
		/// Stored value generator method.
		/// </summary>
		private ValueGenerationDelegate GenerateValue { get; }

		#endregion

		/// <summary>
		/// Value.
		/// </summary>
		public FieldType Value
		{
			get
			{
				//check if invalid
				if (!Valid)
				{
					//generate value
					_Value = GenerateValue();

					//toggle validity flag
					Valid = true;
				}

				return _Value;
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Invalidates stored value.
		/// </summary>
		public void Invalidate()
		{
			//set validity flag
			Valid = false;
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="valueGenerationMethod">Value genration method.</param>
		public VolatileField(ValueGenerationDelegate valueGenerationMethod)
		{
			//store value generator
			GenerateValue = valueGenerationMethod;
		}

		#endregion
	}
}
