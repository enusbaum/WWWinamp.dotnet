using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Web;

namespace ENusbaum.Applications.WWWinamp.Classes
{
    static class ScriptParser
    {
        #region Public Methods

        /// <summary>
        ///     Processes the passed script for items such as Include Tags and Script Tags
        /// </summary>
        /// <param name="bFileContent">byte[] -- Input script</param>
        /// <param name="sFilePath">string -- Current path of the script (used for pathing in includes)</param>
        /// <param name="sInputVariable">string -- Input variable, most times the search term</param>
        /// <param name="blIsAdmin">bool -- true if the current user requesting the script is logged in as a WWWinamp admin</param>
        /// <returns>byte[] -- Processed script</returns>
        public static byte[] ProcessScript(byte[] bFileContent, string sFilePath, string sInputVariable, bool blIsAdmin)
        {
            //This Function Calls one by one the steps in processing a file for input
            string sProcessedOutput = Encoding.ASCII.GetString(bFileContent);

            //Proccess any #INCLUDE tags in the HTML
            sProcessedOutput = ProcessIncludeTags(sProcessedOutput, sFilePath);

            //Process Script Tags
            sProcessedOutput = ProcessScriptTags(sProcessedOutput, sInputVariable, blIsAdmin);

            //Return Processed File to output stream
            return Encoding.ASCII.GetBytes(sProcessedOutput);
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Processes the Playlist WWA files and Generates the WWWinamp Playlist for HTTP Requests
        /// </summary>
        /// <returns>string -- String Containing the Parsed and Generated Playlist HTML</returns>
        private static string GeneratePlaylist()
        {
            try
            {
                string strLineInput = string.Empty;
                string strPlaylistItemMaster = string.Empty;

                //If the handles are missing, report the error and halt render
                if (WinAmpController.hwnd_wa == IntPtr.Zero || WinAmpController.hwnd_pe == IntPtr.Zero) return "No Instance of WinAMP Found.";

                StringBuilder strOutput = new StringBuilder();
                StringBuilder strPlaylistItem = new StringBuilder();
                StringBuilder strPlaylistItemCurrent = new StringBuilder();

                //Input Current Playlist Line Item code into a string builder
                if(!File.Exists(AppConfiguration.configWWWinampHTTPHomeDirectory + "playlist_item.wwa")) return "Error While Generating Playlist.<br><br>Error Description: playlist_item.wwa not found";
                StreamReader oSrPlaylistItem = new StreamReader(AppConfiguration.configWWWinampHTTPHomeDirectory + "playlist_item.wwa");
                while ((strLineInput = oSrPlaylistItem.ReadLine()) != null)
                {
                    strPlaylistItem.Append(strLineInput);
                }
                oSrPlaylistItem.Close();

                //Input Playing Playlist Line Item code into a string builder
                if (!File.Exists(AppConfiguration.configWWWinampHTTPHomeDirectory + "playlist_item_current.wwa")) return "Error While Generating Playlist.<br><br>Error Description: playlist_item_current.wwa not found";
                StreamReader oSrPlaylistItemCurrent = new StreamReader(AppConfiguration.configWWWinampHTTPHomeDirectory + "playlist_item_current.wwa");
                while ((strLineInput = oSrPlaylistItemCurrent.ReadLine()) != null)
                {
                    strPlaylistItemCurrent.Append(strLineInput);
                }
                oSrPlaylistItemCurrent.Close();

                //Create a copy to revert back to everytime I use the StringBuilder Object
                strPlaylistItemMaster = strPlaylistItem.ToString();

                foreach (WinampPlaylistItem oItem in WinampPlaylist.PlaylistItems)
                {
                    if (oItem.SongID == WinampPlaylist.CurrentPosition)
                    {
                        strPlaylistItemCurrent.Replace("|ITEM|", oItem.DisplayName);
                        strPlaylistItemCurrent.Replace("|ID|", oItem.SongID.ToString());
                        strPlaylistItemCurrent.Replace("|NUMBER|", ((int)(oItem.SongID + 1)).ToString());
                        strOutput.Append(strPlaylistItemCurrent.ToString());
                    }
                    else
                    {
                        strPlaylistItem.Replace("|ITEM|", oItem.DisplayName);
                        strPlaylistItem.Replace("|ID|", oItem.SongID.ToString());
                        strPlaylistItem.Replace("|NUMBER|", ((int)(oItem.SongID + 1)).ToString());
                        strOutput.Append(strPlaylistItem.ToString());
                        strPlaylistItem.Length = 0;
                        strPlaylistItem.Append(strPlaylistItemMaster);
                    }

                }
                return strOutput.ToString();
            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
                return "Error While Generating Playlist";
            }
        }

        /// <summary>
        ///     Processes the Library WWA files and Generates the WWWinamp Library for HTTP Requests
        /// </summary>
        /// <param name="sInputString">string -- URL Paramiters</param>
        /// <returns>string -- String Containing the Parsed and Generated Library HTML</returns>
        private static string GenerateLibrary(string sInputString)
        {
            try
            {
                string sLineInput = string.Empty;
                string sLibraryFolderItemMaster = string.Empty;
                string sLibraryFileItemMaster = string.Empty;
                string sSearchTerm = string.Empty;
                int iResultStart = 0;
                int iSearchPage = 1;
                int iEndPos = 0;

                iEndPos = sInputString.LastIndexOf("&");
                if (iEndPos == -1) iEndPos = sInputString.Length;

                sSearchTerm = HttpUtility.UrlDecode(sInputString.Substring(0, iEndPos));

                if (String.IsNullOrEmpty(sSearchTerm)) return "";

                if (sInputString.LastIndexOf("&page=") > -1) iSearchPage = Convert.ToInt16(sInputString.Substring(sInputString.LastIndexOf("&page=") + 6));

                iResultStart = (iSearchPage - 1) * AppConfiguration.configWWWinampHTTPResultsPerPage;
                int iResultEnd = iResultStart + AppConfiguration.configWWWinampHTTPResultsPerPage;
                StringBuilder sOutput = new StringBuilder();
                StringBuilder sLibraryFolderItem = new StringBuilder();
                StringBuilder sLibraryFileItem = new StringBuilder();

                //Input Library Folder Code into a string builder
                StreamReader oSrLibraryFolder = new StreamReader(AppConfiguration.configWWWinampHTTPHomeDirectory + "library_folder.wwa");
                while ((sLineInput = oSrLibraryFolder.ReadLine()) != null)
                {
                    sLibraryFolderItem.Append(sLineInput);
                }
                oSrLibraryFolder.Close();

                //Create a copy to revert back to everytime I use the StringBuilder Object
                sLibraryFolderItemMaster = sLibraryFolderItem.ToString();

                //Input Library File Code into a string builder
                StreamReader oSrLibraryFile = new StreamReader(AppConfiguration.configWWWinampHTTPHomeDirectory + "library_file.wwa");
                while ((sLineInput = oSrLibraryFile.ReadLine()) != null)
                {
                    sLibraryFileItem.Append(sLineInput);
                }
                oSrLibraryFile.Close();

                //Create a copy to revert back to everytime I use the StringBuilder Object
                sLibraryFileItemMaster = sLibraryFileItem.ToString();

                //Search through Media Library for Folders or Files that match the search term
                List<MediaLibraryItem> oSearchResults = new List<MediaLibraryItem>();
                if (sSearchTerm.ToUpper() == "*")
                {
                    oSearchResults = LibraryController.dbMediaLibrary.FindAll(delegate(MediaLibraryItem oItem) { return true; });
                }
                else
                {
                    oSearchResults = LibraryController.dbMediaLibrary.FindAll(delegate(MediaLibraryItem oItem) { return (oItem.FilePath.ToUpper().Contains(sSearchTerm.ToUpper()) || oItem.FileName.ToUpper().Contains(sSearchTerm.ToUpper())); });
                }

                //Trim off records that are to be skipped
                oSearchResults.RemoveRange(0, iResultStart);

                //Trim off reocrds that are after the current search page
                if((oSearchResults.Count - AppConfiguration.configWWWinampHTTPResultsPerPage) > 0) oSearchResults.RemoveRange(AppConfiguration.configWWWinampHTTPResultsPerPage, (oSearchResults.Count - AppConfiguration.configWWWinampHTTPResultsPerPage));

                string sPreviosFolder = string.Empty;
                foreach (MediaLibraryItem oItem in oSearchResults)
                {

                    if (sPreviosFolder != oItem.FilePath)
                    {
                        sPreviosFolder = oItem.FilePath;

                        //Name found in Directory Name, now Search for Files in this Same Directory
                        sLibraryFolderItem.Replace("|ITEM|", oItem.FilePath);
                        sLibraryFolderItem.Replace("|ID|", oItem.FileID.ToString());

                        //Add Folder info to output stream, then clear the FolderItem 
                        sOutput.Append(sLibraryFolderItem.ToString());
                        sLibraryFolderItem.Length = 0;
                        sLibraryFolderItem.Append(sLibraryFolderItemMaster);
                    }

                    //As Long as we're in the same directory, display every file
                    sLibraryFileItem.Replace("|ITEM|", oItem.FileName);
                    sLibraryFileItem.Replace("|ID|", oItem.FileID.ToString());

                    //Add File Info to output stream then clear File into Line Item
                    sOutput.Append(sLibraryFileItem.ToString());
                    sLibraryFileItem.Length = 0;
                    sLibraryFileItem.Append(sLibraryFileItemMaster);
                }

                if (oSearchResults.Count == 0) sOutput.Append("No search results found for the term \"" + sSearchTerm + "\"");
                return sOutput.ToString();
            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
                return "Error While Generating Library";
            }
        }

        private static string Library_CountResults(string sInputString)
        {
            try
            {
                //Extract Search Term from Input String
                int iEndPos = sInputString.LastIndexOf("&");
                if (iEndPos == -1)
                {
                    iEndPos = sInputString.Length;
                }

                string sSearchTerm = sInputString.Substring(0, iEndPos);

                if (String.IsNullOrEmpty(sSearchTerm)) return "";

                //Search through Media Library for Folders or Files that match the search term
                List<MediaLibraryItem> oSearchResults = new List<MediaLibraryItem>();
                if (sSearchTerm.ToUpper() == "*")
                {
                    oSearchResults = LibraryController.dbMediaLibrary.FindAll(delegate(MediaLibraryItem oItem) { return true; });
                }
                else
                {
                    oSearchResults = LibraryController.dbMediaLibrary.FindAll(delegate(MediaLibraryItem oItem) { return (oItem.FilePath.ToUpper().Contains(sSearchTerm.ToUpper()) || oItem.FileName.ToUpper().Contains(sSearchTerm.ToUpper())); });
                }

                return oSearchResults.Count.ToString();
            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
                return "Error While Generating Library Search Results Count";
            }
        }

        /// <summary>
        ///     Processes the passed script for any script tags including Library and Playlist items
        /// </summary>
        /// <param name="sFileContent">string -- Script to be parsed</param>
        /// <param name="sInputVariable">string -- Input Variable, usually search term</param>
        /// <param name="blIsAdmin">bool -- Is the current user requesting this page a WWWinamp Admin</param>
        /// <returns>string -- Parsed script</returns>
        private static string ProcessScriptTags(string sFileContent, string sInputVariable, bool blIsAdmin)
        {
            try
            {

                //Look for Strings
                StringBuilder sParsedOutput = new StringBuilder(sFileContent);
                sParsedOutput.Replace("|PLAYLIST|", GeneratePlaylist());
                sParsedOutput.Replace("|LIBRARY|", GenerateLibrary(sInputVariable));
                sParsedOutput.Replace("|PLAYING_STATUS|", WinAmpController.WinAmpStatus("PLAYING_STATUS"));
                sParsedOutput.Replace("|CURRENT_SONG_TITLE|", WinampPlaylist.PlaylistItems[WinampPlaylist.CurrentPosition].DisplayName);
                sParsedOutput.Replace("|CURRENT_SONG_NUMBER|", WinampPlaylist.CurrentPosition.ToString());
                sParsedOutput.Replace("|CURRENT_SONG_BITRATE|", WinAmpController.WinAmpStatus("CURRENT_SONG_BITRATE"));
                sParsedOutput.Replace("|CURRENT_SONG_SAMPLERATE|", WinAmpController.WinAmpStatus("CURRENT_SONG_SAMPLERATE"));
                sParsedOutput.Replace("|CURRENT_SONG_LENGTH|", WinAmpController.WinAmpStatus("CURRENT_SONG_LENGTH"));
                sParsedOutput.Replace("|CURRENT_SONG_ELAPSED|", WinAmpController.WinAmpStatus("CURRENT_SONG_ELAPSED"));
                sParsedOutput.Replace("|STATUS_ADMIN|", blIsAdmin.ToString());
                sParsedOutput.Replace("|LIBRARY_SIZE|", LibraryController.dbMediaLibrary.Count.ToString());
                sParsedOutput.Replace("|SEARCH_RESULTS_SIZE|", Library_CountResults(sInputVariable));
                sParsedOutput.Replace("|PLAYLIST_SIZE|", WinampPlaylist.PlaylistItems.Count.ToString());
                sParsedOutput.Replace("|PLAYLIST_REMAINING|", WinampPlaylist.CurrentPosition.ToString());
                sParsedOutput.Replace("|CONFIG_RESULTSPERPAGE|", AppConfiguration.configWWWinampHTTPResultsPerPage.ToString());

                if (!blIsAdmin)
                {
                    //If the person IS logged in as Admin, Do not show them HTML between these tags
                    sParsedOutput.Replace("|!ADM|", "");
                    sParsedOutput.Replace("|/!ADM|", "");
                    return FilterAdmin(sParsedOutput);
                }
                else
                {
                    //If the person IS NOT logged in as Admin, Do not show them HTML between these tags
                    sParsedOutput.Replace("|ADM|", "");
                    sParsedOutput.Replace("|/ADM|", "");
                    return FilterNonAdmin(sParsedOutput);
                }
            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
                return "Error Occured while Parsing Script";
            }
        }

        /// <summary>
        ///     Strips any tags/html/text between the |ADM| tags if the user is not logged in as a WWWinamp Admin
        /// </summary>
        /// <param name="sParsedInput">string -- Script to be parsed</param>
        /// <returns>string -- Parsed script</returns>
        private static string FilterAdmin(StringBuilder sParsedInput)
        {
            int iStartIndex = 0;
            int iEndIndex = 0;

            while (true)
            {
                iStartIndex = sParsedInput.ToString().IndexOf("|ADM|");
                iEndIndex = sParsedInput.ToString().IndexOf("|/ADM|");

                if (iStartIndex == -1) break;
                if (iEndIndex == -1) return "Script Error: No |/ADM| tag found!";

                sParsedInput.Remove(iStartIndex, ((iEndIndex + 6) - iStartIndex));
            }
            return sParsedInput.ToString();
        }

        /// <summary>
        ///     Strips any tags/html/text between the |!ADM| tags if the user is logged in as a WWWinamp Admin
        /// </summary>
        /// <param name="sParsedInput">string -- Script to be parsed</param>
        /// <returns>string -- Parsed script</returns>
        private static string FilterNonAdmin(StringBuilder sParsedInput)
        {
            int iStartIndex = 0;
            int iEndIndex = 0;

            while (true)
            {
                iStartIndex = sParsedInput.ToString().IndexOf("|!ADM|");
                iEndIndex = sParsedInput.ToString().IndexOf("|/!ADM|");

                if (iStartIndex == -1) break;
                if (iEndIndex == -1) return "Script Error: No |/!ADM| tag found!";

                sParsedInput.Remove(iStartIndex, ((iEndIndex + 7) - iStartIndex));
            }
            return sParsedInput.ToString();
        }

        /// <summary>
        ///     Handler for processing include tags (<!--#INCLUDE FILE=""-->) within skin files.
        /// </summary>
        /// <param name="sInputScript">string: string containing the data read from the file requested.</param>
        /// <param name="sFilePath">string: string containing the physical path of the file requested.</param>
        /// <returns><b>string:</b> string containing the current HTML output for the file to be sent to the browser</returns>
        private static string ProcessIncludeTags(string sInputScript, string sFilePath)
        {
            //Valid Include tag is <!--#INCLUDE file="myfile.txt"-->
            int iCommentStartPos = 0;
            int iCommendEndPos = 0;
            int iIncludePos = 0;
            int iFilePos = 0;

            //Use A StringBuilder Object for Great Justice
            StringBuilder sOutputScript = new StringBuilder(sInputScript);

            //Search for HTML comment start tag <!--
            iCommentStartPos = sInputScript.IndexOf("<!--");

            //If no Comment tags found, no includes need to be processed so return
            if (iCommentStartPos == -1) { return sInputScript; }

            //Find the End Comment Tag
            iCommendEndPos = sInputScript.IndexOf("-->", iCommentStartPos);

            //If no End tag is found, then no valid include will be processed so return
            if (iCommendEndPos == -1) { return sInputScript; }

            while (iCommentStartPos > 0)
            {
                //See if the #INCLUDE tag is inside the comment tag
                iIncludePos = sInputScript.ToLower().IndexOf("#include", iCommentStartPos, (iCommendEndPos - iCommentStartPos));
                if (iIncludePos > 0)
                {
                    //#INCLUDE tag found between the HTML comment tags
                    //See if the file= is specified in the include
                    iFilePos = sInputScript.ToLower().IndexOf("file=", iIncludePos, (iCommendEndPos - iCommentStartPos));

                    if (iFilePos > 0)
                    {
                        //file= found, valid include. process file included
                        string[] sIncludeFile = sInputScript.Substring(iFilePos, (iCommendEndPos - iCommentStartPos)).Split(new char[] { '\"' });

                        //File name should be sIncludeFile[1]
                        //FileStream fsIncludeFile = new FileStream(sIncludeFile[1], FileMode.Open, FileAccess.Read, FileShare.Read);
                        string sIncludeDirectory = sFilePath.Substring(0, (sFilePath.LastIndexOf(@"/") + 1));
                        StreamReader oSrIncludeFile = new StreamReader(sIncludeDirectory + sIncludeFile[1]);
                        sOutputScript.Replace(sInputScript.Substring(iCommentStartPos, ((iCommendEndPos + 4) - iCommentStartPos)), oSrIncludeFile.ReadToEnd().ToString());
                        oSrIncludeFile.Close();
                    }

                    //Search for HTML comment start tag <!--
                    iCommentStartPos = sInputScript.IndexOf("<!--", (iCommentStartPos + 4));
                }
            }

            return sOutputScript.ToString();
        }

        #endregion
    }
}
