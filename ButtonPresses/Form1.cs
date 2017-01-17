using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing.Imaging;
using Tesseract;
using System.Text.RegularExpressions;
using System.IO;
using System.Net;
using System.Threading;

namespace ButtonPresses
{
    public partial class Form1 : Form
    {
        string parsedString = "";
        public static class Constants
        {
            //windows message id for hotkey
            public const int WM_HOTKEY_MSG_ID = 0x0312;
        }

        public class Variables
        {
            public string parsedString;
        }

        /*
         * Getting the pressed button
         */

        public class KeyHandler
        {
            [DllImport("user32.dll")]
            private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

            [DllImport("user32.dll")]
            private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

            private int key;
            private IntPtr hWnd;
            private int id;

            public KeyHandler(Keys key, Form form)
            {
                this.key = (int)key;
                this.hWnd = form.Handle;
                id = this.GetHashCode();
            }

            public override int GetHashCode()
            {
                return key ^ hWnd.ToInt32();
            }

            public bool Register()
            {
                return RegisterHotKey(hWnd, id, 0, key);
                
            }

            public bool Unregiser()
            {
                return UnregisterHotKey(hWnd, id);
            }
        }

        /*
         * Handling the buttonpress
         */

        private void HandleHotkey()
        {
            if(!backgroundWorker2.IsBusy)
            {
                progressBar1.Visible = true;
                listView1.Items.Clear();
                if (CaptureApplication("Warframe.x64"))
                {
                    progressBar1.Value = 20;
                    if (!backgroundWorker1.IsBusy)
                    {
                        progressBar1.Value = 50;
                        backgroundWorker1.RunWorkerAsync();
                        progressBar1.Value = 90;
                    }
                    else
                    {
                        //backgroundWorker1.CancelAsync();
                    }
                }
            }
            else
            {
                MessageBox.Show("Program is still checking versions!");
            }
        }

        /*
         * User 32 class
         */

