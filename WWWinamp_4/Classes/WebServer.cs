using System;
using System.Collections.Generic;
using System.Text;

//Required for Web Server Module
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Drawing;


namespace ENusbaum.Applications.WWWinamp.Classes
{
    static class WebServer
    {

        #region Public Variables
        public enum Status
        {
            Started,
            Stopped
        }

        public static string ListeningIP
        {
            get { return privListeningIP.ToString(); }
            set { privListeningIP = IPAddress.Parse(value); }

        }

        public static Status ListeningStatus
        {
            get { return privListeningStatus; }
            set { privListeningStatus = value; }
        }

        public static int ListeningPort
        {
            get { return privListeningPort; }
            set { privListeningPort = value; }
        }
        #endregion

        #region Private Variables

        private static TcpListener myListener;
        private static IPAddress privListeningIP;
        private static int privListeningPort;
        private static Status privListeningStatus;

        private static Object threadLock = new Object();
        private static Thread thWWWinampServer = new Thread(new ThreadStart(WWWinampListen));

        #endregion

        #region Public Methods

        public static bool StartListener()
        {
            try
            {
                if (ListeningStatus == Status.Stopped)
                {
                    //start listing on the given port
                    myListener = new TcpListener(privListeningIP, privListeningPort);
                    myListener.ExclusiveAddressUse = true;
                    myListener.Start();
                    LogHandler.LogEvent("Web Service Started at http://" + privListeningIP.ToString() + ":" + privListeningPort.ToString());

                    //start the thread which calls the method 'StartListen'
                    thWWWinampServer.Start();

                    //Set Status
                    ListeningStatus = Status.Started;
                }
                return true;
            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
                return false;
            }
        }

        public static bool StopListener()
        {
            try
            {
                if (ListeningStatus == Status.Started)
                {
                    //Set Status
                    ListeningStatus = Status.Stopped;

                    //Close the Listener
                    myListener.Stop();

                    LogHandler.LogEvent("Web Service Stopped");
                }
                return true;
            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
                return false;
            }
        }

        #endregion

        #region Private Methods

        private static void AcceptConnection()
        {
            //Accept a new connection
            Socket mySocket = myListener.AcceptSocket();

            if (mySocket.Connected)
            {
                //Client Connected

                //make a byte array and receive data from the client 
                Byte[] bReceive = new Byte[mySocket.ReceiveBufferSize];
                int i = mySocket.Receive(bReceive, bReceive.Length, 0);

                //Convert Byte to String
                string sHTTPRequest = Encoding.ASCII.GetString(bReceive);

                //Extract information from HTTP Request Headers
                string sHTTPRequestType = ClientHTTPRequest_RequestType(sHTTPRequest);  //HTTP Request Type (GET/POST)

                //If HTTP Request is an unknown type, close the connection.
                if (sHTTPRequestType == "UNKNOWN") { mySocket.Close(); return; }

                //Verify Request
                if (!ClientHTTPRequest_Validate(sHTTPRequest, ref mySocket)) return;

                //Process any commands sent to WinAmp (Winamp Commands, Download File from Library)
                if (!ClientHTTPRequest_ProcessWinAmpCommand(sHTTPRequest, ref mySocket)) return;

                //Process any commands for WWWinamp (Login, etc.)
                if (!ClientHTTPRequest_ProcessWWWinampCommand(sHTTPRequest, ref mySocket)) return;

                //Send Content to Browser
                ServerHTTPResponse_SendContent(sHTTPRequest, ref mySocket);

                //Close Socket
                if (mySocket.Connected) mySocket.Close();
            }

        }

        /// <summary>
        ///     Parses the requested file extension and returns the MIME type of that file.
        /// </summary>
        /// <param name="sRequestedFile">string -- File requested by client</param>
        /// <returns>string -- MIME type of requested file</returns>
        private static string Server_GetMimeType(string sRequestedFile)
        {
            try
            {
                //if the file name isnt blank (404) it's not a directory being requested
                if ((!String.IsNullOrEmpty(sRequestedFile) && !sRequestedFile.EndsWith("/")) && sRequestedFile.IndexOf(".") > -1)
                {
                    string sFileExt = string.Empty;

                    // Convert to lowercase
                    int iStartPos = sRequestedFile.LastIndexOf(".");
                    sFileExt = sRequestedFile.Substring(iStartPos).ToLower();

                    //Find MIME type for that file extension
                    switch (sFileExt)
                    {
                        case (".ai"): return "application/postscript";

                        case (".aif"):
                        case (".aifc"):
                        case (".aiff"):
                            return "audio/x-aiff";

                        case (".asc"):
                        case (".c"):
                        case (".cc"):
                        case (".f"):
                        case (".f90"):
                        case (".h"):
                        case (".hh"):
                        case (".m"):
                        case (".txt"):
                            return "text/plain";

                        case (".au"): return "audio/basic";
                        case (".avi"): return "video/x-msvideo";

                        case (".bcpio"):
                        case (".bin"):
                        case (".lha"):
                        case (".lzh"):
                        case (".class"):
                        case (".dms"):
                        case (".exe"):
                            return "application/octet-stream";

                        case (".ccad"): return "application/clariscad";
                        case (".cdf"): return "application/x-netcdf";
                        case (".cpio"): return "application/x-cpio";
                        case (".cpt"): return "application/mac-compactpro";
                        case (".csh"): return "application/x-csh";
                        case (".css"): return "text/css";

                        case (".dcr"):
                        case (".dir"):
                            return "application/x-director";


                        case (".doc"): return "application/msword";
                        case (".drw"): return "application/drafting";
                        case (".dvi"): return "application/x-dvi";
                        case (".dwg"): return "application/acad";
                        case (".dxf"): return "application/dxf";
                        case (".dxr"): return "application/x-director";
                        case (".eps"): return "application/postscript";
                        case (".etx"): return "text/x-setext";
                        case (".ez"): return "application/andrew-inset";
                        case (".fli"): return "video/x-fli";
                        case (".gif"): return "image/gif";
                        case (".gtar"): return "application/x-gtar";
                        case (".gz"): return "application/x-gzip";

                        case (".hdf"): return "application/x-hdf";

                        case (".hqx"): return "application/mac-binhex40";

                        case (".htm"):
                        case (".html"):
                            return "text/html";

                        case (".ice"): return "x-conference/x-cooltalk";
                        case (".ief"): return "image/ief";
                        case (".iges"): return "model/iges";
                        case (".igs"): return "model/iges";
                        case (".ips"): return "application/x-ipscript";
                        case (".ipx"): return "application/x-ipix";

                        case (".jpe"):
                        case (".jpeg"):
                        case (".jpg"):
                            return "image/jpeg";

                        case (".js"): return "application/x-javascript";
                        case (".kar"): return "audio/midi";
                        case (".latex"): return "application/x-latex";

                        case (".lsp"): return "application/x-lisp";


                        case (".man"): return "application/x-troff-man";
                        case (".me"): return "application/x-troff-me";
                        case (".mesh"): return "model/mesh";
                        case (".mid"): return "audio/midi";
                        case (".midi"): return "audio/midi";
                        case (".mif"): return "application/vnd.mif";
                        case (".mime"): return "www/mime";
                        case (".mov"): return "video/quicktime";
                        case (".movie"): return "video/x-sgi-movie";

                        case (".mp2"):
                        case (".mp3"):
                        case (".mpe"):
                        case (".mpeg"):
                        case (".mpg"):
                        case (".mpga"):
                            return "audio/mpeg";

                        case (".ms"): return "application/x-troff-ms";
                        case (".msh"): return "model/mesh";
                        case (".nc"): return "application/x-netcdf";
                        case (".oda"): return "application/oda";
                        case (".pbm"): return "image/x-portable-bitmap";
                        case (".pdb"): return "chemical/x-pdb";
                        case (".pdf"): return "application/pdf";
                        case (".pgm"): return "image/x-portable-graymap";
                        case (".pgn"): return "application/x-chess-pgn";
                        case (".png"): return "image/png";
                        case (".pnm"): return "image/x-portable-anymap";
                        case (".ppm"): return "image/x-portable-pixmap";

                        case (".pot"):
                        case (".pps"):
                        case (".ppt"):
                        case (".ppz"):
                            return "application/mspowerpoint";

                        case (".pre"): return "application/x-freelance";
                        case (".prt"): return "application/pro_eng";
                        case (".ps"): return "application/postscript";
                        case (".qt"): return "video/quicktime";
                        case (".ra"): return "audio/x-realaudio";
                        case (".ram"): return "audio/x-pn-realaudio";
                        case (".ras"): return "image/cmu-raster";
                        case (".rgb"): return "image/x-rgb";
                        case (".rm"): return "audio/x-pn-realaudio";
                        case (".roff"): return "application/x-troff";
                        case (".rpm"): return "audio/x-pn-realaudio-plugin";
                        case (".rtf"): return "text/rtf";
                        case (".rtx"): return "text/richtext";
                        case (".scm"): return "application/x-lotusscreencam";
                        case (".set"): return "application/set";
                        case (".sgm"): return "text/sgml";
                        case (".sgml"): return "text/sgml";
                        case (".sh"): return "application/x-sh";
                        case (".shar"): return "application/x-shar";
                        case (".silo"): return "model/mesh";

                        case (".sit"):
                        case (".skd"):
                        case (".skm"):
                        case (".skp"):
                        case (".skt"):
                            return "application/x-koan";

                        case (".smi"):
                        case (".smil"):
                            return "application/smil";

                        case (".snd"): return "audio/basic";
                        case (".sol"): return "application/solids";
                        case (".spl"): return "application/x-futuresplash";
                        case (".src"): return "application/x-wais-source";
                        case (".step"): return "application/STEP";
                        case (".stl"): return "application/SLA";
                        case (".stp"): return "application/STEP";
                        case (".sv4cpio"): return "application/x-sv4cpio";
                        case (".sv4crc"): return "application/x-sv4crc";
                        case (".swf"): return "application/x-shockwave-flash";
                        case (".t"): return "application/x-troff";
                        case (".tar"): return "application/x-tar";
                        case (".tcl"): return "application/x-tcl";
                        case (".tex"): return "application/x-tex";

                        case (".texi"):
                        case (".texinfo"):
                            return "application/x-texinfo";

                        case (".tif"):
                        case (".tiff"):
                            return "image/tiff";

                        case (".tr"): return "application/x-troff";
                        case (".tsi"): return "audio/TSP-audio";
                        case (".tsp"): return "application/dsptype";
                        case (".tsv"): return "text/tab-separated-values";
                        case (".unv"): return "application/i-deas";
                        case (".ustar"): return "application/x-ustar";
                        case (".vcd"): return "application/x-cdlink";
                        case (".vda"): return "application/vda";

                        case (".viv"):
                        case (".vivo"):
                            return "video/vnd.vivo";

                        case (".vrml"): return "model/vrml";
                        case (".wav"): return "audio/x-wav";
                        case (".wrl"): return "model/vrml";
                        case (".xbm"): return "image/x-xbitmap";

                        case (".xlc"):
                        case (".xll"):
                        case (".xlm"):
                        case (".xls"):
                        case (".xlw"):
                            return "application/vnd.ms-excel";

                        case (".xml"): return "text/xml";
                        case (".xpm"): return "image/x-xpixmap";
                        case (".xwd"): return "image/x-xwindowdump";
                        case (".xyz"): return "chemical/x-pdb";
                        case (".zip"): return "application/zip";

                        default:
                            return "unknown/unknown";
                    }
                }
                else
                {
                    return "text/html";
                }

            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
                return "";
            }
        }


