using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text; // ASCII Encoding


namespace NitroMdlConv
{
    public class CMdlFileNavigator : BinaryReader
    {
        readonly byte[] mData;
        

        #region Properties

        public byte[] Data => mData;

        public long DataStartOffset { get; private set; }

        public long ReaderPos  // Only a shorthand
        {
            get => base.BaseStream.Position;
            set => base.BaseStream.Position = value;
        }
        
        #endregion


        #region Public Methods

        //private CMdlFileNavigator() : base() { throw new NotImplementedException("CMdlFileNavigator()"); }  // Not allowed without data
        public CMdlFileNavigator(byte[] src) :
            base(new MemoryStream(src, writable: false), new ASCIIEncoding())
        {
            mData = src;
        }
        

        public void MoveDataStart() => base.BaseStream.Position = DataStartOffset;
        public void MoveCursor(int bytes)
        {
            if (bytes < 0)
            {
                if (base.BaseStream.Position+bytes < 0)
                {
                    base.BaseStream.Position = 0;
                    return;
                }
            } else {
                if (!HasMore(Convert.ToUInt32(bytes)))
                {
                    base.BaseStream.Position = mData.Length;
                    return;
                }
            }
            
            base.BaseStream.Position += bytes;
        }


        public void SkipWhitespace()
        {
            int c = base.PeekChar();  // ansi-char
            while (c == '\r' || c == '\n' || c == ' ' || c == '\0' || c == '\t')  // utf16-chars
            {
                base.BaseStream.Position++;
                c = base.PeekChar();  // -1 on EoF
            }
        }

        
        /// <summary>
        /// Copies next line from stream.
        /// </summary>
        /// <returns>Ansi characters till next line break.</returns>
        public byte[] PeekLine()
        {
            byte[] lineSeq = null;
            long start = ReaderPos;
            
            CTextUtil.DoOnNewLine(mData, start, onNewLine: delegate (long idx) {
                lineSeq = new byte[idx - start];  //excluding return
            });
            
            // No endl found
            if (lineSeq == null)
            {
                lineSeq = new byte[mData.Length - start];
            }

            Array.Copy(mData, start, lineSeq, 0L, Math.Min(lineSeq.Length, mData.Length));

            return lineSeq;
        }

        
        /// <summary>
        /// Check next 4 bytes for a tag.
        /// Starts at current stream position and increments position only on successful match.
        /// </summary>
        /// <returns>
        /// The found tag in UTF16 string representation.
        /// Empty if input was not a supported tag.
        /// </returns>
        public string ReadTag(IReadOnlyList<byte[]> lookForTags)
        {
            if (!HasMore(4))
            {
                return null;
            }
            byte[] read = base.ReadBytes(4);
            if (lookForTags.Any(x => CTextUtil.IsEqual(read, x)))
            {
                return CTextUtil.AnsiToStr(read);
            } else {
                MoveCursor(-read.Length);
                return String.Empty;
            }
        }
        public string ReadTag()
        {
            if (!HasMore(4))
            {
                return null;
            }
            return CTextUtil.AnsiToStr(base.ReadBytes(4));
        }


        /// <summary>
        /// Compares file revisions.
        /// Returns to start position in stream and returns after version header.
        /// Can handle whitespace before the numbers, but number and header must be terminated with a return+newline.
        /// </summary>
        /// <param name="ver">Expected version of mdl content structure</param>
        /// <returns>Whether the current file is compatible.</returns>
        public bool IsBinVersEqual(short ver)
        {
            if (mData.Length < 8)
            {
                return false;
            }

            // get BINVRSN tag at start of file
            ReaderPos = 0;
            DataStartOffset = mData.Length - 1;
            if (!CTextUtil.IsEqual(base.ReadBytes(8), "BINVRSN{"))
            {
                return false;
            }

            // find version start
            SkipWhitespace();

            // get number and move position
            string line = CTextUtil.AnsiToStr(PeekLine());
            if (String.IsNullOrEmpty(line))
            {
                return false;
            }

            if (!int.TryParse(line.Trim(), out int thisVers))
            {
                return false;
            }

            long curPos = ReaderPos;
            curPos += line.Length;  // after number (with possible whitespace)
            if (curPos != mData.Length)
            {
                curPos += 2;  // after <number>\r\n
                CTextUtil.DoOnNewLine(mData, curPos, onNewLine: delegate (long idx) {
                    curPos = idx + 2;  // after }\r\n
                });
            }
            ReaderPos = DataStartOffset = curPos;  // pos can be outside data (eof)
            return ver == thisVers;
        }


        public bool IsAtTag(byte[] tagSeq)
        {
            if ((tagSeq!=null) && (tagSeq.Length > 0))
            {
                return CTextUtil.BinCmp(mData, tagSeq, lookAt: ReaderPos);
            }
            //else{
            //    foreach (var tag in mTags)
            //    {
            //        if (CTextUtil.BinCmp(mData, tag, lookAt:ReaderPos))
            //        {
            //            return true;
            //        }
            //    }
            //}
            return false;
        }
        //public bool IsAtTag(TagInfo.Tags eTag)
        //{
        //    return IsAtTag(mTags[(int)eTag]);
        //}

        
        /// <summary>
        /// Checks for unread data. Does not proceed reader's position.
        /// </summary>
        /// <param name="bytes">
        /// Numer of bytes in the steam asked for.
        /// '0' tests for end-of-file.
        /// Default is 1 byte.
        /// </param>
        /// <returns>Whether any bytes following the current reader position.</returns>
        public bool HasMore(uint bytes=1u)
        {
            if (bytes==0)
            {
                return mData.Length == ReaderPos;  // inverse request (is eof?)
            }
            return (mData.Length - ReaderPos) > bytes-1;
        }
        

        public string ReadText()
        {
            int maxLen = mData.Length;
            long curPos = ReaderPos;
            byte[] seq;

            //Search next null-terminator
            long pos = CTextUtil.FindIndexOf(mData, curPos, '\0');
            
            if (pos < 0)
            {// take all if no terminator found
                pos = maxLen;
            }
            if (pos > curPos)
            {
                seq = new byte[pos - curPos];
            } else {
                return String.Empty;
            }
            Array.Copy(mData, curPos, seq, 0L, seq.Length);
            ReaderPos = (pos == maxLen) ? maxLen : pos + 1;  //after \0 or EoF
            return CTextUtil.AnsiToStr(seq);
        }


        public void SeekBlockStart()
        {//Array.FindIndex(mData, (int)ReaderPos, x => x == '{');
            long pos = CTextUtil.FindIndexOf(mData, ReaderPos, '{');
            ReaderPos = (pos < 0) ? mData.Length : pos + 1;  // after { or EoF
        }


        public void SeekBlockEnd()
        {
            long pos = CTextUtil.FindIndexOf(mData, ReaderPos, '}');
            ReaderPos = (pos < 0) ? mData.Length : pos + 1;  // after } or EoF
        }
        

        public bool TryOnDataSequence(Action onElement)
        {
            if (null == onElement)
            {
                return false;
            }

            uint size=0;
            try{
                size = base.ReadUInt32();
                do{
                    onElement();
                    size--;
                } while (size > 0);
            } catch {
                return false;
            }
            return true;
        }

        #endregion
    }
}
