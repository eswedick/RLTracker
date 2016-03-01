using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;

namespace RL_Stats_App
{
    public partial class Main : Form
    {
        string[] files;
        Bitmap bmp;
        TesseractEngine ocr;
        string tessdata;
        DataTable results;

        const int letterHeight = 49;

        const int length = 75;
        const int nameLength = 300;
        const int scoreLength = 100;

        // x where columns begin
        const int nameX = 1090;
        const int scoreX = 1445;
        const int goalX = 1550;
        const int assistX = 1640;
        const int saveX = 1750;
        const int shotX = 1840;

        // y for where rows start
        const int p1_1 = 451;
        const int p2_1 = 610;

        const int p1_2 = 420;
        const int p2_2 = 480;
        const int p3_2 = 605;
        const int p4_2 = 665;

        const int p1_3 = 1;
        const int p2_3 = 1;
        const int p3_3 = 1;
        const int p4_3 = 1;
        const int p5_3 = 1;
        const int p6_3 = 1;

        #region "Dictionary"
        private Dictionary<string, Rect> zones = new Dictionary<string, Rect>(){
            //solo
            {"1_1_Name", new Rect(nameX, p1_1, nameLength, letterHeight)},
            {"2_1_Name", new Rect(nameX, p2_1, nameLength, letterHeight)},
            {"1_1_Score", new Rect(scoreX, p1_1, scoreLength, letterHeight)},
            {"2_1_Score", new Rect(scoreX, p2_1, scoreLength, letterHeight)},
            {"1_1_Goals", new Rect(goalX, p1_1, length, letterHeight)},
            {"2_1_Goals", new Rect(goalX, p2_1, length, letterHeight)},
            {"1_1_Assists", new Rect(assistX, p1_1, length, letterHeight)},
            {"2_1_Assists", new Rect(assistX, p2_1, length, letterHeight)},
            {"1_1_Saves", new Rect(saveX, p1_1, length, letterHeight)},
            {"2_1_Saves", new Rect(saveX, p2_1, length, letterHeight)},
            {"1_1_Shots", new Rect(shotX, p1_1, length, letterHeight)},
            {"2_1_Shots", new Rect(shotX, p2_1, length, letterHeight)},
        
            //doubles
            {"1_2_Name", new Rect(nameX, p1_2, nameLength, letterHeight)},
            {"2_2_Name", new Rect(nameX, p2_2, nameLength, letterHeight)},
            {"3_2_Name", new Rect(nameX, p3_2, nameLength, letterHeight)},
            {"4_2_Name", new Rect(nameX, p4_2, nameLength, letterHeight)},
            {"1_2_Score", new Rect(scoreX, p1_2, scoreLength, letterHeight)},
            {"2_2_Score", new Rect(scoreX, p2_2, scoreLength, letterHeight)},
            {"3_2_Score", new Rect(scoreX, p3_2, scoreLength, letterHeight)},
            {"4_2_Score", new Rect(scoreX, p4_2, scoreLength, letterHeight)},
            {"1_2_Goals", new Rect(goalX, p1_2, length, letterHeight)},
            {"2_2_Goals", new Rect(goalX, p2_2, length, letterHeight)},
            {"3_2_Goals", new Rect(goalX, p3_2, length, letterHeight)},
            {"4_2_Goals", new Rect(goalX, p4_2, length, letterHeight)},
            {"1_2_Assists", new Rect(assistX, p1_2, length, letterHeight)},
            {"2_2_Assists", new Rect(assistX, p2_2, length, letterHeight)},
            {"3_2_Assists", new Rect(assistX, p3_2, length, letterHeight)},
            {"4_2_Assists", new Rect(assistX, p4_2, length, letterHeight)},
            {"1_2_Saves", new Rect(saveX, p1_2, length, letterHeight)},
            {"2_2_Saves", new Rect(saveX, p2_2, length, letterHeight)},
            {"3_2_Saves", new Rect(saveX, p3_2, length, letterHeight)},
            {"4_2_Saves", new Rect(saveX, p4_2, length, letterHeight)},
            {"1_2_Shots", new Rect(shotX, p1_2, length, letterHeight)},
            {"2_2_Shots", new Rect(shotX, p2_2, length, letterHeight)},
            {"3_2_Shots", new Rect(shotX, p3_2, length, letterHeight)},
            {"4_2_Shots", new Rect(shotX, p4_2, length, letterHeight)}

            //standard
        };
        #endregion

