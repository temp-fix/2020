// Track BuildR
// Available on the Unity3D Asset Store
// Copyright (c) 2015 Jasper Stocker http://support.jasperstocker.com
// For support contact email@jasperstocker.com
//
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.

using System.Collections.Generic;
using UnityEngine;

namespace TrackBuildRUtil
{
    public class DynamicMesh
    {
        public Mesh mesh = new Mesh();
        public List<Vector3> vertices;
        public List<Vector2> uv;
        public List<int> triangles;
        public Bounds bounds;

        public List<Vector4> tangents;
        private int _subMeshes = 1;
        private Dictionary<int, List<int>> subTriangles;
        private DynamicMesh _overflow;

        public DynamicMesh(string newName = "dynmesh")
        {
            mesh.name = newName;
            vertices = new List<Vector3>();
            uv = new List<Vector2>();
            triangles = new List<int>();
            tangents = new List<Vector4>();
            subTriangles = new Dictionary<int, List<int>>();
            subTriangles.Add(0, new List<int>());
            bounds = new Bounds();
        }

        public string name
        {
            get { return mesh.name; }
            set { mesh.name = value; }
        }

        public void Build()
        {
            BuildThisMesh();
            if (_overflow != null)
                _overflow.Build();
        }

        private void BuildThisMesh()
        {
            mesh.Clear();
            mesh.vertices = vertices.ToArray();
            mesh.uv = uv.ToArray();
            mesh.uv2 = new Vector2[0];
            mesh.tangents = tangents.ToArray();
            mesh.bounds = bounds;

            mesh.subMeshCount = 0;//set base submesh count to 1 so we can get started
            _subMeshes = 0;
            foreach (KeyValuePair<int, List<int>> triData in subTriangles)
            {
                int submeshNumber = triData.Key;
                if (_subMeshes <= submeshNumber)//increase the submesh count if it exceeds the current count
                {
                    _subMeshes = submeshNumber + 1;
                    mesh.subMeshCount = submeshNumber + 1;
                }

                mesh.SetTriangles(triData.Value.ToArray(), submeshNumber);
            }

            mesh.RecalculateBounds();
            //            if (_shouldCalculateNormals)
            mesh.RecalculateNormals();
        }

        /// <summary>
        /// Clears the mesh data, ready for nextNormIndex new mesh build
        /// </summary>
        public void Clear()
        {
            vertices.Clear();
            uv.Clear();
            triangles.Clear();
            tangents.Clear();
            bounds.center = Vector3.zero;
            bounds.size = Vector3.zero;
            subTriangles.Clear();
            subTriangles.Add(0, new List<int>());
            _subMeshes = 1;
            _overflow = null;
        }

        public int vertexCount { get { return vertices.Count; } }

        public int triangleCount
        {
            get
            {
                if (triangles == null)
                    return 0;
                return triangles.Count;
            }
        }

        public int subMeshCount { get { return _subMeshes; } }

        public int meshCount
        {
            get
            {
                if(vertexCount == 0)
                    return 0;
                int output = 1;
                if (_overflow != null)
                    output += _overflow.meshCount;
                return output;
            }
        }

        public Mesh[] meshes
        {
            get
            {
                List<Mesh> output = new List<Mesh>();
                output.Add(mesh);
                if (_overflow != null)
                    output.AddRange(_overflow.meshes);
                return output.ToArray();
            }
        }

        public bool isEmpty
        {
            get { return vertexCount == 0; }
        }

        /// <summary>
        /// Add new mesh data - all arrays are ordered together
        /// </summary>
        /// <param customName="verts">And array of verticies</param>
        /// <param customName="uvs">And array of uvs</param>
        /// <param customName="tris">And array of triangles</param>
        /// <param customName="subMesh">The submesh to add the data into</param>
        public void AddData(Vector3[] verts, Vector2[] uvs, int[] tris, Vector3[] norms, Vector4[] tan, int subMesh)
        {
            if (MeshOverflow(verts.Length))
            {
                _overflow.AddData(verts, uvs, tris, norms, tan, subMesh);
                return;
            }

            int indiceBase = vertices.Count;
            vertices.AddRange(verts);
            uv.AddRange(uvs);
            tangents.AddRange(tan);

            if (!subTriangles.ContainsKey(subMesh))
                subTriangles.Add(subMesh, new List<int>());

            int newTriCount = tris.Length;
            for (int t = 0; t < newTriCount; t++)
            {
                int newTri = (indiceBase + tris[t]);
                triangles.Add(newTri);
                subTriangles[subMesh].Add(newTri);
            }
        }

        public void AddPlane(Vector3[] verts, Vector2[] uvs, int submesh)
        {
            if (verts.Length != 4)
                return;
            AddPlane(verts[0], verts[1], verts[2], verts[3], uvs[0], uvs[1], uvs[2], uvs[3], submesh);
        }

