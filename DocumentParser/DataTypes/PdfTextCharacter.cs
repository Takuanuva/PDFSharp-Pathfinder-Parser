using DocumentParser.Utilities.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DocumentParser.DataTypes
{
	/// <summary>
	/// Represents a PDF page object consisting of a single text character.
	/// </summary>
	public class PdfTextCharacter :
		PdfTextElement,
		PdfTextSegment.ISubElement
	{
		#region Sub-classes

		/// <summary>
		/// Represents a text character's style data.
		/// </summary>
		public class Style
		{
			#region Sub-types

			/// <summary>
			/// Possible types of fonts.
			/// </summary>
			[Flags]
			public enum FontType
			{
				Normal = 0b00,
				Bold = 0b01,
				Italic = 0b10,
				BoldItalic = Bold | Italic
			}

			/// <summary>
			/// Represents a family of related styles.
			/// </summary>
			public class Family
			{
				#region Properties

				/// <summary>
				/// Member styles of the family.
				/// </summary>
				public IReadOnlyDictionary<FontType, Style> Members { get; }

				/// <summary>
				/// Parent parser instance.
				/// </summary>
				public PdfDocumentParser Parent { get; }

				#endregion

				#region Constructors

				/// <summary>
				/// Constructor.
				/// </summary>
				/// <param name="parent">Parent parser instance.</param>
				/// <param name="fontFamily">Name of the font family.</param>
				/// <param name="fillColor">Font fill color (use <see cref="NoColorValueSpecified"/> if no fill or otherwise unspecified).></param>
				/// <param name="strokeColor">Font stroke color (use <see cref="NoColorValueSpecified"/> if no fill or otherwise unspecified).</param>
				/// <param name="strokeWidth">Font stroke width.</param>
				/// <param name="fontSize">Defined font size.</param>
				internal Family(
					PdfDocumentParser parent,
					string fontFamily,
					int fillColor,
					int strokeColor,
					float strokeWidth,
					float fontSize)
				{
					//store property values
					Parent = parent;

					//generate member dictionary
					Members = new Dictionary<FontType, Style>
					{
						{ FontType.Normal, new Style(this, fontFamily, false, false, fillColor, strokeColor, strokeWidth, fontSize)},
						{ FontType.Bold, new Style(this, fontFamily, true, false, fillColor, strokeColor, strokeWidth, fontSize)},
						{ FontType.Italic, new Style(this, fontFamily, false, true, fillColor, strokeColor, strokeWidth, fontSize)},
						{ FontType.BoldItalic, new Style(this, fontFamily, true, true, fillColor, strokeColor, strokeWidth, fontSize)}
					};
				}

				#endregion
			}

			#endregion

			#region Properties

			#region Private storage fields
			
			/// <summary>
			/// Dictionary which contains style families. It is indexed (in this order) by: document parser (allowing for indexing for multiple parser instances), and font signature (allowing for determining the style's family).
			/// </summary>
			private static Dictionary<PdfDocumentParser, Dictionary<string, Family>> StyleFamilyDictionary = new Dictionary<PdfDocumentParser, Dictionary<string, Family>>();

			/// <summary>
			/// Private storage field for the FontHeight property.
			/// </summary>
			private float _FontHeight = float.NaN;

			/// <summary>
			/// Private storage field for the AssociatedCharacters property.
			/// </summary>
			private List<PdfTextCharacter> _AssociatedCharacters = new List<PdfTextCharacter>();

			#region Regular expressions

			/// <summary>
			/// Determines font family name.
			/// </summary>
			private static Regex FontFamilyRegex = new Regex(@"^(?:[A-Z]{6}\+)?(.+?)\-?(?:(?:Regular)|(?:Bold)|(?:Italic))*$", RegexOptions.Compiled);

			/// <summary>
			/// Determines whether a font name signifies a bolded font.
			/// </summary>
			private static Regex IsBoldRegex = new Regex("^.+?(Bold)(?:Italic)?$", RegexOptions.Compiled);

			/// <summary>
			/// Determines whether a font name signifies an italicised font.
			/// </summary>
			private static Regex IsItalicRegex = new Regex("^.+?(Italic)(?:Bold)?$", RegexOptions.Compiled);

			#endregion

			#endregion

			/// <summary>
			/// Parent parser instance.
			/// </summary>
			public PdfDocumentParser Parent { get; }

			/// <summary>
			/// Parent style family.
			/// </summary>
			public Family StyleFamily { get; }

			#region Style data

			/// <summary>
			/// Name of the font family.
			/// </summary>
			public string FontFamily { get; }

			/// <summary>
			/// Font bolding flag, true if bolded font, false otherwise.
			/// </summary>
			public bool IsFontBolded { get; }

			/// <summary>
			/// Font italicisation flag, true if italicised font, false otherwise.
			/// </summary>
			public bool IsFontItalicised { get; }

			/// <summary>
			/// Color value to be used if fill or stroke are not present or the relevant color value is otherwise unspecified.
			/// </summary>
			public static int NoColorValueSpecified { get; } = 0x00FFFFFF;

			/// <summary>
			/// Font fill color.
			/// </summary>
			public int FillColor { get; }

			/// <summary>
			/// Font stroke color.
			/// </summary>
			public int StrokeColor { get; }

			/// <summary>
			/// Font stroke width.
			/// </summary>
			public float StrokeWidth { get; }

			/// <summary>
			/// Defined font size.
			/// </summary>
			public float FontSize { get; }

			/// <summary>
			/// Actual font height (measured as distance between starting points of baseline and ascent line). 
			/// </summary>
			public float FontHeight
			{
				get
				{
					//check if value is invalid
					if (float.IsNaN(_FontHeight))
					{
						//generate value
						float sum = 0;
						foreach (PdfTextCharacter character in AssociatedCharacters)
							sum += character.CharacterHeight;
						_FontHeight = sum / AssociatedCharacters.Count;
					}

					return _FontHeight;
				}
			}



			public string IdString
			{
				get { return GetHashCode().ToString("X8"); }
			}

			#endregion

			#region Associated objects

			/// <summary>
			/// Style's root style (can be self).
			/// </summary>
			public Style RootStyle { get; }

			/// <summary>
			/// List of all text characters using the style.
			/// </summary>
			public IReadOnlyList<PdfTextCharacter> AssociatedCharacters
			{
				get { return _AssociatedCharacters; }
			}

			#endregion

			#endregion

			#region Methods

			#region Static utility methods

			/// <summary>
			/// Generates signature string for provided data.
			/// </summary>
			/// <param name="fontFamily">Font family name.</param>
			/// <param name="fontSize">Font size.</param>
			/// <param name="fillColor">Fill color.</param>
			/// <param name="strokeColor">Stroke color.</param>
			/// <param name="strokeWidth">Stroke width.</param>
			/// <returns>Generated signature string.</returns>
			private static string GenerateSignature(
				string fontFamily,
				float fontSize,
				int fillColor,
				int strokeColor,
				float strokeWidth)
			{
				return $"[{fontFamily}][{fontSize}][{fillColor}][{strokeColor}][{strokeWidth}]";
			}

			/// <summary>
			/// Normalizes float value to prevent small rounding errors from interfering with the matching of style data.
			/// </summary>
			/// <param name="val">Value to be normalized.</param>
			/// <returns>Normalized value.</returns>
			private static float NormalizeFloatValue(float val)
			{
				return (float)Math.Pow(2, Math.Round(Math.Log(val, 2), 1));
			}

			#endregion
			
			/// <summary>
			/// Associates provided <see cref="PdfTextCharacter"/> instance with the style.
			/// </summary>
			/// <param name="textCharacter"></param>
			public void AssociateTextCharacter(PdfTextCharacter textCharacter)
			{
				//add character to list
				_AssociatedCharacters.Add(textCharacter);

				//invalidate properties
				_FontHeight = float.NaN;
			}



			public static IReadOnlyList<Style> GetParserStyleList(PdfDocumentParser parser)
			{
				var dict = StyleFamilyDictionary[parser];
				List<Style> styles = new List<Style>();
				foreach (var kv in dict)
				{
					var familyDict = kv.Value.Members;
					foreach (var kvB in familyDict)
						if (kvB.Value.AssociatedCharacters.Count > 0)
							styles.Add(kvB.Value);
				}
				return styles;
			}

			#endregion

			#region Constructors

			/// <summary>
			/// Constructor.
			/// </summary>
			/// <param name="styleFamily">Parent style family.</param>
			/// <param name="fontFamily">Name of the font family.</param>
			/// <param name="isFontBolded">Font bolding flag, true if bolded font, false otherwise</param>
			/// <param name="isFontItalicised">Font italicisation flag, true if italicised font, false otherwise.</param>
			/// <param name="fillColor">Font fill color (use <see cref="NoColorValueSpecified"/> if no fill or otherwise unspecified).></param>
			/// <param name="strokeColor">Font stroke color (use <see cref="NoColorValueSpecified"/> if no fill or otherwise unspecified).</param>
			/// <param name="strokeWidth">Font stroke width.</param>
			/// <param name="fontSize">Defined font size.</param>
			private Style(
				Family styleFamily,
				string fontFamily,
				bool isFontBolded,
				bool isFontItalicised,
				int fillColor,
				int strokeColor,
				float strokeWidth,
				float fontSize)
			{
				//store property values
				StyleFamily = styleFamily;
				Parent = styleFamily.Parent;
				FontFamily = fontFamily;
				IsFontBolded = isFontBolded;
				IsFontItalicised = isFontItalicised;
				FillColor = fillColor;
				StrokeColor = strokeColor;
				StrokeWidth = strokeWidth;
				FontSize = fontSize;
			}

			#region Factory methods

			/// <summary>
			/// Factory method, retrieves style object matching the provided style data (or generates one if none match).
			/// </summary>
			/// <param name="documentContainer">Document parser currently requesting the style data.</param>
			/// <param name="rawFontName">Raw font name string.</param>
			/// <param name="fontSize">Font size value.</param>
			/// <param name="fillColor">Font fill color.</param>
			/// <param name="strokeColor">Font stroke color.</param>
			/// <param name="strokeWidth">Font stroke width.</param>
			/// <returns />
			public static Style FromStyleData(
				PdfDocumentParser documentContainer,
				string rawFontName,
				float fontSize,
				int fillColor,
				int strokeColor,
				float strokeWidth)
			{
				//parse font name data
				string fontFamily = FontFamilyRegex.Match(rawFontName).Value;
				bool isBold = IsBoldRegex.IsMatch(rawFontName);
				bool isItalic = IsItalicRegex.IsMatch(rawFontName);
				FontType fontType = FontType.Normal;
				if (isBold) fontType |= FontType.Bold;
				if (isItalic) fontType |= FontType.Italic;

				//normalize float values
				fontSize = NormalizeFloatValue(fontSize);
				strokeWidth = NormalizeFloatValue(strokeWidth);

				//generate style signature
				string signature = GenerateSignature(fontFamily, fontSize, fillColor, strokeColor, strokeWidth);

				//get document container's dictionary entry
				Dictionary<string, Family> documentContainerDict;
				if (StyleFamilyDictionary.ContainsKey(documentContainer))
					documentContainerDict = StyleFamilyDictionary[documentContainer];
				else
				{
					//generate new dictionary
					documentContainerDict = new Dictionary<string, Family>();

					//add to style dictionary
					StyleFamilyDictionary.Add(documentContainer, documentContainerDict);
				}

				//get style family's dictionary entry
				Family styleFamily;
				if (documentContainerDict.ContainsKey(signature))
					styleFamily = documentContainerDict[signature];
				else
				{
					//generate new family
					styleFamily = new Family(
						documentContainer,
						fontFamily,
						fillColor,
						strokeColor,
						strokeWidth,
						fontSize);

					//add to style dictionary
					documentContainerDict.Add(signature, styleFamily);
				}
				
				return styleFamily.Members[fontType];
			}

			#endregion

			#endregion
		}

		#endregion

		#region Properties

		#region Private storage fields

		#region Regular expressions

		/// <summary>
		/// Determines whether the character is whitespace or not.
		/// </summary>
		private static Regex IsWhitespaceRegex = new Regex(@"\s", RegexOptions.Compiled);

		#endregion

		#endregion

		/// <summary>
		/// Container segment.
		/// </summary>
		public PdfTextMultiElement<PdfTextCharacter> Container { get; set; }

		/// <summary>
		/// Text character value.
		/// </summary>
		public char CharacterValue
		{
			get { return TextString[0]; }
		}

		/// <summary>
		/// Character style data.
		/// </summary>
		public Style CharacterStyle { get; }

		/// <summary>
		/// Character height (measured from start of baseline to start of ascent line).
		/// </summary>
		public float CharacterHeight
		{
			get { return Baseline.Start.DistanceFrom(AscentLine.Start); }
		}
		
		#endregion

		#region Methods

		/// <summary>
		/// Should never be needed to be called, throws exception otherwise.
		/// </summary>
		protected override void GeneratePropertyValues()
		{
			throw new NotImplementedException();
		}
		
		/// <summary>
		/// Invalidation method.
		/// </summary>
		internal override void Invalidate()
		{
			InvalidateContainer();
		}

		/// <summary>
		/// Container invalidation method.
		/// </summary>
		public void InvalidateContainer()
		{
			//check if not null
			if (Container != null) Container.Invalidate();
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="sourcePage">Document page containing the element.</param>
		/// <param name="textCharacter">Text character value.</param>
		/// <param name="baseline">Text baseline.</param>
		/// <param name="ascentLine">Text ascent line.</param>
		/// <param name="descentLine">Text descent line.</param>
		/// <param name="textStyle">Character style data.</param>
		internal PdfTextCharacter(
			PdfPage page,
			char textCharacter,
			Polyline baseline,
			Polyline ascentLine,
			Polyline descentLine,
			Style textStyle) :
			base(page)
		{
			//generate and store property values
			_TextString = "" + textCharacter;
			_IsWhitespace = IsWhitespaceRegex.IsMatch(TextString);
			_Baseline = baseline;
			_AscentLine = ascentLine;
			_DescentLine = descentLine;
			List<Point> points = new List<Point>();
			points.AddRange(Baseline.Points);
			points.AddRange(AscentLine.Points);
			points.AddRange(DescentLine.Points);
			_BoundingBox = Rectangle.Containing(points);
			CharacterStyle = textStyle;
			_CharacterStyleFamilies = new HashSet<Style.Family>() { CharacterStyle.StyleFamily };
			
			//associate character with the style
			CharacterStyle.AssociateTextCharacter(this);
		}

		#endregion
	}
}
