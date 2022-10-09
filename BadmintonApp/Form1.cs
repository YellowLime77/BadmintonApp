﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using Microsoft.Data.Analysis;
using System.IO;
using System.Text.RegularExpressions;

namespace BadmintonApp
{
    public partial class Form1 : Form
    {
        bool shotSelected = false;
        bool situationSelected = false;
        bool turnSelected = false;
        bool winnerSelected = false;
        Button shotButtonSelected;
        Button situationButtonSelected;
        Button turnButtonSelected;
        Button winnerButtonSelected;

        Form2 frm2;

        StringDataFrameColumn startingAreaColumn = new StringDataFrameColumn("Starting Area");
        StringDataFrameColumn endingAreaColumn = new StringDataFrameColumn("Ending Area");
        PrimitiveDataFrameColumn<Point> startingPointColumn = new PrimitiveDataFrameColumn<Point>("Starting Point");
        PrimitiveDataFrameColumn<Point> endingPointColumn = new PrimitiveDataFrameColumn<Point>("Ending Point");
        StringDataFrameColumn shotTypeColumn = new StringDataFrameColumn("Shot Type");
        StringDataFrameColumn situationTypeColumn = new StringDataFrameColumn("Situation Type");
        StringDataFrameColumn turnColumn = new StringDataFrameColumn("Turn");
        StringDataFrameColumn notesColumn = new StringDataFrameColumn("Notes");

        DataFrame df;

        List<Object> pointsArray = new List<Object>();

        public List<List<Object>> rallies { get; set; } = new List<List<Object>>();

        public Form1()
        {
            InitializeComponent();
        }

        private Point LocationOffsetPoint(Point location, Point offset)
        {
            location.Offset(offset.X, offset.Y);
            return location;
        }
        private Point LocationRelativeToScreen(Control control)
        {
            return control.FindForm().PointToClient(control.Parent.PointToScreen(control.Location));
        }

        public string GetAreaOnCourtPlayer(Point location, PictureBox courtPictureBox)
        {
            if (location.Y > courtPictureBox.Height * 0.88235294117)
            {
                if (location.X < courtPictureBox.Width * 0.3448275862)
                {
                    return "Back Left";
                } else if (location.X > courtPictureBox.Width * 0.65517241379)
                {
                    return "Back Right";
                } else
                {
                    return "Back Middle";
                }
            } else if (location.Y > courtPictureBox.Height * 0.5 && courtPictureBox.Height * 0.71493212669 > location.Y)
            {
                if (location.X < courtPictureBox.Width * 0.3448275862)
                {
                    return "Front Left";
                }
                else if (location.X > courtPictureBox.Width * 0.65517241379)
                {
                    return "Front Right";
                }
                else
                {
                    return "Front Middle";
                }
            } else
            {
                if (location.X < courtPictureBox.Width * 0.3448275862)
                {
                    return "Middle Left";
                }
                else if (location.X > courtPictureBox.Width * 0.65517241379)
                {
                    return "Middle Right";
                }
                else
                {
                    return "Middle Middle";
                }
            }
        }

        public Point GetPointFromAreaPlayer(string area, PictureBox courtPictureBox)
        {
            Point resultPoint = new Point();
            if (area.StartsWith("Front"))
            {
                resultPoint.Y = (int)Math.Round(courtPictureBox.Height * 0.64592760181);
            }
            else if (area.StartsWith("Middle"))
            {
                resultPoint.Y = (int)Math.Round(courtPictureBox.Height * 0.78959276018);
            }
            else if (area.StartsWith("Back"))
            {
                resultPoint.Y = (int)Math.Round(courtPictureBox.Height * 0.97171945701); 
            }

            if (area.EndsWith("Left"))
            {
                resultPoint.X = (int)Math.Round(courtPictureBox.Width * 0.1724137931);
            }
            else if (area.EndsWith("Middle"))
            {
                resultPoint.X = (int)Math.Round(courtPictureBox.Width * 0.5);
            }
            else if (area.EndsWith("Right"))
            {
                resultPoint.X = (int)Math.Round(courtPictureBox.Width * 0.82758620689);
            }

            return resultPoint;
        }

