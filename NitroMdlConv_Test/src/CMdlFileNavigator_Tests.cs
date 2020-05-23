using Microsoft.VisualStudio.TestTools.UnitTesting;
using NitroMdlConv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NitroMdlConv.Test
{
    [TestClass]
    public class CMdlFileNavigator_Tests
    {
        public byte[] StrToByte(string utf16content)
        {
            byte[] binary = new byte[utf16content.Length];
            for (int ci = 0; ci < binary.Length; ci++)
            {
                binary[ci] = (byte)utf16content[ci];
            }
            return binary;
        }
        
        public CMdlFileNavigator Setup(string utf16content)
        {
            CMdlFileNavigator obj = new CMdlFileNavigator(StrToByte(utf16content));
            return obj;
        }
        

        [TestMethod]
        public void SkipWhitespace_Test_Space()
        {
            CMdlFileNavigator obj = Setup("    a");

            obj.SkipWhitespace();

            Assert.IsTrue(obj.ReaderPos == 4, "Is @position: " + obj.ReaderPos.ToString());
        }

        [TestMethod]
        public void SkipWhitespace_Test_Endl()
        {
            CMdlFileNavigator obj = Setup("\0\0\0\0a");

            obj.SkipWhitespace();

            Assert.IsTrue(obj.ReaderPos == 4, "Is @position: " + obj.ReaderPos.ToString());
        }

        [TestMethod]
        public void SkipWhitespace_Test_Newline()
        {
            CMdlFileNavigator obj = Setup("\r\n\r\na");

            obj.SkipWhitespace();

            Assert.IsTrue(obj.ReaderPos == 4, "Is @position: " + obj.ReaderPos.ToString());
        }


        [TestMethod]
        public void PeekLine_Test_Expected()
        {
            string str = "<expectedData>";
            CMdlFileNavigator obj = Setup(str + "\r\n");
            byte[] expectedData = StrToByte(str);

            byte[] line = obj.PeekLine();
            
            Assert.IsTrue(line.SequenceEqual(expectedData), "Returned data: " + Encoding.ASCII.GetString(line));
        }

        [TestMethod]
        public void PeekLine_Test_EoF()
        {
            string str = "<expectedData>";
            CMdlFileNavigator obj = Setup(str);
            byte[] expectedData = StrToByte(str);

            byte[] line = obj.PeekLine();

            Assert.IsTrue(line.SequenceEqual(expectedData), "Returned data: " + Encoding.ASCII.GetString(line));
        }

        [TestMethod]
        public void PeekLine_Test_Empty()
        {
            CMdlFileNavigator obj = Setup("");
            byte[] expectedData = new byte[0];

            byte[] line = obj.PeekLine();

            Assert.IsTrue(line.SequenceEqual(expectedData), "Returned data: " + Encoding.ASCII.GetString(line));
        }


        [TestMethod]
        public void ReadTag_Test_Expected()
        {
            string str = "FRME";
            CMdlFileNavigator obj = Setup(str);

            string tag = obj.ReadTag(new List<byte[]>(){ CTextUtil.StrToAnsi(str) });

            Assert.IsTrue(tag.Equals(str) && (obj.ReaderPos == 4), "Returned data: " + tag);
        }

        [TestMethod]
        public void ReadTag_Test_Unexpected()
        {
            string str = "}FRME";
            CMdlFileNavigator obj = Setup(str);

            string tag = obj.ReadTag(new List<byte[]>(){ CTextUtil.StrToAnsi("FRME") });

            Assert.IsTrue(string.IsNullOrEmpty(tag) && (obj.ReaderPos == 0), "Returned data: " + tag);
        }


        [TestMethod]
        public void IsBinVersEqual_Test_Positive()
        {
            string str = "BINVRSN{\t 99 \t\r\n<optionalIndent>}\r\n";
            CMdlFileNavigator obj = Setup(str);
            
            Assert.IsTrue(obj.IsBinVersEqual(99));
        }

        [TestMethod]
        public void IsBinVersEqual_Test_Unsupported()
        {
            string str = "BINVRSN{\r\n    2\r\n}\r\n";
            CMdlFileNavigator obj = Setup(str);

            Assert.IsFalse(obj.IsBinVersEqual(1));
        }

        [TestMethod]
        public void IsBinVersEqual_Test_Empty()
        {
            string str = "";
            CMdlFileNavigator obj = Setup(str);

            Assert.IsFalse(obj.IsBinVersEqual(1));
        }

        [TestMethod]
        public void IsBinVersEqual_Test_Incompatible()
        {
            string str = "<\ra\ndomDa\ta>\0";
            CMdlFileNavigator obj = Setup(str);

            Assert.IsFalse(obj.IsBinVersEqual(1));
        }


        [TestMethod]
        public void HasMore_Test_Positive()
        {
            string str = "BINVRSN{\r\n    1\r\n}\r\nFRMERootNode";
            CMdlFileNavigator obj = Setup(str);
            obj.IsBinVersEqual(1);

            Assert.IsTrue(obj.HasMore());
        }

        [TestMethod]
        public void HasMore_Test_Negative()
        {
            string str = "BINVRSN{\r\n    1\r\n}\r\n";
            CMdlFileNavigator obj = Setup(str);
            obj.IsBinVersEqual(1);

            Assert.IsFalse(obj.HasMore());
        }


        //[TestMethod]
        //public void IsAtTag_Test_Positive()
        //{
        //    byte[] ansi = StrToByte("FRME");
        //     CMdlFileNavigator obj = new CMdlFileNavigator(ansi);

        //    bool result = obj.IsAtTag();

        //    Assert.IsTrue(result);
        //}

        //[TestMethod]
        //public void IsAtTag_Test_Negative()
        //{
        //    byte[] ansi = StrToByte("{\ra\ndomDa\ta}\0");
        //     CMdlFileNavigator obj = new CMdlFileNavigator(ansi);

        //    bool result = obj.IsAtTag();

        //    Assert.IsFalse(result);
        //}

        [TestMethod]
        public void IsAtTag_Test_Specific_Positive()
        {
            string str = "MYTAG{";
            CMdlFileNavigator obj = Setup(str);

            bool result = obj.IsAtTag(StrToByte(str));

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsAtTag_Test_Specific_Negative()
        {
            string str = "{\ra\ndomDa\ta}\0";
            CMdlFileNavigator obj = Setup(str);

            bool result = obj.IsAtTag(StrToByte("MYTAG{"));

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsAtTag_Test_EoF()
        {
            CMdlFileNavigator obj = new CMdlFileNavigator(new byte[0]);

            bool result = obj.IsAtTag(StrToByte("MYTAG{"));

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsAtTag_Test_Specific_Empty()
        {
            string str = "FRME";
            CMdlFileNavigator obj = Setup(str);

            bool result = obj.IsAtTag(new byte[0]);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsAtTag_Test_Specific_Null()
        {
            string str = "FRME";
            CMdlFileNavigator obj = Setup(str);

            bool result = true;
            try{
                result = obj.IsAtTag(null);
            } catch {
                result = true;
            }

            Assert.IsFalse(result);
        }

        //[TestMethod]
        //public void IsAtTag_Test_Enum_Positive()
        //{
        //    CMdlFileNavigator obj = Setup(CMdlFileNavigator.TagInfo.frame);

        //    bool result = obj.IsAtTag(CMdlFileNavigator.TagInfo.Tags.frame);

        //    Assert.IsTrue(result);
        //}

        //[TestMethod]
        //public void IsAtTag_Test_Enum_Negative()
        //{
        //    CMdlFileNavigator obj = Setup(CMdlFileNavigator.TagInfo.mesh);

        //    bool result = obj.IsAtTag(CMdlFileNavigator.TagInfo.Tags.frame);

        //    Assert.IsFalse(result);
        //}


        [TestMethod]
        public void ReadText_Test_Expected()
        {
            string str = " ExampleText";
            CMdlFileNavigator obj = Setup(str + "\0");

            string text = obj.ReadText();

            Assert.IsTrue(text.Equals(str), "Returned data: " + text);
        }

        [TestMethod]
        public void ReadText_Test_EoF()
        {
            CMdlFileNavigator obj = new CMdlFileNavigator(new byte[0]);

            string text = obj.ReadText();

            Assert.IsTrue(text=="", "Returned data: " + text);
        }


        [TestMethod]
        public void SeekBlockStart_Test_Expected()
        {
            string str = "BINVRSN{\r\n    1\r\n}\r\nFRMERootNode";
            CMdlFileNavigator obj = Setup(str);

            obj.SeekBlockStart();
            
            Assert.IsTrue(8==obj.ReaderPos, String.Format("Returned position: {0}", obj.ReaderPos));
        }

        [TestMethod]
        public void SeekBlockStart_Test_NoStart()
        {
            string str = "BINVRSN\r\n    1\r\n}\r\nFRMERootNode";
            CMdlFileNavigator obj = Setup(str);

            obj.SeekBlockStart();

            Assert.IsTrue(str.Length == obj.ReaderPos, String.Format("Returned position: {0}", obj.ReaderPos));
        }

        [TestMethod]
        public void SeekBlockStart_Test_EoF()
        {
            CMdlFileNavigator obj = new CMdlFileNavigator(new byte[0]);
            long pos = obj.BaseStream.Position;

            obj.SeekBlockStart();

            Assert.IsTrue(pos == obj.ReaderPos, String.Format("Returned position: {0}", obj.ReaderPos));
        }
    }
}
