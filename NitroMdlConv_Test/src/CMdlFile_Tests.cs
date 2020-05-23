using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace NitroMdlConv.Test
{
    [TestClass()]
    public class CMdlFile_Tests
    {
        public CMdlFile Setup(string utf16content, out byte[] binary)
        {
            CMdlFile obj = new CMdlFile();
            binary = new byte[utf16content.Length];
            for(int ci = 0; ci<binary.Length; ci++)
            {
                binary[ci] = (byte)utf16content[ci];
            }
            return obj;
        }


        [TestMethod()]
        public void SetData_Test_ErrorInvalid()
        {
            CMdlFile obj = new CMdlFile();

            CMdlFile.MdlFileError result = obj.SetData(null);

            Assert.IsTrue(CMdlFile.MdlFileError.InvalidDataError == result);
        }

        [TestMethod()]
        public void SetData_Test_ErrorEmpty()
        {
            CMdlFile obj = new CMdlFile();

            CMdlFile.MdlFileError result = obj.SetData(new byte[0]);

            Assert.IsTrue(CMdlFile.MdlFileError.EmptyFileError == result);
        }

        [TestMethod]
        public void SetData_Test_ErrorNoContent()
        {
            CMdlFile obj = Setup("BINVRSN{\r\n    1\r\n}\r\n", out byte[] cont);

            CMdlFile.MdlFileError result = obj.SetData(cont);

            Assert.IsTrue(CMdlFile.MdlFileError.NoContentError == result);
        }

        [TestMethod]
        public void SetData_Test_ErrorIncompatible()
        {
            CMdlFile obj = Setup("BINVRSN{\r\n    2\r\n}\r\n", out byte[] cont);

            CMdlFile.MdlFileError result = obj.SetData(cont);

            Assert.IsTrue(CMdlFile.MdlFileError.IncompatibleError == result);
        }

        [TestMethod()]
        public void SetData_Test_Successful()
        {
            CMdlFile obj = Setup("BINVRSN{\r\n    1\r\n}\r\nFRMERootNode\0", out byte[] cont);

            CMdlFile.MdlFileError result = obj.SetData(cont);

            Assert.IsTrue(CMdlFile.MdlFileError.NoError == result);
        }


        [TestMethod()]
        public void LoadFile_Test_Success()
        {
            CMdlFile obj = new CMdlFile();
            string fullPath = Path.Combine(Path.GetFullPath(@"..\..\input"), "test_root.mdl");
            CMdlFile.MdlFileError result = obj.LoadFile(fullPath);

            Assert.IsTrue(CMdlFile.MdlFileError.NoError == result, String.Format("Unexpected result ({0}) loading file {1}", ((int)result).ToString(), fullPath));
        }
        
        [TestMethod]
        public void LoadFile_Test_ErrorNoFile()
        {
            CMdlFile obj = new CMdlFile();

            CMdlFile.MdlFileError result = obj.LoadFile(@"c:\somePath\invalid.file");

            Assert.IsTrue(result == CMdlFile.MdlFileError.NoFileError);
        }

        /*[TestMethod]
        public void LoadFile_Test_ErrorDefault()
        {
            CMdlFile obj = new CMdlFile();

            //Assert.ThrowsException<System.ArgumentException>(() => obj.SetFile(cont), "Unexpected exception");
            Assert.Inconclusive();
        }*/


        [TestMethod()]
        public void HasData_Test_Positive()
        {
            CMdlFile obj = Setup("BINVRSN{\r\n    1\r\n}\r\nFRMERootNode", out byte[] cont);
            obj.SetData(cont);

            bool result = obj.HasData();

            Assert.IsTrue(result);
        }

        [TestMethod()]
        public void HasData_Test_Negative()
        {
            CMdlFile obj = Setup("", out byte[] cont);
            obj.SetData(cont);

            bool result = obj.HasData();

            Assert.IsFalse(result);
        }

    }
}