        public void AddPlane(Vector3[] verts, Vector2[] uvs, int[] tris, int submesh)
        {
            if (verts.Length != 4)
                return;
            AddPlane(verts[0], verts[1], verts[2], verts[3], uvs[0], uvs[1], uvs[2], uvs[3], tris[0], tris[1], tris[2], tris[3], tris[4], tris[5], submesh);
        }

        /// <summary>
        /// Adds the plane to the generic dynamic mesh without specifying UV coords.
        /// </summary>
        /// <param customName='p0,p1,p2,p3'>
        /// 4 Verticies that define the plane
        /// <param customName='subMesh'>
        /// The sub mesh to attch this plan to - in order of Texture library indicies
        /// </param>
        public void AddPlane(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int subMesh)
        {
            Vector2 uv0 = new Vector2(p0.x, p0.y);
            Vector2 uv1 = new Vector2(p3.x, p3.y);
            AddPlane(p0, p1, p2, p3, uv0, uv1, subMesh);
        }

        /// <summary>
        /// Adds the plane to the generic dynamic mesh by specifying min and max UV coords.
        /// </summary>
        /// <param customName='p0,p1,p2,p3'>
        /// 4 Verticies that define the plane
        /// </param>
        /// <param customName='minUV'>
        /// the minimum vertex UV coord.
        /// </param>
        /// </param>
        /// <param customName='maxUV'>
        /// the maximum vertex UV coord.
        /// </param>
        /// <param customName='subMesh'>
        /// The sub mesh to attch this plan to - in order of Texture library indicies
        /// </param>
        public void AddPlane(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector2 minUV, Vector2 maxUV, int subMesh)
        {
            Vector2 uv0 = new Vector2(minUV.x, minUV.y);
            Vector2 uv1 = new Vector2(maxUV.x, minUV.y);
            Vector2 uv2 = new Vector2(minUV.x, maxUV.y);
            Vector2 uv3 = new Vector2(maxUV.x, maxUV.y);

            AddPlane(p0, p1, p2, p3, uv0, uv1, uv2, uv3, subMesh);
        }

        /// <summary>
        /// Adds the plane to the generic dynamic mesh.
        /// </summary>
        /// <param customName='p0,p1,p2,p3'>
        /// 4 Verticies that define the plane
        /// </param>
        /// <param customName='uv0,uv1,uv2,uv3'>
        /// the vertex UV coords.
        /// </param>
        /// <param customName='subMesh'>
        /// The sub mesh to attch this plan to - in order of Texture library indicies
        /// </param>
        public void AddPlane(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3, int subMesh)
        {
            AddPlane(p0, p1, p2, p3, uv0, uv1, uv2, uv3, 0, 2, 1, 1, 2, 3, subMesh);
        }

        public void AddPlane(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3, int tri0, int tri1, int tri2, int tri3, int tri4, int tri5, int subMesh)
        {
            if (MeshOverflow(4))
            {
                _overflow.AddPlane(p0, p1, p2, p3, uv0, uv1, uv2, uv3, subMesh);
                return;
            }
             
            int indiceBase = vertices.Count;
            vertices.Add(p0);
            vertices.Add(p1);
            vertices.Add(p2);
            vertices.Add(p3);

            uv.Add(uv0);
            uv.Add(uv1);
            uv.Add(uv2);
            uv.Add(uv3);

            if (!subTriangles.ContainsKey(subMesh))
                subTriangles.Add(subMesh, new List<int>());

            subTriangles[subMesh].Add(indiceBase + tri0);
            subTriangles[subMesh].Add(indiceBase + tri1);
            subTriangles[subMesh].Add(indiceBase + tri2);

            subTriangles[subMesh].Add(indiceBase + tri3);
            subTriangles[subMesh].Add(indiceBase + tri4);
            subTriangles[subMesh].Add(indiceBase + tri5);

            triangles.Add(indiceBase + tri0);
            triangles.Add(indiceBase + tri1);
            triangles.Add(indiceBase + tri2);

            triangles.Add(indiceBase + tri3);
            triangles.Add(indiceBase + tri4);
            triangles.Add(indiceBase + tri5);

            Vector3 tangentDirection = p1 - p0;
            Vector4 tangent = new Vector4();
            tangent.x = tangentDirection.x;
            tangent.y = tangentDirection.y;
            tangent.z = tangentDirection.z;
            tangent.w = 1;//TODO: Check whether we need to flip the bi normal - I don't think we do with these planes
            tangents.Add(tangent);
            tangents.Add(tangent);
            tangents.Add(tangent);
            tangents.Add(tangent);
        }

        private bool MeshOverflow(int numberOfNewVerts)
        {
            if (_overflow != null)
                return true;
            if (numberOfNewVerts + vertexCount >= 65000)
            {
                _overflow = new DynamicMesh(string.Format("{0}{1}", mesh.name, "i"));
                return true;
            }
            return false;
        }
    }
}