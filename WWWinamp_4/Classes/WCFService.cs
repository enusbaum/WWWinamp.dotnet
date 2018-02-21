using System;
using System.ServiceModel;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ENusbaum.Applications.WWWinamp.Classes.WCFService
{
    #region Service Contract Interface and Operation Contract Methods

    [ServiceContract(Name = "WWWinampWCFService", Namespace = "ENusbaum.Applications.WWWinamp.Classes.WCFService")]
    public interface IWCFService
    {
        [OperationContract]
        [FaultContract(typeof(WWWinampServiceException))]
        bool WWWinampCommand(WWWinampQuery myQuery);

        [OperationContract]
        [FaultContract(typeof(WWWinampServiceException))]
        WWWinampLibrary WWWinampGetLibrary(WWWinampQuery myQuery);

        [OperationContract]
        [FaultContract(typeof(WWWinampServiceException))]
        WWWinampPlaylist WWWinampGetPlaylist(WWWinampQuery myQuery);
    }

    #endregion

    #region Service Class

    public class WWWinampWCFService : IWCFService
    {
        /// <summary>
        ///     This Method allows a client to execute a command to WWWinamp
        /// </summary>
        /// <param name="myQuery"></param>
        /// <returns></returns>
        public bool WWWinampCommand(WWWinampQuery myQuery)
        {
            //Authenticate the Request
            if (!Authenticate(myQuery.Authentication))
            {
                WWWinampServiceException exException = new WWWinampServiceException();
                exException.ExceptionName = "Authentication Failed";
                exException.ExceptionDetails = "The Authentication value provided in the request SOAP message is incorrect or missing. Please verify the value and try again.";
                throw new FaultException<WWWinampServiceException>(exException);
            }

            //Execute Incoming Query
            WinAmpController.WinampCommand(myQuery.WWWinampCommandName, myQuery.WWWinampCommandValue);
            
            return true;
        }

        /// <summary>
        ///     Returns the WWWinamp Library within a WWWinampLibrary Object
        /// </summary>
        /// <param name="myQuery">WWWinampQuery -- WWWinamp Query received by the WWWinamp WCF Service</param>
        /// <returns></returns>
        public WWWinampLibrary WWWinampGetLibrary(WWWinampQuery myQuery)
        {
            try
            {
                //Authenticate the Request
                if (!Authenticate(myQuery.Authentication))
                {
                    WWWinampServiceException exException = new WWWinampServiceException();
                    exException.ExceptionName = "Authentication Failed";
                    exException.ExceptionDetails = "The Authentication value provided in the request SOAP message is incorrect or missing. Please verify the value and try again.";
                    throw new FaultException<WWWinampServiceException>(exException);
                }

                //Populate Object with WWWinamp Library
                WWWinampLibrary oLibrary = new WWWinampLibrary();
                oLibrary.WWWinampLibraryItem = new List<WWWinampLibraryItem>();

                foreach (MediaLibraryItem oInputItem in LibraryController.dbMediaLibrary)
                {
                    WWWinampLibraryItem oOutputItem = new WWWinampLibraryItem();
                    oOutputItem.FileName = oInputItem.FileName;
                    oOutputItem.FilePath = oInputItem.FilePath;
                    oOutputItem.FileID = oInputItem.FileID.ToString();
                    oLibrary.WWWinampLibraryItem.Add(oOutputItem);
                }

                //Return Object
                return oLibrary;
            }
            catch (Exception eException)
            {
                //Log the error locally
                LogHandler.LogError(eException);

                //If configured, send the error to the client using a SOAP Exception
                if (AppConfiguration.configWWWinampWCFSendErrorsToClient)
                {
                    //Send the error to the client
                    WWWinampServiceException exSoapException = new WWWinampServiceException();
                    exSoapException.ExceptionName = eException.Message;
                    exSoapException.ExceptionDetails = eException.StackTrace;
                    throw new FaultException<WWWinampServiceException>(exSoapException);
                }
                else
                {
                    return new WWWinampLibrary();
                }
            }
        }

        /// <summary>
        ///     WCF Service to return the current Playlist
        /// </summary>
        /// <param name="myQuery">WWWinampQuery -- Contains the WWWinamp Query</param>
        /// <returns>WWWinampPlaylist -- Contains the current Winamp Playlist</returns>
        public WWWinampPlaylist WWWinampGetPlaylist(WWWinampQuery myQuery)
        {
            try
            {
                //Authenticate the Request
                if (!Authenticate(myQuery.Authentication))
                {
                    WWWinampServiceException exException = new WWWinampServiceException();
                    exException.ExceptionName = "Authentication Failed";
                    exException.ExceptionDetails = "The Authentication value provided in the request SOAP message is incorrect or missing. Please verify the value and try again.";
                    throw new FaultException<WWWinampServiceException>(exException);
                }

                //Create Response Object
                WWWinampPlaylist oPlaylist = new WWWinampPlaylist();
                oPlaylist.WWWinampPlaylistItem = new List<WWWinampPlaylistItem>();
                oPlaylist.CurrentPosition = WinampPlaylist.CurrentPosition.ToString();

                //Populate Response Object
                foreach (WinampPlaylistItem oItem in WinampPlaylist.PlaylistItems)
                {
                    WWWinampPlaylistItem oNewItem = new WWWinampPlaylistItem();
                    oNewItem.DisplayName = oItem.DisplayName;
                    oNewItem.FileName = oItem.FileName;
                    oNewItem.FilePath = oItem.FilePath;
                    oNewItem.SongID = oItem.SongID.ToString();

                    //Add Item to WCF SOAP output
                    oPlaylist.WWWinampPlaylistItem.Add(oNewItem);
                }

                //Return Object
                return oPlaylist;
            }
            catch (Exception eException)
            {
                //Log the error locally
                LogHandler.LogError(eException);

                //If configured, send the error to the client using a SOAP Exception
                if (AppConfiguration.configWWWinampWCFSendErrorsToClient)
                {
                    //Send the error to the client
                    WWWinampServiceException exSoapException = new WWWinampServiceException();
                    exSoapException.ExceptionName = eException.Message;
                    exSoapException.ExceptionDetails = eException.StackTrace;
                    throw new FaultException<WWWinampServiceException>(exSoapException);
                }
                else
                {
                    return new WWWinampPlaylist();
                }
            }
        }

        private bool Authenticate(string sAuthenticationString)
        {
            try
            {
                if (sAuthenticationString == Convert.ToBase64String(Functions.Create_SHA1Hash(Encoding.ASCII.GetBytes(AppConfiguration.configWWWinampWCFAuthentication)))) return true;
                return false;
            }
            catch (Exception eException)
            {
                //Log the error locally
                LogHandler.LogError(eException);

                return false;
            }

        }
    }

    #endregion

    #region Service Host Class

    internal class WWWinampServiceHost
    {
        internal static ServiceHost myWWWinampServiceHost = null;

        internal static bool StartService()
        {
            try
            {
                // Consider putting the baseAddress in the configuration system
                // and getting it here with AppSettings
                Uri baseAddress = new Uri("http://" + AppConfiguration.configWWWinampWCFListeningIP + ":" + AppConfiguration.configWWWinampWCFListeningPort.ToString() + "/WCFService");

                // Instantiate new ServiceHost 
                myWWWinampServiceHost = new ServiceHost(typeof(WWWinampWCFService), baseAddress);

                myWWWinampServiceHost.Open();

                LogHandler.LogEvent("WCF Service Started at " + baseAddress.AbsoluteUri);

                return true;
            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
                return false;
            }
        }

        internal static bool StopService()
        {
            try
            {
                // Call StopService from your shutdown logic (i.e. dispose method)
                if (myWWWinampServiceHost.State != CommunicationState.Closed)
                {
                    LogHandler.LogEvent("WCF Service Stopped");
                    myWWWinampServiceHost.Close();
                }
                return true;
            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
                return false;
            }
        }
    }

    #endregion

    #region Data Contract Classes

    [DataContract(Name = "WWWinampQuery", Namespace = "ENusbaum.Applications.WWWinamp.Classes.WCFService")]
    public class WWWinampQuery
    {

        [DataMember(Name = "Authentication", IsRequired = true, EmitDefaultValue = false, Order = 1)]
        public string Authentication;

        [DataMember(Name = "WWWinampCommandName", IsRequired = true, EmitDefaultValue = false, Order = 2)]
        public string WWWinampCommandName;

        [DataMember(Name = "WWWinampCommandValue", IsRequired = true, EmitDefaultValue = false, Order = 3)]
        public string WWWinampCommandValue;
    }

    [DataContract(Name = "WWWinampLibrary", Namespace = "ENusbaum.Applications.WWWinamp.Classes.WCFService")]
    public class WWWinampLibrary
    {
        [DataMember(Name = "WWWinampLibraryItem", IsRequired = true, Order = 1, EmitDefaultValue = false)]
        public List<WWWinampLibraryItem> WWWinampLibraryItem;
    }

    [DataContract(Name = "WWWinampLibraryItem,", Namespace = "ENusbaum.Applications.WWWinamp.Classes.WCFService")]
    public class WWWinampLibraryItem
    {
        [DataMember(Name = "FileID", IsRequired = true, Order = 1, EmitDefaultValue = false)]
        public string FileID;

        [DataMember(Name = "FileName", IsRequired = true, Order = 2, EmitDefaultValue = false)]
        public string FileName;

        [DataMember(Name = "FilePath", IsRequired = true, Order = 3, EmitDefaultValue = false)]
        public string FilePath;
    }

    [DataContract(Name = "WWWinampPlaylist", Namespace = "ENusbaum.Applications.WWWinamp.Classes.WCFService")]
    public class WWWinampPlaylist
    {
        [DataMember(Name = "WWWinampPlaylistItem", IsRequired = true, Order = 1, EmitDefaultValue = false)]
        public List<WWWinampPlaylistItem> WWWinampPlaylistItem;

        
        [DataMember(Name = "WWWinampPlaylistPoisition", IsRequired = true, Order = 2, EmitDefaultValue = false)]
        public string CurrentPosition;
         
    }

    [DataContract(Name = "WWWinampPlaylistItem,", Namespace = "ENusbaum.Applications.WWWinamp.Classes.WCFService")]
    public class WWWinampPlaylistItem
    {
        [DataMember(Name = "SongID", IsRequired = true, Order = 1, EmitDefaultValue = false)]
        public string SongID;

        [DataMember(Name = "FilePath", IsRequired = true, Order = 2, EmitDefaultValue = false)]
        public string FilePath;

        [DataMember(Name = "FileName", IsRequired = true, Order = 3, EmitDefaultValue = false)]
        public string FileName;

        [DataMember(Name = "DisplayName", IsRequired = true, Order = 4, EmitDefaultValue = false)]
        public string DisplayName;
    }

    [DataContract(Name = "WWWinampServiceException,", Namespace = "ENusbaum.Applications.WWWinamp.Classes.WCFService")]
    public class WWWinampServiceException
    {
        [DataMember(Name = "ExceptionName", IsRequired = true, Order = 1, EmitDefaultValue = false)]
        public string ExceptionName;

        [DataMember(Name = "ExceptionDetails", IsRequired = true, Order = 2, EmitDefaultValue = false)]
        public string ExceptionDetails;
    }

    #endregion

}

