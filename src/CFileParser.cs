using System;
using System.Collections.Generic;
using System.Linq;

using NitroMdlConv.Common;
//using NitroMdlConv.Mdl;


namespace NitroMdlConv
{
    public class CFileParser
    {
        public struct TagInfo
        {
            public enum Tags : int{ root=0, mesh};
            public const string root = "FRME";
            public const string mesh = "MESH";
            public const string collision = "CLSN";
            //public const string morph = "MRPH";
            //public const string physic = "PHSX";
        };

        
        readonly static IReadOnlyList<byte[]> mTags = new[]{
            new byte[]{ 0x46, 0x52, 0x4d, 0x45 },  // FRME
            new byte[]{ 0x4d, 0x45, 0x53, 0x48 },  // MESH
            new byte[]{ 0x43, 0x4c, 0x53, 0x4e },  // CLSN
        };

        //readonly CMdlFile file;

        
        //private CFileParser()
        //{
        //    throw new NotImplementedException("CFileParser()");  // Not allowed without file
        //}


        //public CFileParser(CMdlFile file)
        //{
        //    this.file = file ?? throw new ArgumentNullException(nameof(file));
        //}


        #region public Methods

        public static bool TryParse(CMdlFile file, ref Entities hierachy)
        {
            if ((null == file) || !file.IsValid)
            {
                return false;
            }
            //if (null == hierachy.meshes)  // preserve when reading morphs
            //{
            //    hierachy.meshes = new List<Mdl.CMesh>();
            //}
            
            string readNode;
            bool cancel=false;
            CMdlFileNavigator reader = file.Reader;

            // Read nodes
            reader.MoveDataStart();  // in case of restart
            if (TagInfo.root != reader.ReadTag())  // read and seek
            {// Does not start with RootNode
                return false;
            }
            reader.SkipWhitespace();  // \r\n
            CFrameParser.Parse(reader, out Mdl.CFrame currFrame);
            //int nodeCount = hierachy.rootNode.Childs.Count;
            //if (file.IsMorph && (null != hierachy.rootNode) && nodeCount > 0)
            //{
            //    hierachy.rootNode.Childs.RemoveRange(1, nodeCount - 1);
            //    hierachy.rootNode.Childs.AddRange(currFrame.Childs);
            //} else {
            //    hierachy.rootNode = currFrame;
            //}
            hierachy.rootNode = currFrame;  // morphs have no skeleton at index 0!
            reader.SkipWhitespace();

            // Read mesh data
            var parsedMeshes = new List<Mdl.CMesh>();
            while (!cancel && !String.IsNullOrEmpty(readNode = reader.ReadTag(mTags)))
            {
                switch (readNode)
                {
                case TagInfo.root:
                    cancel = true;
                    break;

                case TagInfo.mesh:
                    Mdl.CMesh currMesh = null;
                    if (file.IsMorph)
                    {
                        if ((cancel = !CMeshParser.TryParseMorph(reader, out Mdl.CMorph morph)) ||
                            (null == hierachy.meshes) || !hierachy.meshes.Any(msh => msh != null))  //< not a reason to cancel
                        {
                            break;
                        }
                        var matches = hierachy.meshes.Where(
                            msh => msh.VerticesCount() == morph.VerticesCount() &&
                            file.Filename.Contains(msh.Name));
                        if (matches.Any())
                        {
                            currMesh = matches.First()?.CreateMorphedMesh(morph);
                        }
                    } else {
                        if (cancel = !CMeshParser.TryParse(reader, out currMesh))
                        {
                            break;
                        }
                        currMesh.OriginName = file.Filename;
                    }
                    if (null != currMesh && currMesh.IsValid())
                    {
                        parsedMeshes.Add(currMesh);
                    }
                    break;

                case TagInfo.collision:  // skip (text block with 4 line body)
                    reader.SeekBlockStart();
                    reader.SeekBlockEnd();
                    break;

                default:
                    cancel = true;  // not implemented
                    break;
                }

                reader.SkipWhitespace();  // \r\n after each data block
            }
            if (!cancel)
            {
                hierachy.meshes = parsedMeshes;
            }
            return !cancel;
        }
        
        #endregion
    }
}
