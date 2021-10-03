using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PdfDocumentData;
using System.Drawing.Drawing2D;

namespace SliceAdjuster
{
	public partial class SectionControl : UserControl
	{
		private Form1 MainForm;
		public PageSection TargetSection { get; private set; }
		
		private void SectionControl_Load(object sender, EventArgs e)
		{
			//update control visibility
			SubsectionsContainer_Panel.Visible = SubsectionVisibility_CheckBox.Checked;

			//generate sub-components
			UpdateInterface();
		}

		public void ValidateProperties()
		{
			UpdateInterface();
		}

		/// <summary>
		/// Updates interface elements.
		/// </summary>
		private void UpdateInterface()
		{
			//update colors
			BackColor = Color.FromArgb(TargetSection.InnerColor);

			//update labels
			SectionId_Label.Text = $"<{TargetSection.Id.ToString("000000")}>";
			SectionColumnPlacement_Label.Text = $"[{TargetSection.ParentColumnIndex.ToString("00")}]";
			SectionRowPlacement_Label.Text = $"[{TargetSection.ParentRowIndex.ToString("00")}]";
			ColumnCount_Label.Text = $"[{TargetSection.ColumnCount.ToString()}]";
			RowCount_Label.Text = $"[{TargetSection.RowCount.ToString()}]";
			SubsectionCount_Label.Text = $"[{TargetSection.SubsectionCount.ToString()}]";

			//update text
			SectionText_TextBox.Text = TargetSection.SectionText;

			//update delim labels
			{
				//update border labels
				BorderCoordLeft_Label.Text = TargetSection.BoundingBox.LeftX.ToString();
				BorderCoordRight_Label.Text = TargetSection.BoundingBox.RightX.ToString();
				BorderCoordBottom_Label.Text = TargetSection.BoundingBox.BottomY.ToString();
				BorderCoordTop_Label.Text = TargetSection.BoundingBox.TopY.ToString();

				//update column labels
				{
					//ensure proper label count
					while (ColumnDelimContainer_Panel.Controls.Count < TargetSection.ColumnDelims.Count)
					{
						//add label
						var label = new Label();
						label.Dock = DockStyle.Bottom;
						label.TextAlign = ContentAlignment.MiddleCenter;
						ColumnDelimContainer_Panel.Controls.Add(label);
					}
					while (ColumnDelimContainer_Panel.Controls.Count > TargetSection.ColumnDelims.Count)
					{
						//remove label
						ColumnDelimContainer_Panel.Controls.RemoveAt(ColumnDelimContainer_Panel.Controls.Count - 1);
					}

					//update label text
					for (int i = 0; i < TargetSection.ColumnDelims.Count; i++)
						(ColumnDelimContainer_Panel.Controls[i] as Label).Text = TargetSection.ColumnDelims[i].ToString();
				}

				//update row labels
				{
					//ensure proper label count
					while (RowDelimContainer_Panel.Controls.Count < TargetSection.RowDelims.Count)
					{
						//add label
						var label = new Label();
						label.Dock = DockStyle.Top;
						label.TextAlign = ContentAlignment.MiddleCenter;
						RowDelimContainer_Panel.Controls.Add(label);
					}
					while (RowDelimContainer_Panel.Controls.Count > TargetSection.RowDelims.Count)
					{
						//remove label
						RowDelimContainer_Panel.Controls.RemoveAt(RowDelimContainer_Panel.Controls.Count - 1);
					}

					//update label text
					for (int i = 0; i < TargetSection.RowDelims.Count; i++)
						(RowDelimContainer_Panel.Controls[i] as Label).Text = TargetSection.RowDelims[i].ToString();
				}
			}

			//update subsectiom controls
			{
				//check if table
				if (TargetSection.ColumnCount > 1 && TargetSection.RowCount > 1)
				{
					//enable rowify button
					RowifyLines_Button.Enabled = true;
					RowifyLines_Button.Visible = true;

					//clear subsection controls
					SubsectionsContainer_Panel.Controls.Clear();
				}
				else
				{
					//disable rowify button
					RowifyLines_Button.Enabled = false;
					RowifyLines_Button.Visible = false;

					//get dock type
					DockStyle dockType;
					if (TargetSection.ColumnCount > 1)
						dockType = DockStyle.Bottom;
					else
						dockType = DockStyle.Top;

					//get subsection control list
					List<SectionControl> controls = new List<SectionControl>();
					for (int i = 0; i < TargetSection.SubsectionCount; i++)
					{
						//get subsection
						var subsection = TargetSection.Subsections[i];

						//find section control
						SectionControl control = null;
						for (int j = 0; j < SubsectionsContainer_Panel.Controls.Count; j++)
						{
							control = SubsectionsContainer_Panel.Controls[j] as SectionControl;
							if (control.TargetSection == subsection)
								break;
							control = null;
						}

						//check if control was not found
						if (control == null)
							control = new SectionControl(MainForm, subsection);

						//update dock type
						control.Dock = dockType;

						//add to list
						controls.Add(control);
					}

					//clear subsections
					SubsectionsContainer_Panel.Controls.Clear();

					//add and update subsections
					foreach (var control in controls)
					{
						SubsectionsContainer_Panel.Controls.Add(control);
						(control as SectionControl).UpdateInterface();
					}
				}
			}
		}