        /// <summary>
        ///     Checks to see if the requested file is restricted, such as an .htpasswd file
        /// </summary>
        /// <param name="sHTTPRequest"></param>
        /// <returns>bool -- true if the file requested is restricted and forbidden</returns>
        private static bool Client_IsRequestingRestrictedFile(string sHTTPRequest)
        {
            try
            {
                //We use a switch here for future versions, where more files may be added.
                switch (ClientHTTPRequest_RequestedFile(sHTTPRequest).ToLower())
                {
                    case (".htpasswd"): //contains passwords and user information for the current directory
                        return true;
                }
                return false;
            }
            catch (Exception e)
            {
                LogHandler.LogError(e, sHTTPRequest);
                return false;
            }
        }


        /// <summary>
        ///     Sends the HTTP Response header to the client connection
        /// </summary>
        /// <param name="sHTTPRequest"></param>
        /// <param name="iTotBytes"></param>
        /// <param name="sStatusCode"></param>
        /// <param name="mySocket"></param>
        /// <param name="sFileName"></param>
        private static void ServerHTTPResponse_Header(string sHTTPRequest, long iTotBytes, string sStatusCode, ref Socket mySocket, string sFileName)
        {
            try
            {
                //Log The Request
                if (mySocket.Connected) LogHandler.LogAccess(DateTime.Now.ToString() + "\t" + mySocket.RemoteEndPoint.ToString() + "\t" + sStatusCode + "\t" + ClientHTTPRequest_RequestedURL(sHTTPRequest) + "\t" + ClientHTTPRequest_UserAgent(sHTTPRequest));
                
                StringBuilder sHTTPResponse = new StringBuilder("");
                sHTTPResponse.Append(ClientHTTPRequest_HTTPVersion(sHTTPRequest) + " " + sStatusCode + "\r\n");
                sHTTPResponse.Append("Server: " +  AppConfiguration.configProgramName + AppConfiguration.configProgramVersion + "\r\n");
                if (sStatusCode == "401 Unauthorized") sHTTPResponse.Append("WWW-Authenticate: basic realm=\"WWWinamp v4.0 Login\"\r\n");   //401, so include AUTH header
                sHTTPResponse.Append("Content-Type: " + Server_GetMimeType(sFileName) + "\r\n");
                sHTTPResponse.Append("Content-Length: " + iTotBytes + "\r\n");
                if (AppConfiguration.configWWWinampHTTPAllowCompression && ClientHTTPRequest_AcceptedEncoding(sHTTPRequest) && CompressionAllowed(sFileName)) sHTTPResponse.Append("Content-Encoding: " + AppConfiguration.configWWWinampHTTPCompressionType + "\r\n");
                sHTTPResponse.Append("Content-Disposition: filename=\"" + sFileName + "\"\r\n\r\n");
                Byte[] bHTTPResponse = Encoding.ASCII.GetBytes(sHTTPResponse.ToString());
                ServerHTTPResponse_SendData(bHTTPResponse, ref mySocket);
            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
            }
        }

        /// <summary>
        ///     Sends the data to the provided socket.
        /// </summary>
        /// <param name="bSendData"></param>
        /// <param name="mySocket"></param>
        private static void ServerHTTPResponse_SendData(Byte[] bSendData, ref Socket mySocket)
        {
            try
            {
                if (mySocket.Connected)
                {

                    int numBytes = mySocket.Send(bSendData, bSendData.Length, SocketFlags.None);
                    if (numBytes <= 0)
                    {
                        //Socket Error
                        LogHandler.LogEvent("Socket Error");
                    }
                    else
                    {
                        //Bytes Sent
                    }
                }
                else
                {
                    //Connection Dropped
                    LogHandler.LogEvent("Connection Dropped");
                }
            }
            catch (SocketException e)
            {
                //Must be a Socket Error, close socket
                mySocket.Close();
                LogHandler.LogError(e);

            }
        }

        /// <summary>
        ///     Depedning on the Value HTTPCompressionType (defined in WWWINAMP.XML), it will
        ///     apply GZIP or DEFLATE compression to the byte array passed to CompressData().
        /// </summary>
        /// <param name="bInput"></param>
        /// <returns>byte[] -- byte array containing compressed data</returns>
        private static byte[] CompressData(byte[] bInput)
        {
            try
            {
                //The Memory Stream is where we'll store the newly compressed data
                MemoryStream msMemoryStream = new MemoryStream();

                if (AppConfiguration.configWWWinampHTTPCompressionType == "gzip")
                {
                     
                    //Create a GZIP Compression stream
                    GZipStream compressedStream = new GZipStream(msMemoryStream, CompressionMode.Compress, true);

                    //Write the compressed data to the memory stream
                    compressedStream.Write(bInput, 0, bInput.Length);

                    // Close the Compression stream
                    compressedStream.Close();
                }
                if (AppConfiguration.configWWWinampHTTPCompressionType == "deflate")
                {
                    //Create a DEFLATE compreesion stream
                    DeflateStream compressedStream = new DeflateStream(msMemoryStream, CompressionMode.Compress, true);

                    //Write the compressed data to the memory stream
                    compressedStream.Write(bInput, 0, bInput.Length);

                    // Close the Compression stream
                    compressedStream.Close();
                }

                return msMemoryStream.ToArray();
            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
                return new byte[] { 0 };
            }

        }

