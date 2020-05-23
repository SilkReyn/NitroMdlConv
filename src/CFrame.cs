using System;
using System.Collections.Generic;

using NitroMdlConv.Common;


namespace NitroMdlConv.Mdl
{
    public class CFrame
    {
        public string Name { get; set; }
        public Transform3DF Transform { get; private set; } = Transform3DF.Identity();
        public List<CFrame> Childs { get; } = new List<CFrame>();
        public bool IsDirty { get; set; } = false;


        public CFrame(string name)
        {
            Name = name ?? String.Empty;
            Transform = Transform3DF.Identity();
        }

        public CFrame(string name, float[] matrix4x4_colMajor)
        {
            Name = name ?? String.Empty;
            SetTransform(matrix4x4_colMajor);
        }

        public void SetTransform(float[] matrix4x4_colMajor)
        {
            if (matrix4x4_colMajor.Length < 16)
            {
                throw new ArgumentException("CFrame(): Requires 16 values.", "float[] matrix4x4_colMajor");
            }
            // Round to zero for small numbers
            // (once on set, later computation is not checked)
            for (int i=matrix4x4_colMajor.Length-1; i>=0; --i)
            {
                if (System.Math.Abs(matrix4x4_colMajor[i]) < 1.0e-08F)
                {
                    matrix4x4_colMajor[i] = 0f;
                }
            }
            Transform3DF trns;
            trns.m00 = matrix4x4_colMajor[0];
            trns.m10 = matrix4x4_colMajor[1];
            trns.m20 = matrix4x4_colMajor[2];

            trns.m01 = matrix4x4_colMajor[4];
            trns.m11 = matrix4x4_colMajor[5];
            trns.m21 = matrix4x4_colMajor[6];

            trns.m02 = matrix4x4_colMajor[8];
            trns.m12 = matrix4x4_colMajor[9];
            trns.m22 = matrix4x4_colMajor[10];

            trns.m03 = matrix4x4_colMajor[12];
            trns.m13 = matrix4x4_colMajor[13];
            trns.m23 = matrix4x4_colMajor[14];
            // last line of homogenous matrix is thrown away
            Transform = trns;
        }

        public bool HasChilds() => Childs.Count > 0;
    }
}
