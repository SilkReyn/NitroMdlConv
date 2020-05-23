using System;
using System.Collections.Generic;

using NitroMdlConv.Common;
using NitroMdlConv.Mdl;


namespace NitroMdlConv
{
    public class CMeshParser
    {
        public struct TagInfo
        {
            public enum Tags : int { vertex = 0, normal, textureUV, face, materials, weight, unsupported1 };
            public const string vertex = "VRTX";
            public const string normal = "NRML";
            public const string textureUV = "TXUV";
            public const string face = "FACE";
            public const string materials = "MLST";
            public const string weight = "SKIN";
            public const string unsupported1 = "TSPC";  // transparency? vertex color?
            // vertex color TSPC
        };
        

        static readonly IReadOnlyList<byte[]> mTags = new[] {
            new byte[] { 0x56, 0x52, 0x54, 0x58 },  // VRTX
            new byte[] { 0x4e, 0x52, 0x4d, 0x4c },  // NRML
            new byte[] { 0x54, 0x58, 0x55, 0x56 },  // TXUV
            new byte[] { 0x46, 0x41, 0x43, 0x45 },  // FACE
            new byte[] { 0x4d, 0x4c, 0x53, 0x54 },  // MLST
            new byte[] { 0x53, 0x4b, 0x49, 0x4e },  // SKIN
            new byte[] { 0x54, 0x53, 0x50, 0x43 },  // TSPC
        };

