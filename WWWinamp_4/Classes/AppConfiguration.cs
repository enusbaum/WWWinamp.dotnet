using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Net;
using System.Net.Sockets; // For Sockets Code

namespace ENusbaum.Applications.WWWinamp.Classes
{
    static class AppConfiguration
    {
        #region Public Variables

        public static string configProgramName
        {
            get { return "WWWinamp"; }
        }

        public static string configProgramVersion
        {
            get { return "4.1 Build " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Build.ToString(); }
        }

        public static string configProgramAuthor
        {
            get { return "(c)2004-" + System.DateTime.Now.Year + " Eric Nusbaum, All Rights Reserved."; }
        }

        public static bool configWWWinampStartHTTP
        {
            get
            {
                string _sKey = "WWWinamp.StartHTTP";
                bool _blOutput = false;

                if (!ConfigurationOptionExists(_sKey)) return _blOutput;

                if (!bool.TryParse(ConfigurationManager.AppSettings[_sKey].ToString(), out _blOutput))
                {
                    LogHandler.LogError(new Exception(ValueNotFoundMessage(_sKey)));
                }

                return _blOutput;
            }
        }

        public static bool configWWWinampStartWCF
        {
            get
            {
                string _sKey = "WWWinamp.StartWCF";
                bool _blOutput = false;

                if (!ConfigurationOptionExists(_sKey)) return _blOutput;

                if (!bool.TryParse(ConfigurationManager.AppSettings[_sKey].ToString(), out _blOutput))
                {
                    LogHandler.LogError(new Exception(ValueNotFoundMessage(_sKey)));
                }
                return _blOutput;
            }               
        }

        public static string configWinampHomeDirectory
        {

            get
            {
                string _sKey = "Winamp.ProgramDirectory";
                string _sOutput = string.Empty;

                if (!ConfigurationOptionExists(_sKey)) return _sOutput;

                _sOutput = ConfigurationManager.AppSettings[_sKey].ToString();

                if (String.IsNullOrEmpty(_sOutput))
                {
                    LogHandler.LogError(new Exception(ValueNotFoundMessage(_sKey)));
                }

                return _sOutput;
            }
        }

        public static string configWinampPlaylistDirectory
        {
            get
            {
                string _sKey = "Winamp.PlaylistDirectory";
                string _sOutput = string.Empty;

                if (!ConfigurationOptionExists(_sKey)) return _sOutput;

                _sOutput = ConfigurationManager.AppSettings[_sKey].ToString();

                if (String.IsNullOrEmpty(_sOutput))
                {
                    LogHandler.LogError(new Exception(ValueNotFoundMessage(_sKey)));
                }

                return _sOutput;
            }
        }

        public static string configWWWinampWCFListeningIP
        {
            get
            {
                string _sKey = "WWWinamp.WCF.ListeningIP";
                string _sOutput = string.Empty;

                if (!ConfigurationOptionExists(_sKey)) return _sOutput;

                _sOutput = ConfigurationManager.AppSettings[_sKey].ToString().ToLower();

                if (_sOutput.Equals("default"))
                {
                    IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());

                    //Loop until we find the first IPv4 address
                    for (int iLoop = 0; iLoop < ipEntry.AddressList.Length; iLoop++)
                    {
                        if (ipEntry.AddressList[iLoop].AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ipEntry.AddressList[iLoop].ToString();
                        }
                    }

                }
                else
                {
                    return _sOutput;
                }
                LogHandler.LogError(new Exception(ValueNotFoundMessage(_sKey)));
                return string.Empty;
            }
        }

        public static int configWWWinampWCFListeningPort
        {

            get
            {
                string _sKey = "WWWinamp.WCF.ListeningPort";
                Int16 _iOutput = 0;

                if (!ConfigurationOptionExists(_sKey)) return _iOutput;

                if (!Int16.TryParse(ConfigurationManager.AppSettings[_sKey].ToString(), out _iOutput))
                {
                    LogHandler.LogError(new Exception(ValueNotFoundMessage(_sKey)));
                }
                return _iOutput;
            }
        }

        public static string configWWWinampWCFAuthentication
        {
            get
            {
                string _sKey = "WWWinamp.WCF.Authentication";
                string _sOutput = string.Empty;

                if (!ConfigurationOptionExists(_sKey)) return _sOutput;

                _sOutput = ConfigurationManager.AppSettings[_sKey].ToString();

                if (String.IsNullOrEmpty(_sOutput))
                {
                    LogHandler.LogError(new Exception(ValueNotFoundMessage(_sKey)));
                }
                return _sOutput;
            }
        }