		#region Edit methods

		private static List<(PointF, PointF, Color, DashStyle, float)> GenerateVerticalHighlight(PageSection section, float coord, DashStyle dashStyle, float width)
		{
			PointF start =
				new PointF(
					coord,
					section.BoundingBox.BottomY);
			PointF end =
				new PointF(
					coord,
					section.BoundingBox.TopY);
			return new List<(PointF, PointF, Color, DashStyle, float)>()
			{
				(
					start,
					end,
					Color.Black,
					DashStyle.Solid,
					width + 4
				),
				(
					start,
					end,
					Color.White,
					dashStyle,
					width
				)
			};
		}
		private static List<(PointF, PointF, Color, DashStyle, float)> GenerateHorizontalHighlight(PageSection section, float coord, DashStyle dashStyle, float width)
		{
			PointF start =
				new PointF(
					section.BoundingBox.LeftX,
					coord);
			PointF end =
				new PointF(
					section.BoundingBox.RightX,
					coord);
			return new List<(PointF, PointF, Color, DashStyle, float)>()
			{
				(
					start,
					end,
					Color.Black,
					DashStyle.Solid,
					width + 4
				),
				(
					start,
					end,
					Color.White,
					dashStyle,
					width
				)
			};
		}
		private static List<(PointF, PointF, Color, DashStyle, float)> GenerateColumnHighlights(PageSection section, DashStyle highlightStyle)
		{
			//initialize output list
			var output = new List<(PointF, PointF, Color, DashStyle, float)>();

			//generate colors
			Color innerColor = Color.White;
			Color outerColor = Color.Black;

			//add delim lines
			foreach (float coord in section.ColumnDelims)
				output.AddRange(GenerateVerticalHighlight(section, coord, highlightStyle, 2));

			//add bounding box lines
			{
				output.AddRange(GenerateVerticalHighlight(section, section.BoundingBox.LeftX, highlightStyle, 4));
				output.AddRange(GenerateVerticalHighlight(section, section.BoundingBox.RightX, highlightStyle, 4));
			}

			return output;
		}
		private static List<(PointF, PointF, Color, DashStyle, float)> GenerateRowHighlights(PageSection section, DashStyle highlightStyle)
		{
			//initialize output list
			var output = new List<(PointF, PointF, Color, DashStyle, float)>();

			//generate colors
			Color innerColor = Color.White;
			Color outerColor = Color.Black;

			//add delim lines
			foreach (float coord in section.RowDelims)
				output.AddRange(GenerateHorizontalHighlight(section, coord, highlightStyle, 2));

			//add bounding box lines
			{
				output.AddRange(GenerateHorizontalHighlight(section, section.BoundingBox.BottomY, highlightStyle, 4));
				output.AddRange(GenerateHorizontalHighlight(section, section.BoundingBox.TopY, highlightStyle, 4));
			}

			return output;
		}