        /// <summary>
        ///     Checks to see if the file being requested is allowed to be compressed.
        ///     Defined in app.config under tag WWWinamp.HTTP.CompressionFileTypes
        /// </summary>
        /// <param name="sFileName"></param>
        /// <returns>bool:True if the file is allowed to be compressed using CompressData()</returns>
        private static bool CompressionAllowed(string sFileName)
        {
            try
            {
                if (String.IsNullOrEmpty(sFileName) || sFileName.EndsWith("/")) return false;  //404 or Directory List

                int iStartPos = sFileName.ToLower().LastIndexOf(".");
                string sFileExt = sFileName.Substring(iStartPos);

                if (AppConfiguration.configWWWinampHTTPCompressionFileTypes.IndexOf(sFileExt) > -1) return true;

                return false;
            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
                return false;
            }
        }

        /// <summary>
        ///     Inspects the Accept-Encoding header in the HTTP Request to see if the client can
        ///     support the HTTP compression method specified in app.config under WWWinamp.HTTP.CompressionMode (if specified).
        /// </summary>
        /// <param name="sHTTPRequest"></param>
        /// <returns>bool:true -- Client supports currently specified compression method</returns>
        private static bool ClientHTTPRequest_AcceptedEncoding(string sHTTPRequest)
        {
            try
            {
                if (sHTTPRequest.IndexOf("Accept-Encoding: ") > 0)
                {
                    int iStartPos = sHTTPRequest.IndexOf("Accept-Encoding: ") + 17;    //Character Where "Accept-Encoding" starts
                    int iEndPos = sHTTPRequest.IndexOf("\r\n", iStartPos) - iStartPos;  //Number of characters under end of "Accept-Encoding"
                    string sAcceptedEncoding = sHTTPRequest.Substring(iStartPos, iEndPos);  //String storing supported Encoding methods

                    string[] sAcceptedEncodingArray = sAcceptedEncoding.Split(new char[] { ',' });  //Split the string into an array based on ","
                    foreach (string sAcceptedEncodingType in sAcceptedEncodingArray)    // Loop through the array and see if the Accepted Encoding matches the one we're using
                    {
                        if (sAcceptedEncodingType == AppConfiguration.configWWWinampHTTPCompressionType ) return true; //Match found, return true
                    }
                }
                return false;   //Accept-Encoding Header not found or values do not match current compression.
            }
            catch (Exception e)
            {
                LogHandler.LogError(e, sHTTPRequest);
                return false;
            }
        }

        /// <summary>
        ///     Extracts the HTML version supported by the client browser.
        /// </summary>
        /// <param name="sHTTPRequest"></param>
        /// <returns>string -- HTTP/1.0 or HTTP/1.1 usually</returns>
        private static string ClientHTTPRequest_HTTPVersion(string sHTTPRequest)
        {
            try
            {
                int iStartPos = sHTTPRequest.IndexOf("HTTP", 1);
                return sHTTPRequest.Substring(iStartPos, 8);
            }
            catch (Exception e)
            {
                LogHandler.LogError(e, sHTTPRequest);
                return "";
            }
        }

        /// <summary>
        ///     Extracts the Requested file name and path from the HTTP request. 
        ///     If the URL is just a path (i.e.: http://www.enusbaum.com/), then 
        ///     it'll return the default file for that path, which is specified
        ///     in WWWINAMP.XML.
        /// </summary>
        /// <param name="sHTTPRequest"></param>
        /// <returns>string -- file name of file requested in the URL (i.e.: index.html)</returns>
        private static string ClientHTTPRequest_RequestedFile(string sHTTPRequest)
        {
            try
            {
                string sDirName = string.Empty;

                int iEndPos = sHTTPRequest.IndexOf("HTTP", 1); //End Position of the requested file
                if (iEndPos == -1) return "";   //HTTP not found? Can't be a valid HTTP Request

                if (sHTTPRequest.IndexOf("?", 1) < iEndPos && sHTTPRequest.IndexOf("?", 1) > -1) iEndPos = sHTTPRequest.IndexOf("?", 1) + 1;  //If There's a ? before the HTTP, means the URL also had variables passed

                string sRequest = sHTTPRequest.Substring(0, iEndPos - 1);

                sRequest.Replace("/", "\\");    //Replace any instances of backslashes with forward

                string sLocalDir = ClientHTTPRequest_RequestedFileLocalPath(sHTTPRequest);
                string sRequestedObject = sRequest.Substring((sRequest.LastIndexOf("/") + 1), (sRequest.Length - (sRequest.LastIndexOf("/") + 1)));

                //Files get Priority over Directories, so check to see if a file exists by by that name first
                if (File.Exists(sLocalDir + sRequestedObject) || ClientHTTPRequest_EmbeddedResource(sHTTPRequest))
                {
                    return sRequestedObject;    //File is found by that name
                }

                if (!sDirName.StartsWith("/")) sDirName += "/";
                if (!sDirName.EndsWith("/")) sDirName += "/";

                if (Directory.Exists(sLocalDir))
                {
                    //Directory Exists, is the default file in that directory?
                    if (File.Exists(sLocalDir + sDirName + AppConfiguration.configWWWinampHTTPDefaultFile))
                    {
                        return AppConfiguration.configWWWinampHTTPDefaultFile; //Default File Found! Reutrn it.
                    }
                }

                return ""; //Neither Exist? Return blank
            }
            catch (Exception e)
            {
                LogHandler.LogError(e, sHTTPRequest);
                return "";
            }
        }

        /// <summary>
        ///     Extracts the location of the file requested (i.e.: http://www.enusbaum.com/images/)
        ///     and returns the physical path to that folder (i.e.: c:\WWWinamp\images\).
        /// </summary>
        /// <param name="sHTTPRequest"></param>
        /// <returns>string -- physical path to folder specified in the URL (i.e.: c:\WWWinamp\images\)</returns>
        private static string ClientHTTPRequest_RequestedFileLocalPath(string sHTTPRequest)
        {
            try
            {
                string sDirName = string.Empty;

                int iEndPos = sHTTPRequest.IndexOf("HTTP", 1); //End Position of the requested file
                if (iEndPos == -1) return "";   //HTTP not found? Can't be a valid HTTP Request
                
                if (sHTTPRequest.IndexOf("?", 1) < iEndPos && sHTTPRequest.IndexOf("?", 1) > -1) iEndPos = sHTTPRequest.IndexOf("?", 1) + 1;  //If There's a ? before the HTTP, means the URL also had variables passed

                string sRequest = sHTTPRequest.Substring(0, iEndPos - 1);

                sRequest.Replace("/", "\\");    //Replace any instances of backslashes with forward

                //Extract the entire path to see if it is a directory in itself
                if (Directory.Exists(AppConfiguration.configWWWinampHTTPHomeDirectory + sRequest.Substring(sRequest.IndexOf("/"), (sRequest.Length - sRequest.IndexOf("/")))))
                {
                    sDirName = sRequest.Substring(sRequest.IndexOf("/"), (sRequest.Length - sRequest.IndexOf("/"))) + "/";
                }
                
                //See if they're requesting a FILE that exists
                if (File.Exists(AppConfiguration.configWWWinampHTTPHomeDirectory + sRequest.Substring(sRequest.IndexOf("/"), (sRequest.Length - sRequest.IndexOf("/")))))
                {
                    sDirName = sRequest.Substring(sRequest.IndexOf("/"), sRequest.LastIndexOf("/") - 3);
                }

                //Directory NOR file exist?? Return 404
                if (String.IsNullOrEmpty(sDirName)) return "";

                //Set Local Directory that is being queried
                string sLocalDir = AppConfiguration.configWWWinampHTTPHomeDirectory + sDirName;

                if (Directory.Exists(sLocalDir + "/") || sDirName == "/icons/")
                {
                    return sLocalDir;    //Directory is found, so sending default file specified in WWWINAMP.XML
                }

                return ""; //Neither Exist? Return blank, we'll return it 404
            }
            catch (Exception e)
            {
                LogHandler.LogError(e, sHTTPRequest);
                return "";
            }
        }