        public static bool configWWWinampWCFSendErrorsToClient
        {
            get
            {
                string _sKey = "WWWinamp.WCF.SendErrorsToClient";
                bool _blOutput = false;

                if (!ConfigurationOptionExists(_sKey)) return _blOutput;

                if (!bool.TryParse(ConfigurationManager.AppSettings[_sKey].ToString(), out _blOutput))
                {
                    LogHandler.LogError(new Exception(ValueNotFoundMessage(_sKey)));
                }
                return _blOutput;
            }    
        }

        public static bool configWWWinampHTTPAllowCompression
        {
            get 
            {
                string _sKey = "WWWinamp.HTTP.CompressionType";
                string _sOutput = string.Empty;

                if (!ConfigurationOptionExists(_sKey)) return false;

                _sOutput = ConfigurationManager.AppSettings[_sKey].ToString().ToLower();

                if (String.IsNullOrEmpty(_sOutput)) LogHandler.LogError(new Exception(ValueNotFoundMessage(_sKey)));
                if (_sOutput.Equals("none")) return false;
                return true; 
            }
        }

        public static string configWWWinampHTTPCompressionType
        {
            get
            {
                string _sKey = "WWWinamp.HTTP.CompressionType";
                string _sOutput = string.Empty;

                if (!ConfigurationOptionExists(_sKey)) return _sOutput;

                _sOutput = ConfigurationManager.AppSettings[_sKey].ToString().ToLower();

                if (!_sOutput.Equals("none")) return _sOutput;
                if (String.IsNullOrEmpty(_sOutput)) LogHandler.LogError(new Exception(ValueNotFoundMessage(_sKey)));
                return string.Empty;
            }
        }

        public static string configWWWinampHTTPListeningIP
        {
            get
            {
                string _sKey = "WWWinamp.HTTP.ListeningIP";
                string _sOutput = string.Empty;

                if (!ConfigurationOptionExists(_sKey)) return _sOutput;

                _sOutput = ConfigurationManager.AppSettings[_sKey].ToString().ToLower();

                if (_sOutput.Equals("default"))
                {
                    IPHostEntry ipEntry = Dns.GetHostEntry(Dns.GetHostName());

                    //Loop until we find the first IPv4 address
                    for (int iLoop = 0; iLoop < ipEntry.AddressList.Length; iLoop++)
                    {
                        if (ipEntry.AddressList[iLoop].AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ipEntry.AddressList[iLoop].ToString();
                        }
                    }
                }
                else
                {
                    return _sOutput;
                }
                if (String.IsNullOrEmpty(_sOutput)) LogHandler.LogError(new Exception(ValueNotFoundMessage(_sKey)));
                return string.Empty;
            }
        }

        public static int configWWWinampHTTPListeningPort
        {
            get
            {
                string _sKey = "WWWinamp.HTTP.ListeningPort";
                Int16 _iOutput = 0;

                if (!ConfigurationOptionExists(_sKey)) return _iOutput;

                if (!Int16.TryParse(ConfigurationManager.AppSettings[_sKey].ToString(), out _iOutput))
                {
                    LogHandler.LogError(new Exception(ValueNotFoundMessage(_sKey)));
                }
                return _iOutput;
            }
        }

        public static int configWWWinampHTTPResultsPerPage
        {
            get
            {
                string _sKey = "WWWinamp.HTTP.ResultsPerPage";
                Int16 _iOutput = 0;

                if (!ConfigurationOptionExists(_sKey)) return _iOutput;

                if (!Int16.TryParse(ConfigurationManager.AppSettings[_sKey].ToString(), out _iOutput))
                {
                    LogHandler.LogError(new Exception(ValueNotFoundMessage(_sKey)));
                }
                return _iOutput;
            }
        }

        public static string configWWWinampHTTPCompressionFileTypes
        {
            get
            {
                string _sKey = "WWWinamp.HTTP.CompressionFileTypes";
                string _sOutput = string.Empty;

                if (!ConfigurationOptionExists(_sKey)) return _sOutput;

                _sOutput = ConfigurationManager.AppSettings[_sKey].ToString();

                if (String.IsNullOrEmpty(_sOutput))
                {
                    LogHandler.LogError(new Exception(ValueNotFoundMessage(_sKey)));
                }
                return _sOutput.ToLower();
            }
        }