        public Main()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            results = new DataTable();
            results.Columns.Add("player");
            results.Columns.Add("rank");
            results.Columns.Add("score");
            results.Columns.Add("goals");
            results.Columns.Add("assists");
            results.Columns.Add("saves");
            results.Columns.Add("shots");
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            files = (string[])e.Data.GetData(DataFormats.FileDrop);
            run();
        }

        private void run()
        {
            results.Rows.Clear();

            //get screenshot file path
            textBox1.Text = files[0];

            //image preprocessing
            var outImage = processFile(files);

            //get bitmap for ocr
            bmp = new Bitmap(outImage);

            //create ocr
            tessdata = Application.StartupPath + "\\tessdata";
            ocr = new TesseractEngine(tessdata, "eng", EngineMode.Default);

            //call appropriate function to parse scores
            if (radioButton1.Checked)
            {
                recognizeSolo(outImage);
            }
            else if (radioButton2.Checked)
            {
                recognizeDoubles(outImage);

            }
            else
            {
                recognizeStandard(outImage);
            }
        }

        private string processFile(string[] files)
        {
            string image = files[0].Replace(".jpg", "_out.jpg");
            var outImage = image;

            //delete file if it exists
            try
            {
                FileSystemInfo fsi = new FileInfo(image);
                if (fsi.Exists)
                {
                    fsi.Delete();
                }

                var colorImage = AForge.Imaging.Image.FromFile(files[0]);
                var grayscaleImage = AForge.Imaging.Filters.Grayscale.CommonAlgorithms.BT709.Apply(colorImage);
                var invertedImage = new AForge.Imaging.Filters.Invert().Apply(grayscaleImage);
                var threshImage = new AForge.Imaging.Filters.Threshold(145).Apply(invertedImage);

                threshImage.Save(outImage);
            }
            catch (Exception ex)
            {

            }
            return outImage;
        }

        private void recognizeSolo(string outImage)
        {
            //name
            var player1Name = ocrProcess("1_1_Name");
            var player2Name = ocrProcess("2_1_Name");

            //rank
            var player1Rank = ocrProcess("1_1_Name", false);
            var player2Rank = ocrProcess("2_1_Name", false);

            // start looking for just numbers //
            ocr.SetVariable("tessedit_char_whitelist", "0123456789");

            //score
            var player1Score = ocrProcess("1_1_Score");
            var player2Score = ocrProcess("2_1_Score");

            // single character only //
            ocr.DefaultPageSegMode = PageSegMode.SingleChar;

            //goals
            var player1Goals = ocrProcess("1_1_Goals");
            var player2Goals = ocrProcess("2_1_Goals");

            //assists
            var player1assists = ocrProcess("1_1_Assists");
            var player2assists = ocrProcess("2_1_Assists");

            //saves
            var player1saves = ocrProcess("1_1_Saves");
            var player2saves = ocrProcess("2_1_Saves");

            //shots
            var player1shots = ocrProcess("1_1_Shots");
            var player2shots = ocrProcess("2_1_Shots");

            //add items to grid
            results.Rows.Add(new object[] { player1Name, player1Rank, player1Score, player1Goals, player1assists, player1saves, player1shots });
            results.Rows.Add(new object[] { player2Name, player2Rank, player2Score, player2Goals, player2assists, player2saves, player2shots });

            //bind data
            dgStats.DataSource = results;

        }

