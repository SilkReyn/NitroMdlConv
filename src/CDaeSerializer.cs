using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Threading.Tasks;

using NitroMdlConv.Common;


namespace NitroMdlConv
{
    public static class XmlExt
    {
        public static void WriteAttElement(this XmlWriter xml, string element, string[] attributes, params string[] values)
        {
            xml.WriteStartElement(element);
            int upper = Math.Min(attributes.Length, values.Length);
            for (int i=0; i<upper; ++i)
            {
                xml.WriteAttributeString(attributes[i], values[i]);
            }
            xml.WriteEndElement();
        }

        public static void WriteSpacedValues<T>(this XmlWriter xml, params T[] values)
            where T:struct, IConvertible, IComparable
        {
            if (null == values || values.Length < 1)
            {
                return;
            }
            xml.WriteValue(values);
        }
    }

    public class CDaeSerializer
    {
        const string V4_ZERO = "0 0 0 1";
        const string COLOR = "color";
        const string FLOAT = "float";
        const string ID = "id";
        const string SEPARATOR = " ";
        const string NAME = "name";
        const string SOURCE = "source";
        const string COUNT = "count";
        const string PARAM = "param";
        const string INPUT = "input";
        const string SEMANTIC = "semantic";
        const string TYPE = "type";
        const string SID = "sid";
        const string _SKIN = "-skin";
        const string _ARRAY = "-array";
        const string _MESH = "-mesh";

        readonly string[] ASTR_SEM_SRC = new string[] { SEMANTIC, SOURCE };


        public string RootName { get; set; }