        static readonly byte[] mBONE_ANSI = new byte[] { 0x42, 0x4f, 0x4e, 0x45 };
        static readonly byte[] mMTRL_ANSI = new byte[] { 0x4d, 0x54, 0x52, 0x4c };
        
        
        public static bool TryParse(CMdlFileNavigator reader, out CMesh parsedMesh)
        {
            parsedMesh = null;
            if (null == reader)
            {
                return false;
            }
            CMesh mesh = new CMesh{Name = reader.ReadText()};
            string meshTag;
            
            // Data could contain end block symbol.
            // If errors occur, the whole process must be canceled,
            // because reader can be at unexpected position (segmentation fault).
            bool passed = true;

            reader.SeekBlockStart();  // begin mesh
            while (passed && !String.IsNullOrEmpty(meshTag = reader.ReadTag(mTags)))
            {
                reader.SeekBlockStart();  // begin data
                switch (meshTag)
                {
                case TagInfo.vertex:  // repeatedly entering here appends more vertice points to same mesh
                {
                    float[] pt3d = new float[3];
                    passed &= reader.TryOnDataSequence(onElement: delegate () {  // basically a foreach vec3
                        for (int i=0; i<3; ++i)
                        {
                            pt3d[i] = reader.ReadSingle();
                        }
                        reader.MoveCursor(4);  // we discard the 4th dimension (4B)
                        mesh.AddVertex(pt3d);
                    });
                    break;
                }//VRTX

                case TagInfo.normal:  // repeatedly entering here appends more normals to same mesh
                {
                    float[] pt3d = new float[3];
                    passed &= reader.TryOnDataSequence(onElement: delegate () {
                        for (int i=0; i<3; ++i)
                        {
                            pt3d[i] = reader.ReadSingle();
                        }
                    mesh.AddNormal(pt3d);  // stores as flipped
                    });
                    break;
                }//NRML

                case TagInfo.textureUV:  // repeatedly entering here appends more coordinates to same mesh
                {
                    float u, v;
                    passed &= reader.TryOnDataSequence(onElement: delegate () {
                        u = reader.ReadSingle();
                        v = reader.ReadSingle();
                        mesh.AddTexCoord(u, v);  // expect normalized values and mirrors them
                    });
                    break;
                }//TXUV

                case TagInfo.face:
                {
                    ushort[] idx = new ushort[3];
                    passed &= reader.TryOnDataSequence(onElement: delegate () {
                        for (int i=0; i<3; ++i)
                        {
                            idx[i] = reader.ReadUInt16();
                        }
                        mesh.AssignFace(idx);
                    });
                    break;
                }//FACE

                case TagInfo.materials:
                    passed &= ReadInMaterialList();  // MTRL
                    reader.SkipWhitespace();  // \r\n
                    while ('}'!=reader.PeekChar())
                    {// improve parsing stability on garbage text passages
                        reader.SeekBlockEnd();
                        reader.SkipWhitespace();
                    }
                    break;

                case TagInfo.weight:
                    while (reader.IsAtTag(mBONE_ANSI))
                    {
                        reader.MoveCursor(4);  // skip 'BONE'
                        passed &= ReadInWeights(out VertexGrp influence);
                        if (!influence.IsNullOrEmpty())
                        {
                            mesh.AssignWeight(influence);
                        }
                    }
                    break;

                case TagInfo.unsupported1:
                    try {
                        int valCnt = reader.ReadInt32();
                        reader.MoveCursor(valCnt * 24);
                    } catch {
                        passed = false;
                    }
                    break;

                default:
                    // try to skip with block end (but no error message)
                    //passed = false;
                    break;
                }//switch

                reader.SeekBlockEnd();  // end data
            }
            reader.SeekBlockEnd();  // end mesh

            if (!mesh.IsValid())
            {
                return false;
            }
            parsedMesh = mesh;
            return passed;
            
            bool ReadInMaterialList()
            {
                // Read material count and index ranges
                uint size=0;
                uint[] iStart;
                uint[] iStop;
                try{
                    size = reader.ReadUInt32();  // Empty MTRL bodys might follow that are not counted!
                    iStart = new uint[size];
                    iStop = new uint[size];
                    for (int i=0; i<size; i++)
                    {
                        iStart[i] = reader.ReadUInt32();
                        iStop[i] = reader.ReadUInt32();
                    }
                } catch {
                    return false;
                }
            
                // Begin of material description
                string matNam;
                long currPos;
                int bindsCnt;
                for (int matN=0; matN<size; ++matN)
                {
                    reader.SkipWhitespace();
                    if (!reader.IsAtTag(mMTRL_ANSI))
                    {
                        return false;
                    }
                    reader.MoveCursor(4);
                
                    // Fetch material name
                    matNam = null;
                    currPos = reader.ReaderPos;  // right after tag
                    CTextUtil.DoOnNewLine(reader.Data, currPos, onNewLine: delegate (long nLPos) {
                        if (nLPos > currPos)
                        {
                            byte[] lineSeq = new byte[nLPos - currPos - 1];  // exclude '{'
                            Array.Copy(reader.Data, currPos, lineSeq, 0L, lineSeq.Length);
                            matNam = CTextUtil.AnsiToStr(lineSeq).Trim();  // remove preceeding whitespace
                            currPos = reader.ReaderPos = nLPos + 2;  // after \r\n
                        }
                    });
                    if (String.IsNullOrEmpty(matNam))
                    {
                        return false;
                    }
                
                    mesh?.AssignMaterial(matNam, iStart[matN], iStop[matN]);
            
                    // Determine lenght of description part
                    bindsCnt = 0;
                    reader.SeekBlockStart();  // after SBST{
                    CTextUtil.DoOnNewLine(reader.Data, reader.ReaderPos, delegate (long nLPos) {
                        reader.ReaderPos = nLPos + 2;  // in newline
                        if (!Int32.TryParse(CTextUtil.AnsiToStr(reader.PeekLine()).Trim(), out bindsCnt))
                        {
                            bindsCnt = -1;
                        }
                    });
                    if (bindsCnt < 1)
                    {
                        return false;
                    }

                    // Skip material description
                    do{
                        reader.SeekBlockEnd();  // end of binding block
                        bindsCnt--;
                    } while (bindsCnt > 0);
                    reader.SeekBlockEnd(); // end of material description
                    reader.SeekBlockEnd(); // end of material block
                }//for matN
                return true;  // may contain no materials
            }
            bool ReadInWeights(out VertexGrp boneInfluence)
            {
                boneInfluence = new VertexGrp();

                // Read bone influence
                reader.SeekBlockStart();
                string boneNam = reader.ReadText();
                uint size=0;
                int[] ids;
                float[] weights;
                try{
                    size = reader.ReadUInt32();
                    ids = new int[size];
                    weights = new float[size];
                    for (uint i=0; i<size; ++i)
                    {
                        ids[i] = reader.ReadInt32();
                    }
                    for (uint i=0; i<size; ++i)
                    {
                        weights[i] = reader.ReadSingle();
                    }
                } catch {
                    return false;
                }
            
                // Read bind matrix
                float[] buff = new float[16];
                int numVal = 0;
                Transform3DF mat;
                try {
                    for (; (numVal < buff.Length) && reader.HasMore(4); ++numVal)  // 4B float
                    {
                        buff[numVal] = reader.ReadSingle();
                    }
                    mat = new Transform3DF {
                        m00 = buff[0], m01 = buff[4], m02 = buff[8], m03 = buff[12],
                        m10 = buff[1], m11 = buff[5], m12 = buff[9], m13 = buff[13],
                        m20 = buff[2], m21 = buff[6], m22 = buff[10], m23 = buff[14]
                    };
                } catch {
                    if (numVal < 64)
                    {
                        reader.MoveCursor(64-numVal*4);  // sizeOf(mat) == 64
                    }
                    mat = Transform3DF.Identity();
                }
                reader.SeekBlockEnd();

                // Assign bone weight map
                if (size>0)
                {
                    //var weightMap = new System.Collections.Specialized.OrderedDictionary();
                    var weightMap = new Dictionary<int, float>();
                    for (int i=0; i<size; ++i)
                    {
                        weightMap[ids[i]] = weights[i];  // overwrite if exist, add if not
                    }
                    boneInfluence.groupName = boneNam;
                    boneInfluence.mapping = weightMap;
                    boneInfluence.bindMatrix = mat;
                }
                return true;
            }

        }//tryparse