        private class User32
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct Rect
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }

            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);
        }

        /*
         * Capturing the application
         */

        private bool CaptureApplication(string procName)
        {
            if(Process.GetProcessesByName("Warframe.x64") != null)
            {
                progressBar1.Visible = true;
                int width = Convert.ToInt16(Screen.PrimaryScreen.Bounds.Width);
                int height = Convert.ToInt16(Screen.PrimaryScreen.Bounds.Height);


                var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                Graphics graphics = Graphics.FromImage(bmp);
                graphics.CopyFromScreen(0, 0, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);


                string str = string.Format(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Screenshot.tif");
                try
                {
                    bmp.Save(str, System.Drawing.Imaging.ImageFormat.Tiff);
                }
                catch
                {

                }
                return true;
            }
            else
            {
                MessageBox.Show("Warframe is not running!");
                return false;
            }
        }

        /*
         * Dunno what is this
         */

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Constants.WM_HOTKEY_MSG_ID)
                HandleHotkey();
            base.WndProc(ref m);
        }

        private string[] downloadLinebyLine(string url)
        {
            string[] download;

            var client = new WebClient();
            using (var stream = client.OpenRead(url))
            using (var reader = new StreamReader(stream))
            {
                string line;
                int i = 0;
                while((line = reader.ReadLine()) != null)
                {
                    download[i] = line;
                }
            }

                return download;
        }

        /*
         * Getting fresh price database
         */

        private void updateDatabase()
        {
            if (progressBar2.InvokeRequired)
            {
                progressBar2.Invoke(new MethodInvoker(delegate { progressBar2.Visible = true; }));
            }
            string filename = "wf_drops.txt";
            WebClient client = new WebClient();
            string database = "";
            try
            {
                database = client.DownloadString(new Uri("http://188.240.208.209/" + filename));
                if (progressBar2.InvokeRequired)
                {
                    progressBar2.Invoke(new MethodInvoker(delegate { progressBar2.Value = 50; }));
                }
            }
            catch (WebException e)
            {
                MessageBox.Show(e.Message + "\r\nUsing old database, so prices won't be correct!");
            }
            MessageBox.Show(database);
            if (File.Exists(filename))
            {
                TextWriter tw = new StreamWriter(filename);
                tw.Write(database);
                tw.Close();
                File.WriteAllText(filename, database);
            }
            else
            {
                File.Create(filename);
                TextWriter tw = new StreamWriter(filename);
                tw.Write(database);
            }
            if (progressBar2.InvokeRequired)
            {
                progressBar2.Invoke(new MethodInvoker(delegate { progressBar2.Visible = false; }));
            }
        }

        /*
         * Getting the drops
         */

        private void matchAgainsDrops(string text)
        {
            string[] drops = File.ReadAllLines("wf_drops.txt");
            foreach (var item in drops)
            {
                var isIt = Regex.Match(text, item, RegexOptions.IgnoreCase);
                if (isIt.Success)
                {
                    listView1.Items.Add(new ListViewItem(new string[] { item, "Dukat" }));
                }
            }
        }

        /*
         * Extracting the string from the picture
         */

        private string getStringFromPic()
        {
            string pictureString = "";
            var pict = string.Format(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\Screenshot.tif");
            try
            {
                using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
                {
                    using (var img = Pix.LoadFromFile(pict))
                    {
                        using (var page = engine.Process(img))
                        {
                            var text = page.GetText();
                            pictureString = text;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
            parsedString = pictureString;
            return pictureString;
        }
        public Form1()
        {
            InitializeComponent();
            listView1.View = View.Details;
            ghk = new KeyHandler(Keys.End, this);
            ghk.Register();
            backgroundWorker1.WorkerReportsProgress = true;
            progressBar1.Visible = false;
            progressBar2.Visible = false;
        }

        /*
         * Get's the most update version's number [No updating here, just checking!]
         */

        public void getVersion()
        {
            if(progressBar2.InvokeRequired)
            {
                progressBar2.Invoke(new MethodInvoker(delegate { progressBar2.Visible = true; }));
            }
            WebClient client = new WebClient();
            string version = "";
            try
            {
                version = client.DownloadString(new Uri("http://188.240.208.209/text.txt"));
                if (progressBar2.InvokeRequired)
                {
                    progressBar2.Invoke(new MethodInvoker(delegate { progressBar2.Value = 50; }));
                }
            }
            catch (WebException e)
            {
                MessageBox.Show(e.Message + "\r\nUsing old database, so prices won't be correct!");
            }
            if(File.Exists("version.txt"))
            {
                string[] currVer = File.ReadAllLines("version.txt");
                if (Regex.IsMatch(version, currVer[0]))
                {
                    MessageBox.Show("Program is up to date!");
                }
                else
                {
                    MessageBox.Show("There is a newer version of the program!");
                }
            }
            else
            {
                File.Create("version.txt");
                TextWriter tw = new StreamWriter("version.txt");
                tw.Write("1.0.0");
            }
            if (progressBar2.InvokeRequired)
            {
                progressBar2.Invoke(new MethodInvoker(delegate { progressBar2.Visible = false; }));
            }
        }
        

        /*
         * Background worker, so the form doesn't freeze
         */

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            getStringFromPic();
            backgroundWorker1.ReportProgress(100);
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(e.Cancelled)
            {
                // This can't happen but whatever
            }
            else if(e.Error != null)
            {
                MessageBox.Show("Error in runtime, try again.");
            }
            else
            {
                matchAgainsDrops(parsedString);
                progressBar1.Value = 100;
                progressBar1.Visible = false;
            }
        }

        /*
         * Version check and download fresh database after form loaded
         */

        private void Form1_Shown(object sender, EventArgs e)
        {
            if (!backgroundWorker2.IsBusy)
            {
                backgroundWorker2.RunWorkerAsync();
            }
        }

        private void backgroundWorker2_DoWork(object sender, DoWorkEventArgs e)
        {
            //getVersion();
            updateDatabase();
        }
    }
}