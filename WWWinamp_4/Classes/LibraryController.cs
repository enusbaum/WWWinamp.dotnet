using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ENusbaum.Applications.WWWinamp.Classes
{
    static class LibraryController
    {
        #region Public Variables

        public static List<MediaLibraryItem> dbMediaLibrary
        {
            get { return privdbMediaLibrary; }
            set { privdbMediaLibrary = value; }
        }

        #endregion

        #region Private Variables
        private static List<MediaLibraryItem> privdbMediaLibrary = new List<MediaLibraryItem>();

        #endregion

        #region Public Methods

        /// <summary>
        ///     Scans the specified folder and subfolders for media and saves them to dbDatabase[]::mediaDB[]
        /// </summary>
        public static void BuildMediaLibrary()
        {
            try
            {
                DateTime dtScanStart = DateTime.Now;

                //Clear out Current Library
                privdbMediaLibrary = new List<MediaLibraryItem>();

                string[] sMediaDirectoryArray = AppConfiguration.configWWWinampMediaHomeDirectory.Split(new char[] { ';' });
                foreach (string sDirectory in sMediaDirectoryArray)
                {
                    if (!Directory.Exists(sDirectory))
                    {
                        LogHandler.LogEvent("ERROR WalkDirectory(): Directory \"" + sDirectory + "\" does not exist, skipping.\r");
                    }
                    else
                    {
                        WalkDirectory(new DirectoryInfo(sDirectory));
                    }
                }

                DateTime dtScanEnd = DateTime.Now;
                LogHandler.LogEvent("Media Library Built! (" + privdbMediaLibrary.Count.ToString() + " files scanned, " + Convert.ToString(dtScanEnd.Subtract(dtScanStart)) + " elapsed)");
            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
            }

        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Walks the specified directory saving files found to dbDatabase[]::mediaDB[] and also walks subdirectories
        /// </summary>
        /// <param name="directory">DirectoryInfo -- Directory to walk</param>
        private static void WalkDirectory(DirectoryInfo directory)
        {
            try
            {
                string[] sMediaTypeArray = AppConfiguration.configWWWinampMediaFileTypes.Split(new char[] { ';' });

                // Scan all files in the current path
                foreach (FileInfo file in directory.GetFiles())
                {
                    foreach (string sMediaType in sMediaTypeArray)
                    {
                        if (file.Name.EndsWith(sMediaType))
                        {
                            //Add To Media Library
                            MediaLibraryItem oItem = new MediaLibraryItem();
                            oItem.FileName = file.Name;
                            oItem.FilePath = directory.FullName;
                            oItem.FileID = privdbMediaLibrary.Count;
                            privdbMediaLibrary.Add(oItem);
                            break;
                        }
                    }
                }

                DirectoryInfo[] subDirectories = directory.GetDirectories();

                // Scan this current directory for any sub directories and walk them
                foreach (DirectoryInfo subDirectory in subDirectories)
                {
                    //Verify The Directory Exists
                    if (!Directory.Exists(directory.FullName))
                    {
                        LogHandler.LogEvent("ERROR WalkDirectory(): Directory \"" + directory + "\" does not exist, skipping.\r");
                    }
                    else
                    {
                        WalkDirectory(subDirectory);
                    }
                }
            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
            }
        }

        #endregion
    }

    public class MediaLibraryItem
    {
        #region Public Variables

        public string FileName
        {
            get { return privFileName; }
            set { privFileName = value; }
        }

        public string FilePath
        {
            get { return privFilePath; }
            set { privFilePath = value; }
        }

        public int FileID
        {
            get { return privFileID; }
            set { privFileID = value; }
        }

        #endregion

        #region Private Variables

        private string privFileName;
        private string privFilePath;
        private int privFileID;

        #endregion
    }
}