        /// <summary>
        ///     Extracts the directory requested in the URL
        /// </summary>
        /// <param name="sHTTPRequest"></param>
        /// <returns>string -- containing the directory requeted in the URL (i.e.: http://www.enusbaum.com/images/test.gif would return /images/)</returns>
        private static string ClientHTTPRequest_RequestedFileURLPath(string sHTTPRequest)
        {
            try
            {
                int iEndPos = sHTTPRequest.IndexOf("HTTP", 1); //End Position of the requested file
                if (iEndPos == -1) return "";   //HTTP not found? Can't be a valid HTTP Request

                string sRequest = sHTTPRequest.Substring(0, iEndPos - 1);

                sRequest.Replace("/", "\\");    //Replace any instances of backslashes with forward

                //Extract The directory Name Requested in the URL
                return sRequest.Substring(sRequest.IndexOf("/"), sRequest.LastIndexOf("/") - 3);
            }
            catch (Exception e)
            {
                LogHandler.LogError(e, sHTTPRequest);
                return "";
            }
        }

        /// <summary>
        ///     Extracts the URL variable or POST string from the HTTP request.
        /// </summary>
        /// <param name="sHTTPRequest"></param>
        /// <returns>string -- string from the URL after the "?", containing variables and their values.</returns>
        private static string ClientHTTPRequest_Variables(string sHTTPRequest)
        {
            try
            {
                if (sHTTPRequest.Substring(0, 3) == "GET") //GET Variables will be passed in the URL
                {
                    if (sHTTPRequest.IndexOf("/") < sHTTPRequest.IndexOf("?") && sHTTPRequest.IndexOf("?") < sHTTPRequest.IndexOf("\r\n"))
                    {
                        //If Statement:
                        //If the URL does not have a "?" as part of the URL requested: /what?/index.html
                        //If the request has a URL variable in the first line
                        return sHTTPRequest.Substring(sHTTPRequest.IndexOf("?"), (sHTTPRequest.IndexOf(" HTTP") - sHTTPRequest.IndexOf("?")));
                    }
                }
                if (sHTTPRequest.Substring(0, 4) == "POST") //POST Variables will be in the body of the request after the header
                {
                    if (sHTTPRequest.IndexOf("\r\n\r\n") > -1)
                    {
                        int iEndPos = sHTTPRequest.IndexOf("\0");   //POST variables will be followed by nulls "\0" because of the byte array buffer
                        int iStartPos = sHTTPRequest.IndexOf("\r\n\r\n") + 4;   //Variables start after request header
                        return "?" + sHTTPRequest.Substring(iStartPos, (iEndPos - iStartPos));
                    }
                }
                return "";
            }
            catch (Exception e)
            {
                LogHandler.LogError(e, sHTTPRequest);
                return "";
            }
        }

        /// <summary>
        ///     Extracts the Value of a single variable from the variable string passed
        /// </summary>
        /// <param name="sVariables">string -- Variable string generated by ClientHTTPRequest_Variables()</param>
        /// <param name="sVariable">string -- Variable who's value is to be returned</param>
        /// <returns>string -- Value of sVariable</returns>
        private static string ClientHTTPRequest_VariableValue(string sVariables, string sVariable)
        {
            //Does the string start with a "?", if so, remove it
            if (sVariables.StartsWith("?")) sVariables = sVariables.Substring(1, sVariables.Length - 1);

            //Split the incoming variable array on the "&" character
            string[] sVariableArray = sVariables.Split(new char[] { '&' });

            //No Vairables found in the string?
            if (sVariableArray.Length == 0) return "";

            for (int iLoop = 0; iLoop < sVariableArray.Length; iLoop++)
            {
                if (sVariableArray[iLoop].StartsWith(sVariable + "="))
                {
                    return sVariableArray[iLoop].Substring(sVariableArray[iLoop].IndexOf("=") + 1, (sVariableArray[iLoop].Length - (sVariableArray[iLoop].IndexOf("=") + 1)));
                }
            }
            return "";
        }

        /// <summary>
        ///     Inspects the Authorization header to see if the user is logged in as an admin.
        ///     The username and password is specified in WWWINAMP.XML
        /// </summary>
        /// <param name="sHTTPRequest"></param>
        /// <returns>bool:true -- User is logged in as admin</returns>
        private static bool ClientHTTPRequest_ProcessAuthWWWinampAdmin(string sHTTPRequest)
        {
            try
            {
                int iStartPos = sHTTPRequest.IndexOf("Authorization: Basic ", 1) + 21;
                if (iStartPos > -1)
                {
                    int iLength = sHTTPRequest.IndexOf("\r", iStartPos) - iStartPos;
                    string sHttpAuth = sHTTPRequest.Substring(iStartPos, iLength);
                    byte[] binaryAdminLogin = Encoding.ASCII.GetBytes(AppConfiguration.configWWWinampHTTPAdminLogin);
                    if (sHttpAuth == Convert.ToBase64String(binaryAdminLogin)) return true;
                }
                return false;
            }
            catch (Exception e)
            {
                LogHandler.LogError(e, sHTTPRequest);
                return false;
            }
        }