        private void recognizeDoubles(string outImage)
        {
            //name
            var player1Name = ocrProcess("1_2_Name");
            var player2Name = ocrProcess("2_2_Name");
            var player3Name = ocrProcess("3_2_Name");
            var player4Name = ocrProcess("4_2_Name");

            //rank
            var player1Rank = ocrProcess("1_2_Name", false);
            var player2Rank = ocrProcess("2_2_Name", false);
            var player3Rank = ocrProcess("3_2_Name", false);
            var player4Rank = ocrProcess("4_2_Name", false);

            // start looking for just numbers //
            ocr.SetVariable("tessedit_char_whitelist", "0123456789");

            //score
            var player1Score = ocrProcess("1_2_Score");
            var player2Score = ocrProcess("2_2_Score");
            var player3Score = ocrProcess("3_2_Score");
            var player4Score = ocrProcess("4_2_Score");

            // single character only //
            ocr.DefaultPageSegMode = PageSegMode.SingleChar;

            //goals
            var player1Goals = ocrProcess("1_2_Goals");
            var player2Goals = ocrProcess("2_2_Goals");      
            var player3Goals = ocrProcess("3_2_Goals");         
            var player4Goals =  ocrProcess("4_2_Goals");

            //assists
            var player1assists = ocrProcess("1_2_Assists");
            var player2assists = ocrProcess("2_2_Assists");
            var player3assists = ocrProcess("3_2_Assists");         
            var player4assists = ocrProcess("4_2_Assists");     

            //saves
            var player1saves = ocrProcess("1_2_Saves");
            var player2saves = ocrProcess("2_2_Saves");
            var player3saves = ocrProcess("3_2_Saves");      
            var player4saves = ocrProcess("4_2_Saves");        

            //shots
            var player1shots = ocrProcess("1_2_Shots");
            var player2shots = ocrProcess("2_2_Shots");
            var player3shots = ocrProcess("3_2_Shots");
            var player4shots = ocrProcess("4_2_Shots");

            //add items to grid
            results.Rows.Add(new object[] { player1Name, player1Rank, player1Score, player1Goals, player1assists, player1saves, player1shots });
            results.Rows.Add(new object[] { player2Name, player2Rank, player2Score, player2Goals, player2assists, player2saves, player2shots });
            results.Rows.Add(new object[] { player3Name, player3Rank, player3Score, player3Goals, player3assists, player3saves, player3shots });
            results.Rows.Add(new object[] { player4Name, player4Rank, player4Score, player4Goals, player4assists, player4saves, player4shots });

            //bind data
            dgStats.DataSource = results;
        }

        private void recognizeStandard(string outImage)
        {
            Bitmap bmp = new Bitmap(outImage);
            var tessdata = Application.StartupPath + "\\tessdata";
            var ocr = new TesseractEngine(tessdata, "eng", EngineMode.Default);

            //var process = ocr.Process(bmp, player1NameRect);
            //var player1Info = process.GetText().Trim().Split('\n');
            //process.Dispose();

            //process = ocr.Process(bmp, player2NameRect);
            //var player2Info = process.GetText().Trim().Split('\n');
            //process.Dispose();

            //player1Name = player1Info[0];
            //player2Name = player2Info[0];
            //if (player1Info.Length == 1)
            //    player1Rank = "";
            //else
            //    player1Rank = player1Info[1];
            //if (player2Info.Length == 1)
            //    player2Rank = "";
            //else
            //    player2Rank = player2Info[1];

            //ocr.SetVariable("tessedit_char_whitelist", "0123456789");

            //process = ocr.Process(bmp, player1ScoreRect);
            //var player1Score = process.GetText().Trim();
            //process.Dispose();

            //process = ocr.Process(bmp, player2ScoreRect);
            //var player2Score = process.GetText().Trim();
            //process.Dispose();

            //process = ocr.Process(bmp, player1GoalsRect);
            //var player1Goals = process.GetText().Trim();
            //process.Dispose();

            //process = ocr.Process(bmp, player2GoalsRect);
            //var player2Goals = process.GetText().Trim();
            //process.Dispose();

            //dgStats.Items.Add(new ListViewItem(new string[]
            //{
            //    player1Name,
            //    player1Rank,
            //    player1Score,
            //    player1Goals
            //}));
            //dgStats.Items.Add(new ListViewItem(new string[]
            //{
            //    player2Name,
            //    player2Rank,
            //    player2Score,
            //    player2Goals
            //}));
            Console.WriteLine("Jones");
        }

        private string ocrProcess(string zone, Boolean returnFirst = true)
        {
            var data = "";
            var process = ocr.Process(bmp, zones[zone]);
            if (zone.Contains("Name"))
            {
                data = SplitNameRank(process.GetText().Trim().Split('\n'), returnFirst);
            }
            else
            {
                data = process.GetText().Trim();
            }
            process.Dispose();

            return data;

        }

        private string SplitNameRank(string[] data, Boolean ReturnName)
        {
            if (ReturnName)
            {
                return data[0];
            }
            else
            {
                if (data.Count<string>() > 1)
                {
                    return data[1];
                }
                else
                {
                    return "";
                }
            }

        }

    }
}
