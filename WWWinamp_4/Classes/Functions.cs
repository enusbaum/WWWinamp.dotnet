using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Drawing;
using System.Security.Cryptography;

namespace ENusbaum.Applications.WWWinamp.Classes
{
    static class Functions
    {
        #region Private Variables

        private static Object threadLock = new Object();

        #endregion

        #region Public Methods

        /// <summary>
        ///     Reads a specified Embedded Resrouce from the WWWinamp Assembly and stores it in a byte[]
        /// </summary>
        /// <param name="sFileName"></param>
        /// <returns>byte[] -- containing data from specified Embedded Resource</returns>
        public static byte[] ReadEmbeddedResource(string sFileName)
        {
            Assembly asResources = Assembly.GetExecutingAssembly(); //Referrence to the current assembly
            string[] sResNames = asResources.GetManifestResourceNames(); //Store list of resources for the current assembly in an array

            foreach (string sResourceName in sResNames)
            {
                if (sResourceName.EndsWith(sFileName))
                {
                    BinaryReader oBR = new BinaryReader(asResources.GetManifestResourceStream(sResourceName));
                    return oBR.ReadBytes((int)asResources.GetManifestResourceStream(sResourceName).Length);
                }
            }
            return new byte[] { 0 };
        }

        public static Bitmap ReadEmbeddedResourceToBitmap(string sFileName)
        {
            MemoryStream msImage = new MemoryStream(Functions.ReadEmbeddedResource(sFileName));
            return new Bitmap(msImage);

        }

        /// <summary>
        ///     Reads the first X bytes specified from the file specified into a byte[]
        /// </summary>
        /// <param name="sLocalFile"></param>
        /// <param name="iBytesToRead"></param>
        /// <returns>byte[] - first x bytes of file specified</returns>
        public static byte[] ReadLocalFile(string sLocalFile, int iBytesToRead)
        {
            try
            {
                lock (threadLock)
                {
                    using (FileStream oFS = new FileStream(sLocalFile, FileMode.Open, FileAccess.Read))
                    {
                        using (BinaryReader oBR = new BinaryReader(oFS))
                        {
                            return oBR.ReadBytes(iBytesToRead);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
                return new byte[] { 0 };
            }
        }

        /// <summary>
        ///     Reads the file specified into a byte[]
        /// </summary>
        /// <param name="sLocalFile"></param>
        /// <returns>byte[] - contents of file specified</returns>
        public static byte[] ReadLocalFileToByteArray(string sLocalFile)
        {
            try
            {
                lock (threadLock)
                {
                    using (FileStream oFS = new FileStream(sLocalFile, FileMode.Open, FileAccess.Read))
                    {
                        using (BinaryReader oBR = new BinaryReader(oFS))
                        {
                            return oBR.ReadBytes((int)oFS.Length);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
                return new byte[] { 0 };
            }
        }

        /// <summary>
        ///     Reads the specified file and returns it as a MemoryStream
        /// </summary>
        /// <param name="sLocalFile">string -- path to the local file</param>
        /// <returns>MemoryStream -- containing the contents the file to be read</returns>
        public static MemoryStream ReadLocalFileToMemoryStream(string sLocalFile)
        {
            try
            {
                if (!File.Exists(sLocalFile)) return new MemoryStream(new byte[] { 0 });
                lock (threadLock)
                {
                    using (FileStream oFS = new FileStream(sLocalFile, FileMode.Open, FileAccess.Read))
                    {
                        using (BinaryReader oBR = new BinaryReader(oFS))
                        {
                            return new MemoryStream(oBR.ReadBytes((int)oFS.Length));
                        }
                    }
                }

            }
            catch (Exception e)
            {
                LogHandler.LogError(e);
                return new MemoryStream(new byte[] { 0 });
            }
        }

        /// <summary>
        ///     Returns an SHA1 hash of the byte array passed to it
        /// </summary>
        /// <param name="byInput">byte[] -- byte array to create SHA1 from</param>
        /// <returns>byte[] -- byte array containing the SHA1 hash</returns>
        public static byte[] Create_SHA1Hash(byte[] byInput)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            return sha1.ComputeHash(byInput);
        }

        /// <summary>
        ///     Returns an MD5 has of the byte array passed to it
        /// </summary>
        /// <param name="byInput">byte[] -- byte array to create MD5 from</param>
        /// <returns>byte[] -- byte array containing the MD5 hash</returns>
        public static byte[] Create_MD5Hash(byte[] byInput)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            return md5.ComputeHash(byInput);
        }

        #endregion
    }
}
