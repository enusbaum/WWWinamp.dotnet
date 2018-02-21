using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ENusbaum.Applications.WWWinamp.Forms;

namespace ENusbaum.Applications.WWWinamp.Classes
{
    static class LogHandler
    {
        #region Public Variables

        public static StringBuilder EventLog
        {
            get { return prvEventLog; }
        }

        public static StringBuilder ErrorLog
        {
            get { return prvErrorLog; }
        }

        #endregion

        #region Private Variables

        private static StringBuilder prvEventLog = new StringBuilder(string.Empty);
        private static StringBuilder prvErrorLog = new StringBuilder(string.Empty);
        private static Object threadLock = new Object();

        #endregion

        #region Public Methods

        /// <summary>
        ///     Error Handler for Errors without Extra Information being passed
        /// </summary>
        /// <param name="e">Exception -- Contains the Exception</param>
        public static void LogError(Exception e)
        {
            LogError(e, "");
        }

        /// <summary>
        ///     Error Handler for Errors with Extra Information
        /// </summary>
        /// <param name="e">Exception -- Contains the Exception</param>
        /// <param name="sExtraInformation">string -- Extra Information to be included in the error report</param>
        public static void LogError(Exception e, string sExtraInformation)
        {
            StringBuilder sException = new StringBuilder(string.Empty);
            //Create the Exception Report
            sException.Append("-------------------------------\r\nException Occurred:\r\n" + e.ToString() + "\r\n\r\nExtra Information:\r\n\r\n" + sExtraInformation + "\r\n");
            sException.Append("This Information has been saved to ERROR.LOG in the WWWinamp program directory. Please email this file to WWWINAMP@ENUSBAUM.COM. Thanks!\r\n-------------------------------\r\n");

            //Add to Event Log
            EventLog.Append(sException.ToString());

            //Save detailed information to error.log 
            FileInfo fErrorLog = new FileInfo("error.log");
            StreamWriter srOutput = fErrorLog.AppendText();
            srOutput.WriteLine(AppConfiguration.configProgramName + " " + AppConfiguration.configProgramVersion + "\t" + System.DateTime.Now.ToString() + "\t" + e.ToString());
            srOutput.Close();

        }

        public static void LogEvent(string sEvent)
        {
            //Add Item to the event log
            EventLog.Append("[ " + DateTime.Now.ToString() + " ] " + sEvent + "\r\n");
        }

        public static void LogAccess(string sAccess)
        {
            //Write item to the access log
            WriteAccessLog(sAccess);

            //If they want to see access log information in the Event Log, write it there too
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Logs information to the ACCESS.LOG file
        /// </summary>
        /// <param name="sLogItem">string -- Item to be logged</param>
        private static void WriteAccessLog(string sLogItem)
        {
            lock (threadLock)
            {

                FileInfo fErrorLog = new FileInfo("access.log");
                StreamWriter srOutput = fErrorLog.AppendText();
                srOutput.WriteLine(sLogItem);
                srOutput.Close();
            }
        }

        #endregion
    }
}
