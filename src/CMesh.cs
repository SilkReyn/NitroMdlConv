using System.Collections.Generic;
using System.Linq;

using NitroMdlConv.Common;


namespace NitroMdlConv.Mdl
{
    public class CMesh : CMorph
    {
        //List<Vector3DF> mVerts = new List<Vector3DF>();
        //List<Vector3DF> mNorms = new List<Vector3DF> ();
        List<Vector2DF> mUVs = new List<Vector2DF>();
        List<Triangle> mTris = new List<Triangle>();
        List<Reference> mMats = new List<Reference>();  // Holds face indices
        List<VertexGrp> mWeights = new List<VertexGrp>();
        
        public string OriginName {get; set;}
        //public IReadOnlyList<Vector3DF> Vertices => mVerts.AsReadOnly();
        //public IReadOnlyList<Vector3DF> Normals => mNorms.AsReadOnly();
        public IReadOnlyList<Vector2DF> TexCoords => mUVs.AsReadOnly();
        public IReadOnlyList<Triangle> Faces => mTris.AsReadOnly();
        public IReadOnlyList<Reference> MaterialSlots => mMats.AsReadOnly();
        public IReadOnlyList<VertexGrp> Weights => mWeights.AsReadOnly();
        public Dictionary<string, float> Shapes { get; } = new Dictionary<string, float>();
        public bool IsMorphed { get; private set; } = false;

        public override bool IsValid() =>
            // TODO: add name validation?
            (base.mVerts.Count > 2) &&
            (base.mNorms.Count > 0) &&
            (mTris.Count > 0) &&
            (mMats.Count > 0) &&
            !HasInvalidFaces();

        public bool HasUV() => mUVs.Count > 0;

        public bool HasInvalidFaces() => mTris.Any(tri => !tri.IsValid());  // TODO: Make use of these evaluations

        public bool HasUnassignedWeights() => (mWeights.Count > 0) ? mWeights.Any(g => g.IsNullOrEmpty()) : false;

        public int WeightsLenght() => mWeights.Sum(g => g.mapping?.Count ?? 0);

        //public void AddVertex(float[] xyz) => mVerts.Add(new Vector3DF(xyz));

        //public void AddNormal(float[] xyz) => mNorms.Add(new Vector3DF(xyz));

        // Expects 2D-normals with top left origin
        public void AddTexCoord(float u, float v) => mUVs.Add(new Vector2DF { x = u, y = 1f-v });

        public void AssignWeight(VertexGrp grp) => mWeights.Add(grp);  // Can be assigned to non-existing vertices

        public void AssignFace(ushort[] indices)
        {
            if (indices.Length >= 3)
            {
                mTris.Add(new Triangle{
                iVert1 = indices[2],
                iVert2 = indices[1],
                iVert3 = indices[0]
                });
            }
        }

        public void AssignMaterial(string matName, uint beginAtFaceIdx, uint stopAtFaceIdx)
        {
            mMats.Add(new Reference{
                iFirst = beginAtFaceIdx,
                iLast = stopAtFaceIdx,
                name = matName
            });
        }

        public CMesh CreateMorphedMesh(CMorph morph)
        {
            // shallow copy
            CMesh clone = (CMesh)this.MemberwiseClone();
            clone.IsMorphed = morph.MorphMesh(clone);
            Shapes[clone.Name] = 0f;  // weight setting 0-1f
            return clone;
        }
    }// class
}// namespace
