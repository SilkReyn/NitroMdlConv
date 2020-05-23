using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using NitroMdlConv.Mdl;


namespace NitroMdlConv.Test
{
    [TestClass]
    public class FrameParser_Tests
    {
        [TestMethod]
        public void Parse_Root()
        {
            CMdlFile fl = new CMdlFile(Path.Combine(Path.GetFullPath(@"..\..\input"), "test_root.mdl"));

            CFrameParser.Parse(fl.Reader, out CFrame root);

            if (root == null)
            {
                Assert.Fail("Root was null");
            }

            bool eval =
                (root.Name == "FRMERootNode") &&  // frame detection is omitted
                (root.Childs.Count == 0);

            Assert.IsTrue(
                eval, String.Format("Unexpected - Name: \"{0}\" Childs: {1}",
                root.Name,
                root.Childs.Count));
        }

        [TestMethod]
        public void Parse_2Entity()
        {
            CMdlFile fl = new CMdlFile(Path.Combine(Path.GetFullPath(@"..\..\input"), "test_FRME-o1o2.mdl"));

            CFrameParser.Parse(fl.Reader, out CFrame root);

            if (root == null)
            {
                Assert.Fail("Root was null");
            }

            bool eval1 =
                (root.Name == "FRMERootNode") &&
                (root.Childs.Count == 2);
            var childs = root.Childs;
            bool eval2 =
                (childs[0].Name == "object1") &&
                (childs[0].Childs.Count == 0) &&
                (childs[1].Name == "object2") &&
                (childs[1].Childs.Count == 0);

            Assert.IsTrue(
                eval1, String.Format("Unexpected - Name: \"{0}\" Childs: {1}",
                root.Name,
                root.Childs.Count));
            Assert.IsTrue(
                eval2, String.Format("Unexpected - O1 Name: \"{0}\" O1 Childs: {1}\nO2 Name: \"{2}\" O2 Childs: {3}",
                childs[0].Name, childs[0].Childs.Count,
                childs[1].Name, childs[1].Childs.Count));
        }
        
        [TestMethod]
        public void Parse_2Entity_1Subchild()
        {
            CMdlFile fl = new CMdlFile(Path.Combine(Path.GetFullPath(@"..\..\input"), "test_FRME-o1o2c21.mdl"));

            CFrameParser.Parse(fl.Reader, out CFrame root);

            if (root == null)
            {
                Assert.Fail("Root was null");
            }

            bool eval1 = root.Childs.Count == 2;
            bool eval2 = root.Childs[0].Childs.Count == 0;
            bool eval3 = root.Childs[1].Childs.Count == 1;

            Assert.IsTrue(
                eval1 && eval2 && eval3,
                String.Format("Results - e1: {0}, e2: {1}, e3: {2}",
                eval1, eval2, eval3));
        }

        [TestMethod]
        public void Parse_Corrupt_DataSize()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void Parse_Corrupt_NoName()
        {//FRME 0TRNS{...
            Assert.Inconclusive();
        }
    }
}