		#endregion

		#region Interface events

		private void ColumnDelimsAdd_Button_Click(object sender, EventArgs e)
		{
			//start edit
			(ParentForm as Form1).StartEdit(
				(PointF point, MouseButtons clickButtons) =>
				{
					if ((clickButtons & MouseButtons.Left) != 0)
						TargetSection.AddColumnDelim(
							point.X,
							PageSection.SubdivisionSiding.LowerSide);
					else if ((clickButtons & MouseButtons.Right) != 0)
						TargetSection.AddColumnDelim(
							point.X,
							PageSection.SubdivisionSiding.HigherSide);
				}, 
				GenerateColumnHighlights(TargetSection, DashStyle.Solid));
		}

		private void RowDelimsAdd_Button_Click(object sender, EventArgs e)
		{
			//start edit
			(ParentForm as Form1).StartEdit(
				(PointF point, MouseButtons clickButtons) =>
				{
					if ((clickButtons & MouseButtons.Left) != 0)
						TargetSection.AddRowDelim(
							point.Y,
							PageSection.SubdivisionSiding.LowerSide);
					else if ((clickButtons & MouseButtons.Right) != 0)
						TargetSection.AddRowDelim(
							point.Y,
							PageSection.SubdivisionSiding.HigherSide);
				},
				GenerateRowHighlights(TargetSection, DashStyle.Solid));
		}

