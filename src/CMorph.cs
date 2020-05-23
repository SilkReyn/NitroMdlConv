using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NitroMdlConv.Common;


namespace NitroMdlConv.Mdl
{
    public class CMorph
    {
        protected List<Vector3DF> mVerts = new List<Vector3DF>();
        protected List<Vector3DF> mNorms = new List<Vector3DF> ();
        
        
        public string Name {get; set;}
        public IReadOnlyList<Vector3DF> Vertices => mVerts.AsReadOnly();
        public IReadOnlyList<Vector3DF> Normals => mNorms.AsReadOnly();
        
        
        public virtual bool IsValid() =>
            !String.IsNullOrEmpty(Name) &&
            (mVerts.Count > 2) &&
            (mNorms.Count > 0);

        public int VerticesCount() => mVerts.Count();


        public void AddVertex(float[] xyz) => mVerts.Add(new Vector3DF(xyz));

        // Expects normals of a left-hand system? (front face points toward positive (depth) axis)
        public void AddNormal(float[] xyz) => mNorms.Add(new Vector3DF(xyz));  //.Flip() Actually, Normals makes no difference if imported by collada into blender

        public bool MorphMesh(CMesh targetMesh)
        {
            if ((targetMesh?.mVerts.Count ?? 0) != mVerts.Count || String.IsNullOrWhiteSpace(targetMesh.Name))
            {
                return false;
            }
            //targetMesh.mVerts.Clear(); might be undesired if data is referenced somewhere else
            //targetMesh.mNorms.Clear();
            targetMesh.mVerts = mVerts;
            targetMesh.mNorms = mNorms;

            // Move morph-target name and set morph shape name.
            targetMesh.OriginName = targetMesh.Name;
            targetMesh.Name = Name.StartsWith("morph") ? Name : "morph_" + Name;
            return true; // targetMesh.IsValid();
        }
    }
}
