using DocumentParser.DataTypes;
using DocumentParser.Utilities.Geometry;
using System.Collections.Generic;

namespace DocumentParser.Utilities.DataExtraction
{
	/*public abstract class PdfDataExtractor
	{
		#region Properties

		#region Private storage fields

		/// <summary>
		/// Data extraction state flags.
		/// </summary>
		[System.Flags]
		private enum DataGenerationState
		{
			NoneReady = 0,
			DocumentReady = 1,
			PageReady = 2,
			SegmentReady = 3,
			CharacterReady = 4,
			Finalize = NoneReady
		}

		/// <summary>
		/// Current generator state.
		/// </summary>
		private DataGenerationState State = DataGenerationState.NoneReady;

		/// <summary>
		/// Private storage field for the Document property.
		/// </summary>
		private Document _Document = null;

		/// <summary>
		/// Currently processed page.
		/// </summary>
		private DocumentPage CurrentPage = null;
		
		/// <summary>
		/// Text style associated with next segment.
		/// </summary>
		private TextStyle NextSegmentStyle = null;

		/// <summary>
		/// Currently processed segment.
		/// </summary>
		private TextSegment CurrentSegment = null;
		
		#endregion

		/// <summary>
		/// Extracted text document.
		/// </summary>
		public Document Document
		{
			get
			{
				//check if document has been generated
				if (_Document == null)
				{
					//generate document
					ExtractDocument();
				}

				return _Document;
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Data extraction method, implement actual document data extraction here.
		/// </summary>
		protected abstract void ExtractDocumentData();

		/// <summary>
		/// Extracts and generates the output document.
		/// </summary>
		public void ExtractDocument()
		{
			//initialize document object
			_Document = new Document();

			//adjust generator state
			State = DataGenerationState.DocumentReady;

			//extract document
			ExtractDocumentData();

			//finalize document
			FinalizeObjects(DataGenerationState.Finalize);
		}

		#region Document generation control

		/// <summary>
		/// Finalizes currently existing objects until target state is reached. Throws <see cref="System.InvalidOperationException"/> if target state would require initializing objects instead.
		/// </summary>
		/// <param name="targetState">Target generator state.</param>
		private void FinalizeObjects(DataGenerationState targetState)
		{
			//check target state
			if (targetState > State)
				throw new System.InvalidOperationException("Object finalization failed: target generator state requires object initialization.");
			
			//finalize objects
			switch (State)
			{
				case DataGenerationState.CharacterReady:
					//fall through to next case
					goto case DataGenerationState.SegmentReady;

				case DataGenerationState.SegmentReady:
					//check if all required objects have been finalized
					if (targetState >= DataGenerationState.SegmentReady) break;
					
					//set object to null
					CurrentSegment = null;
					
					//fall through to next case
					goto case DataGenerationState.PageReady;

				case DataGenerationState.PageReady:
					//check if all required objects have been finalized
					if (targetState >= DataGenerationState.PageReady) break;
					
					//set object to null
					CurrentPage = null;

					//fall through to next case
					goto case DataGenerationState.DocumentReady;

				case DataGenerationState.DocumentReady:
					//check if all required objects have been finalized
					if (targetState >= DataGenerationState.DocumentReady) break;
					
					//fall through to next case
					goto case DataGenerationState.NoneReady;

				case DataGenerationState.NoneReady:
					break;

				default:
					throw new System.NotImplementedException($"Switch case for current state not implemented. Current state number: { State }.");
			}

			//adjust generator state
			State = targetState;
		}

		/// <summary>
		/// Ends current page of document and starts the next one.
		/// </summary>
		/// <param name="mediaBox">Page's media box.</param>
		/// <param name="cropBox">Page's crop box.</param>
		/// <param name="trimBox">Page's trim box.</param>
		/// <param name="artBox">Page's art box.</param>
		/// <param name="bleedBox">Page's bleed box.</param>
		protected void NextPage(
			Rectangle mediaBox,
			Rectangle cropBox = null,
			Rectangle trimBox = null,
			Rectangle artBox = null,
			Rectangle bleedBox = null)
		{
			//finalize objects until correct generator state is reached
			FinalizeObjects(DataGenerationState.DocumentReady);
			
			//create new page
			CurrentPage = new DocumentPage(
				Document, 
				mediaBox, 
				cropBox, 
				trimBox, 
				artBox, 
				bleedBox);

			//adjust generator state
			State = DataGenerationState.PageReady;
		}
		
		/// <summary>
		/// Ends current segment within the line and starts the next one.
		/// </summary>
		/// <param name="fontName">Text font name.</param>
		/// <param name="fontHeight">Text font height.</param>
		/// <param name="fillColor">Text fill color.</param>
		/// <param name="strokeColor">Text stroke color.</param>
		protected void NextSegment(
			string fontName,
			float fontHeight,
			int fillColor,
			int strokeColor)
		{
			//get segment style
			TextStyle nextSegmentStyle = Document.GetTextStyle(
				fontName,
				fontHeight,
				fillColor,
				strokeColor);

			//generate next segment
			NextSegment(nextSegmentStyle);
		}

		/// <summary>
		/// Ends current segment within the line and starts the next one.
		/// </summary>
		/// <param name="nextSegmentStyle" />
		private void NextSegment(
			TextStyle nextSegmentStyle)
		{
			//finalize objects until correct generator state is reached
			FinalizeObjects(DataGenerationState.PageReady);

			//store next segment style
			NextSegmentStyle = nextSegmentStyle;

			//adjust generator state
			State = DataGenerationState.SegmentReady;
		}

		/// <summary>
		/// Adds a character to the current segment.
		/// </summary>
		/// <param name="character">Added character.</param>
		/// <param name="descentLine">Character descent line.</param>
		/// <param name="baseline">Character baseline.</param>
		/// <param name="ascentLine">Character ascent line.</param>
		protected void AddCharacter(
			char character,
			Line descentLine,
			Line baseline,
			Line ascentLine)
		{
			//ensure correct state
			FinalizeObjects(DataGenerationState.SegmentReady);

			//generate character object
			TextCharacter characterObj = new TextCharacter(
				CurrentPage,
				character,
				descentLine,
				baseline,
				ascentLine);

			//check if segment exists
			if (CurrentSegment != null)
			{
				//attempt to add character to current segment
				if (CurrentSegment.TryAddCharacter(characterObj, NextSegmentStyle))
					return;

				//go to next segment
				NextSegment(characterObj.Style);
			}

			//create new segment
			TextSegment segment = new TextSegment(
				CurrentPage,
				characterObj, );

			//check if line exists
			if (CurrentLine != null)
			{
				//attempt to add segment to current line
				if (CurrentLine.TryAddSegment(segment))
				{
					//save segment
					CurrentSegment = segment;

					return;
				}

				//go to next line
				NextLine();

				//go to next segment
				NextSegment(characterObj.Style);
			}

			//create new line
			CurrentLine = new TextLine(CurrentPage, segment);

			//save segment
			CurrentSegment = segment;
		}

		/// <summary>
		/// Adds an image to the page.
		/// </summary>
		/// <param name="resourceSignature">Unique signature string of the image's used resource.</param>
		/// <param name="position">Image's position.</param>
		protected void AddImage(
			string resourceSignature,
			Point position)
		{
			//check if correct state
			if (State < DataGenerationState.PageReady)
				throw new System.InvalidOperationException("Object finalization failed: target generator state requires object initialization.");
			
			//create image object
			Image image = new Image(
				CurrentPage,
				resourceSignature,
				position);

			//finalize image constwruction
			image.FinalizeConstructionAAA();
		}

		/// <summary>
		/// Verifies whether the image resource dictionary of the document contains the provided key.
		/// </summary>
		/// <param name="resourceSignature">Resource's unique signature string.</param>
		/// <returns>True if image resource dictionary contains the key, false otherwise..</returns>
		protected bool IsImageResourcePresent(string resourceSignature)
		{
			return Document.IsImageResourcePresent(resourceSignature);
		}

		/// <summary>
		/// Retrieves image resource from dictionary.
		/// </summary>
		/// <param name="resourceSignature">Resource's unique signature string.</param>
		/// <returns>Image resource matching the provided resource signature.</returns>
		private ImageResource GetImageBitmap(string resourceSignature)
		{
			return Document.GetImageResource(resourceSignature);
		}

		/// <summary>
		/// Adds image resource to the dictionary.
		/// </summary>
		/// <param name="resourceSignature">Resource's unique signature string.</param>
		/// <param name="bitmap">Image's bitmap.</param>
		protected void AddImageResource(string resourceSignature, System.Drawing.Bitmap bitmap)
		{
			//create image resource
			ImageResource imageResource = new ImageResource(
					Document,
					resourceSignature,
					bitmap);

			//finalize construction
			imageResource.FinalizeConstructionAAA();
		}

		#endregion

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		protected PdfDataExtractor()
		{ }

		#endregion
	}*/
}