		private void ColumnDelimsMove_Button_Click(object sender, EventArgs e)
		{
			//start edit
			(ParentForm as Form1).StartEdit(
				(PointF selectPoint, MouseButtons selectClickButtons) =>
				{
					//find which edge of the clicked column should be adjusted
					PageSection.SubdivisionSiding columnSide;
					if ((selectClickButtons & MouseButtons.Left) != 0)
						columnSide = PageSection.SubdivisionSiding.LowerSide;
					else if ((selectClickButtons & MouseButtons.Right) != 0)
						columnSide = PageSection.SubdivisionSiding.HigherSide;
					else
						return;

					//check if click was out of bounds
					if (selectPoint.X < TargetSection.BoundingBox.LeftX ||
						selectPoint.X > TargetSection.BoundingBox.RightX)
						return;

					//find which column has been clicked
					int clickedColumnIndex = 0;
					for (; clickedColumnIndex < TargetSection.ColumnDelims.Count; clickedColumnIndex++)
						if (selectPoint.X < TargetSection.ColumnDelims[clickedColumnIndex])
							break;

					//declare delim adjustment function
					void adjustDelim(
						PageSection editTargetSection,
						int editTargetDelimIndex)
					{
						//generate highlight list
						var editHighlights = new List<(PointF, PointF, Color, DashStyle, float)>();
						{
							editHighlights.AddRange(
								GenerateVerticalHighlight(
									editTargetSection,
									(editTargetDelimIndex == 0 ? editTargetSection.BoundingBox.LeftX : editTargetSection.ColumnDelims[editTargetDelimIndex - 1]),
									DashStyle.Dot,
									2));
							editHighlights.AddRange(
								GenerateVerticalHighlight(
									editTargetSection,
									editTargetSection.ColumnDelims[editTargetDelimIndex],
									DashStyle.Solid,
									4));
							editHighlights.AddRange(
								GenerateVerticalHighlight(
									editTargetSection,
									(editTargetDelimIndex == editTargetSection.ColumnDelims.Count - 1 ? editTargetSection.BoundingBox.RightX : editTargetSection.ColumnDelims[editTargetDelimIndex + 1]),
									DashStyle.Dot,
									2));
						};

						//start edit
						(ParentForm as Form1).StartEdit(
							(PointF editPoint, MouseButtons editClickButtons) =>
							{
								//check if left clicked    
								if ((editClickButtons & MouseButtons.Left) != 0)
								{
									//adjust delim
									editTargetSection.ModifyColumnDelim(editTargetDelimIndex, editPoint.X);
								}
							},
							editHighlights);
					}

					//check if left edge was selected
					if (clickedColumnIndex == 0 && columnSide == PageSection.SubdivisionSiding.LowerSide)
					{
						//adjust left edge
						void adjustLeftEdge(PageSection section)
						{
							//check if not root node
							if (section.Parent != null)
							{
								//check if section resides within leftmost column
								if (section.ParentColumnIndex == 0)
									adjustLeftEdge(section.Parent);
								else
									adjustDelim(
										section.Parent,
										section.ParentColumnIndex - 1);
							}
							else
							{
								//generate highlight list
								var editHighlights = new List<(PointF, PointF, Color, DashStyle, float)>();
								{
									editHighlights.AddRange(
										GenerateVerticalHighlight(
												section,
												section.BoundingBox.LeftX,
												DashStyle.Solid,
												4));
									editHighlights.AddRange(
										GenerateVerticalHighlight(
												section,
												(section.ColumnDelims.Count == 0 ? section.BoundingBox.RightX : section.ColumnDelims.First()),
												DashStyle.Dot,
												2));
								};

								//start edit
								(ParentForm as Form1).StartEdit(
									(PointF editPoint, MouseButtons editClickButtons) =>
									{
										//check if left clicked    
										if ((editClickButtons & MouseButtons.Left) != 0)
										{
											//adjust edge
											(section as RootSection).ContentArea = new BoxCoords(
												editPoint.X,
												section.BoundingBox.RightX,
												section.BoundingBox.BottomY,
												section.BoundingBox.TopY);
										}
									},
									editHighlights);
							}
						}
						adjustLeftEdge(TargetSection);
					}

					//check if right edge was selected
					else if (clickedColumnIndex == TargetSection.ColumnCount - 1 && columnSide == PageSection.SubdivisionSiding.HigherSide)
					{
						//adjust right edge
						void adjustRightEdge(PageSection section)
						{
							//check if not root node
							if (section.Parent != null)
							{
								//check if section resides within leftmost column
								if (section.ParentColumnIndex == section.ColumnCount - 1)
									adjustRightEdge(section.Parent);
								else
									adjustDelim(
										section.Parent,
										section.ParentColumnIndex);
							}
							else
							{
								//generate highlight list
								var editHighlights = new List<(PointF, PointF, Color, DashStyle, float)>();
								{
									editHighlights.AddRange(
										GenerateVerticalHighlight(
											section,
											section.BoundingBox.RightX,
											DashStyle.Solid,
											4));
									editHighlights.AddRange(
										GenerateVerticalHighlight(
											section,
											(section.ColumnDelims.Count == 0 ? section.BoundingBox.LeftX : section.ColumnDelims.Last()),
											DashStyle.Dot,
											2));
								};

								//start edit
								(ParentForm as Form1).StartEdit(
									(PointF editPoint, MouseButtons editClickButtons) =>
									{
										//check if left clicked    
										if ((editClickButtons & MouseButtons.Left) != 0)
										{
											//adjust edge
											(section as RootSection).ContentArea = new BoxCoords(
												section.BoundingBox.LeftX,
												editPoint.X,
												section.BoundingBox.BottomY,
												section.BoundingBox.TopY);
										}
									},
									editHighlights);
							}
						}
						adjustRightEdge(TargetSection);
					}

					//adjust delim
					else
						adjustDelim(
							TargetSection,
							columnSide == PageSection.SubdivisionSiding.HigherSide ? clickedColumnIndex : clickedColumnIndex - 1);
				},
				GenerateColumnHighlights(TargetSection, DashStyle.Solid));
		}