        public static string configWWWinampHTTPHomeDirectory
        {
            get
            {
                string _sKey = "WWWinamp.HTTP.HomeDirectory";
                string _sOutput = string.Empty;

                if (!ConfigurationOptionExists(_sKey)) return _sOutput;

                _sOutput = ConfigurationManager.AppSettings[_sKey].ToString();

                if (String.IsNullOrEmpty(_sOutput))
                {
                    LogHandler.LogError(new Exception(ValueNotFoundMessage(_sKey)));
                }

                return _sOutput.ToLower();
            }
        }

        public static string configWWWinampHTTPDefaultFile
        {
            get
            {
                string _sKey = "WWWinamp.HTTP.DefaultFile";
                string _sOutput = string.Empty;

                if (!ConfigurationOptionExists(_sKey)) return _sOutput;

                _sOutput = ConfigurationManager.AppSettings[_sKey].ToString();

                if (String.IsNullOrEmpty(_sOutput))
                {
                    LogHandler.LogError(new Exception(ValueNotFoundMessage(_sKey)));
                }
                return _sOutput.ToLower();
            }
        }

        public static string configWWWinampHTTPAdminLogin
        {
            get
            {
                string _sKey = "WWWinamp.HTTP.AdminLogin";
                string _sOutput = string.Empty;

                if (!ConfigurationOptionExists(_sKey)) return _sOutput;

                _sOutput = ConfigurationManager.AppSettings[_sKey].ToString();

                if (String.IsNullOrEmpty(_sOutput))
                {
                    LogHandler.LogError(new Exception(ValueNotFoundMessage(_sKey)));
                }
                return _sOutput;
            }
        }

        public static bool configWWWinampHTTPAllowDirectoryListing
        {
            get
            {
                string _sKey = "WWWinamp.HTTP.AllowDirectoryListing";
                bool _blOutput = false;

                if (!ConfigurationOptionExists(_sKey)) return _blOutput;

                if (!bool.TryParse(ConfigurationManager.AppSettings[_sKey].ToString(), out _blOutput))
                {
                    LogHandler.LogError(new Exception(ValueNotFoundMessage(_sKey)));
                }
                return _blOutput;
            }
        }

        public static string configWWWinampMediaHomeDirectory
        {
            get
            {
                string _sKey = "WWWinamp.Media.HomeDirectory";
                string _sOutput = string.Empty;

                if (!ConfigurationOptionExists(_sKey)) return _sOutput;

                _sOutput = ConfigurationManager.AppSettings[_sKey].ToString();

                if (String.IsNullOrEmpty(_sOutput))
                {
                    LogHandler.LogError(new Exception(ValueNotFoundMessage(_sKey)));
                }

                return _sOutput;
            }
        }

        public static string configWWWinampMediaFileTypes
        {
            get
            {
                string _sKey = "WWWinamp.Media.FileTypes";
                string _sOutput = string.Empty;

                if (!ConfigurationOptionExists(_sKey)) return _sOutput;

                _sOutput = ConfigurationManager.AppSettings[_sKey].ToString();

                if (String.IsNullOrEmpty(_sOutput))
                {
                    LogHandler.LogError(new Exception(ValueNotFoundMessage(_sKey)));
                }
                return _sOutput;
            }
        }

        public static string configWWWinampMediaCoverArtImage
        {
            get
            {
                string _sKey = "WWWinamp.Media.CoverArtImage";
                string _sOutput = string.Empty;

                if (!ConfigurationOptionExists(_sKey)) return _sOutput;

                _sOutput = ConfigurationManager.AppSettings[_sKey].ToString();

                if (String.IsNullOrEmpty(_sOutput))
                {
                    LogHandler.LogError(new Exception(ValueNotFoundMessage(_sKey)));
                }
                return _sOutput;
            }
        }

        #endregion

        #region Public Methods

        public static bool configWWWinampHTTPCommand(string sWWWinampCommand)
        {
            return Convert.ToBoolean(ConfigurationSettings.AppSettings["WWWinamp.HTTP.Commands." + sWWWinampCommand].ToString());
        }

        #endregion

        #region Private Methods

        private static string ValueNotFoundMessage(string sKey)
        {
            return "The configuration option \"" + sKey + "\" was not found or was invalid. Please verify that it exists and that it's value is correct then restart WWWinamp.";
        }

        private static string DirectoryorFileNotFoundMessage(string sKey)
        {
            return "The directory or file specified in \"" + sKey + "\" was not found.";
        }

        private static bool ConfigurationOptionExists(string sKey)
        {
            if (ConfigurationManager.AppSettings[sKey] == null)
            {
                LogHandler.LogError(new Exception(ValueNotFoundMessage(sKey)));
                return false;
            }
            return true;
        }

        #endregion
    }
}
