﻿using System.Diagnostics;
using System.Text;
using System.Xml;
using THREE;

namespace URDFLoader;

#pragma warning disable CS8625

/// **********************************************************************************************************************************************************************
/// in order to get materials working you will need to duplicate verts until they match the number of normals/uvs, since collada allows multiple normals and uvs per vert
/// **********************************************************************************************************************************************************************
public class ColladaLite
{
    private struct DaeInput
    {
        public string semantic;
        public string source;
        public int offset;
    }

    public List<Mesh> meshes { get; } = new();
    public List<string> textureNames { get; } = new();

    public ColladaLite(string content)
    {
        var doc = new XmlDocument();
        doc.LoadXml(content);
        XmlNode? colladaNode = null;
        foreach (XmlNode childNode in doc.ChildNodes) {
            if (childNode.Name == "COLLADA") {
                colladaNode = childNode;
                break;
            }
        }
        Debug.Assert(colladaNode is not null);

        foreach (XmlNode childNode in colladaNode.ChildNodes) {
            if (childNode.Name == "library_images") {
                foreach (XmlNode imageNode in childNode.ChildNodes) {
                    if (imageNode.Name == "image" && imageNode.HasChildNodes) {
                        if (imageNode.FirstChild != null && imageNode.FirstChild.Name == "init_from") {
                            (textureNames ?? (textureNames = new List<string>())).Add(imageNode.FirstChild.InnerText);
                        }
                    }
                }
            }
            else if (childNode.Name == "library_geometries") {
                if (childNode.HasChildNodes) {
                    var fc = Helper.GetXmlNodeChildByName(childNode, "geometry");
                    foreach (XmlNode mesh in fc!.ChildNodes) {
                        if (mesh.Name != "mesh") {
                            continue;
                        }

                        var sources = new Dictionary<string, float[]>();
                        var vertsSource = "null";
                        Vector3[]? triangles = null;
                        Vector2[]? uvs = null;
                        int[]? indices = null;
                        var inputs = new List<DaeInput>();
                        var triCount = 0;
                        foreach (XmlNode node in mesh.ChildNodes) {
                            if (node.Name == "source") {
                                var fa = Helper.GetXmlNodeChildByName(node, "float_array");
                                if (fa != null) {
                                    Debug.Assert(node.Attributes != null);
                                    sources.Add(node.Attributes["id"]!.Value,
                                        fa.InnerText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(f => float.Parse(f))
                                            .ToArray());
                                }
                            }
                            else if (node.Name == "vertices") {
                                var vs = Helper.GetXmlNodeChildByName(node, "input");
                                if (vs != null) {
                                    Debug.Assert(vs.Attributes != null);
                                    vertsSource = vs.Attributes["source"]!.Value.Replace("#", "");
                                }
                            }
                            else if (node.Name == "triangles") {
                                Debug.Assert(node.Attributes != null);
                                triCount = int.Parse(node.Attributes["count"]!.Value); // * 3;
                                inputs.AddRange(Helper.GetXmlNodeChildrenByName(node, "input")
                                    .Select(inputNode => new DaeInput {
                                        semantic = inputNode.Attributes!["semantic"]!.Value,
                                        source = inputNode.Attributes["source"]!.Value,
                                        offset = int.Parse(inputNode.Attributes["offset"]!.Value)
                                    }));
                                indices = Helper.GetXmlNodeChildByName(node, "p")!.InnerText
                                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(i => int.Parse(i))
                                    .ToArray();
                            }
                        }

                        foreach (DaeInput input in inputs) {
                            var source = input.source.Replace("#", "");
                            if (sources.ContainsKey(source)) {
                                if (input.semantic == "TEXCOORD") {
                                    var temp = new List<Vector2>();
                                    for (int i = 0; i < sources[source].Length; i += 2) {
                                        temp.Add(new Vector2(sources[source][i], sources[source][i + 1]));
                                    }
                                    uvs = temp.ToArray();
                                }
                                else if (input.semantic == "NORMAL") {
                                    //not actually dealing with normals right now
                                }
                            }
                            else if (input.semantic == "VERTEX") {
                                var temp = new List<Vector3>();
                                for (int i = 0; i < sources[vertsSource].Length; i += 3) {
                                    temp.Add(new Vector3(
                                        sources[vertsSource][i],
                                        sources[vertsSource][i + 1],
                                        sources[vertsSource][i + 2]));
                                }
                                triangles = temp.ToArray();
                            }
                        }

                        if (triangles != null && triangles.Length > 2) {
                            Debug.Assert(indices != null);
                            var sb = new StringBuilder();
                            var tris = new Face3[triCount];
                            var uvsActual = new Vector2[triangles.Length];
                            var uvOffset = inputs.First(u => u.semantic == "TEXCOORD").offset;
                            for (int i = 0; i < tris.Length; i++) {
                                tris[i].a = indices[i * 3 + 0];
                                tris[i].b = indices[i * 3 + 1];
                                tris[i].c = indices[i * 3 + 2];
                            }

                            THREE.Geometry temp = new();
                            temp.Vertices = triangles.ToList();
                            temp.Faces = tris.ToList();
                            temp.Uvs = uvsActual.ToList();
                            temp.ComputeFaceNormals();

                            Mesh tmp = new(temp, material: null);
                            meshes.Add(tmp);
                        }
                    }
                }
            }
        }
    }

}