		private void RowDelimsMove_Button_Click(object sender, EventArgs e)
		{
			//start edit
			(ParentForm as Form1).StartEdit(
				(PointF selectPoint, MouseButtons selectClickButtons) =>
				{
					//find which edge of the clicked row should be adjusted
					PageSection.SubdivisionSiding rowSide;
					if ((selectClickButtons & MouseButtons.Left) != 0)
						rowSide = PageSection.SubdivisionSiding.LowerSide;
					else if ((selectClickButtons & MouseButtons.Right) != 0)
						rowSide = PageSection.SubdivisionSiding.HigherSide;
					else
						return;

					//check if click was out of bounds
					if (selectPoint.Y < TargetSection.BoundingBox.BottomY ||
						selectPoint.Y > TargetSection.BoundingBox.TopY)
						return;

					//find which row has been clicked
					int clickedRowIndex = 0;
					for (; clickedRowIndex < TargetSection.RowDelims.Count; clickedRowIndex++)
						if (selectPoint.Y < TargetSection.RowDelims[clickedRowIndex])
							break;

					//declare delim adjustment function
					void adjustDelim(
						PageSection editTargetSection,
						int editTargetDelimIndex)
					{
						//generate highlight list
						var editHighlights = new List<(PointF, PointF, Color, DashStyle, float)>();
						{
							editHighlights.AddRange(
								GenerateHorizontalHighlight(
									editTargetSection,
									(editTargetDelimIndex == 0 ? editTargetSection.BoundingBox.BottomY : editTargetSection.RowDelims[editTargetDelimIndex - 1]),
									DashStyle.Dot,
									2));
							editHighlights.AddRange(
								GenerateHorizontalHighlight(
									editTargetSection,
									editTargetSection.RowDelims[editTargetDelimIndex],
									DashStyle.Solid,
									4));
							editHighlights.AddRange(
								GenerateHorizontalHighlight(
									editTargetSection,
									(editTargetDelimIndex == editTargetSection.RowDelims.Count - 1 ? editTargetSection.BoundingBox.TopY : editTargetSection.RowDelims[editTargetDelimIndex + 1]),
									DashStyle.Dot,
									2));
						};

						//start edit
						(ParentForm as Form1).StartEdit(
							(PointF editPoint, MouseButtons editClickButtons) =>
							{
								//check if left clicked    
								if ((editClickButtons & MouseButtons.Left) != 0)
								{
									//adjust delim
									editTargetSection.ModifyRowDelim(editTargetDelimIndex, editPoint.Y);
								}
							},
							editHighlights);
					}

					//check if bottom edge was selected
					if (clickedRowIndex == 0 && rowSide == PageSection.SubdivisionSiding.LowerSide)
					{
						//adjust bottom edge
						void adjustBottomEdge(PageSection section)
						{
							//check if not root node
							if (section.Parent != null)
							{
								//check if section resides within bottommost row
								if (section.ParentRowIndex == 0)
									adjustBottomEdge(section.Parent);
								else
									adjustDelim(
										section.Parent,
										section.ParentRowIndex - 1);
							}
							else
							{
								//generate highlight list
								var editHighlights = new List<(PointF, PointF, Color, DashStyle, float)>();
								{
									editHighlights.AddRange(
										GenerateHorizontalHighlight(
											section,
											section.BoundingBox.BottomY,
											DashStyle.Solid,
											4));
									editHighlights.AddRange(
										GenerateHorizontalHighlight(
											section,
											(section.RowDelims.Count == 0 ? section.BoundingBox.TopY : section.RowDelims.First()),
											DashStyle.Dot,
											2));
								};

								//start edit
								(ParentForm as Form1).StartEdit(
									(PointF editPoint, MouseButtons editClickButtons) =>
									{
										//check if left clicked    
										if ((editClickButtons & MouseButtons.Left) != 0)
										{
											//adjust edge
											(section as RootSection).ContentArea = new BoxCoords(
												section.BoundingBox.LeftX,
												section.BoundingBox.RightX,
												editPoint.Y,
												section.BoundingBox.TopY);
										}
									},
									editHighlights);
							}
						}
						adjustBottomEdge(TargetSection);
					}

					//check if top edge was selected
					else if (clickedRowIndex == TargetSection.RowCount - 1 && rowSide == PageSection.SubdivisionSiding.HigherSide)
					{
						//adjust top edge
						void adjustTopEdge(PageSection section)
						{
							//check if not root node
							if (section.Parent != null)
							{
								//check if section resides within topmost row
								if (section.ParentRowIndex == section.RowCount - 1)
									adjustTopEdge(section.Parent);
								else
									adjustDelim(
										section.Parent,
										section.ParentRowIndex);
							}
							else
							{
								//generate highlight list
								var editHighlights = new List<(PointF, PointF, Color, DashStyle, float)>();
								{
									editHighlights.AddRange(
										GenerateHorizontalHighlight(
											section,
											section.BoundingBox.TopY,
											DashStyle.Solid,
											4));
									editHighlights.AddRange(
										GenerateHorizontalHighlight(
											section,
											(section.RowDelims.Count == 0 ? section.BoundingBox.BottomY : section.RowDelims.Last()),
											DashStyle.Dot,
											2));
								};

								//start edit
								(ParentForm as Form1).StartEdit(
									(PointF editPoint, MouseButtons editClickButtons) =>
									{
										//check if left clicked    
										if ((editClickButtons & MouseButtons.Left) != 0)
										{
											//adjust edge
											(section as RootSection).ContentArea = new BoxCoords(
												section.BoundingBox.LeftX,
												section.BoundingBox.RightX,
												section.BoundingBox.BottomY,
												editPoint.Y);
										}
									},
									editHighlights);
							}
						}
						adjustTopEdge(TargetSection);
					}

					//adjust delim
					else
						adjustDelim(
							TargetSection,
							rowSide == PageSection.SubdivisionSiding.HigherSide ? clickedRowIndex : clickedRowIndex - 1);
				},
				GenerateRowHighlights(TargetSection, DashStyle.Solid));
		}

