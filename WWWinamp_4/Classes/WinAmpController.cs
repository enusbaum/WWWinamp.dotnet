using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;

// Required to use unmanaged external DLL's
using System.Runtime.InteropServices;

namespace ENusbaum.Applications.WWWinamp.Classes
{
    static class WinAmpController
    {
        #region Public Variables

        public static IntPtr hwnd_wa
        {
            get { return privHwnd_wa; }
        }

        public static IntPtr hwnd_pe
        {
            get { return privHwnd_pe; }
        }

        #endregion

        #region Private Variables

        //ThreadLock
        static private Object threadLock = new Object();

        //External Referrences to Win32 API
        [DllImport("user32.dll")] private static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true)] private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", EntryPoint = "GetDesktopWindow")] private static extern IntPtr GetDesktopWindow();
        [DllImport("user32.dll")] private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
        [DllImport("user32.dll")] private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        //IPC Declarations
        static private int WM_SYSCOMMAND = 0x0112;
        static private int WM_USER = 0x0400;
        static private int WM_COPYDATA = 0x004A;

        static private int WINAMP_STOP = 40047;
        static private int WINAMP_PLAY = 40045;
        static private int WINAMP_SKIPFWD = 40048;
        static private int WINAMP_SKIPBACK = 40044;
        static private int WINAMP_PAUSE = 40046;
        static private int WINAMP_ISPLAYING = 104;
        static private int WINAMP_GETINFO = 126;
        static private int WINAMP_GETOUTPUTTIME = 105;
        static private int WINAMP_SETVOLUME = 122;
        static private int WINAMP_EXIT = 40001;
        static private int WINAMP_BALANCE = 123;
        static private int WINAMP_RESTART = 135;
        static private int WINAMP_REPEAT = 40022;
        static private int WINAMP_SHUFFLE = 40023;
        static private int PLAYLIST_INSERT = 106;
        static private int PLAYLIST_REMOVE = 104;
        static private int PLAYLIST_WRITEPLAYLIST = 120;
        static private int PLAYLIST_JUMPTOPOS = 121;
        static private int PLAYLIST_CLEAR = 101;
        static private int PLAYLIST_CURRENTPOS = 125;
        static private int PLAYLIST_CURRENTLENGTH = 124;

        //Process Handles
        static private IntPtr privHwnd_wa;
        static private IntPtr hwnd_top;
        static private IntPtr privHwnd_pe;

        /// <summary>
        ///     Struct for copying data over to WinAmp for playlist entries
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct COPYDATASTRUCT
        {
            public uint dwData;
            public uint cbData;
            public uint lpData;
        }

        /// <summary>
        ///     Struct for file inforamtion, used in Playlist Entries
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MediaFileInfo
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public char[] file;
            public int index;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Looks for a running copy of WinAmp and grabs the handles for it.
        /// </summary>
        /// <returns>bool -- true if handles were found</returns>
        public static bool GetWinampHandles()
        {
            privHwnd_wa = FindWindow("Winamp v1.x", null);
            if (privHwnd_wa == (IntPtr)0)
            {
                LogHandler.LogEvent("No WinAmp Instance Found!");
                LogHandler.LogEvent("Please verify that WinAmp is running and the CLASSIC SKIN is being used.");
                return false;
            }
            hwnd_top = GetDesktopWindow();
            privHwnd_pe = GetWinampPlaylistPointer();
            return true;
        }

        /// <summary>
        ///     Sends a command to Winamp using it's publically visible IPC.
        /// </summary>
        /// <param name="sParsedCommand"></param>
        /// <param name="sPassedValue"></param>
        public static void WinampCommand(string sParsedCommand, string sPassedValue)
        {
            string sFileLocation;

            try
            {

                //Parse this command first, because if WinAMP isnt open, we need to open it and get handles.
                if (sParsedCommand == "startwinamp")
                {
                    //Execute WinAMP
                    System.Diagnostics.Process.Start(AppConfiguration.configWinampHomeDirectory + "winamp.exe");

                    //Wait 5 Seconds for WinAmp to start
                    Thread.Sleep(5000);

                    //Get Handles
                    GetWinampHandles();
                }

                //If the handles are missing, then do not try to call API's
                if (privHwnd_wa == IntPtr.Zero || hwnd_pe == IntPtr.Zero) return;

                switch (sParsedCommand)
                {
                    case "stop":
                        SendMessage(privHwnd_wa, WM_SYSCOMMAND, (IntPtr)WINAMP_STOP, IntPtr.Zero);
                        break;

                    case "play":
                        if (WinampPlaylist.PlaylistItems.Count > 0) SendMessage(privHwnd_wa, WM_SYSCOMMAND, (IntPtr)WINAMP_PLAY, IntPtr.Zero);
                        break;

                    case "skipfwd":
                        SendMessage(privHwnd_wa, WM_SYSCOMMAND, (IntPtr)WINAMP_SKIPFWD, IntPtr.Zero);
                        break;

                    case "skipback":
                        SendMessage(privHwnd_wa, WM_SYSCOMMAND, (IntPtr)WINAMP_SKIPBACK, IntPtr.Zero);
                        break;

                    case "pause":
                        SendMessage(privHwnd_wa, WM_SYSCOMMAND, (IntPtr)WINAMP_PAUSE, IntPtr.Zero);
                        break;

                    case "repeat":
                        SendMessage(privHwnd_wa, WM_SYSCOMMAND, (IntPtr)WINAMP_REPEAT, IntPtr.Zero);
                        break;

                    case "shuffle":
                        SendMessage(privHwnd_wa, WM_SYSCOMMAND, (IntPtr)WINAMP_SHUFFLE, IntPtr.Zero);
                        break;

                    case "volume":
                        if (Convert.ToInt16(sPassedValue) >= 0 && Convert.ToInt16(sPassedValue) <= 255)
                        {
                            SendMessage(privHwnd_wa, WM_USER, (IntPtr)Convert.ToInt16(sPassedValue), (IntPtr)WINAMP_SETVOLUME);
                        }
                        break;

                    case "balance":
                        if (Convert.ToInt16(sPassedValue) >= 0 && Convert.ToInt16(sPassedValue) <= 255)
                        {
                            SendMessage(privHwnd_wa, WM_USER, (IntPtr)Convert.ToInt16(sPassedValue), (IntPtr)WINAMP_BALANCE);
                        }
                        break;

                    case "addfile":
                        if (Convert.ToInt16(sPassedValue) <= LibraryController.dbMediaLibrary.Count)
                        {
                            sFileLocation = LibraryController.dbMediaLibrary[Convert.ToInt16(sPassedValue)].FilePath + @"\" + LibraryController.dbMediaLibrary[Convert.ToInt16(sPassedValue)].FileName;
                            AddFileToWinAmpPlaylist(sFileLocation);
                        }
                        break;

                    case "addfolder":
                        //iPassedValue contains 1st file of the folder to be added.
                        if (Convert.ToInt16(sPassedValue) <= LibraryController.dbMediaLibrary.Count)
                        {
                            for (int iLoop = 1; iLoop < LibraryController.dbMediaLibrary.Count; iLoop++)
                            {
                                if (LibraryController.dbMediaLibrary[iLoop].FilePath == LibraryController.dbMediaLibrary[Convert.ToInt16(sPassedValue)].FilePath)
                                {
                                    sFileLocation = LibraryController.dbMediaLibrary[iLoop].FilePath + @"\" + LibraryController.dbMediaLibrary[iLoop].FileName;
                                    AddFileToWinAmpPlaylist(sFileLocation);
                                }
                            }
                        }
                        break;

                    case "jumppos":
                        //Sets Highlighted Song on Playlist by PlaylistID
                        SendMessage(privHwnd_wa, WM_USER, (IntPtr)Convert.ToInt16(sPassedValue), (IntPtr)PLAYLIST_JUMPTOPOS);

                        //Play highlighted song
                        WinampCommand("play", "0");
                        break;

                    case "removefile":
                        //Remove song from playlist based on PlaylistID
                        SendMessage(hwnd_pe, WM_USER, (IntPtr)PLAYLIST_REMOVE, (IntPtr)Convert.ToInt16(sPassedValue));
                        break;

                    case "clear":
                        //Clear Playlist
                        SendMessage(privHwnd_wa, WM_USER, IntPtr.Zero, (IntPtr)PLAYLIST_CLEAR);
                        break;

                    case "rescan":
                        //Call Library Updader
                        Thread thBuildMediaLibrary = new Thread(new ThreadStart(LibraryController.BuildMediaLibrary));
                        thBuildMediaLibrary.Start();
                        break;

                    case "shutdown":
                        //Closes WWWinamp
                        System.Environment.Exit(1);
                        break;

                    case "closewinamp":
                        //Closes WinAmp
                        SendMessage(privHwnd_wa, WM_SYSCOMMAND, (IntPtr)WINAMP_EXIT, IntPtr.Zero);
                        break;

                    case "restartwinamp":
                        //Restarts WinAmp
                        SendMessage(privHwnd_wa, WM_USER, IntPtr.Zero, (IntPtr)WINAMP_RESTART);

                        //Wait 5 Seconds for WinAmp to start
                        Thread.Sleep(5000);

                        //Get Handles
                        GetWinampHandles();
                        break;

                    case "saveplaylist":
                        //Call IPC to save playlist to WINAMP.M3U
                        lock (threadLock)
                        {
                            SendMessage(privHwnd_wa, WM_USER, IntPtr.Zero, (IntPtr)PLAYLIST_WRITEPLAYLIST);
                        }

                        //Copy WINAMP.M3U to SavedPlaylists Folder as filename specified in URL/POST
                        File.Copy(AppConfiguration.configWinampHomeDirectory + "winamp.m3u", AppConfiguration.configWinampPlaylistDirectory + sPassedValue);
                        break;
                    case "moveup":
                        Playlist_MoveSongUp(Convert.ToInt16(sPassedValue));
                        break;

                    case "movedown":
                        Playlist_MoveSongDown(Convert.ToInt16(sPassedValue));
                        break;


                }
            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
            }

        }

        /// <summary>
        ///     Detects commands to return status' from certain parts of WinAmp
        /// </summary>
        /// <param name="sCommand">string -- Command specifiying status being requested</param>
        /// <returns>string -- value of status</returns>
        public static string WinAmpStatus(string sCommand)
        {
            try
            {
                //If the handles are missing, then do not try to call API's
                if (privHwnd_wa == IntPtr.Zero || hwnd_pe == IntPtr.Zero) return "";

                int iOutput = 0;

                switch (sCommand)
                {
                    case "PLAYING_STATUS":
                        switch (SendMessage(privHwnd_wa, WM_USER, IntPtr.Zero, (IntPtr)WINAMP_ISPLAYING))
                        {
                            case 0:
                                return "Stopped";
                            case 1:
                                return "Playing";
                            case 3:
                                return "Paused";
                            default:
                                return "";
                        }
                    case "CURRENT_SONG_BITRATE":
                        iOutput = SendMessage(privHwnd_wa, WM_USER, (IntPtr)1, (IntPtr)WINAMP_GETINFO);
                        break;
                    case "CURRENT_SONG_SAMPLERATE":
                        iOutput = SendMessage(privHwnd_wa, WM_USER, (IntPtr)0, (IntPtr)WINAMP_GETINFO);
                        break;
                    case "CURRENT_SONG_LENGTH":
                        iOutput = SendMessage(privHwnd_wa, WM_USER, (IntPtr)1, (IntPtr)WINAMP_GETOUTPUTTIME);
                        break;
                    case "CURRENT_SONG_ELAPSED":
                        iOutput = SendMessage(privHwnd_wa, WM_USER, (IntPtr)0, (IntPtr)WINAMP_GETOUTPUTTIME);
                        break;
                }
                return iOutput.ToString();
            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
                return "";
            }

        }


        /// <summary>
        ///     Addds the specified file to the WinAmp playlist
        /// </summary>
        /// <param name="sFileLocation">string -- Location of the file ot be added to the playlist (i.e.: c:\mp3\test.mp3)</param>
        public static void AddFileToWinAmpPlaylist(string sFileLocation)
        {
            try
            {

                MediaFileInfo mediaFile;
                mediaFile.file = new char[256];
                char[] cTemp = sFileLocation.ToCharArray();

                //We must pad File Name to be 256 bytes, as to fill the Marshaled off Memory
                for (int iLoop = 0; iLoop < sFileLocation.Length; iLoop++)
                {
                    mediaFile.file[iLoop] = cTemp[iLoop];
                }

                mediaFile.index = 0;
                IntPtr mediaFileMemory = Marshal.AllocCoTaskMem(256 + sizeof(int));
                Marshal.StructureToPtr(mediaFile, mediaFileMemory, false);

                //Build the COPYDATASTRUCT which will contain the information for WinAmp
                COPYDATASTRUCT cds = new COPYDATASTRUCT();
                cds.dwData = (uint)PLAYLIST_INSERT;
                cds.lpData = (uint)(mediaFileMemory.ToInt32());
                cds.cbData = (uint)(mediaFile.file.Length);
                IntPtr cdsMemory = Marshal.AllocCoTaskMem(3 * sizeof(int)); //Since structure contains 3x uint, map memory of same size
                Marshal.StructureToPtr(cds, cdsMemory, false);

                //Send Message to WinAmp Playlist
                SendMessage(hwnd_pe, WM_COPYDATA, IntPtr.Zero, (IntPtr)cdsMemory.ToInt32());

                //Clean up memmory allocated as to not cause any memory leaks
                Marshal.FreeCoTaskMem(cdsMemory);
                Marshal.FreeCoTaskMem(mediaFileMemory);

                //Write out Updated Playlist
                lock (threadLock)
                {
                    SendMessage(privHwnd_wa, WM_USER, IntPtr.Zero, (IntPtr)PLAYLIST_WRITEPLAYLIST);
                }
            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
            }
        }

        /// <summary>
        ///     Generates a new WINAMP.M3U from WinAmp, which is the current playlist that is loaded
        /// </summary>
        public static void Playlist_GenerateWinampM3U()
        {
            try
            {
                SendMessage(privHwnd_wa, WM_USER, IntPtr.Zero, (IntPtr)PLAYLIST_WRITEPLAYLIST);
            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
            }
        }

        /// <summary>
        ///     Retrieves the currently playing song ID from the playlist
        /// </summary>
        /// <returns>int -- Currently playing song Playlist ID</returns>
        public static int Playlist_CurrentSongNumber()
        {
            try
            {
                return SendMessage(privHwnd_wa, WM_USER, (IntPtr)0, (IntPtr)PLAYLIST_CURRENTPOS);

            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
                return 0;
            }
        }

       

        /// <summary>
        ///     Moves the specified file up one slot in the playlist.
        /// </summary>
        /// <param name="iSongToMoveUp">int -- Playlist ID of song to move up</param>
        /// <returns>bool -- true if move was successful</returns>
        public static bool Playlist_MoveSongUp(int iSongToMoveUp)
        {
            //Cache Playlist to a string[]
            int iPlaylistLength = WinampPlaylist.PlaylistItems.Count;

            //Save Current Position
            int iPlaylistPosition = WinampPlaylist.CurrentPosition;

            //If Playlist is empty or only one track, skip
            if (iPlaylistLength <= 1) return false;

            string[] sPlaylistItems = new string[100000];
            for (int iLoop = 0; iLoop < iPlaylistLength; iLoop++)
            {
                sPlaylistItems[iLoop] = WinampPlaylist.PlaylistItems[iLoop].FilePath + WinampPlaylist.PlaylistItems[iLoop].FileName;
            }

            //Clear Playlist
            SendMessage(privHwnd_wa, WM_USER, IntPtr.Zero, (IntPtr)PLAYLIST_CLEAR);

            //Add Items to the playlist stopping at (ItemToMoveUp-1)
            for (int iLoop = 0; iLoop < (iSongToMoveUp - 1); iLoop++)
            {
                AddFileToWinAmpPlaylist(sPlaylistItems[iLoop]);
            }

            //Add Item to Move up, Add Item that WAS before it
            AddFileToWinAmpPlaylist(sPlaylistItems[iSongToMoveUp]);
            AddFileToWinAmpPlaylist(sPlaylistItems[iSongToMoveUp - 1]);

            //Add Item after item to select until end
            for (int iLoop = (iSongToMoveUp + 1); iLoop < iPlaylistLength; iLoop++)
            {
                AddFileToWinAmpPlaylist(sPlaylistItems[iLoop]);
            }

            //Set current playlist position
            if (iSongToMoveUp <= (iPlaylistPosition + 1))
            {
                SendMessage(privHwnd_wa, WM_USER, (IntPtr)(iPlaylistPosition + 1), (IntPtr)PLAYLIST_JUMPTOPOS);
            }
            if (iSongToMoveUp > (iPlaylistPosition + 1))
            {
                SendMessage(privHwnd_wa, WM_USER, (IntPtr)iPlaylistPosition, (IntPtr)PLAYLIST_JUMPTOPOS);
            }

            //Give the playlist a chance to update
            Thread.Sleep(500);

            return true;
        }


        /// <summary>
        ///     Moves the specified file down one slot in the playlist.
        /// </summary>
        /// <param name="iSongToMoveDown">int -- Playlist ID of song to move down</param>
        /// <returns>bool -- true if move was successful</returns>
        public static bool Playlist_MoveSongDown(int iSongToMoveDown)
        {
            //Cache Playlist to a string[]
            int iPlaylistLength = WinampPlaylist.PlaylistItems.Count;

            //Save Current Position
            int iPlaylistPosition = WinampPlaylist.CurrentPosition;

            //If Playlist is empty or only one track, skip
            if (iPlaylistLength <= 1) return false;

            List<WinampPlaylistItem> oOriginalPlaylist = new List<WinampPlaylistItem>();

            //Backup the Current Playlist
            foreach (WinampPlaylistItem oItem in WinampPlaylist.PlaylistItems)
            {
                oOriginalPlaylist.Add(oItem);
            }

            //Clear Playlist
            SendMessage(privHwnd_wa, WM_USER, IntPtr.Zero, (IntPtr)PLAYLIST_CLEAR);

            //Add Items to the playlist stopping at (ItemToMoveUp-1)
            for (int iLoop = 0; iLoop <= (iSongToMoveDown - 1); iLoop++)
            {
                AddFileToWinAmpPlaylist(oOriginalPlaylist[iLoop].FilePath + oOriginalPlaylist[iLoop].FileName);
            }

            //Add Item to Move up, Add Item that WAS before it
            AddFileToWinAmpPlaylist(oOriginalPlaylist[iSongToMoveDown + 1].FilePath + oOriginalPlaylist[iSongToMoveDown + 1].FileName);
            AddFileToWinAmpPlaylist(oOriginalPlaylist[iSongToMoveDown].FilePath + oOriginalPlaylist[iSongToMoveDown].FileName);


            //Add Item after item to select until end
            for (int iLoop = (iSongToMoveDown + 2); iLoop < iPlaylistLength; iLoop++)
            {
                AddFileToWinAmpPlaylist(oOriginalPlaylist[iLoop].FilePath + oOriginalPlaylist[iLoop].FileName);
            }

            //Set current playlist position
            if (iSongToMoveDown <= (iPlaylistPosition + 1))
            {
                SendMessage(privHwnd_wa, WM_USER, (IntPtr)(iPlaylistPosition - 1), (IntPtr)PLAYLIST_JUMPTOPOS);
            }
            if (iSongToMoveDown > (iPlaylistPosition + 1))
            {
                SendMessage(privHwnd_wa, WM_USER, (IntPtr)iPlaylistPosition, (IntPtr)PLAYLIST_JUMPTOPOS);
            }

            //Give the playlist a chance to update
            Thread.Sleep(500);

            return true;
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Looks for a running copy of WinAmp and grabs the handle for the Playlist editor
        /// </summary>
        /// <returns></returns>
        private static IntPtr GetWinampPlaylistPointer()
        {
            try
            {
                //This Routine ensures that the Playlist Pointer we are using is for the instance of Winamp we've selected.
                IntPtr threadWinamp;
                IntPtr threadPlaylist;
                IntPtr ptrPlaylist = IntPtr.Zero;

                for (int iLoop = 0; iLoop < 100; iLoop++)
                {
                    ptrPlaylist = FindWindowEx(hwnd_top, IntPtr.Zero, "Winamp PE", null);
                    threadWinamp = GetWindowThreadProcessId(privHwnd_wa, IntPtr.Zero);
                    threadPlaylist = GetWindowThreadProcessId(ptrPlaylist, IntPtr.Zero);
                    if (threadWinamp == threadPlaylist)
                    {
                        break;
                    }
                }
                return ptrPlaylist;
            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
                return IntPtr.Zero;

            }
        }

        #endregion
    }

    static class WinampPlaylist
    {
        //ThreadLock
        static private Object threadLock = new Object();

        public static int CurrentPosition
        {
            get { return WinAmpController.Playlist_CurrentSongNumber(); }
        }

        public static List<WinampPlaylistItem> PlaylistItems
        {
            get { return ReloadPlaylist(); }
        }

        private static List<WinampPlaylistItem> ReloadPlaylist()
        {
            lock (threadLock)
            {
                //Generate an updated winamp.m3u
                WinAmpController.Playlist_GenerateWinampM3U();

                //Create a new blank list
                List<WinampPlaylistItem> oWinampPlaylist = new List<WinampPlaylistItem>();

                string strLineInput = string.Empty;

                //Read Current Playlist File until EOF
                using (StreamReader oSrPlaylist = new StreamReader(Functions.ReadLocalFileToMemoryStream(AppConfiguration.configWinampPlaylistDirectory + "winamp.m3u")))
                {
                    while ((strLineInput = oSrPlaylist.ReadLine()) != null)
                    {
                        if (strLineInput.Substring(0, 7) == "#EXTINF")
                        {
                            //Extended Information Found

                            //Declare Blank Item
                            WinampPlaylistItem oItem = new WinampPlaylistItem();

                            oItem.SongID = oWinampPlaylist.Count;
                            oItem.DisplayName = strLineInput.Substring(strLineInput.IndexOf(",") + 1);

                            //Read Next Line, which is the file name
                            strLineInput = oSrPlaylist.ReadLine();
                            oItem.FileName = strLineInput.Substring(strLineInput.LastIndexOf("\\") + 1);
                            oItem.FilePath = strLineInput.Substring(0, strLineInput.LastIndexOf("\\") + 1);

                            //Add Item to the playlist
                            oWinampPlaylist.Add(oItem);
                        }
                        else if (strLineInput.Substring(0, 7) == "#EXTM3U")
                        {
                            //Beginning of Playlist, do nothing.
                        }
                        else
                        {
                            //Extended Information Found

                            //Declare Blank Item
                            WinampPlaylistItem oItem = new WinampPlaylistItem();
                            oItem.SongID = oWinampPlaylist.Count;
                            oItem.DisplayName = strLineInput.Substring(strLineInput.LastIndexOf("\\") + 1);
                            oItem.FileName = strLineInput.Substring(strLineInput.LastIndexOf("\\") + 1);
                            oItem.FilePath = strLineInput.Substring(0, strLineInput.LastIndexOf("\\") + 1);

                            //Add Item to the playlist
                            oWinampPlaylist.Add(oItem);
                        }
                    }
                }
                return oWinampPlaylist;
            }
        }
        
    }

    class WinampPlaylistItem
    {
        private int privIntSongID;
        private string privStrFilePath;
        private string privStrFileName;
        private string privStrDisplayName;

        /// <summary>
        ///     This file's Playlist ID
        /// </summary>
        public int SongID
        {
            get { return privIntSongID; }
            set { privIntSongID = value; }
        }

        /// <summary>
        ///     This file's Playlist File Path
        /// </summary>
        public string FilePath
        {
            get { return privStrFilePath; }
            set { privStrFilePath = value; }
        }

        /// <summary>
        ///     This is the file's name FILENAME.EXT
        /// </summary>
        public string FileName
        {
            get { return privStrFileName; }
            set { privStrFileName = value; }
        }

        /// <summary>
        ///     This file's Playlist Display Name (if available)
        /// </summary>
        public string DisplayName
        {
            get { return privStrDisplayName; }
            set { privStrDisplayName = value; }
        }
    }
}
