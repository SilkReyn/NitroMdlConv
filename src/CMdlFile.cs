using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NitroMdlConv;

namespace NitroMdlConv
{

    public class CMdlFile
    {
        public const short MDL_FILE_REV = 1;

        public enum MdlFileError {
            NoError,
            InvalidPathError,
            NoFileError,
            InvalidDataError,
            EmptyFileError,
            IncompatibleError,
            NoContentError,
            DefaultError
        };


        public CMdlFileNavigator Reader { get; private set; } = null;
        public bool IsValid { get; private set; } = false;
        public bool IsMorph { get; set; } = false;
        public string Filename { get; private set; }

        
        public CMdlFile(){ }


        public CMdlFile(string fullpath)
        {
            if (MdlFileError.NoError != LoadFile(fullpath))
            {
                throw new ArgumentException(String.Format("Failed to load from {0}", fullpath));
            }
        }


        /// <summary>
        /// Checks if the loaded file has data available.
        /// </summary>
        /// <returns>Whether any bytes are in buffer.</returns>
        public bool HasData()
        {
            if (Reader == null)
            {
                return false;
            }

            if (Reader.Data.Length > 1)
            {
                return true;
            }

            return false;
        }
        

        public MdlFileError SetData(byte[] src)
        {
            if (src == null)
            {
                return MdlFileError.InvalidDataError;
            }
            if (src.Length < 1)
            {
                return MdlFileError.EmptyFileError;
            }
            if (null != Reader)
            {
                Reader.Close();
                Reader.Dispose();
            }
            Reader = new CMdlFileNavigator(src);
            IsValid = false;
            if (!HasData())
            {
                return MdlFileError.EmptyFileError;
            }
            if (!Reader.IsBinVersEqual(MDL_FILE_REV))
            {
                return MdlFileError.IncompatibleError;
            }
            if (!Reader.HasMore())  // Makes no sense without version check done!
            {
                return MdlFileError.NoContentError;
            }

            IsValid = true;
            return MdlFileError.NoError;
        }


        /// <summary>
        /// Load a mdl file from disk.
        /// </summary>
        /// <param name="path">Full path to mdl file</param>
        /// <returns>Wether loading mdl file was successful</returns>
        public MdlFileError LoadFile(string path)
        {
            if (String.IsNullOrWhiteSpace(path))
            {
                return MdlFileError.InvalidPathError;
            }

            Filename = Path.GetFileNameWithoutExtension(path);

            if (!File.Exists(path))
            {
                return MdlFileError.NoFileError;
            }

            IsMorph = Filename.Contains("morph");

            try{
                // Can read files of max 2.147 GB
                return SetData(File.ReadAllBytes(path));
            } catch {
                return MdlFileError.DefaultError;
            }
            
        }
        
    }
}
