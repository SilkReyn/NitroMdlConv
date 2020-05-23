using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace NitroMdlConv.Test
{
    [TestClass]
    public class CFileParser_Tests
    {
        [TestMethod]
        public void CFileParser_Create_Successful()
        {
            Assert.Inconclusive();  // TODO: Create test files and implement test cases
        }

        [TestMethod]
        public void CFileParser_Create_NullArgument()
        {
            Assert.Inconclusive();
        }


        [TestMethod]
        public void TryParse_Successful()
        {
            string fullPath = Path.Combine(Path.GetFullPath(@"..\..\input"), "sonico_base.mdl");
            //CFileParser obj = new CFileParser(new CMdlFile(fullPath));

            var entries = new Common.Entities();
            bool result = CFileParser.TryParse(new CMdlFile(fullPath), ref entries);

            if (entries.rootNode == null)
            {
                Assert.Fail("Root was null");
            }

            if (entries.meshes == null)
            {
                Assert.Fail("Meshes was null");
            }

            Assert.IsTrue(
                result && entries.meshes.Count > 0,
                $"Meshcount: {entries.meshes.Count}");
        }

        [TestMethod]
        public void TryParse_InvalidFile()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TryParse_NoRoot()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void TryParse_LooseFrames()
        {
            Assert.Inconclusive();
        }
    }
}
