using UnityEngine;
using System.Collections.Generic;

namespace PolyReduction
{
    [ExecuteInEditMode]
    public class PolyReducer : MonoBehaviour
    {
        //inner class to hold initial mesh data
        private class ModelData
        {
            private List<Vector3> m_verts;
            public List<Vector3> Verts
            {
                get
                {
                    return m_verts;
                }
            }

            private List<int> m_tris;
            public List<int> Tris
            {
                get
                {
                    return m_tris;
                }
            }

            public ModelData(List<Vector3> verts, List<int> tris)
            {
                m_verts = verts;
                m_tris = tris;
            }
        }

        private List<Vertex> m_vertices;
        private List<Triangle> m_triangles;

        private List<int> m_collapseMap; // to which neighbor each vertex collapses

        private ModelData m_data;

        private Mesh m_mesh;

        //Set a value of a maximum vertex count to render
        public int m_renderedVerticesCount;
        private int m_prevRenderedVerticesCount;

        public void Start()
        {
            Debug.Log("PrepareModel");
            PrepareModel();
        }

        /**
        * Prepare the data from the model attached to this component
        * Once this step is done, we can cut down the vertices on this model and create low-polygons models
        **/
        public void PrepareModel()
        {
            List<int> permutation;
            //m_data = GetMeshData();
            //WriteModelDataToFile(1.0f);
            m_data = GetDummyData();
            ProgressiveMesh(m_data.Verts, m_data.Tris, out m_collapseMap, out permutation);
            PermuteVertices(permutation);

            m_renderedVerticesCount = m_data.Verts.Count;
            m_prevRenderedVerticesCount = m_renderedVerticesCount;

            Debug.Log("model setup with %i:" + m_renderedVerticesCount + " vertices");
        }

        /**
        * Return the data contained inside the mesh as one single object
        **/
        private ModelData GetMeshData()
        {
            m_mesh = GetComponent<MeshFilter>().sharedMesh;

            if (m_mesh == null)
                throw new System.Exception("A mesh has to be added to this object MeshFilter component in order to perform a polygon reduction on it");

            List<Vector3> verts = new List<Vector3>(m_mesh.vertices);
            List<int> tris = new List<int>(m_mesh.triangles);

            return new ModelData(verts, tris);
        }

        /**
        * Render this model using exactly m_maxVertices
        **/
        public void RenderModel()
        {
            List<Vector3> cutVertices;
            List<int> cutTriangles;
            CutMeshPolygons(out cutVertices, out cutTriangles, m_renderedVerticesCount);
            RefreshModel(cutVertices, cutTriangles);
        }

        /**
        * Return a list of vertices and indices where player has specified a maximum count of vertices for the initial model
        **/
        public void CutMeshPolygons(out List<Vector3> cutVertices, out List<int> cutTriangles, int maxVertices)
        {
            //no work to do here
            if (maxVertices >= m_data.Verts.Count)
            {
                cutVertices = m_data.Verts;
                cutTriangles = m_data.Tris;
                return;
            }

            cutVertices = m_data.Verts.GetRange(0, maxVertices);
            cutTriangles = new List<int>();

            for (int i = 0; i != m_data.Tris.Count; i += 3)
            {
                int p0 = Map(m_data.Tris[i], maxVertices);
                int p1 = Map(m_data.Tris[i + 1], maxVertices);
                int p2 = Map(m_data.Tris[i + 2], maxVertices);

                //one-dimensional (flat) triangle
                if (p0 == p1 || p1 == p2 || p2 == p0)
                    continue;

                cutTriangles.Add(p0);
                cutTriangles.Add(p1);
                cutTriangles.Add(p2);
            }
        }

        /**
        * Map the index of one vertex according to a number of maximum rendered vertices using the relevant collapse map
        **/
        private int Map(int a, int mx)
        {
            if (mx <= 0) return 0;
            while (a >= mx)
            {
                a = m_collapseMap[a];
            }
            return a;
        }