        public static bool TryParseMorph(CMdlFileNavigator reader, out CMorph parsedMorph)
        {
            parsedMorph = null;
            if (null == reader)
            {
                return false;
            }

            CMorph morph = new CMorph {
                Name = reader.ReadText()
            };
            reader.SeekBlockStart();

            bool passed = true;
            string meshTag;
            while (passed && !String.IsNullOrEmpty(meshTag = reader.ReadTag(mTags)))
            {
                reader.SeekBlockStart();
                switch (meshTag)
                {
                case TagInfo.vertex:
                {
                    float[] pt3d = new float[3];
                    passed &= reader.TryOnDataSequence(onElement: delegate () {
                        for (int i=0; i<3; ++i)
                        {
                            pt3d[i] = reader.ReadSingle();
                        }
                        reader.MoveCursor(4);
                        morph.AddVertex(pt3d);
                    });
                    break;
                }//VRTX

                case TagInfo.normal:
                {
                    float[] pt3d = new float[3];
                    passed &= reader.TryOnDataSequence(onElement: delegate () {
                        for (int i=0; i<3; ++i)
                        {
                            pt3d[i] = reader.ReadSingle();
                        }
                    morph.AddNormal(pt3d);
                    });
                    break;
                }//NRML
                
                case TagInfo.unsupported1:
                    try {
                        int valCnt = reader.ReadInt32();
                        reader.MoveCursor(valCnt * 24);
                    } catch {
                        passed = false;
                    }
                    break;

                default:  // known but unhandled tag
                    passed = false;
                    break;
                }//switch

                reader.SeekBlockEnd();
            }
            reader.SeekBlockEnd();

            if (!morph.IsValid())
            {
                return false;
            }
            parsedMorph = morph;
            return passed;
            
        }//tryparsemorph

    }// cls
}// namespace