        private void processShotButtonClick(Button button)
        {
                shotSelected = true;
                shotButtonSelected = button;
                button.BackColor = SystemColors.ActiveCaption;
                foreach (Button control in button.Parent.Controls)
                {
                    if (control != button)
                    {
                        control.BackColor = Color.Transparent;
                    }
                }
        }

        private void processWinnerButtonClick(Button button)
        {
                winnerSelected = true;
                winnerButtonSelected = button;
                button.BackColor = SystemColors.ActiveCaption;
                foreach (Button control in button.Parent.Controls)
                {
                    if (control != button)
                    {
                        control.BackColor = Color.Transparent;
                    }
                }
        }

        public Point GetPointFromObject(Object obj)
        {
            int X = int.Parse(Regex.Match(obj.ToString(), "(?<=X=)(.*)(?=,)").Groups[0].Value);
            int Y = int.Parse(Regex.Match(obj.ToString(), "(?<=Y=)(.*)(?=})").Groups[0].Value);
            return new Point(X, Y);
        }

        public void SelectShotFromChars(char type, char shot)
        {
            List<Button> fastList = new List<Button>() { flatClearButton, fastDropButton, fullSmashButton, farBlockButton, farNetButton, crossNetButton, pushButton, lowServeButton};
            List<Button> slowList = new List<Button>() { highClearButton, slowDropButton, halfSmashButton, closeBlockButton, closeNetButton, netSpinButton, liftButton, highServeButton};
            List<char> shotCharList = new List<char>() { 'q', 'a', 'w', 's', 'e', 'd', 'r', 'f' };

            try
            {
                int index = shotCharList.FindIndex(a => a == shot);
                if (type == 'c')
                {
                    processShotButtonClick(fastList[index]);
                }
                else
                {
                    processShotButtonClick(slowList[index]);
                }
            } catch { }
        }

        public void OpenFile()
        {

            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                DefaultExt = "txt",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Filter = "Text files (*.txt)|*.txt",
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                List<List<object>> newRallies = new List<List<object>>();
                string[] openFile = File.ReadAllLines(openFileDialog.FileName);

                foreach (var (item, index) in openFile.WithIndex())
                {
                    if (item.Contains("[rally]:"))
                    {
                        List<object> tempRally = new List<object>();
                        DataFrame tempDataFrame = new DataFrame();
                        StringDataFrameColumn tempStartingAreaColumn = new StringDataFrameColumn("Starting Area");
                        StringDataFrameColumn tempEndingAreaColumn = new StringDataFrameColumn("Ending Area");
                        PrimitiveDataFrameColumn<Point> tempStartingPointColumn = new PrimitiveDataFrameColumn<Point>("Starting Point");
                        PrimitiveDataFrameColumn<Point> tempEndingPointColumn = new PrimitiveDataFrameColumn<Point>("Ending Point");
                        StringDataFrameColumn tempShotTypeColumn = new StringDataFrameColumn("Shot Type");
                        StringDataFrameColumn tempSituationTypeColumn = new StringDataFrameColumn("Situation Type");
                        StringDataFrameColumn tempTurnColumn = new StringDataFrameColumn("Turn");
                        StringDataFrameColumn tempNotesColumn = new StringDataFrameColumn("Notes");

                        for (int i = index + 1; i < openFile.Length; i++)
                        {
                            if (openFile[i].Contains("[rally]:"))
                            {
                                break;
                            }
                            else if (openFile[i].Contains("[shot]:"))
                            {
                                string[] stringArray = openFile[i + 1].Split('|');

                                tempStartingAreaColumn.Append(stringArray[0]);
                                tempEndingAreaColumn.Append(stringArray[1]);
                                tempStartingPointColumn.Append(GetPointFromObject(stringArray[2]));
                                tempEndingPointColumn.Append(GetPointFromObject(stringArray[3]));
                                tempShotTypeColumn.Append(stringArray[4]);
                                tempSituationTypeColumn.Append(stringArray[5]);
                                tempTurnColumn.Append(stringArray[6]);
                                tempNotesColumn.Append(stringArray[7]);

                                tempDataFrame = new DataFrame(tempStartingAreaColumn,tempEndingAreaColumn,tempStartingPointColumn,tempEndingPointColumn,tempShotTypeColumn,tempSituationTypeColumn,tempTurnColumn, tempNotesColumn);
                            }
                        }
                        tempRally.Add(tempDataFrame);
                        tempRally.Add(openFile[index + 1]);
                        newRallies.Add(tempRally);
                    }
                }

                Debug.WriteLine(rallies == newRallies);

                rallies = newRallies;
                //Reset:
                if (frm2 != null)
                {
                    frm2.Close();
                }

                dataframeLabel.Text = "Dataframe:";

                string fileContents = "";

                DataFrame df = new DataFrame();
                foreach (List<Object> list in rallies)
                {
                    fileContents += "[rally]:\n";

                    df = list[0] as DataFrame;

                    fileContents += list[1].ToString() + "\n";

                    foreach (Object row in df.Rows)
                    {
                        fileContents += "[shot]:\n";

                        foreach (Object item in (row as DataFrameRow))
                        {
                            fileContents += item.ToString() + "|";
                        }

                        fileContents.Substring(0, fileContents.Length - 1);
                        fileContents += "\n";
                    }
                }

                Debug.WriteLine("Loaded file: " + fileContents);
            }
        }