		private void ColumnDelimsDelete_Button_Click(object sender, EventArgs e)
		{
			//check if invalid state
			if (TargetSection.ColumnCount == 1)
				return;

			//start edit
			(ParentForm as Form1).StartEdit(
				(PointF point, MouseButtons clickButtons) =>
				{
					//find which column has been clicked
					int clickedColumnIndex = 0;
					for (; clickedColumnIndex < TargetSection.ColumnDelims.Count; clickedColumnIndex++)
						if (point.X < TargetSection.ColumnDelims[clickedColumnIndex])
							break;

					//check click type
					if ((clickButtons & MouseButtons.Left) != 0)
					{
						//check if invalid selection
						if (clickedColumnIndex == 0)
							return;

						//remove delim
						TargetSection.RemoveColumnDelim(
							clickedColumnIndex - 1,
							PageSection.SubdivisionSiding.HigherSide);
					}
					else if ((clickButtons & MouseButtons.Right) != 0)
					{
						//check if invalid selection
						if (clickedColumnIndex == TargetSection.ColumnCount - 1)
							return;

						//remove delim
						TargetSection.RemoveColumnDelim(
							clickedColumnIndex,
							PageSection.SubdivisionSiding.LowerSide);
					}
				},
				GenerateColumnHighlights(TargetSection, DashStyle.Solid));
		}

