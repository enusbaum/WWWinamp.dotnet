//Required for Windows Form Application
using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

//For Media Library Building/Threading
using System.Threading;

//For Static Classes
using ENusbaum.Applications.WWWinamp.Classes;
using ENusbaum.Applications.WWWinamp.Classes.WCFService;


namespace ENusbaum.Applications.WWWinamp.Forms
{
    public partial class frmMain : Form
    {


        public frmMain()
        {
            InitializeComponent();
        }

        #region =[Variable Declarations]=

        //ThreadLock Object
        private Object threadLock = new Object();

        //CallBack Delegate for Upating Log TextBox from within a Thread
        delegate void SetTextCallback(string sText);

        //Private Vairable to measure log length
        private int iCurrentLogSize = 0;

        #endregion


        #region =[Form Events & Functions ]=
        private void Form1_Resize(object sender, System.EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.Visible = false;
                notifyIcon1.Visible = true;
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Visible = true;
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            notifyIcon1.Visible = false;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Remove the Tray Icon if it's there
            notifyIcon1.Visible = false;

            //Exit Program
            Environment.Exit(-1);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                //Set Program Title
                this.Text = AppConfiguration.configProgramName + " " + AppConfiguration.configProgramVersion + " Server";

                //Set System Tray ToolTip Title
                notifyIcon1.Text = AppConfiguration.configProgramName + " " + AppConfiguration.configProgramVersion;

                //Set Default Status for Services
                WebServer.ListeningStatus = WebServer.Status.Stopped;

                //Check OS Compatability and Warn user about data files in Vista
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    LogHandler.LogEvent("!!");
                    LogHandler.LogEvent("!! WWWinamp has detected you are using Windows Vista (or newer)");
                    LogHandler.LogEvent("!! Please be aware that Windows Vista does not allow writing to the Program Files folder");
                    LogHandler.LogEvent("!! and that you must change the \"Winamp Directory\" value in WWWINAMP.XML to the correct");
                    LogHandler.LogEvent("!! folder which contains the \"Roaming\" Application data. This folder is commonly:");
                    LogHandler.LogEvent(@"!! C:\Users\<USER NAME>\AppData\Roaming\Winamp\");
                    LogHandler.LogEvent("!! If you do not do this, the Playlist will not generate correctly. Thanks!");
                    LogHandler.LogEvent("!!");
                }

                //Get WinAMP Handles
                LogHandler.LogEvent("Retrieving WinAMP Handles...");
                if(!WinAmpController.GetWinampHandles()) return;

                //Build Media Library               
                LogHandler.LogEvent("Building Media Library...");
                
                //Call Build Thread
                Thread thBuildMediaLibrary = new Thread(new ThreadStart(LibraryController.BuildMediaLibrary));
                thBuildMediaLibrary.Start();

                //Start HTTP Daemon if Specified
                if (AppConfiguration.configWWWinampStartHTTP)
                {
                    WebServer.ListeningIP = AppConfiguration.configWWWinampHTTPListeningIP;
                    WebServer.ListeningPort = AppConfiguration.configWWWinampHTTPListeningPort;
                    if(WebServer.StartListener()) toolStripSplitButton2.Image = Functions.ReadEmbeddedResourceToBitmap("OK.bmp");
                }

                //Start WCF Daemon if Specified
                if (AppConfiguration.configWWWinampStartWCF)
                {
                    WWWinampServiceHost.StartService();
                    toolStripSplitButton1.Image = Functions.ReadEmbeddedResourceToBitmap("OK.bmp");
                }
            }
            catch (Exception eException)
            {
                LogHandler.LogError(eException);
            }

        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.enusbaum.com");
        }

        private void createAddUserTohtpasswdFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ENusbaum.Applications.WWWinamp.Forms.frmAddUser myForm = new ENusbaum.Applications.WWWinamp.Forms.frmAddUser();
            myForm.Show();
        }
        #endregion

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (iCurrentLogSize < LogHandler.EventLog.Length)
            {
                textBox1.Text = LogHandler.EventLog.ToString();
                textBox1.SelectionStart = textBox1.Text.Length;
                textBox1.ScrollToCaret();
                iCurrentLogSize = LogHandler.EventLog.Length;
            }
        }

        /// <summary>
        ///     Start WCF Server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void runningToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (WWWinampServiceHost.StartService())
            {
                toolStripSplitButton1.Image = Functions.ReadEmbeddedResourceToBitmap("OK.bmp");
                toolStripSplitButton1.ToolTipText = "Service Started";
            }
        }

        /// <summary>
        ///     Stop WCF Server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void stoppedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(WWWinampServiceHost.StopService())
            {
                toolStripSplitButton1.Image = Functions.ReadEmbeddedResourceToBitmap("Critical.bmp");
                toolStripSplitButton1.ToolTipText = "Service Stopped";
            }
        }

        /// <summary>
        ///     Start Web Server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WebServer.ListeningIP = AppConfiguration.configWWWinampHTTPListeningIP;
            WebServer.ListeningPort = AppConfiguration.configWWWinampHTTPListeningPort;
            if (WebServer.StartListener()) toolStripSplitButton2.Image = Functions.ReadEmbeddedResourceToBitmap("OK.bmp");
        }

        /// <summary>
        ///     Stop Web Server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void stoppedToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (WebServer.StopListener())
            {   
                toolStripSplitButton2.Image = Functions.ReadEmbeddedResourceToBitmap("Critical.bmp");
                toolStripSplitButton2.ToolTipText = "Service Stopped";
            }
        }

    }
}