        /// <summary>
        ///     Authenticates user based on specified .htpasswd file
        /// </summary>
        /// <param name="sHTTPRequest"></param>
        /// <param name="sPasswordFile"></param>
        /// <returns>bool -- true is user was authenticated successfully</returns>
        private static bool ClientHTTPRequest_ProcessHTTPAuth(string sHTTPRequest, string sPasswordFile)
        {
            try
            {
                int iStartPos = sHTTPRequest.IndexOf("Authorization: Basic ", 1);
                if (iStartPos > -1)
                {
                    //Found Client AUTH, now check password file for user
                    iStartPos += 21;
                    //Extract AUTH Header from HTTP Request
                    int iLength = sHTTPRequest.IndexOf("\r", iStartPos) - iStartPos;
                    string sHttpAuth = Encoding.ASCII.GetString(Convert.FromBase64String(sHTTPRequest.Substring(iStartPos, iLength)));


                    //Read Password file to string array
                    string[] sPasswordFileData = Encoding.ASCII.GetString(Functions.ReadLocalFileToByteArray(sPasswordFile)).Replace("\n", "").Split(new char[] { '\r' });  //Split the string into an array based on ","
                    foreach (string sPassword in sPasswordFileData)    // Loop through the array and see if the Accepted Encoding matches the one we're using
                    {
                        //See if the AUTH string submitted by the browser is in the .htpasswd file
                        if (sPassword.Contains("{SHA}"))
                        {
                            //SHA hash in .htpasswd
                            if (Convert_PlaintextAuthToSHAAuth(sHttpAuth).Equals(sPassword)) return true;   //Matches SHA Hash!
                        }
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                LogHandler.LogError(e, sHTTPRequest);
                return false;
            }
        }

        /// <summary>
        ///     Takes the input of a raw Auth (user:password) and converts it to an SHA1 Auth (user:{SHA1}hash)
        /// </summary>
        /// <param name="sAuth"></param>
        /// <returns>string -- containing the auth string with an SHA1 hashed password</returns>
        private static string Convert_PlaintextAuthToSHAAuth(string sAuth)
        {
            string[] sPassword = sAuth.Split(new char[] { ':' });  //Extract the password
            return sPassword[0] + ":{SHA}" + Convert.ToBase64String(Functions.Create_SHA1Hash(Encoding.ASCII.GetBytes(sPassword[1])));
        }

        /// <summary>
        ///     Verifies that a Authentication is present in the current HTTP Request
        /// </summary>
        /// <param name="sHTTPRequest"></param>
        /// <returns>bool -- True is the user is Authenticating or Authentication is present in the HTTP Request Header</returns>
        private static bool ClientHTTPRequest_HTTPAuth(string sHTTPRequest)
        {
            try
            {
                int iStartPos = sHTTPRequest.IndexOf("Authorization: Basic ", 1) + 21;
                if (iStartPos > -1) return true; //User is Authenticating or AUTH already present
                return false;
            }
            catch (Exception e)
            {
                LogHandler.LogError(e, sHTTPRequest);
                return false;
            }
        }

        /// <summary>
        ///     Extracts the USER-AGENT header from the HTTP Request
        /// </summary>
        /// <param name="sHTTPRequest"></param>
        /// <returns>string -- containing the USER-AGENT from the HTTP Request</returns>
        private static string ClientHTTPRequest_UserAgent(string sHTTPRequest)
        {
            try
            {
                int iStartPos = sHTTPRequest.IndexOf("User-Agent:", 1);
                if (iStartPos == -1) return "UNKNOWN AGENT";
                int iLength = sHTTPRequest.IndexOf("\n", iStartPos) - iStartPos;
                return sHTTPRequest.Substring(iStartPos, iLength);
            }
            catch (Exception e)
            {
                LogHandler.LogError(e, sHTTPRequest);
                return "";
            }
        }

        /// <summary>
        ///     Extracts the URL requested in the HTTP Request
        /// </summary>
        /// <param name="sHTTPRequest"></param>
        /// <returns>string -- containing the URL requested (i.e. /images/file1.jpg)</returns>
        private static string ClientHTTPRequest_RequestedURL(string sHTTPRequest)
        {
            try
            {
                int iEndPos = sHTTPRequest.IndexOf("HTTP", 1); //End Position of the requested file
                if (iEndPos == -1) return "";   //HTTP not found? Can't be a valid HTTP Request

                return sHTTPRequest.Substring(4, iEndPos - 5);
            }
            catch (Exception e)
            {
                LogHandler.LogError(e, sHTTPRequest);
                return "";
            }
        }

        /// <summary>
        ///     Extracts the HTTP Request Type 
        /// </summary>
        /// <param name="sHTTPRequest"></param>
        /// <returns>string -- containing the HTTP Request Type (GET/POST or UNKNOWN)</returns>
        private static string ClientHTTPRequest_RequestType(string sHTTPRequest)
        {
            try
            {
                if (sHTTPRequest.Substring(0, 3) == "GET") return "GET";
                if (sHTTPRequest.Substring(0, 4) == "POST") return "POST";
                return "UNKNOWN";
            }
            catch (Exception e)
            {
                LogHandler.LogError(e, sHTTPRequest);
                return "";
            }
        }

        /// <summary>
        ///     Validates an HTTP Request Header to see if it meets the minimum requirements to be processed
        /// </summary>
        /// <param name="sHTTPRequest">string -- HTTP Request Header from Client</param>
        /// <param name="mySocket">Socket -- Socket connection to Client</param>
        /// <returns>bool -- false if HTTP request cannot be validated or contains incorrect data</returns>
        private static bool ClientHTTPRequest_Validate(string sHTTPRequest, ref Socket mySocket)
        {
            try
            {
                //Supported/Valid Request Type and URL is valid
                if (ClientHTTPRequest_RequestType(sHTTPRequest) == "UNKNOWN" || !ClientHTTPRequest_ValidateURL(sHTTPRequest))
                {
                    ServerHTTPResponse_Error(ServerHTTPResponse_GenerateHTTPClientErrorMessage("400 Bad Request","Bad Request","Due to malformed syntax, the request could not be understood by the server. The client should not repeat the request without modifications."), "400 Bad Request", ClientHTTPRequest_RequestedFile(sHTTPRequest), sHTTPRequest, ref mySocket);
                    mySocket.Close();
                    return false;
                }

                //File Not Found
                if (!ClientHTTPRequest_RequestedFileExists(sHTTPRequest) && !ClientHTTPRequest_EmbeddedResource(sHTTPRequest))
                {
                    ServerHTTPResponse_Error(ServerHTTPResponse_GenerateHTTPClientErrorMessage("404 Not Found","Not Found","The requested URL " + ClientHTTPRequest_RequestedURL(sHTTPRequest) + " was not found on this server."), "404 Not Found", ClientHTTPRequest_RequestedFile(sHTTPRequest), sHTTPRequest, ref mySocket);
                    mySocket.Close();
                    return false;
                }

                //If Request is for a directory and directory listing is not allowed via the XML
                if (String.IsNullOrEmpty(ClientHTTPRequest_RequestedFile(sHTTPRequest)) && !AppConfiguration.configWWWinampHTTPAllowDirectoryListing)
                {
                    ServerHTTPResponse_Error(ServerHTTPResponse_GenerateHTTPClientErrorMessage("403 Forbidden","Forbidden","You don't have permission to access " + ClientHTTPRequest_RequestedFileURLPath(sHTTPRequest) + ClientHTTPRequest_RequestedFile(sHTTPRequest) + "\r\n on this server."), "403 Forbidden", ClientHTTPRequest_RequestedFile(sHTTPRequest), sHTTPRequest, ref mySocket);
                    mySocket.Close();
                    return false;
                }

                //Check to see if the directory requested or any of it's parents, up to the web root have a ".htpasswd" file.
                for (int iLoop = ClientHTTPRequest_RequestedFileLocalPath(sHTTPRequest).Length; iLoop > AppConfiguration.configWWWinampHTTPHomeDirectory .Length; iLoop--)
                {
                    if (File.Exists(ClientHTTPRequest_RequestedFileLocalPath(sHTTPRequest).Substring(0, ClientHTTPRequest_RequestedFileLocalPath(sHTTPRequest).Substring(0, iLoop).Replace("/", @"\").LastIndexOf(@"\")) + @"\.htpasswd"))
                    {
                        //Found .htpasswd

                        //Is the user already authenticating?
                        if (ClientHTTPRequest_HTTPAuth(sHTTPRequest))
                        {
                            //AUTH already present, validate it
                            if (!ClientHTTPRequest_ProcessHTTPAuth(sHTTPRequest, ClientHTTPRequest_RequestedFileLocalPath(sHTTPRequest).Substring(0, ClientHTTPRequest_RequestedFileLocalPath(sHTTPRequest).Substring(0, iLoop).Replace("/", @"\").LastIndexOf(@"\")) + @"\.htpasswd"))
                            {
                                //AUTH Failure, send Auth Header
                                ServerHTTPResponse_Error(ServerHTTPResponse_GenerateHTTPClientErrorMessage("401 Unauthorized", "Authorization Required", "This server could not verify that you are authorized to access the document requested. Either you supplied the wrong credentials (e.g., bad password), or your browser doesn't understand how to supply the credentials required."), "401 Unauthorized", ClientHTTPRequest_RequestedFile(sHTTPRequest), sHTTPRequest, ref mySocket);
                                mySocket.Close();
                                return false;
                            }
                        }
                        else
                        {
                            //Send Auth Header
                            ServerHTTPResponse_Error(ServerHTTPResponse_GenerateHTTPClientErrorMessage("401 Unauthorized","Authorization Required","This server could not verify that you are authorized to access the document requested. Either you supplied the wrong credentials (e.g., bad password), or your browser doesn't understand how to supply the credentials required."), "401 Unauthorized", ClientHTTPRequest_RequestedFile(sHTTPRequest), sHTTPRequest, ref mySocket);
                            mySocket.Close();
                            return false;
                        }
                    }

                }

                //Check to see that the requested file isnt restricted
                if (Client_IsRequestingRestrictedFile(sHTTPRequest))
                {
                    ServerHTTPResponse_Error(ServerHTTPResponse_GenerateHTTPClientErrorMessage("403 Forbidden","Forbidden","You don't have permission to access " + ClientHTTPRequest_RequestedFileURLPath(sHTTPRequest) + ClientHTTPRequest_RequestedFile(sHTTPRequest) + "\r\n on this server."), "403 Forbidden", ClientHTTPRequest_RequestedFile(sHTTPRequest), sHTTPRequest, ref mySocket);
                    mySocket.Close();
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                LogHandler.LogError(e, sHTTPRequest);
                return false;
            }
        }

        private static string ServerHTTPResponse_GenerateHTTPClientErrorMessage(string strErrorCode, string strTitle, string strDescription)
        {
            return "<!DOCTYPE HTML public static \"-//IETF//DTD HTML 2.0//EN\">\r\n<html><head>\r\n<title>" + strErrorCode + "</title>\r\n</head><body>\r\n<h1>" + strTitle + "</h1>\r\n<p>" + strDescription + "</p>\r\n</body></html><hr><address>" + AppConfiguration.configProgramName + " " + AppConfiguration.configProgramVersion + "</address>";
        }

        /// <summary>
        ///     Verified that the URL requested is valid and is not trying to exploit anything
        /// </summary>
        /// <param name="sHTTPRequest"></param>
        /// <returns></returns>
        private static bool ClientHTTPRequest_ValidateURL(string sHTTPRequest)
        {
            try
            {
                //Check for illegal parent pathing in the URL
                if (ClientHTTPRequest_RequestedURL(sHTTPRequest).IndexOf("/..") > -1) return false;

                return true;
            }
            catch (Exception e)
            {
                LogHandler.LogError(e, sHTTPRequest);
                return false;
            }
        }

        /// <summary>
        ///     Processes a WinAmp Command passed in the URL or POST Variables as "q" (i.e. ?q=play)
        /// </summary>
        /// <param name="sHTTPRequest"></param>
        /// <param name="mySocket"></param>
        /// <returns>bool -- successfully executed command</returns>
        private static bool ClientHTTPRequest_ProcessWinAmpCommand(string sHTTPRequest, ref Socket mySocket)
        {
            try
            {
                string sVariables = ClientHTTPRequest_Variables(sHTTPRequest); //Variables passed in the URL or POST

                //Extract WinAmp Command (if Submitted)
                if (sVariables.StartsWith("?q="))
                {
                    string sParsedCommand = ClientHTTPRequest_VariableValue(sVariables, "q").ToLower();
                    string sPassedValue = ClientHTTPRequest_VariableValue(sVariables, "value").ToLower();

                    //Verify this user has access to the command sent, if not, just exit the function.
                    if (!ClientHTTPRequest_CheckWinAmpCommandPermission(sHTTPRequest, sParsedCommand)) return true;

                    //if (sWinAmpCommand.ToLower().LastIndexOf("&value=") > -1) sPassedValue = sWinAmpCommand.Substring(sWinAmpCommand.IndexOf("&value=") + 7, (sWinAmpCommand.Length - (sWinAmpCommand.IndexOf("&value=") + 7)));

                    //Switch the Command Sent, we need to handle a few differently
                    switch (sParsedCommand)
                    {
                        case "download":
                            //Requesting to download file, extract the file and transmit it
                            string sRequestedLocalFile = LibraryController.dbMediaLibrary[Convert.ToInt16(sPassedValue)].FileName;
                            string sRequestedLocalFilePath = LibraryController.dbMediaLibrary[Convert.ToInt16(sPassedValue)].FilePath + @"\";

                            //Send Header
                            FileInfo oFI = new FileInfo(sRequestedLocalFilePath + sRequestedLocalFile);
                            ServerHTTPResponse_Header(sHTTPRequest, oFI.Length, "200 OK", ref mySocket, sRequestedLocalFile);

                            //Read/Send File
                            ServerHTTPResponse_SendData(Functions.ReadLocalFileToByteArray(sRequestedLocalFilePath + sRequestedLocalFile), ref mySocket);
                            return false;
                        default:
                            //Execute Command
                            WinAmpController.WinampCommand(sParsedCommand, sPassedValue);
                            break;
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                LogHandler.LogError(e, sHTTPRequest);
                return false;
            }
        }

        /// <summary>
        ///     Processes a command that is for WWWinamp (such as ?login)
        /// </summary>
        /// <param name="sHTTPRequest"></param>
        /// <param name="mySocket"></param>
        /// <returns>bool -- true is command is executed successfuly</returns>
        private static bool ClientHTTPRequest_ProcessWWWinampCommand(string sHTTPRequest, ref Socket mySocket)
        {
            try
            {
                string sVariables = ClientHTTPRequest_Variables(sHTTPRequest); //Variables passed in the URL or POST

                if(String.IsNullOrEmpty(sVariables)) return true;

                //User Authentication for Admin Login (if not already logged in)
                if (sVariables.StartsWith("?login") && !ClientHTTPRequest_ProcessAuthWWWinampAdmin(sHTTPRequest))
                {
                    //Format The Message
                    ServerHTTPResponse_Error(ServerHTTPResponse_GenerateHTTPClientErrorMessage("401 Unauthorized","Authorization Required","This server could not verify that you are authorized to access the document requested. Either you supplied the wrong credentials (e.g., bad password), or your browser doesn't understand how to supply the credentials required."), "401 Unauthorized", ClientHTTPRequest_RequestedFile(sHTTPRequest), sHTTPRequest, ref mySocket);
                    return false;
                }

                //Cover Image Requested
                if (sVariables.LastIndexOf("?PlaylistCover") > -1 || sVariables.LastIndexOf("?LibraryCover") > -1)
                {
                    byte[] bResponse = new byte[] { 0 };
                    string sPassedValue = ClientHTTPRequest_VariableValue(sVariables, "value");
                    string sCover = string.Empty;

                    //No Value was passed? Just close the connection as the request is invalid.
                    if (String.IsNullOrEmpty(sPassedValue))
                    {
                        mySocket.Close();
                        return false;
                    }

                    string sCoverArtFile = AppConfiguration.configWWWinampMediaCoverArtImage;

                    if (sVariables.StartsWith("?PlaylistCover"))
                    {
                        string sPlaylistItem = WinampPlaylist.PlaylistItems[Convert.ToInt16(sPassedValue)].FilePath;
                        //deprecated - string sPlaylistItem = WinAmpController.Playlist_GetItempPathByID(Convert.ToInt16(sPassedValue));

                        if (sPlaylistItem.StartsWith("http://") || string.IsNullOrEmpty(sPlaylistItem)) return false;

                        //Get Current Playlist Item Path
                        sCover = WinampPlaylist.PlaylistItems[Convert.ToInt16(sPassedValue)].FilePath;
                        //deprecated - sCover = sPlaylistItem.Substring(0, WinAmpController.Playlist_GetItempPathByID(Convert.ToInt16(sPassedValue)).LastIndexOf(@"\")) + @"\";

                        if (AppConfiguration.configWWWinampMediaCoverArtImage == "FIRST_FOUND")
                        {
                            sCoverArtFile = Server_FindFirstFileByExt(sCover, ".jpg");
                        }

                        sCover += sCoverArtFile;

                    }
                    if (sVariables.StartsWith("?LibraryCover"))
                    {
                        sCover = LibraryController.dbMediaLibrary[Convert.ToInt16(sPassedValue)].FilePath + @"\";

                        if (AppConfiguration.configWWWinampMediaCoverArtImage == "FIRST_FOUND")
                        {
                            sCoverArtFile = Server_FindFirstFileByExt(sCover, ".jpg");
                        }

                        sCover += sCoverArtFile;
                    }

                    if (File.Exists(sCover))
                    {
                        //Read file content if it exists
                        bResponse = Functions.ReadLocalFileToByteArray(sCover);

                        if (!String.IsNullOrEmpty(ClientHTTPRequest_VariableValue(sVariables, "height")) || !String.IsNullOrEmpty(ClientHTTPRequest_VariableValue(sVariables, "width")))
                        {
                            Image imCoverImage = Image.FromFile(sCover);

                            //Set Height/Width
                            int iHeight = imCoverImage.Height;
                            int iWidth = imCoverImage.Width;

                            if (!String.IsNullOrEmpty(ClientHTTPRequest_VariableValue(sVariables, "height"))) iHeight = Convert.ToInt16(ClientHTTPRequest_VariableValue(sVariables, "height"));
                            if (!String.IsNullOrEmpty(ClientHTTPRequest_VariableValue(sVariables, "height"))) iWidth = Convert.ToInt16(ClientHTTPRequest_VariableValue(sVariables, "width"));

                            //Max Supported Image size is 1024x768, as to prevent exploit
                            if (iHeight > 1024) iHeight = 1024;
                            if (iWidth > 768) iWidth = 768;

                            Bitmap bOutput = new Bitmap(iWidth, iHeight);
                            Graphics gResizeImage = Graphics.FromImage((Image)bOutput);
                            gResizeImage.DrawImage(imCoverImage, 0, 0, iWidth, iHeight);
                            gResizeImage.Dispose();

                            //Declare Memory Stream to save otuput to
                            MemoryStream msResizedRespons = new MemoryStream();

                            //Save output to memeory stream
                            ((Image)bOutput).Save(msResizedRespons, System.Drawing.Imaging.ImageFormat.Jpeg);

                            //Save the Response
                            bResponse = msResizedRespons.ToArray();

                        }
                    }
                    else
                    {
                        //Display 1x1 clear GIF
                        bResponse = Functions.ReadEmbeddedResource("one_pixel.gif");
                    }


                    //Send Header
                    ServerHTTPResponse_Header(sHTTPRequest, bResponse.Length, "200 OK", ref mySocket, "cover.jpg");

                    //Send Data
                    ServerHTTPResponse_SendData(bResponse, ref mySocket);

                    //Close Connection
                    mySocket.Close();
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                LogHandler.LogError(e, sHTTPRequest);
                return false;
            }
        }

        /// <summary>
        ///     Extracts the Search Query submitted by the URL/POST
        /// </summary>
        /// <param name="sHTTPRequest">string -- String containing the HTTP Request</param>
        /// <returns>string -- search query submitted</returns>
        private static string ClientHTTPRequest_SearchQuery(string sHTTPRequest)
        {
            try
            {
                string sVariables = ClientHTTPRequest_Variables(sHTTPRequest); //Variables passed in the URL or POST

                //Extract Search Query (if Submitted)
                if (sVariables.LastIndexOf("?s=") > -1)
                {
                    return sVariables.Substring(sVariables.LastIndexOf("?s=") + 3).ToLower();
                }

                return "";
            }
            catch (Exception e)
            {
                LogHandler.LogError(e, sHTTPRequest);
                return "";
            }
        }

        /// <summary>
        ///     This function returns the first file found in a folder with the specified File Extension
        /// </summary>
        /// <param name="sDirectoryName">string -- Directory to be searched</param>
        /// <param name="sFileExt">string -- File Extension to search for</param>
        /// <returns></returns>
        private static string Server_FindFirstFileByExt(string sDirectoryName, string sFileExt)
        {
            try
            {
                //Check to verify directory exists
                if (!Directory.Exists(sDirectoryName)) return "";

                DirectoryInfo directory = new DirectoryInfo(sDirectoryName);

                // Scan all files in the current path
                foreach (FileInfo file in directory.GetFiles())
                {
                    if (file.Name.EndsWith(sFileExt))
                    {
                        return file.Name;
                    }
                }

                return "";
            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
                return "";
            }
        }

        

        /// <summary>
        ///     Accepts a DateTime input and returns a string containing the date and time in directory list format
        /// </summary>
        /// <param name="dtInputDate"></param>
        /// <returns>string -- date and time in "dd-MMM-YYYY HH:mm" format.</returns>
        private static string Server_ConvertDateTimeToDirectoryListDateTime(DateTime dtInputDate)
        {

            //Set day to "DD" format
            string sDay = dtInputDate.ToString("dd");
            if (sDay.Length == 1) sDay = "0" + sDay;

            //Set month to "MMM"
            string sMonth = dtInputDate.ToString("MMM");

            //Set Time to ##:## 24 hour clock
            string sTime = dtInputDate.ToString("HH") + ":" + dtInputDate.ToString("mm");

            return sDay + "-" + sMonth + "-" + dtInputDate.Year.ToString() + " " + sTime;

        }

        /// <summary>
        ///     Takes a file size and converts it to the format used in a directory list
        /// </summary>
        /// <param name="lFileSize"></param>
        /// <returns>string -- formated as "####K/M/G"</returns>
        private static string Server_ConvertFileSizeToDirecotyListFileSize(long lFileSize)
        {

            if (lFileSize < 1024) return ((string)(lFileSize.ToString() + " ")).PadLeft(6, ' ');

            double dFileSize = Convert.ToDouble(lFileSize);

            //bytes to kilobytes
            dFileSize = Math.Round(dFileSize / 1000, 1);

            if (dFileSize < 1024) return ((string)(dFileSize.ToString() + "K")).PadLeft(6, ' ');

            //kilobytes to megabytes
            dFileSize = Math.Round(dFileSize / 1000, 1);

            if (dFileSize < 1024) return ((string)(dFileSize.ToString() + "M")).PadLeft(6, ' ');

            //megabytes to gigabytes
            dFileSize = Math.Round(dFileSize / 1000, 1);

            //Stop with Gigabytes.. for now ;-)
            return ((string)(dFileSize.ToString() + "G")).PadLeft(6, ' ');

        }

        /// <summary>
        ///     Reads HTTP header and sends data requested to the client browser
        /// </summary>
        /// <param name="sHTTPRequest"></param>
        /// <param name="mySocket"></param>
        /// <returns>bool -- true if the data was sent successfully.</returns>
        private static bool ServerHTTPResponse_SendContent(string sHTTPRequest, ref Socket mySocket)
        {
            try
            {
                byte[] bResponse = new byte[] { 0 };

                //text/html file type, and an actual file is requested, treat it as a parsed script.
                if (Server_GetMimeType(ClientHTTPRequest_RequestedFile(sHTTPRequest)) == "text/html" & !String.IsNullOrEmpty(ClientHTTPRequest_RequestedFile(sHTTPRequest))) bResponse = ScriptParser.ProcessScript(Functions.ReadLocalFileToByteArray(ClientHTTPRequest_RequestedFileLocalPath(sHTTPRequest) + ClientHTTPRequest_RequestedFile(sHTTPRequest)), ClientHTTPRequest_RequestedFileLocalPath(sHTTPRequest), ClientHTTPRequest_SearchQuery(sHTTPRequest), ClientHTTPRequest_ProcessAuthWWWinampAdmin(sHTTPRequest));

                //text/html file type and no file was passed, must be a directory request
                if (Server_GetMimeType(ClientHTTPRequest_RequestedFile(sHTTPRequest)) == "text/html" & String.IsNullOrEmpty(ClientHTTPRequest_RequestedFile(sHTTPRequest))) bResponse = ServerHTTPResponse_GenerateDirectoryList(sHTTPRequest);

                //file is not text/html and the request is for an embedded resource
                if (Server_GetMimeType(ClientHTTPRequest_RequestedFile(sHTTPRequest)) != "text/html" & ClientHTTPRequest_EmbeddedResource(sHTTPRequest)) bResponse = Functions.ReadEmbeddedResource(ClientHTTPRequest_RequestedFile(sHTTPRequest));

                //file is not text/html and the request is NOT for an embedded resource
                if (Server_GetMimeType(ClientHTTPRequest_RequestedFile(sHTTPRequest)) != "text/html" & !ClientHTTPRequest_EmbeddedResource(sHTTPRequest)) bResponse = Functions.ReadLocalFileToByteArray(ClientHTTPRequest_RequestedFileLocalPath(sHTTPRequest) + ClientHTTPRequest_RequestedFile(sHTTPRequest));

                //Check for Compression
                if (AppConfiguration.configWWWinampHTTPAllowCompression && ClientHTTPRequest_AcceptedEncoding(sHTTPRequest) && CompressionAllowed(ClientHTTPRequest_RequestedFile(sHTTPRequest))) bResponse = CompressData(bResponse);

                //Send Header
                ServerHTTPResponse_Header(sHTTPRequest, bResponse.Length, "200 OK", ref mySocket, ClientHTTPRequest_RequestedFile(sHTTPRequest));

                //Send Data
                ServerHTTPResponse_SendData(bResponse, ref mySocket);
                return true;
            }
            catch (Exception e)
            {
                LogHandler.LogError(e, sHTTPRequest);
                return false;
            }
        }

        /// <summary>
        ///     Sends an HTTP error message to the Client Browser
        /// </summary>
        /// <param name="sErrorMessage">string -- Containing Error message to be displayed on the page</param>
        /// <param name="sStatusCode">string -- Status code of HTTP Error</param>
        /// <param name="sRequestedLocalFile">string -- Local File being requested</param>
        /// <param name="sHTTPRequest">string -- HTTP Request</param>
        /// <param name="mySocket">ref Socket -- Socket object of Client Connection</param>
        /// <returns></returns>
        private static bool ServerHTTPResponse_Error(string sErrorMessage, string sStatusCode, string sRequestedLocalFile, string sHTTPRequest, ref Socket mySocket)
        {
            try
            {
                //Format The Message
                ServerHTTPResponse_Header(sHTTPRequest, sErrorMessage.Length, sStatusCode, ref mySocket, ClientHTTPRequest_RequestedFile(sHTTPRequest));

                //Send to the browser
                ServerHTTPResponse_SendData(Encoding.ASCII.GetBytes(sErrorMessage), ref mySocket);

                //Close the Socket
                mySocket.Close();

                return true;
            }
            catch (Exception e)
            {
                LogHandler.LogError(e, sHTTPRequest);
                return false;
            }
        }

        /// <summary>
        ///     Generates HTML required to display a directory list
        /// </summary>
        /// <param name="sHTTPRequest"></param>
        /// <returns>byte[] -- containing HTML for Directory List</returns>
        private static byte[] ServerHTTPResponse_GenerateDirectoryList(string sHTTPRequest)
        {
            StringBuilder sbDirectoryList = new StringBuilder();
            DirectoryInfo diDirectory = new DirectoryInfo(ClientHTTPRequest_RequestedFileLocalPath(sHTTPRequest));
            sbDirectoryList.Append("<!DOCTYPE HTML public static \"-//W3C//DTD HTML 3.2 Final//EN\">\r\n<html>\r\n<head>\r\n<title>Index of " + ClientHTTPRequest_RequestedFileURLPath(sHTTPRequest) + "</title>\r\n</head>\r\n<body>\r\n<h1>Index of " + ClientHTTPRequest_RequestedFileURLPath(sHTTPRequest) + "</h1><pre><img src=\"/embedded/blank.gif\" alt=\"Icon \"> Name                    Last modified      Size  Description<hr><img src=\"/embedded/back.gif\" alt=\"[DIR]\"> <a href=\"../\">Parent Directory</a>                               -\r\n");

            //Crawl Folders
            DirectoryInfo[] subDirectories = diDirectory.GetDirectories();
            foreach (DirectoryInfo subDirectory in subDirectories)
            {
                string sFolderName = subDirectory.Name;
                if (sFolderName.Length > 23) sFolderName = sFolderName.Substring(0, 19) + "..>";

                sbDirectoryList.Append("<img src=\"/embedded/folder.gif\" alt=\"[DIR]\"> <a href=\"" + subDirectory.Name + "/\">" + sFolderName + "/</a>" + sFolderName.PadRight(22, Convert.ToChar(" ")).Replace(sFolderName, "") + " " + Server_ConvertDateTimeToDirectoryListDateTime(subDirectory.LastWriteTime) + "      -\r\n");
            }

            //Default Icon to Unknown
            string sEmbeddedImage = "unknown.gif";

            //Crawl Files
            foreach (FileInfo file in diDirectory.GetFiles())
            {

                switch (Server_GetMimeType(file.Name).Substring(0, Server_GetMimeType(file.Name).IndexOf("/")))
                {
                    case ("application"):
                        sEmbeddedImage = "binary.gif";
                        break;
                    case ("image"):
                        sEmbeddedImage = "image2.gif";
                        break;
                    case ("audio"):
                        sEmbeddedImage = "sound2.gif";
                        break;
                    case ("video"):
                        sEmbeddedImage = "movie.gif";
                        break;
                    case ("text"):
                        sEmbeddedImage = "text.gif";
                        break;
                    default:
                        sEmbeddedImage = "unknown.gif";
                        break;

                }

                //Format File Name for display
                string sFileName = file.Name;
                if (sFileName.Length > 23) sFileName = sFileName.Substring(0, 20) + "..>";

                sbDirectoryList.Append("<img src=\"/embedded/" + sEmbeddedImage + "\" alt=\"[   ]\"> <a href=\"" + file.Name + "\">" + sFileName + "</a>" + sFileName.PadRight(23, Convert.ToChar(" ")).Replace(sFileName, "") + " " + Server_ConvertDateTimeToDirectoryListDateTime(file.LastWriteTime) + "  " + Server_ConvertFileSizeToDirecotyListFileSize(file.Length) + "\r\n");
            }

            //Footer
            sbDirectoryList.Append("<hr></pre>\r\n<address>" + AppConfiguration.configProgramName + " " + AppConfiguration.configProgramVersion + "</address>\r\n</body></html>");

            return Encoding.ASCII.GetBytes(sbDirectoryList.ToString());
        }



        /// <summary>
        ///     Checks to see if the URL Requested is an embedded resource
        /// </summary>
        /// <param name="sHTTPRequest"></param>
        /// <returns>bool - true is the resource requested is embedded in the assembly</returns>
        private static bool ClientHTTPRequest_EmbeddedResource(string sHTTPRequest)
        {
            if (ClientHTTPRequest_RequestedFileURLPath(sHTTPRequest) == "/embedded/") return true;

            return false;
        }

        /// <summary>
        ///     Checks to see if the File or Directory requeted in the URL exists on the server
        /// </summary>
        /// <param name="sHTTPRequest"></param>
        /// <returns>bool -- true is the file or directory is found</returns>
        private static bool ClientHTTPRequest_RequestedFileExists(string sHTTPRequest)
        {
            if (ClientHTTPRequest_EmbeddedResource(sHTTPRequest)) return true; //If the resource requested is embedded, return true

            if (File.Exists(ClientHTTPRequest_RequestedFileLocalPath(sHTTPRequest) + ClientHTTPRequest_RequestedFile(sHTTPRequest))) return true;

            if (Directory.Exists(ClientHTTPRequest_RequestedFileLocalPath(sHTTPRequest))) return true;

            return false;
        }

        /// <summary>
        ///     Checks to see if the user has access to execute the requested command
        /// </summary>
        /// <param name="sHTTPRequest"></param>
        /// <param name="sCommand"></param>
        /// <returns>bool -- returns true if the user has access to execute the command</returns>
        private static bool ClientHTTPRequest_CheckWinAmpCommandPermission(string sHTTPRequest, string sCommand)
        {
            try
            {
                //If the User if logged in as admin, let them execute anything.
                if(ClientHTTPRequest_ProcessAuthWWWinampAdmin(sHTTPRequest)) return true;

                //If they're not logged in as admin, and the command is set to FALSE in the config, then let them execute it as well
                if (!AppConfiguration.configWWWinampHTTPCommand(sCommand)) return true;

                return false;
            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
                return false;
            }
        }


        /// <summary>
        ///     Main WWWinamp HTTP Listener Thread
        /// </summary>
        private static void WWWinampListen()
        {
            try
            {
                while (ListeningStatus == Status.Started)
                {
                    if (myListener.Pending())
                    {
                        Thread thWWWinampListen = new Thread(new ThreadStart(WWWinampAcceptConnection));
                        thWWWinampListen.Start();
                    }

                    //Sleep the thread for 1 milisecond as to keep the CPU from being loaded
                    Thread.Sleep(1);
                }

            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
            }
            finally
            {
                if (ListeningStatus == Status.Started)
                {
                    //If for any reason the thread is going to exit because of error, restart it.
                    LogHandler.LogEvent("Restarting Listener");
                    WWWinampListen();
                }
            }

        }

        /// <summary>
        ///     This Function is fired off when a new HTTP connection is accepted. This is the main method for a HTTP Request.
        /// </summary>
        private static void WWWinampAcceptConnection()
        {
            //Accept a new connection
            Socket mySocket = myListener.AcceptSocket();

            if (mySocket.Connected)
            {
                //Client Connected

                //make a byte array and receive data from the client 
                Byte[] bReceive = new Byte[mySocket.ReceiveBufferSize];
                int i = mySocket.Receive(bReceive, bReceive.Length, 0);

                //Convert Byte to String
                string sHTTPRequest = Encoding.ASCII.GetString(bReceive);

                //Extract information from HTTP Request Headers
                string sHTTPRequestType = ClientHTTPRequest_RequestType(sHTTPRequest);  //HTTP Request Type (GET/POST)

                //If HTTP Request is an unknown type, close the connection.
                if (sHTTPRequestType == "UNKNOWN") { mySocket.Close(); return; }

                //Verify Request
                if (!ClientHTTPRequest_Validate(sHTTPRequest, ref mySocket)) return;

                //URL/Request Validated, Execute any WinAmp Command if present

                //Process any commands sent to WinAmp (Winamp Commands, Download File from Library)
                if (!ClientHTTPRequest_ProcessWinAmpCommand(sHTTPRequest, ref mySocket)) return;

                //Process any commands for WWWinamp (Login, etc.)
                if (!ClientHTTPRequest_ProcessWWWinampCommand(sHTTPRequest, ref mySocket)) return;

                //Send Content to Browser
                ServerHTTPResponse_SendContent(sHTTPRequest, ref mySocket);

                //Close Socket
                if (mySocket.Connected) mySocket.Close();
            }

        }

        #endregion
    }
}
