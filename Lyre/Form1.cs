//42
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Newtonsoft.Json;
using System.IO;
using System.Threading;

namespace Lyre
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public Wer preferences; // preferences object
        private string filePreferences = "preferences.json";
        private Panel ccContainer;
        private Panel ccTopBar;
        private Panel ccDownloadsContainer;
        private DownloadContainer dcMain;
        private Panel ccFormMinimize;
        private Panel ccFormMaximize;
        private Panel ccFormClose;
        private Panel ccDownloadsDirectory;
        private Label ccHint;

        private void Form1_Load(object sender, EventArgs e)
        {
            loadPreferences();
            InitComponents();
            ResizeComponents();
        }

        private void InitComponents()
        {
            this.Text = "Lyre - A music app by Robert Barachini";
            this.FormClosing += Form1_FormClosing;
            this.DoubleBuffered = true;
            this.Width = Wer.formWidth;
            this.Height = Wer.formHeight;
            this.Top = Wer.formTop;
            this.Height = Wer.formHeight;
            this.SizeChanged += Form1_SizeChanged;
            this.KeyDown += Paste_KeyDown;
            this.KeyUp += Paste_KeyUp;
            this.BackColor = Wer.colorForeground;
            //this.FormBorderStyle = FormBorderStyle.None;
            this.MouseMove += Form1_MouseMove;

            ccContainer = new Panel();
            ccContainer.Parent = this;
            this.Controls.Add(ccContainer);
            ccContainer.BackColor = Wer.colorForeground;
            ccContainer.Dock = DockStyle.Fill;

            ccTopBar = new Panel();
            ccTopBar.Parent = ccContainer;
            ccContainer.Controls.Add(ccTopBar);
            ccTopBar.BackColor = Wer.colorBackground;

            ccDownloadsContainer = new Panel();
            ccDownloadsContainer.Parent = ccContainer;
            ccContainer.Controls.Add(ccDownloadsContainer);
            ccDownloadsContainer.BackColor = Wer.colorForeground;
            ccDownloadsContainer.AutoScroll = true;

            ccFormMinimize = new Panel();
            ccFormMinimize.Parent = ccTopBar;
            ccTopBar.Controls.Add(ccFormMinimize);
            ccFormMinimize.Cursor = Cursors.Hand;
            ccFormMinimize.BackgroundImageLayout = ImageLayout.Zoom;
            ccFormMinimize.BackgroundImage = getImage(Path.Combine(Wer.resourcesDirectory, Wer.FormButtons_Minimize));
            ccFormMinimize.BackColor = ccTopBar.BackColor;
            ccFormMinimize.Click += CcFormMinimize_Click;

            ccFormMaximize = new Panel();
            ccFormMaximize.Parent = ccTopBar;
            ccTopBar.Controls.Add(ccFormMaximize);
            ccFormMaximize.Cursor = Cursors.Hand;
            ccFormMaximize.BackgroundImageLayout = ImageLayout.Zoom;
            ccFormMaximize.BackgroundImage = getImage(Path.Combine(Wer.resourcesDirectory, Wer.FormButtons_Maximize));
            ccFormMaximize.BackColor = ccTopBar.BackColor;
            ccFormMaximize.Click += CcFormMaximize_Click;

            ccFormClose = new Panel();
            ccFormClose.Parent = ccTopBar;
            ccTopBar.Controls.Add(ccFormClose);
            ccFormClose.Cursor = Cursors.Hand;
            ccFormClose.BackgroundImageLayout = ImageLayout.Zoom;
            ccFormClose.BackgroundImage = getImage(Path.Combine(Wer.resourcesDirectory, Wer.FormButtons_CloseBig));
            ccFormClose.BackColor = ccTopBar.BackColor;
            ccFormClose.Click += CcFormClose_Click;

            ccDownloadsDirectory = new Panel();
            ccDownloadsDirectory.Parent = ccTopBar;
            ccTopBar.Controls.Add(ccDownloadsDirectory);
            ccDownloadsDirectory.Cursor = Cursors.Hand;
            ccDownloadsDirectory.BackgroundImageLayout = ImageLayout.Zoom;
            ccDownloadsDirectory.BackgroundImage = getImage(Path.Combine(Wer.resourcesDirectory, Wer.IMG_Directory));
            ccDownloadsDirectory.BackColor = ccTopBar.BackColor;
            ccDownloadsDirectory.Click += CcDownloadsDirectory_Click;

            ccHint = new Label();
            ccHint.Parent = ccTopBar;
            ccTopBar.Controls.Add(ccHint);
            ccHint.Font = new Font(Wer.fontDefault.FontFamily, 20, GraphicsUnit.Pixel);
            ccHint.Text = "ALPHA preview : Paste Youtube links anywhere, really ...";
            ccHint.ForeColor = Color.White;
            ccHint.BackColor = Wer.colorBackground;
        }

        private void CcDownloadsDirectory_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Choose future downloads destination directory.";
                if(Wer.downloadsDirectory.Equals("downloads"))
                {
                    folderDialog.SelectedPath = Path.Combine(Directory.GetCurrentDirectory(), Wer.downloadsDirectory);
                }
                else
                {
                    folderDialog.SelectedPath = Wer.downloadsDirectory;
                }
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    Wer.downloadsDirectory = folderDialog.SelectedPath;
                }
            }
        }

        private void ResizeComponents()
        {
            this.SuspendLayout();

            //Wer.formWidth = this.Width;
            //Wer.formHeight = this.Height;

            //ccContainer.Top = 0;
            //ccContainer.Left = 0;
            //ccContainer.Width = this.Width;
            //ccContainer.Height = this.Height;

            ccTopBar.Top = 0;
            ccTopBar.Left = 0;
            ccTopBar.Width = ccContainer.Width;
            ccTopBar.Height = 50;

            ccDownloadsContainer.Top = ccTopBar.Top + ccTopBar.Height;
            ccDownloadsContainer.Left = 0;
            ccDownloadsContainer.Width = ccContainer.Width;
            ccDownloadsContainer.Height = ccContainer.Height - ccTopBar.Height;

            int barMargin = 10;
            ccFormClose.Top = barMargin;
            ccFormClose.Height = ccTopBar.Height - (ccFormClose.Top * 2);
            ccFormClose.Width = ccFormClose.Height;
            ccFormClose.Left = ccTopBar.Width - ccFormClose.Width - barMargin;

            ccFormMaximize.Top = ccFormClose.Top;
            ccFormMaximize.Height = ccFormClose.Height;
            ccFormMaximize.Width = ccFormClose.Width;
            ccFormMaximize.Left = ccFormClose.Left - ccFormMaximize.Width - barMargin;

            ccFormMinimize.Top = ccFormClose.Top;
            ccFormMinimize.Height = ccFormClose.Height;
            ccFormMinimize.Width = ccFormClose.Width;
            ccFormMinimize.Left = ccFormMaximize.Left - ccFormMinimize.Width - barMargin;

            ccDownloadsDirectory.Top = barMargin;
            ccDownloadsDirectory.Left = barMargin;
            ccDownloadsDirectory.Height = ccFormClose.Height;
            ccDownloadsDirectory.Width = ccFormClose.Width;

            ccHint.Top = barMargin;
            ccHint.Left = ccDownloadsDirectory.Left + ccDownloadsDirectory.Width + barMargin;
            ccHint.Width = 500;
            ccHint.Height = ccFormClose.Height;


            if (DownloadContainer.downloads.Count > 0)
            {
                resizeDcMain();
            }

            this.ResumeLayout();
        }

        private void resizeDcMain()
        {
            dcMain = DownloadContainer.downloads.First.Value;
            dcMain.Top = 30;
            dcMain.Left = 50;
            dcMain.Width = ccDownloadsContainer.Width - (dcMain.Left * 2);
            dcMain.Height = 100;
        }

        private void Form1_MouseMove(object sender, MouseEventArgs e)
        {

        }

        private void CcFormClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void CcFormMaximize_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            else
            {
                this.WindowState = FormWindowState.Maximized;
            }
        }

        private void CcFormMinimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        public static Image getImage(string path)
        {
            int counter = 0;
            while (System.IO.File.Exists(path) == false)
            {
                Application.DoEvents();
                Thread.Sleep(50);
                counter += 50;
                if (counter > 1000)
                {
                    break;
                }
            }
            try
            {
                Image img = Image.FromStream(new MemoryStream(System.IO.File.ReadAllBytes(path)));
                return img;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            savePreferences();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            try
            {
                ResizeComponents();
            }
            catch (Exception ex)
            {

            }
        }

        private bool copyPasteDown = false;
        private void Paste_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Control && e.KeyCode == Keys.V && copyPasteDown == false)
            {
                // queue download url-s
                string clipboardString = Clipboard.GetText();
                string[] links = clipboardString.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                foreach(string l in links)
                {
                    Application.DoEvents();
                    newDownload(l);
                }
                copyPasteDown = true;
            }
            else if(e.KeyCode == Keys.F11)
            {
                if(this.WindowState == FormWindowState.Maximized)
                {
                    this.WindowState = FormWindowState.Normal;
                    this.FormBorderStyle = FormBorderStyle.Sizable;
                }
                else
                {
                    this.FormBorderStyle = FormBorderStyle.None;
                    this.WindowState = FormWindowState.Maximized;
                }
            }
        }

        private void newDownload(string url)
        {
            DownloadContainer newDc = new DownloadContainer();
            if (DownloadContainer.downloads.Count == 1)
            {
                dcMain = newDc;
                dcMain.Parent = ccDownloadsContainer;
                resizeDcMain();
            }
            newDc.download(url, Wer.downloadsDirectory, true);
        }

        private void Paste_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.V)
            {
                copyPasteDown = false;
            }
        }

        private void loadPreferences()
        {
            if (File.Exists(filePreferences))
            {
                try
                {
                    //JsonConvert.PopulateObject("", wer);
                    string fileString = File.ReadAllText(filePreferences);
                    preferences = JsonConvert.DeserializeObject<Wer>(fileString);
                }
                catch(Exception ex)
                {
                    preferences = new Wer();
                    savePreferences();
                }
            }
            else
            {
                preferences = new Wer();
                savePreferences();
            }
        }

        public void savePreferences()
        {
            string jsonString = JsonConvert.SerializeObject(preferences, Formatting.Indented);
            File.WriteAllText("preferences.json", jsonString);
        }
    }
}