        public void SaveGame()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog() {
                DefaultExt = "txt",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Filter = "Text files (*.txt)|*.txt",
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string fileContents = "";

                DataFrame df = new DataFrame();
                foreach (List<Object> list in rallies)
                {
                    fileContents += "[rally]:\n";

                    df = list[0] as DataFrame;

                    fileContents += list[1].ToString() + "\n";

                    foreach (Object row in df.Rows)
                    {
                        fileContents += "[shot]:\n";
                        
                        foreach (Object item in (row as DataFrameRow))
                        {
                            fileContents += item.ToString() + "|";
                        }

                        fileContents.Substring(0, fileContents.Length - 1);
                        fileContents += "\n";
                    }
                }

                File.WriteAllText(saveFileDialog.FileName, fileContents);
                Debug.WriteLine("Saved file: " + fileContents);
            }
        }

        public string GetAreaOnCourtOpponent(Point location, PictureBox courtPictureBox)
        {
            if (location.Y < courtPictureBox.Height * (1 - 0.88235294117))
            {
                if (location.X > courtPictureBox.Width * (1 - 0.3448275862))
                {
                    return "Back Left";
                }
                else if (location.X < courtPictureBox.Width * (1 - 0.65517241379))
                {
                    return "Back Right";
                }
                else
                {
                    return "Back Middle";
                }
            }
            else if (location.Y < courtPictureBox.Height * 0.5 && courtPictureBox.Height * (1 - 0.71493212669) < location.Y)
            {
                if (location.X > courtPictureBox.Width * (1 - 0.3448275862))
                {
                    return "Front Left";
                }
                else if (location.X < courtPictureBox.Width * (1 - 0.65517241379))
                {
                    return "Front Right";
                }
                else
                {
                    return "Front Middle";
                }
            }
            else
            {
                if (location.X > courtPictureBox.Width * (1 - 0.3448275862))
                {
                    return "Middle Left";
                }
                else if (location.X < courtPictureBox.Width * (1 - 0.65517241379))
                {
                    return "Middle Right";
                }
                else
                {
                    return "Middle Middle";
                }
            }
        }

        public Point GetPointFromAreaOpponent(string area, PictureBox courtPictureBox)
        {
            Point resultPoint = new Point();
            if (area.StartsWith("Front"))
            {
                resultPoint.Y = (int)Math.Round(courtPictureBox.Height * 0.35407239819);
            }
            else if (area.StartsWith("Middle"))
            {
                resultPoint.Y = (int)Math.Round(courtPictureBox.Height * 0.21040723981);
            }
            else if (area.StartsWith("Back"))
            {
                resultPoint.Y = (int)Math.Round(courtPictureBox.Height * 0.02828054298);
            }

            if (area.EndsWith("Left"))
            {
                resultPoint.X = (int)Math.Round(courtPictureBox.Width * 0.82758620689);
            }
            else if (area.EndsWith("Middle"))
            {
                resultPoint.X = (int)Math.Round(courtPictureBox.Width * 0.5);
            }
            else if (area.EndsWith("Right"))
            {
                resultPoint.X = (int)Math.Round(courtPictureBox.Width * 0.1724137931); 
            }

            return resultPoint;
        }

        private void processSituationButtonClick(Button button)
        {
            situationSelected = true;
            situationButtonSelected = button;
            button.BackColor = SystemColors.ActiveCaption;
            foreach (Button control in button.Parent.Controls)
            {
                if (control != button)
                {
                    control.BackColor = Color.Transparent;
                }
            }
        }

        private void processTurnButtonClick(Button button)
        {
                turnSelected = true;
                turnButtonSelected = button;
                button.BackColor = SystemColors.ActiveCaption;
                foreach (Button control in button.Parent.Controls)
                {
                    if (control != button)
                    {
                        control.BackColor = Color.Transparent;
                    }
                }
        }

        private void CourtMouseClick(object sender, MouseEventArgs e) {
            if (e.Location.Y < courtPanel.Height / 2)
            {
                endPictureBox.Location = LocationOffsetPoint(e.Location, new Point(-endPictureBox.Width/2, -endPictureBox.Height / 2));
            } else
            {
                startPictureBox.Location = LocationOffsetPoint(e.Location, new Point(-startPictureBox.Width / 2, -startPictureBox.Height / 2));
            }

            courtPictureBox.Invalidate();
        }

        private void submitButton_Click(object sender, EventArgs e)
        {
            if (shotSelected == false || situationSelected == false || turnSelected == false)
            {
                return;
            }

            if (turnButtonSelected.Text.Contains("Player"))
            {
                startingAreaColumn.Append(GetAreaOnCourtPlayer(LocationOffsetPoint(startPictureBox.Location, new Point(startPictureBox.Width / 2, startPictureBox.Height / 2)), courtPictureBox));
                endingAreaColumn.Append(GetAreaOnCourtOpponent(LocationOffsetPoint(endPictureBox.Location, new Point(endPictureBox.Width / 2, endPictureBox.Height / 2)), courtPictureBox));
            } else if (turnButtonSelected.Text.Contains("Opponent"))
            {
                endingAreaColumn.Append(GetAreaOnCourtPlayer(LocationOffsetPoint(startPictureBox.Location, new Point(startPictureBox.Width / 2, startPictureBox.Height / 2)), courtPictureBox));
                startingAreaColumn.Append(GetAreaOnCourtOpponent(LocationOffsetPoint(endPictureBox.Location, new Point(endPictureBox.Width / 2, endPictureBox.Height / 2)), courtPictureBox));
            }
            
            startingPointColumn.Append(LocationOffsetPoint(startPictureBox.Location, new Point(startPictureBox.Width / 2, startPictureBox.Height / 2)));
            endingPointColumn.Append(LocationOffsetPoint(endPictureBox.Location, new Point(endPictureBox.Width / 2, endPictureBox.Height / 2)));
            shotTypeColumn.Append(shotButtonSelected.Text);
            situationTypeColumn.Append(situationButtonSelected.Text);
            turnColumn.Append(turnButtonSelected.Text);
            notesColumn.Append("");

            df = new DataFrame(startingAreaColumn, endingAreaColumn, startingPointColumn, endingPointColumn, shotTypeColumn, situationTypeColumn, turnColumn, notesColumn);
            dataframeLabel.Text = "Dataframe:" + "\n" + df.ToString();

            if (turnButtonSelected == playerButton)
            {
                processTurnButtonClick(opponentButton);
            }
            else if (turnButtonSelected == opponentButton)
            {
                processTurnButtonClick(playerButton);
            }

            if (situationButtonSelected == attackingButton)
            {
                processSituationButtonClick(defendingButton);
            }
            else if (situationButtonSelected == defendingButton)
            {
                processSituationButtonClick(attackingButton);
            }

            fastInputTextBox.Text = "";
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            df = new DataFrame(startingAreaColumn, endingAreaColumn, startingPointColumn, endingPointColumn, shotTypeColumn, situationTypeColumn, turnColumn);
        }

        private void submitRallyButton_Click(object sender, EventArgs e)
        {
            if (winnerSelected == false)
            {
                return;
            }

            pointsArray.Add(df);
            pointsArray.Add(winnerButtonSelected.Text);

            rallies.Add(pointsArray);

            foreach (List<Object> rally in rallies)
            {
                foreach(Object dataframe in rally)
                {
                    Debug.WriteLine(dataframe);
                }
            }

            startingAreaColumn = new StringDataFrameColumn("Starting Area");
            endingAreaColumn = new StringDataFrameColumn("Ending Area");
            startingPointColumn = new PrimitiveDataFrameColumn<Point>("Starting Point");
            endingPointColumn = new PrimitiveDataFrameColumn<Point>("Ending Point");
            shotTypeColumn = new StringDataFrameColumn("Shot Type");
            situationTypeColumn = new StringDataFrameColumn("Situation Type");
            turnColumn = new StringDataFrameColumn("Turn");
            notesColumn = new StringDataFrameColumn("Notes");

            df = new DataFrame(startingAreaColumn, endingAreaColumn, startingPointColumn, endingPointColumn, shotTypeColumn, situationTypeColumn, turnColumn, notesColumn);

            pointsArray = new List<Object>();

            dataframeLabel.Text = "Dataframe: ";

            if (winnerButtonSelected == playerWinnerButton)
            {
                processTurnButtonClick(playerButton);
            } else if (winnerButtonSelected == opponentWinnerButton)
            {
                processTurnButtonClick(opponentButton);
            }
        }

        private void analysisWindowButton_Click(object sender, EventArgs e)
        {
            if (frm2 == null)
            {
                Debug.WriteLine("Form 2 is null, created a new one");
                frm2 = new Form2()
                {
                    rallies = rallies
                };
                frm2.FormClosed += new FormClosedEventHandler(frm2_FormClosed);

                frm2.Show(this);
            }
            else
            {
                Debug.WriteLine("Focused on Form 2");
                frm2.Focus();
                frm2.BringToFront();
            }
        }

        void frm2_FormClosed(object sender, EventArgs e)
        {
            Debug.WriteLine("Form 2 closed");
            frm2 = null;
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveGame();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var confirmResult = MessageBox.Show("Do you want to save the game?",
                                     "Save File",
                                     MessageBoxButtons.YesNoCancel);

            if (confirmResult == DialogResult.Yes)
            {
                SaveGame();

                for (int i = Application.OpenForms.Count - 1; i != -1; i = Application.OpenForms.Count - 1)
                {
                    Application.OpenForms[i].Close();
                }
            }
            else if (confirmResult == DialogResult.No)
            {
                for (int i = Application.OpenForms.Count - 1; i != -1; i = Application.OpenForms.Count - 1)
                {
                    Application.OpenForms[i].Close();
                }
            }
        }

        public void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var confirmResult = MessageBox.Show("Do you want to save the game?",
                                     "Save File",
                                     MessageBoxButtons.YesNoCancel);

            if (confirmResult == DialogResult.Yes)
            {
                SaveGame();

                Process process = null;
                try
                {
                    process = Process.GetCurrentProcess();
                    process.WaitForExit(1000);
                } catch
                {
                }
                for (int i = Application.OpenForms.Count - 1; i != -1; i = Application.OpenForms.Count - 1)
                {
                    Application.OpenForms[i].Close();
                }
                Process.Start("BadmintonApp");
            }
            else if (confirmResult == DialogResult.No)
            {
                Process process = null;
                try
                {
                    process = Process.GetCurrentProcess();
                    process.WaitForExit(1000);
                }
                catch
                {
                }
                for (int i = Application.OpenForms.Count - 1; i != -1; i = Application.OpenForms.Count - 1)
                {
                    Application.OpenForms[i].Close();
                }
                Process.Start("BadmintonApp");
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFile();
        }

        private void flatClearButton_Click(object sender, EventArgs e)
        {
            processShotButtonClick(flatClearButton);
        }

        private void highClearButton_Click(object sender, EventArgs e)
        {
            processShotButtonClick(highClearButton);
        }

        private void fastDropButton_Click(object sender, EventArgs e)
        {
            processShotButtonClick(fastDropButton);
        }

        private void slowDropButton_Click(object sender, EventArgs e)
        {
            processShotButtonClick(slowDropButton);
        }

        private void halfSmashButton_Click(object sender, EventArgs e)
        {
            processShotButtonClick(halfSmashButton);
        }

        private void fullSmashButton_Click(object sender, EventArgs e)
        {
            processShotButtonClick(fullSmashButton);
        }

        private void closeNetButton_Click(object sender, EventArgs e)
        {
            processShotButtonClick(closeNetButton);
        }

        private void farNetButton_Click(object sender, EventArgs e)
        {
            processShotButtonClick(farNetButton);
        }

        private void netSpinButton_Click(object sender, EventArgs e)
        {
            processShotButtonClick(netSpinButton);
        }

        private void crossNetButton_Click(object sender, EventArgs e)
        {
            processShotButtonClick(crossNetButton);
        }

        private void liftButton_Click(object sender, EventArgs e)
        {
            processShotButtonClick(liftButton);
        }

        private void pushButton_Click(object sender, EventArgs e)
        {
            processShotButtonClick(pushButton);
        }

        private void closeBlockButton_Click(object sender, EventArgs e)
        {
            processShotButtonClick(closeBlockButton);
        }

        private void farBlockButton_Click(object sender, EventArgs e)
        {
            processShotButtonClick(farBlockButton);
        }

        private void lowServeButton_Click(object sender, EventArgs e)
        {
            processShotButtonClick(lowServeButton);
        }

        private void highServeButton_Click(object sender, EventArgs e)
        {
            processShotButtonClick(highServeButton);
        }

        private void neutralButton_Click(object sender, EventArgs e)
        {
            processSituationButtonClick(neutralButton);
        }

        private void attackingButton_Click(object sender, EventArgs e)
        {
            processSituationButtonClick(attackingButton);
        }

        private void defendingButton_Click(object sender, EventArgs e)
        {
            processSituationButtonClick(defendingButton);
        }

        private void playerButton_Click(object sender, EventArgs e)
        {
            processTurnButtonClick(playerButton);
        }

        private void opponentButton_Click(object sender, EventArgs e)
        {
            processTurnButtonClick(opponentButton);
        }

        private void playerWinnerButton_Click(object sender, EventArgs e)
        {
            processWinnerButtonClick(playerWinnerButton);
        }

        private void opponentWinnerButton_Click(object sender, EventArgs e)
        {
            processWinnerButtonClick(opponentWinnerButton);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogForm dialogForm = new DialogForm();
            dialogForm.Show();
        }

        private void courtPictureBox_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (var pen = new Pen(Color.DarkSlateGray, 5))
            {
                g.DrawLine(pen, LocationOffsetPoint(startPictureBox.Location, new Point(startPictureBox.Width / 2, startPictureBox.Height / 2)), LocationOffsetPoint(endPictureBox.Location, new Point(endPictureBox.Width / 2, endPictureBox.Height / 2)));
            }
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            HelpForm helpForm = new HelpForm();
            helpForm.Show();
        }

        private void fastInputTextBox_TextChanged(object sender, EventArgs e)
        {
            if (fastInputTextBox.Text.Length > 1)
            {
                int i = 1;
                char type;
                char shot;
                char[] shotChars = { 'q', 'w', 'e', 'r', 'a', 's', 'd', 'f' };

                try
                {
                    while (fastInputTextBox.Text[^i] != 'c' && fastInputTextBox.Text[^i] != 'v')
                    {
                        i++;
                    }
                    type = fastInputTextBox.Text[^i];

                    i = 1;

                    while (!shotChars.Contains(fastInputTextBox.Text[^i]))
                    {
                        i++;
                    }
                    shot = fastInputTextBox.Text[^i];

                    SelectShotFromChars(type, shot);
                } catch { }
            }
        }

        private void fastInputTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Space)
            {
                submitButton.PerformClick();
            }

            Point point = new Point();
            bool isPlayer = true;

            if (turnButtonSelected == playerButton)
            {
                isPlayer = true;
                if (e.KeyChar == '1')
                {
                    point = GetPointFromAreaPlayer("Back Left", courtPictureBox);
                }
                else if (e.KeyChar == '2')
                {
                    point = GetPointFromAreaPlayer("Back Middle", courtPictureBox);
                }
                else if (e.KeyChar == '3')
                {
                    point = GetPointFromAreaPlayer("Back Right", courtPictureBox);
                }
                else if (e.KeyChar == '4')
                {
                    point = GetPointFromAreaPlayer("Middle Left", courtPictureBox);
                }
                else if (e.KeyChar == '5')
                {
                    point = GetPointFromAreaPlayer("Middle Middle", courtPictureBox);
                }
                else if (e.KeyChar == '6')
                {
                    point = GetPointFromAreaPlayer("Middle Right", courtPictureBox);
                }
                else if (e.KeyChar == '7')
                {
                    point = GetPointFromAreaPlayer("Front Left", courtPictureBox);
                }
                else if (e.KeyChar == '8')
                {
                    point = GetPointFromAreaPlayer("Front Middle", courtPictureBox);
                }
                else if (e.KeyChar == '9')
                {
                    point = GetPointFromAreaPlayer("Front Right", courtPictureBox);
                }
            } else if (turnButtonSelected == opponentButton)
            {
                isPlayer = false;
                if (e.KeyChar == '1')
                {
                    point = GetPointFromAreaOpponent("Front Right", courtPictureBox);
                }
                else if (e.KeyChar == '2')
                {
                    point = GetPointFromAreaOpponent("Front Middle", courtPictureBox);
                }
                else if (e.KeyChar == '3')
                {
                    point = GetPointFromAreaOpponent("Front Left", courtPictureBox);
                }
                else if (e.KeyChar == '4')
                {
                    point = GetPointFromAreaOpponent("Middle Right", courtPictureBox);
                }
                else if (e.KeyChar == '5')
                {
                    point = GetPointFromAreaOpponent("Middle Middle", courtPictureBox);
                }
                else if (e.KeyChar == '6')
                {
                    point = GetPointFromAreaOpponent("Middle Left", courtPictureBox);
                }
                else if (e.KeyChar == '7')
                {
                    point = GetPointFromAreaOpponent("Back Right", courtPictureBox);
                }
                else if (e.KeyChar == '8')
                {
                    point = GetPointFromAreaOpponent("Back Middle", courtPictureBox);
                }
                else if (e.KeyChar == '9')
                {
                    point = GetPointFromAreaOpponent("Back Left", courtPictureBox);
                }
            }

            if (point != new Point(0, 0))
            {
                if (isPlayer == true)
                {
                    startPictureBox.Location = LocationOffsetPoint(point, new Point(-startPictureBox.Width / 2, -startPictureBox.Height / 2));
                }
                else
                {
                    endPictureBox.Location = LocationOffsetPoint(point, new Point(-endPictureBox.Width / 2, -endPictureBox.Height / 2));
                }

                courtPictureBox.Invalidate();
            }
        }

        private void pictureBox_SizeChanged(object sender, EventArgs e)
        {
            PictureBox box = sender as PictureBox;
            box.Width = 20;
            box.Height = 20;
        }

        private void pictureBox_LoadCompleted(object sender, AsyncCompletedEventArgs e)
        {
            PictureBox box = sender as PictureBox;
            box.Width = 20;
            box.Height = 20;
        }
    }
}