		private void RowDelimsDelete_Button_Click(object sender, EventArgs e)
		{
			//check if invalid state
			if (TargetSection.RowCount == 1)
				return;

			//start edit
			(ParentForm as Form1).StartEdit(
				(PointF point, MouseButtons clickButtons) =>
				{
					//find which row has been clicked
					int clickedRowIndex = 0;
					for (; clickedRowIndex < TargetSection.RowDelims.Count; clickedRowIndex++)
						if (point.Y < TargetSection.RowDelims[clickedRowIndex])
							break;

					//check click type
					if ((clickButtons & MouseButtons.Left) != 0)
					{
						//check if invalid selection
						if (clickedRowIndex == 0)
							return;

						//remove delim
						TargetSection.RemoveRowDelim(
							clickedRowIndex - 1,
							PageSection.SubdivisionSiding.HigherSide);
					}
					else if ((clickButtons & MouseButtons.Right) != 0)
					{
						//check if invalid selection
						if (clickedRowIndex == TargetSection.RowCount - 1)
							return;

						//remove delim
						TargetSection.RemoveRowDelim(
							clickedRowIndex,
							PageSection.SubdivisionSiding.LowerSide);
					}
				},
				GenerateRowHighlights(TargetSection, DashStyle.Solid));
		}

		private void RowifyLines_Button_Click(object sender, EventArgs e)
		{
			//check if invalid state
			if (TargetSection.RowCount == 1 || TargetSection.ColumnCount == 1)
				return;

			//start edit
			(ParentForm as Form1).StartEdit(
				(PointF point, MouseButtons clickButtons) =>
				{
					//find which row has been clicked
					int clickedRowIndex = 0;
					for (; clickedRowIndex < TargetSection.RowDelims.Count; clickedRowIndex++)
						if (point.Y < TargetSection.RowDelims[clickedRowIndex])
							break;

					//separate lines into rows
					{
						//get row bounds
						float highBound = (clickedRowIndex < TargetSection.RowDelims.Count) ? TargetSection.RowDelims[clickedRowIndex] : TargetSection.BoundingBox.TopY;
						float lowBound = (clickedRowIndex > 0) ? TargetSection.RowDelims[clickedRowIndex] : TargetSection.BoundingBox.BottomY;

						//get characters within clicked row
						HashSet<TextCharacter> characters = new HashSet<TextCharacter>();
						foreach (var character in TargetSection.SectionCharacters)
							if (character.BoundingBox.Center.Y <= highBound &&
								character.BoundingBox.Center.Y >= lowBound)
								characters.Add(character);

						//generate upper and lower bounds of rows
						List<float> upperBounds = new List<float>();
						List<float> lowerBounds = new List<float>();
						foreach (var character in characters)
						{
							bool foundRow = false;
							for (int i = 0; i < upperBounds.Count; i++)
							{
								if (character.BoundingBox.BottomY < upperBounds[i] &&
									character.BoundingBox.TopY > lowerBounds[i])
								{
									upperBounds[i] = Math.Max(upperBounds[i], character.BoundingBox.TopY);
									lowerBounds[i] = Math.Min(lowerBounds[i], character.BoundingBox.BottomY);
									foundRow = true;
									break;
								}
							}
							if (foundRow) continue;
							upperBounds.Add(character.BoundingBox.TopY);
							lowerBounds.Add(character.BoundingBox.BottomY);
						}

						//add rows to section
						for (int i = 1; i < lowerBounds.Count; i++)
							TargetSection.AddRowDelim((upperBounds[i - 1] + lowerBounds[i]) / 2, PageSection.SubdivisionSiding.HigherSide);
					}
				},
				GenerateRowHighlights(TargetSection, DashStyle.Solid));
		}

		/// <summary>
		/// Checkbox check event.
		/// </summary>
		/// <param name="sender" />
		/// <param name="e" />
		private void SubsectionVisibility_CheckBox_CheckedChanged(object sender, EventArgs e)
		{
			//set visibility
			SubsectionsContainer_Panel.Visible = SubsectionVisibility_CheckBox.Checked;
		}
		
		#endregion

		#region Contructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="mainForm">Form which created the control.</param>
		/// <param name="targetSection">Target section.</param>
		public SectionControl(
			Form1 mainForm,
			PageSection targetSection)
		{
			//store references
			MainForm = mainForm;
			TargetSection = targetSection;

			//initialize control
			InitializeComponent();
		}

		#endregion

	}
}