        public void Serialize(Stream dst, Entities data)
        {
            if (null == dst || null == data.meshes || null ==data.rootNode)
            {
                throw new ArgumentException("CDaeSerializer::Serialize: Passed 'null'-Argument");
            }

            XmlWriter writer = XmlWriter.Create(dst, new XmlWriterSettings() {
                Indent = true,
                IndentChars = "\t",

                // defaults
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
                NewLineOnAttributes = false,
                Encoding = Encoding.UTF8
            });
            
            var materialNames = data.meshes
                .SelectMany(msh => msh.MaterialSlots)
                .Select(mat => mat.name)
                .Distinct();
            dst.Position = 0;
            string srcId, baseName;
            List<Mdl.CFrame> nodesList = data.rootNode.Childs;
            if (!String.IsNullOrWhiteSpace(RootName))
            {
                baseName = RootName;
            } else if (!String.IsNullOrWhiteSpace(data.rootNode.Name)){
                baseName = data.rootNode.Name;
            } else {
                baseName = "RootNode";
            }

            // XML Header
            writer.WriteStartDocument();
            writer.WriteStartElement("COLLADA", "http://www.collada.org/2005/11/COLLADASchema");  // root with default namespace
            writer.WriteAttributeString("version", "1.4.1");
            writer.WriteAttributeString("xmlns", "xsi", null, "http://www.w3.org/2001/XMLSchema-instance");  // declares a namespace prefixed with xsi

            #region File meta
            writer.WriteStartElement("asset");
            {
                writer.WriteStartElement("contributor");
                {
                    writer.WriteElementString("author", "Nitro+");
                    writer.WriteElementString("authoring_tool", "Nitro Mdl-Converter");
                    writer.WriteEndElement();
                }
                writer.WriteElementString("created", DateTime.Now.ToString("s"));

                // Unit
                writer.WriteStartElement("unit");
                writer.WriteAttributeString(NAME, "millimeter");  // Millimeters?
                writer.WriteAttributeString("meter", "0.001");
                writer.WriteEndElement();

                writer.WriteElementString("up_axis", "Z_UP");  // Geometries seem to be at least on Zup
                writer.WriteEndElement();  // asset
            }
            #endregion

            // Texture paths (not supported, requires parsing .mtl files)
            writer.WriteStartElement("library_images");
            writer.WriteEndElement();

            #region Material shading
            writer.WriteStartElement("library_effects");
            {
                writer.WriteStartElement("effect");
                {   writer.WriteAttributeString(ID, "shadeless-effect");

                    writer.WriteStartElement("profile_COMMON");
                    {
                        writer.WriteStartElement("technique");
                        {   writer.WriteAttributeString(SID, "common");

                            writer.WriteStartElement("lambert");
                            {
                                addEffectItem("emission", COLOR, V4_ZERO);
                                addEffectItem("ambient", COLOR, V4_ZERO);
                                addEffectItem("diffuse", COLOR, "1 1 1 1");
                                //addEffectItem("specular", COLOR, V4_HALF);
                                //addEffectItem("shininess", FLOAT, "50");
                                addEffectItem("index_of_refraction", FLOAT, "1");
                                writer.WriteEndElement();  // phong
                            }
                            writer.WriteEndElement();  // technique
                        }
                        writer.WriteEndElement();  // profile
                    }
                    writer.WriteEndElement();  // effect
                }
                writer.WriteEndElement();  // effects

                void addEffectItem(string item, string type, string value)
                {
                    writer.WriteStartElement(item);
                    writer.WriteStartElement(type);
                    writer.WriteAttributeString(SID, item);
                    writer.WriteString(value);
                    writer.WriteEndElement();  // type
                    writer.WriteEndElement();  // item
                }
            }
            #endregion
            
            #region Materials
            writer.WriteStartElement("library_materials");
            foreach (string name in materialNames)
            {
                writer.WriteStartElement("material");
                writer.WriteAttributeString(ID, $"{name}-material");
                writer.WriteAttributeString(NAME, name);
                writer.WriteStartElement("instance_effect");
                writer.WriteAttributeString("url", "#shadeless-effect");
                writer.WriteEndElement();  // instance_effect
                writer.WriteEndElement();  // material
            }
            writer.WriteEndElement(); // materials
            #endregion
            
            #region Meshes
            writer.WriteStartElement("library_geometries");
            {
                string meshId;
                bool hasUV;
                Mdl.CMesh mesh;
                var meshGrps = data.meshes.GroupBy(msh => msh.Name);
                foreach (var meshSet in meshGrps)
                {
                    mesh = meshSet.First(msh => msh.IsValid() && !String.IsNullOrWhiteSpace(msh.Name));
                    if ((null == mesh) || mesh.HasInvalidFaces())
                    {
                        Console.WriteLine("CDaeSerializer: Skipped a mesh geometry!");
                        continue;
                    }
                    hasUV = mesh.HasUV();

                    meshId = mesh.IsMorphed ? $"{mesh.OriginName}_{mesh.Name}{_MESH}" : mesh.Name + _MESH;
                    writer.WriteStartElement("geometry");
                    {   writer.WriteAttributeString(ID, meshId);
                        writer.WriteAttributeString(NAME, mesh.Name);

                        writer.WriteStartElement("mesh");
                        {
                            // 3D Coordinates (mesh)
                            addSource3D(mesh.Vertices, id: $"{meshId}-positions");
                            addSource3D(mesh.Normals, id: $"{meshId}-normals");

                            // UV mapping
                            if (hasUV)
                            {
                                writer.WriteStartElement(SOURCE);
                                {   writer.WriteAttributeString(ID, srcId = $"{meshId}-map-0");

                                    var pts2D = mesh.TexCoords;
                                    int cnt = pts2D.Count;
                                    writer.WriteStartElement("float_array");
                                    {   writer.WriteAttributeString(ID, srcId+=_ARRAY);
                                        writer.WriteAttributeString(COUNT, (2 * cnt).ToString());
                                        
                                        writer.WriteString(pts2D[0].ToString());
                                        for (int vn=1; vn<cnt; ++vn)
                                        {
                                            writer.WriteWhitespace(SEPARATOR);
                                            writer.WriteString(pts2D[vn].ToString());
                                        }
                                        writer.WriteEndElement();  // float array
                                    }
                                    addTechCommon(srcId, cnt, 2, FLOAT, "S", "T");
                                    writer.WriteEndElement();  // map-0
                                }
                            }// has uv

                            // Vertices
                            writer.WriteStartElement("vertices");
                            writer.WriteAttributeString("id", $"{meshId}-vertices");
                            writer.WriteAttElement(INPUT, ASTR_SEM_SRC, "POSITION", $"#{meshId}-positions");
                            writer.WriteEndElement();  // vertices

                            // Faces
                            var meshFaces = mesh.Faces;  // creates readOnly on call
                            var matRefs = mesh.MaterialSlots;
                            for (int mn=0; mn<matRefs.Count; ++mn)
                            {
                                addTrianglesByMaterial(meshFaces, matRefs[mn]);
                            }
                            writer.WriteEndElement();  // mesh
                        }
                        writer.WriteEndElement();  // geometry
                    }
                }// foreach mesh
                writer.WriteEndElement(); // geometries

                void addSource3D(IReadOnlyList<Vector3DF> pts3d, string id)
                {
                    writer.WriteStartElement(SOURCE);
                    {   writer.WriteAttributeString(ID, id);

                        string mpa = id + _ARRAY;
                        writer.WriteStartElement("float_array");
                        {   writer.WriteAttributeString(ID, mpa);
                            writer.WriteAttributeString(COUNT, (pts3d.Count * 3).ToString());
                            
                            writer.WriteString(pts3d[0].ToString());  // not in specific xml format
                            for (int vn=1; vn<pts3d.Count; ++vn)
                            {
                                writer.WriteWhitespace(SEPARATOR);
                                writer.WriteString(pts3d[vn].ToString());
                            }
                            writer.WriteEndElement();  // float array
                        }
                        addTechCommon(mpa, pts3d.Count, 3, FLOAT, "X", "Y", "Z");
                        writer.WriteEndElement();  // source
                    }
                }
                void addTrianglesByMaterial(IReadOnlyList<Triangle>triSrc, Reference matRef)
                {
                    if (matRef.IndexCount() < 1)
                    {// no faces assigned
                        return;
                    }
                    writer.WriteStartElement("triangles");
                    writer.WriteAttributeString("material", $"{matRef.name}-material");
                    writer.WriteAttributeString(COUNT, matRef.IndexCount().ToString());
                    
                    // List mapped components
                    string[] inputAtrb = new string[] { SEMANTIC, SOURCE, "offset", "set" };
                    writer.WriteAttElement(INPUT, inputAtrb, "VERTEX", $"#{meshId}-vertices", "0");
                    writer.WriteAttElement(INPUT, inputAtrb, "NORMAL", $"#{meshId}-normals", "1");
                    if (hasUV)
                    {
                        writer.WriteAttElement(INPUT, inputAtrb, "TEXCOORD", $"#{meshId}-map-0", "2", "0");
                    }

                    // Write indices of each component that is mapped to the triangle edges (interlaced)
                    // Currently handles only rectangular mapping (each mapped component has equal element count)
                    writer.WriteStartElement("p");
                    long upperLim = Math.Min(matRef.iLast, triSrc.Count-1);
                    for (int faceN = (int)Math.Min(matRef.iFirst, triSrc.Count); faceN <= upperLim; ++faceN)
                    {
                        ushort ia = triSrc[faceN].iVert1;
                        ushort ib = triSrc[faceN].iVert2;
                        ushort ic = triSrc[faceN].iVert3;
                        if (hasUV)
                        {// vertice, normal and texture coordinate indices
                            writer.WriteSpacedValues(ia, ia, ia, ib, ib, ib, ic, ic, ic);
                        } else {
                            writer.WriteSpacedValues(ia, ia, ib, ib, ic, ic);
                        }
                        if (faceN < upperLim)
                        {
                            writer.WriteWhitespace(SEPARATOR);
                        }
                    }
                    writer.WriteEndElement();  // p
                    writer.WriteEndElement();  // triangles
                }
            }
            #endregion

            var baseMeshes = data.meshes.Where(msh => !msh.IsMorphed);
            int meshNodesCount = baseMeshes.Count();
            #region Skinning
            writer.WriteStartElement("library_controllers");
            if (nodesList.Count > 1)
            {
                IReadOnlyList<VertexGrp> meshWeights;
                int weightsCnt;
                bool hasBones, hasWeights;
                string frameName;
                var frameGrps = nodesList.GetRange(1, nodesList.Count-1)  // leave out first, which is armature
                    .Where(frm => !String.IsNullOrWhiteSpace(frm.Name))
                    .GroupBy(frm => frm.Name);
                foreach (var frmSet in frameGrps)
                {
                    Mdl.CMesh mesh = null;
                    frameName = frmSet.First().Name;

                    // index 0 MUST be armature and the order of meshes and related nodes in the list must be same
                    int meshIdx = nodesList.FindIndex(frm => frm.Name == frameName) - 1;
                    if (meshIdx >= 0 && (meshIdx < meshNodesCount))
                    {
                        mesh = baseMeshes.ElementAt(meshIdx);
                    }
                    if (null == mesh)  
                    {
                        continue;
                    }
                    
                    srcId = $"{baseName}_{frameName}{_SKIN}";
                    writer.WriteStartElement("controller");
                    {   writer.WriteAttributeString(ID, srcId);
                        writer.WriteAttributeString(NAME, baseName);

                        writer.WriteStartElement("skin");
                        {   writer.WriteAttributeString(SOURCE, $"#{mesh.Name}{_MESH}");

                            // Transforms object to world space,
                            // also visible as mesh Transform
                            // Effectively moves origin of mesh, but must fit to bone transform
                            writer.WriteStartElement("bind_shape_matrix");
                            writer.WriteSpacedValues(
                                1, 0, 0, 0,
                                0, 1, 0, 0,
                                0, 0, 1, 0,
                                0, 0, 0, 1);  // identity
                            writer.WriteEndElement(); // bind shape

                            meshWeights = mesh.Weights;
                            weightsCnt = meshWeights.Count;
                            if (weightsCnt < 1)
                            {
                                writer.WriteEndElement();  // skin
                                writer.WriteEndElement();  // controller
                                continue;  // next mesh
                            }

                            // Bone src
                            var distinctBones = meshWeights
                                    .Select(w => w.groupName)
                                    .Where(gn => !String.IsNullOrWhiteSpace(gn))
                                    .Distinct();  // TODO: Merge identically named groups?
                            if (hasBones = distinctBones.Any())
                            {
                                writer.WriteStartElement(SOURCE);
                                {   writer.WriteAttributeString(ID, srcId+="-joints");

                                    writer.WriteStartElement("Name_array");
                                    writer.WriteAttributeString(ID, srcId+=_ARRAY);
                                    writer.WriteAttributeString(COUNT, weightsCnt.ToString());

                                    writer.WriteValue(distinctBones);  // IEnumerable
                                    writer.WriteEndElement();  // name array

                                    addTechCommon(srcId, weightsCnt, stride: 1, type: NAME, values: "JOINT");
                                    writer.WriteEndElement();  // source joints
                                }
                            }

                            // Bind-pose src
                            // transforms bones from world space to space of the root bone
                            srcId = $"{baseName}_{frameName}{_SKIN}";
                            writer.WriteStartElement(SOURCE);
                            {   writer.WriteAttributeString(ID, srcId+="-bind_poses");

                                writer.WriteStartElement("float_array");
                                writer.WriteAttributeString(ID, srcId+=_ARRAY);
                                writer.WriteAttributeString(COUNT, (weightsCnt * 16).ToString());

                                // TODO: matrices could be undefined
                                writer.WriteString(meshWeights[0].bindMatrix.ToString());  // returns 16 floats as string
                                for (int wn=1; wn<weightsCnt; ++wn)
                                {
                                    writer.WriteWhitespace(SEPARATOR);
                                    writer.WriteString(meshWeights[wn].bindMatrix.ToString());
                                }
                                writer.WriteEndElement();  // array

                                addTechCommon(srcId, weightsCnt, stride: 16, type: "float4x4", values: "TRANSFORM");
                                writer.WriteEndElement();  // source binds
                            }

                            // Weight src
                            int weightsLen = mesh.WeightsLenght();
                            if (hasWeights = (weightsLen > 0))
                            {
                                srcId = $"{baseName}_{frameName}{_SKIN}";
                                writer.WriteStartElement(SOURCE);
                                {   writer.WriteAttributeString(ID, srcId+="-weights");

                                    writer.WriteStartElement("float_array");
                                    writer.WriteAttributeString(ID, srcId+=_ARRAY);
                                    writer.WriteAttributeString(COUNT, weightsLen.ToString());
                                
                                    if (!meshWeights[0].IsNullOrEmpty())  // Has assigned bone, but no weight paint
                                    {
                                        writer.WriteValue(meshWeights[0].mapping.Values);  // ValueCollection
                                    }
                                    for (int wn=1; wn<weightsCnt; ++wn)
                                    {
                                        if (!meshWeights[wn].IsNullOrEmpty())
                                        {
                                            writer.WriteWhitespace(SEPARATOR);
                                            writer.WriteValue(meshWeights[wn].mapping.Values);
                                        }
                                    }
                                    writer.WriteEndElement();  // array
                                    addTechCommon(srcId, weightsLen, stride: 1, type: FLOAT, values: "WEIGHT");
                                    writer.WriteEndElement();  // source weights
                                }
                            }// if weights assigned

                            // Bone-binding
                            if (hasBones)
                            {
                                srcId = $"#{baseName}_{frameName}{_SKIN}";
                                writer.WriteStartElement("joints");
                                writer.WriteAttElement(INPUT, ASTR_SEM_SRC, "JOINT", srcId+"-joints");
                                writer.WriteAttElement(INPUT, ASTR_SEM_SRC, "INV_BIND_MATRIX", srcId+"-bind_poses");
                                writer.WriteEndElement();  // joints
                            }

                            // Vertex-Weight map
                            if (hasBones && hasWeights)
                            {
                                
                                int vertCnt = mesh.VerticesCount();
                                writer.WriteStartElement("vertex_weights");
                                {   writer.WriteAttributeString(COUNT, vertCnt.ToString());

                                    string[] inputAtrb = new string[] { SEMANTIC, SOURCE, "offset" };
                                    writer.WriteAttElement(INPUT, inputAtrb, "JOINT", srcId+"-joints", "0");
                                    writer.WriteAttElement(INPUT, inputAtrb, "WEIGHT", srcId+"-weights", "1");

                                    // Collect bindings
                                    List<int> vMap = new List<int>();
                                    var boneWeightSeq = new List<int>();
                                    int idxOffs;
                                    writer.WriteStartElement("vcount");  // number of assigned bones per vertex
                                    for (int vn=0; vn<vertCnt; ++vn)
                                    {// iterate mesh vertices
                                        vMap.Clear();
                                        idxOffs = 0;
                                        Dictionary<int, float> wMap;
                                        for (int wn=0; wn<weightsCnt; wn++)
                                        {// iterate assigned bone influences
                                            wMap = meshWeights[wn].mapping;
                                            if((wMap?.ContainsKey(vn) ?? false) &&
                                                !String.IsNullOrWhiteSpace(meshWeights[wn].groupName))
                                            {
                                                vMap.Add(wn);  // bone index
                                                int mN = 0;
                                                foreach (var kvp in wMap)
                                                {// iterate map to find index of weight
                                                 // keys are not in order of addition, but sorted by weight-index
                                                 // the dictionary MUST NOT have changed since writing out the values
                                                    if (kvp.Key == vn)
                                                    {
                                                        vMap.Add(idxOffs+mN);  // map element index
                                                        break;
                                                    }
                                                    ++mN;
                                                }
                                            }
                                            idxOffs += wMap.Count;
                                        }
                                        writer.WriteValue(vMap.Count >> 1); writer.WriteWhitespace(SEPARATOR);
                                        boneWeightSeq.AddRange(vMap);
                                    }
                                    writer.WriteEndElement();  // vcount
                                    
                                    writer.WriteStartElement("v");
                                    writer.WriteValue(boneWeightSeq);  // List<T>
                                    boneWeightSeq.Clear();
                                    writer.WriteEndElement();  // v
                                    writer.WriteEndElement();  // vertex weights
                                }
                            }// has bones or weight assigned
                            writer.WriteEndElement();  // skin
                        }
                        writer.WriteEndElement();  // controller
                    }
                }// each mesh
            }// if has nodes
            #endregion

            #region Morphes
            foreach (var mesh in baseMeshes)
            {
                var shapes = mesh.Shapes.Keys.ToArray();
                if (null == mesh || shapes.Length < 1)
                {
                    continue;
                }

                // Link shape keys to target mesh
                srcId = mesh.OriginName + "_" + mesh.Name;  // origin or base name?
                writer.WriteStartElement("controller");
                {
                    writer.WriteAttributeString(ID, srcId + "-morph");
                    writer.WriteAttributeString(NAME, mesh.Name);
                        
                    writer.WriteStartElement("morph");
                    {   writer.WriteAttributeString(SOURCE, "#" + mesh.Name + _MESH);  //references the geometry
                        writer.WriteAttributeString("method", "NORMALIZED");

                        
                        writer.WriteStartElement(SOURCE);
                        {   writer.WriteAttributeString(ID, srcId += "-targets");
                                
                            writer.WriteStartElement("IDREF_array");
                            writer.WriteAttributeString(ID, srcId += _ARRAY);
                            writer.WriteAttributeString(COUNT, shapes.Length.ToString());

                            writer.WriteString($"{mesh.Name}_{shapes[0]}{_MESH}");
                            for (int i=1; i<shapes.Length; ++i)
                            {
                                writer.WriteWhitespace(SEPARATOR); writer.WriteString($"{mesh.Name}_{shapes[i]}{_MESH}");
                            }
                            writer.WriteEndElement();  // array
                            
                            addTechCommon(srcId, shapes.Length, stride: 1, type: "IDREF", values: "IDREF");
                            writer.WriteEndElement();  // target source
                        }
                        srcId = mesh.OriginName + "_" + mesh.Name;
                        writer.WriteStartElement(SOURCE);
                        {   writer.WriteAttributeString(ID, srcId += "-weights");

                            writer.WriteStartElement("float_array");
                            writer.WriteAttributeString(ID, srcId += _ARRAY);
                            writer.WriteAttributeString(COUNT, shapes.Length.ToString());
                            writer.WriteValue(mesh.Shapes.Values);
                            writer.WriteEndElement();  // float array
                            addTechCommon(srcId, shapes.Length, stride: 1, type: FLOAT, values: "MORPH_WEIGHT");
                            writer.WriteEndElement();  // weight source
                        }
                        srcId = "#" + mesh.OriginName + "_" + mesh.Name;
                        writer.WriteStartElement("targets");
                        writer.WriteAttElement(INPUT, ASTR_SEM_SRC, "MORPH_TARGET", srcId + "-targets");
                        writer.WriteAttElement(INPUT, ASTR_SEM_SRC, "MORPH_WEIGHT", srcId + "-weights");
                        writer.WriteEndElement();  //targets
                        writer.WriteEndElement();  //morph
                    }
                    writer.WriteEndElement();  // morph controller
                }
            }// each non-morph

            writer.WriteEndElement();  // controller lib
            #endregion

            #region Object lib
            writer.WriteStartElement("library_visual_scenes");
            {
                writer.WriteStartElement("visual_scene");
                {   writer.WriteAttributeString(ID, "Scene");
                    writer.WriteAttributeString(NAME, "Scene");

                    // Object nodes
                    // Origin of the object instances
                    if (data.rootNode.HasChilds())
                    {
                        string node0_Id;  // TODO: find root of multiple armatures

                        // Armature
                        writer.WriteStartElement("node");
                        {   writer.WriteAttributeString(ID, baseName);
                            writer.WriteAttributeString(NAME, baseName);
                            writer.WriteAttributeString(TYPE, "NODE");

                            writer.WriteStartElement("matrix");
                            writer.WriteAttributeString(SID, "transform");
                    
                            writer.WriteString(Transform3DF.Identity().ToString());  //data.rootNode.Transform.ToString()
                            writer.WriteEndElement();  // matrix
                        
                            node0_Id = addJoint(nodesList[0]);  // first must be skeleton
                            writer.WriteEndElement();  // node
                        }

                        // Mesh nodes
                        string nodeName;
                        for (int fn=1; fn<nodesList.Count; ++fn)
                        {
                            if (!String.IsNullOrWhiteSpace(nodeName=nodesList[fn].Name))
                            {
                                // This fails when multiple meshes contain same name fragment:
                                //boundMaterialNames: data.meshes.Find(msh => nodeName.Contains(msh.Name))?.MaterialSlots.Select(slot => slot.name),

                                // This works only when nodes and related meshes in the list appear in same order!
                                addMeshNode(
                                    nodesList[fn],
                                    boundMaterialNames: baseMeshes.ElementAt(fn-1).MaterialSlots.Select(slot => slot.name),
                                    controllerId: $"{baseName}_{nodeName}{_SKIN}",
                                    skeletionId: node0_Id
                                );
                            }
                        }
                    }
                    writer.WriteEndElement();  // vis scene
                }
                writer.WriteEndElement();  // vis scene lib

                string addJoint(Mdl.CFrame node)
                {
                    if (null == node)
                    {
                        throw new ArgumentException("CDaeSerializer::Serialize::addJoint: Passed 'null'-argument", "node");
                    }
                    string skeletonId = String.IsNullOrWhiteSpace(baseName) ? node.Name : $"{baseName}_{node.Name}";
                    writer.WriteStartElement("node");
                    writer.WriteAttributeString(ID, skeletonId);
                    writer.WriteAttributeString(NAME, node.Name);
                    writer.WriteAttributeString(SID, node.Name);
                    writer.WriteAttributeString(TYPE, "JOINT");

                    writer.WriteStartElement("matrix");
                    writer.WriteAttributeString(SID, "transform");
                    
                    writer.WriteString(node.Transform.ToString());
                    writer.WriteEndElement();  // matrix
                    
                    if (node.HasChilds())
                    {
                        foreach (var child in node.Childs)
                        {
                            addJoint(child);
                        }
                    }
                    writer.WriteEndElement();  // node
                    return skeletonId;
                }
                void addMeshNode(Mdl.CFrame node, IEnumerable<string> boundMaterialNames, string controllerId, string skeletionId)
                {
                    if ((null==node) ||(null==boundMaterialNames) || (null==controllerId) || (null==skeletionId))
                    {
                        throw new ArgumentException("CDaeSerializer::Serialize::addMeshNode: Passed 'null'-argument");
                    }
                    writer.WriteStartElement("node");
                    writer.WriteAttributeString(ID, node.Name);  // does not support nested meshes
                    writer.WriteAttributeString(NAME, node.Name);
                    writer.WriteAttributeString(TYPE,"NODE");

                    // This seem to have no effect at Blender import
                    // 
                    //writer.WriteStartElement("matrix");
                    //{   writer.WriteAttributeString(SID, "transform");

                    //    writer.WriteString(node.Transform.ToString());
                    //    writer.WriteEndElement();  // matrix
                    //}
                    writer.WriteStartElement("instance_controller");
                    {   writer.WriteAttributeString("url", "#" + controllerId);

                        writer.WriteElementString("skeleton", "#" + skeletionId);

                        writer.WriteStartElement("bind_material");
                        {
                            writer.WriteStartElement("technique_common");
                            string[] atrb = new string[] { "symbol", "target" };
                            foreach (string mat in boundMaterialNames)
                            {
                                writer.WriteAttElement("instance_material", atrb, mat+"-material", $"#{mat}-material");
                            }
                            writer.WriteEndElement();  // tech
                            writer.WriteEndElement();  // bind mat
                        }
                        writer.WriteEndElement();  // instance controller
                    }
                    writer.WriteEndElement();  // node
                }
            }
            #endregion

            // Instances
            writer.WriteStartElement("scene");
            writer.WriteAttElement("instance_visual_scene", new string[] { "url" }, "#Scene");
            writer.WriteEndElement();  //scene
            
            writer.Close();
            writer.Dispose();
            return;

            void addTechCommon(string id, int count, int stride, string type, params string[] values)
            {
                writer.WriteStartElement("technique_common");
                writer.WriteStartElement("accessor");
                writer.WriteAttributeString(SOURCE, "#"+id);
                writer.WriteAttributeString(COUNT, count.ToString());
                writer.WriteAttributeString("stride", stride.ToString());
                string[] paramAtrb = new string[] { NAME, TYPE };
                foreach (var val in values)
                {
                    writer.WriteAttElement(PARAM, paramAtrb, val, type);
                }
                writer.WriteEndElement();  // accessor
                writer.WriteEndElement();  // technique
            }
        }// Serialize

    }// class
}//namespace