        /**
         * Core function of the algorithm
         * **/
        private void ProgressiveMesh(List<Vector3> verts, List<int> tris, out List<int> map, out List<int> permutation)
        {
            PrepareMeshData(verts, tris);

            ComputeAllEdgeCollapseCosts();

            int[] mapArray = new int[m_vertices.Count]; // allocate space
            int[] permutationArray = new int[m_vertices.Count]; // allocate space
            map = new List<int>(mapArray);
            permutation = new List<int>(permutationArray);

            // reduce the object down to nothing:
            while (m_vertices.Count > 0)
            {
                // get the next vertex to collapse
                Vertex mn = MinimumCostEdge();
                // keep track of this vertex, i.e. the collapse ordering
                permutation[mn.ID] = m_vertices.Count - 1;
                // keep track of vertex to which we collapse to
                map[m_vertices.Count - 1] = (mn.m_collapse != null) ? mn.m_collapse.ID : -1;
                // Collapse this edge
                Collapse(mn, mn.m_collapse);
            }
            // reorder the map list based on the collapse ordering
            for (int i = 0; i < map.Count; i++)
            {
                map[i] = (map[i] == -1) ? 0 : permutation[map[i]];
            }
        }

        /**
         * Transform the inital mesh data (verts and tris) to one which is more appropriate to our algorithm (with more info in it)
         * **/
        private void PrepareMeshData(List<Vector3> verts, List<int> tris)
        {
            m_vertices = new List<Vertex>(verts.Count);
            for (int i = 0; i != verts.Count; i++)
            {
                m_vertices.Add(new Vertex(verts[i], i));
            }

            m_triangles = new List<Triangle>(tris.Count);
            for (int i = 0; i != tris.Count; i += 3)
            {
                Vertex v0 = m_vertices[tris[i]];
                Vertex v1 = m_vertices[tris[i + 1]];
                Vertex v2 = m_vertices[tris[i + 2]];

                Triangle triangle = new Triangle(v0, v1, v2);
                m_triangles.Add(triangle);

                //Set this triangle as an adjacent triangle for every point
                v0.AddAdjacentTriangle(triangle);
                v1.AddAdjacentTriangle(triangle);
                v2.AddAdjacentTriangle(triangle);

                //for each triangle vertex, set the 2 opposite points as neighbors
                v0.AddNeighbor(v1);
                v0.AddNeighbor(v2);
                v1.AddNeighbor(v0);
                v1.AddNeighbor(v2);
                v2.AddNeighbor(v0);
                v2.AddNeighbor(v1);
            }
        }

        /**
        * Reorder the vertices and triangles according to the permutation array
        **/
        private void PermuteVertices(List<int> permutation)
        {
            if (permutation.Count != m_data.Verts.Count)
                throw new System.Exception("permutation list and initial vertices are not of the same size");

            // rearrange the vertex Array 
            List<Vector3> tmpArray = new List<Vector3>(m_data.Verts);

            for (int i = 0; i < m_data.Verts.Count; i++)
            {
                m_data.Verts[permutation[i]] = tmpArray[i];
            }

            // update the changes in the entries in the triangle Array
            for (int i = 0; i < m_data.Tris.Count; i += 3)
            {
                m_data.Tris[i] = permutation[m_data.Tris[i]];
                m_data.Tris[i + 1] = permutation[m_data.Tris[i + 1]];
                m_data.Tris[i + 2] = permutation[m_data.Tris[i + 2]];
            }
        }

        /**
      * Compute the cost to collapse a specific edge defined by vertices u and v
      * **/
        private float ComputeEdgeCollapseCost(Vertex u, Vertex v)
        {
            // if we collapse edge uv by moving u to v then how 
            // much different will the model change, i.e. how much "error".
            // Texture, vertex normal, and border vertex code was removed
            // to keep this demo as simple as possible.
            // The method of determining cost was designed in order 
            // to exploit small and coplanar regions for
            // effective polygon reduction.
            // Is is possible to add some checks here to see if "folds"
            // would be generated.  i.e. normal of a remaining face gets
            // flipped.  I never seemed to run into this problem and
            // therefore never added code to detect this case.
            int i;
            float edgelength = (v.m_position - u.m_position).magnitude;
            float curvature = 0;

            // find the "sides" triangles that are on the edge uv
            List<Triangle> sides = new List<Triangle>();
            for (i = 0; i < u.AdjacentTriangles.Count; i++)
            {
                if (u.AdjacentTriangles[i].HasVertex(v))
                {
                    sides.Add(u.AdjacentTriangles[i]);
                }
            }
            // use the triangle facing most away from the sides 
            // to determine our curvature term
            for (i = 0; i < u.AdjacentTriangles.Count; i++)
            {
                Vector3 n1 = u.AdjacentTriangles[i].m_normal;
                float mincurv = 1; // curve for face i and closer side to it
                for (int j = 0; j < sides.Count; j++)
                {
                    // use dot product of face normals
                    Vector3 n2 = sides[j].m_normal;
                    float dotprod = n1.x * n2.x + n1.y * n2.y + n1.z * n2.z;
                    mincurv = Mathf.Min(mincurv, (1 - dotprod) / 2.0f);
                }
                curvature = Mathf.Max(curvature, mincurv);
            }
            // the more coplanar the lower the curvature term   
            return edgelength * curvature;
        }

