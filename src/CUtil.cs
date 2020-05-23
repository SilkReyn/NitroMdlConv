using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NitroMdlConv
{
    public class CTextUtil
    {
        private CTextUtil() { }  // Util container, no sense in instantiating


        public static string AnsiToStr(byte[] ansiBytes)
        {
            if (ansiBytes.Length < 1)
            {
                return String.Empty;
            }
            return new string(Encoding.ASCII.GetChars(ansiBytes));
        }


        public static byte[] StrToAnsi(string str)
        {
            if (String.IsNullOrEmpty(str))
            {
                return new byte[0];
            }
            return Encoding.Convert(Encoding.Unicode, Encoding.ASCII, Encoding.Unicode.GetBytes(str));
        }


        public static bool BinCmp(byte[] lookIn, byte[] lookFor, long lookAt=0L)
        {
            // Sanity checks
            if ((lookAt + lookFor.Length) > lookIn.Length)
            {
                return false;
            } else if (lookAt < 0) {
                lookAt = 0;
            }
            
            for (int i=lookFor.Length-1; i>=0; --i)
            {
                if (lookIn[lookAt + i] != lookFor[i])
                {
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// Wrapper for System.Linq.IEnumerable.SequenceEqual
        /// </summary>
        /// <param name="binary">Byte array where to look in.</param>
        /// <param name="str">UTF-16 string what to look for.</param>
        /// <returns></returns>
        public static bool IsEqual(byte[] binary, string str)
        {
            if (binary.Length != str.Length)
            {
                return false;
            }
            return binary.SequenceEqual(StrToAnsi(str));
        }
        public static bool IsEqual(byte[] binaryA, byte[] binaryB)
        {
            return binaryA.SequenceEqual(binaryB);
        }


        public static bool BinContains(byte[] lookIn, string lookFor)
        {
            if (lookIn.Length < lookFor.Length)
            {
                return false;
            }

            return AnsiToStr(lookIn).Contains(lookFor);
        }


        public static void DoOnNewLine(byte[] input, long startIdx, Action<long> onNewLine)
        {
            for (long i = startIdx; i < input.Length; ++i)
            {
                if ((input[i] == 13) && (input[i + 1] == 10))  // \r\n
                {
                    onNewLine(i);
                    break;
                }
            }
        }


        public static long FindIndexOf(byte[] input, long startIdx, char triggerVal)
        {
            for (long i = startIdx; i < input.Length; ++i)
            {
                if (triggerVal == input[i])
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
