//using System;
//using System.Collections.Generic;
using System.Globalization;
//using System.Linq;

/*
  related Net 4.6 features (2015)
  Current Culture can be set
  Has FormattableString with .Invariant()
  Has vector3 and matrix4x4
 */

namespace NitroMdlConv.Common
{
    public struct Entities
    {
        public Mdl.CFrame rootNode;
        public System.Collections.Generic.List<Mdl.CMesh> meshes;

        public bool IsDefined() => (null != rootNode) && (null != meshes);
        public bool HasAny() => (rootNode?.HasChilds() ?? false) || ((meshes?.Count ?? 0) > 0);
    }


    public struct Transform3DF
    {
        public float m00, m01, m02, m03;
        public float m10, m11, m12, m13;
        public float m20, m21, m22, m23;

        //public void SetTransform(float[] matrix4x3_rowMajor)
        //{
        //    if (matrix4x3_rowMajor.Length < 12)
        //    {
        //        return;
        //    }
        //    m00 = matrix4x3_rowMajor[0];
        //    m01 = matrix4x3_rowMajor[1];
        //    m02 = matrix4x3_rowMajor[2];
        //    m03 = matrix4x3_rowMajor[3];

        //    m10 = matrix4x3_rowMajor[4];
        //    m11 = matrix4x3_rowMajor[5];
        //    m12 = matrix4x3_rowMajor[6];
        //    m13 = matrix4x3_rowMajor[7];

        //    m20 = matrix4x3_rowMajor[8];
        //    m21 = matrix4x3_rowMajor[9];
        //    m22 = matrix4x3_rowMajor[10];
        //    m23 = matrix4x3_rowMajor[11];
        //}
        
        public static Transform3DF Identity()
        {
            Transform3DF trns = default(Transform3DF);  // zeroes
            trns.m00 = 1f;
            trns.m11 = 1f;
            trns.m22 = 1f;
            return trns;  // identity
        }
        
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:g7} {1:g7} {2:g7} {3:g7} {4:g7} {5:g7} {6:g7} {7:g7} {8:g7} {9:g7} {10:g7} {11:g7} {12} {13} {14} {15}",
                m00, m01, m02, m03, m10, m11, m12, m13, m20, m21, m22, m23, 0, 0, 0, 1
                );
        }

        // transform.inv = rotation.transpose * -translate
    }


    public struct Vector4DF
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public override string ToString()
        {
            return System.String.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:g7} {1:g7} {2:g7} {3:g7}", x, y, z, w);
        }
    }


    public struct Vector3DF
    {
        public float x;
        public float y;
        public float z;

        public Vector3DF(float[] xyz)
        {
            x = y = z = 0.0f;
            switch (xyz.Length)
            {
            default:
                if (xyz.Length < 1)
                {
                    break;
                }
                goto case 3;
            case 3:
                z = xyz[2];
                goto case 2;
            case 2:
                y = xyz[1];
                goto case 1;
            case 1:
                x = xyz[0];
                break;
            }
        }

        public Vector3DF Flip()
        {
            x = -x; y = -y; z = -z;  // might become '-0'
            return this;
        }

        public override string ToString()
        {
            return System.String.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:g7} {1:g7} {2:g7}", x, y, z);
        }
    }


    public struct Vector2DF
    {
        public float x;
        public float y;

        public override string ToString()
        {
            return x.ToString("g7", CultureInfo.InvariantCulture.NumberFormat)
                + " "
                + y.ToString("g7", CultureInfo.InvariantCulture.NumberFormat);
        }
    }


    public struct Triangle
    {
        public ushort iVert1;
        public ushort iVert2;
        public ushort iVert3;
        public bool IsValid() => (iVert1 != iVert2) && (iVert1 != iVert3) && (iVert2 != iVert3);
    }


    public struct Reference
    {
        public uint iFirst;
        public uint iLast;
        public string name;
        public uint IndexCount() => (iFirst != iLast) ? (uint)System.Math.Abs(iLast - iFirst) + 1 : 0;
    }


    public struct VertexGrp
    {
        public string groupName;
        public System.Collections.Generic.Dictionary<int, float> mapping;
        public Transform3DF bindMatrix;  // TODO: check if it needs to be inverted

        public bool IsNullOrEmpty() => (mapping == null) ? true : mapping.Count == 0;
        //public bool IsNullOrTrivial() =>(mapping == null) ? true : mapping.All(map => map.Value == 0);
    }
    
}