        /**
       * Compute the cost to collapse a specific edge to one of its neighbors (the one with the least cost is chosen)
       * **/
        private void ComputeEdgeCostAtVertex(Vertex v)
        {
            // compute the edge collapse cost for all edges that start
            // from vertex v.  Since we are only interested in reducing
            // the object by selecting the min cost edge at each step, we
            // only cache the cost of the least cost edge at this vertex
            // (in member variable collapse) as well as the value of the 
            // cost (in member variable objdist).
            if (v.Neighbors.Count == 0)
            {
                // v doesn't have neighbors so it costs nothing to collapse
                v.m_collapse = null;
                v.m_cost = -0.01f;
                return;
            }
            v.m_cost = 1000000;
            v.m_collapse = null;
            // search all neighboring edges for "least cost" edge
            for (int i = 0; i < v.Neighbors.Count; i++)
            {
                float dist;
                dist = ComputeEdgeCollapseCost(v, v.Neighbors[i]);
                if (dist < v.m_cost)
                {
                    v.m_collapse = v.Neighbors[i];  // candidate for edge collapse
                    v.m_cost = dist;             // cost of the collapse
                }
            }
        }

        /**
       * Compute the cost to collapse an edge for every edge in the mesh
       * **/
        private void ComputeAllEdgeCollapseCosts()
        {
            // For all the edges, compute the difference it would make
            // to the model if it was collapsed.  The least of these
            // per vertex is cached in each vertex object.
            for (int i = 0; i < m_vertices.Count; i++)
            {
                ComputeEdgeCostAtVertex(m_vertices[i]);
            }
        }

        /**
        * Collapse the vertex u onto the vertex v
        * **/
        private void Collapse(Vertex u, Vertex v)
        {
            // Collapse the edge uv by moving vertex u onto v
            // Actually remove tris on uv, then update tris that
            // have u to have v, and then remove u.
            if (v == null)
            {
                // u is a vertex all by itself so just delete it
                u.Delete();
                m_vertices.Remove(u);
                return;
            }

            List<Vertex> tmp = new List<Vertex>(u.Neighbors.Count);
            // make tmp a list of all the neighbors of u
            for (int i = 0; i < u.Neighbors.Count; i++)
            {
                tmp.Add(u.Neighbors[i]);
            }
            // delete triangles on edge uv:
            for (int i = u.AdjacentTriangles.Count - 1; i >= 0; i--)
            {
                Triangle adjacentTriangle = u.AdjacentTriangles[i];
                if (adjacentTriangle.HasVertex(v))
                {
                    m_triangles.Remove(adjacentTriangle);
                    adjacentTriangle.Delete();
                }
            }

            // update remaining triangles to have v instead of u
            for (int i = u.AdjacentTriangles.Count - 1; i >= 0; i--)
            {
                u.AdjacentTriangles[i].ReplaceVertex(u, v);
            }

            u.Delete();
            m_vertices.Remove(u);

            // recompute the edge collapse costs for neighboring vertices
            for (int i = 0; i < tmp.Count; i++)
            {
                ComputeEdgeCostAtVertex(tmp[i]);
            }
        }

        /**
        * Return the vertex with the 'least cost' to collapse
        * **/
        private Vertex MinimumCostEdge()
        {
            // Find the edge that when collapsed will affect model the least.
            // This funtion actually returns a Vertex, the second vertex
            // of the edge (collapse candidate) is stored in the vertex data.
            // Serious optimization opportunity here: this function currently
            // does a sequential search through an unsorted list :-(
            // Our algorithm could be O(n*lg(n)) instead of O(n*n)
            Vertex mn = m_vertices[0];
            for (int i = 0; i < m_vertices.Count; i++)
            {
                if (m_vertices[i].m_cost < mn.m_cost)
                {
                    mn = m_vertices[i];
                }
            }
            return mn;
        }

        /**
         * Build some data to test our algorithm
         * **/
        private ModelData GetDummyData()
        {
            //float[,] dummyVertices = RabbitData.rabbit_vertices;
            //int[,] dummyTriangles = RabbitData.rabbit_triangles;
            float[,] dummyVertices = PlaneData.plane_vertices;
            int[,] dummyTriangles = PlaneData.plane_triangles;

            List<Vector3> verts = new List<Vector3>(dummyVertices.GetLength(0));
            for (int i = 0; i != dummyVertices.GetLength(0); i++)
            {
                float x = dummyVertices[i, 0];
                float y = dummyVertices[i, 1];
                float z = dummyVertices[i, 2];

                verts.Add(new Vector3(x, y, z));
            }

            List<int> tris = new List<int>(dummyTriangles.Length);
            for (int i = 0; i != dummyTriangles.GetLength(0); i++)
            {
                tris.Add(dummyTriangles[i, 0]);
                tris.Add(dummyTriangles[i, 1]);
                tris.Add(dummyTriangles[i, 2]);
            }

            m_mesh = new Mesh();
            GetComponent<MeshFilter>().sharedMesh = m_mesh;
            m_mesh.vertices = verts.ToArray();
            m_mesh.triangles = tris.ToArray();

            ModelData dummyData = new ModelData(verts, tris);
            return dummyData;
        }

        /**
        * Reassign vertices and triangles to the model
        **/
        private void RefreshModel(List<Vector3> vertices, List<int> triangles)
        {
            m_mesh.Clear();
            m_mesh.vertices = vertices.ToArray();
            m_mesh.triangles = triangles.ToArray();
        }


        public void Update()
        {
            if (m_data != null)
            {
                if (m_renderedVerticesCount != m_prevRenderedVerticesCount)
                {
                    if (m_renderedVerticesCount < 0)
                        m_renderedVerticesCount = 0;
                    if (m_renderedVerticesCount > m_data.Verts.Count)
                        m_renderedVerticesCount = m_data.Verts.Count;

                    RenderModel();

                    m_prevRenderedVerticesCount = m_renderedVerticesCount;
                }
            }
        }

        private void WriteModelDataToFile(float scale)
        {
            string strData = "";

            //c# file immutable code
            strData += "using UnityEngine;\n\n";
            strData += "namespace PolyReduction\n";
            strData += "{\n";
            strData += "\tpublic class PlaneData\n";
            strData += "\t{\n";

            //vertices
            strData += "\t\tpublic static float[,] plane_vertices =\n";
            strData += "\t\t{\n";
            for (int i = 0; i != m_data.Verts.Count; i++)
            {
                Vector3 vertex = m_data.Verts[i];
                string strVertex = "\t\t\t{" + (vertex.x * scale) + "f," + (vertex.y * scale) + "f," + (vertex.z * scale) + "f}";
                strData += strVertex;
                if (i < m_data.Verts.Count - 1)
                    strData += ",";
                strData += "\n";
            }

            strData += "\t\t};\n\n";

            //triangles
            strData += "\t\tpublic static int[,] plane_triangles =\n";
            strData += "\t\t{\n";
            for (int i = 0; i != m_data.Tris.Count; i += 3)
            {
                string strTri = "\t\t\t{" + m_data.Tris[i] + "," + m_data.Tris[i + 1] + "," + m_data.Tris[i + 2] + "}";
                strData += strTri;
                if (i < m_data.Tris.Count - 1)
                    strData += ",";
                strData += "\n";
            }


            strData += "\t\t};\n";

            //end of c# file
            strData += "\t}\n}";

            // Write the string to a file.
            string pathToScriptsFolder = "C:\\Unity_workspace\\PolyReduction\\Assets\\Scripts";
            System.IO.StreamWriter file = new System.IO.StreamWriter(pathToScriptsFolder + "\\PlaneData.cs");
            file.WriteLine(strData);

            file.Close();
        }
    }
}
