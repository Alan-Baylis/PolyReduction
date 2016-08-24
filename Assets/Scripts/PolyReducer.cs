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

        private List<Wedge> m_initialWedges;
        private List<Wedge> m_wedges;
        //private List<WedgeTriangle> m_wedgeTriangles;

        private List<int> m_collapseMap; // to which neighbor each wedge collapses

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
        * Once this step is done, we can cut down the vertices on this model and create low-polygon models
        **/
        public void PrepareModel()
        {
            List<int> permutation;
            //m_data = GetMeshData();
            //WriteModelDataToFile(100.0f);
            m_data = GetDummyData();
            ProgressiveMesh(m_data, out m_collapseMap, out permutation);
            //PermuteWedges(permutation);

            //m_renderedVerticesCount = data.Verts.Count;
            //m_prevRenderedVerticesCount = m_renderedVerticesCount;

            //Debug.Log("model setup with %i:" + m_renderedVerticesCount + " vertices");
        }

        /**
        * Core function of the algorithm
        * **/
        private void ProgressiveMesh(ModelData data, out List<int> map, out List<int> permutation)
        {
            PrepareMeshData(data.Verts, data.Tris);

            ComputeAllEdgeCollapseCosts();

            for (int w = 0; w != m_wedges.Count; w++)
            {
                Debug.Log("wedge cost:" + m_wedges[w].m_cost);
            }

            int[] mapArray = new int[m_wedges.Count]; // allocate space
            int[] permutationArray = new int[m_wedges.Count]; // allocate space
            map = new List<int>(mapArray);
            permutation = new List<int>(permutationArray);

            //// reduce the object down to nothing:
            //while (m_wedges.Count > 0)
            //{
            //    // get the next vertex to collapse
            //    Wedge mn = MinimumCostEdge();
            //    // keep track of this vertex, i.e. the collapse ordering
            //    permutation[mn.ID] = m_wedges.Count - 1;
            //    // keep track of vertex to which we collapse to
            //    map[m_wedges.Count - 1] = (mn.m_collapse != null) ? mn.m_collapse.ID : -1;
            //    // Collapse this edge
            //    Collapse(mn, mn.m_collapse);
            //}
            //// reorder the map list based on the collapse ordering
            //for (int i = 0; i < map.Count; i++)
            //{
            //    map[i] = (map[i] == -1) ? 0 : permutation[map[i]];
            //}
        }

        /**
         * Transform the inital mesh data (verts and tris) to one which is more appropriate to our algorithm (with more info in it)
         * **/
        private void PrepareMeshData(List<Vector3> verts, List<int> tris)
        {
            m_wedges = new List<Wedge>();
            Vertex[] vertices = new Vertex[verts.Count];

            //First sort vertices into wedges
            for (int i = 0; i != verts.Count; i++)
            {
                Vertex vertex = new Vertex(verts[i], i);
                vertices[i] = vertex;

                Wedge wedge = WedgeForPosition(verts[i]);
                if (wedge != null)
                {
                    wedge.AddVertex(vertex);
                }
                else
                {
                    wedge = new Wedge(verts[i], i);
                    wedge.AddVertex(vertex);
                    m_wedges.Add(wedge);
                }
            }

            //Build neighbourly relations between vertices and wedges using the triangles of the model
            //m_wedgeTriangles = new List<WedgeTriangle>();
            for (int i = 0; i != tris.Count; i += 3)
            {
                Vertex v0 = vertices[tris[i]];
                Vertex v1 = vertices[tris[i + 1]];
                Vertex v2 = vertices[tris[i + 2]];

                Triangle triangle = new Triangle(v0, v1, v2);
                //m_triangles.Add(triangle);

                //Set this triangle as an adjacent triangle for every vertex
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

                v0.m_parentWedge.AddNeighbor(v1.m_parentWedge);
                v0.m_parentWedge.AddNeighbor(v2.m_parentWedge);
                v1.m_parentWedge.AddNeighbor(v0.m_parentWedge);
                v1.m_parentWedge.AddNeighbor(v2.m_parentWedge);
                v2.m_parentWedge.AddNeighbor(v0.m_parentWedge);
                v2.m_parentWedge.AddNeighbor(v1.m_parentWedge);
            }

            m_initialWedges = new List<Wedge>(m_wedges);
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
            Vector3[] cutVertices;
            int[] cutTriangles;
            CutMeshPolygons(out cutVertices, out cutTriangles, m_renderedVerticesCount);
            RefreshModel(cutVertices, cutTriangles);
        }

        /**
        * Return a list of vertices and indices where player has specified a maximum count of vertices for the initial model
        **/
        public void CutMeshPolygons(out Vector3[] cutVertices, out int[] cutTriangles, int maxWedges)
        {
            //no work to do here
            if (maxWedges >= m_initialWedges.Count)
            {
                cutVertices = m_data.Verts.ToArray();
                cutTriangles = m_data.Tris.ToArray();
                return;
            }

            //copy IDs of collapsed vertices into an array and sort it by ascending order
            List<CollapsedVertex> collapsedVertices = MapVertices(maxWedges);
            int[] collapsedVerticesIDs = new int[collapsedVertices.Count];
            for (int i = 0; i != collapsedVerticesIDs.Length; i++)
            {
                collapsedVerticesIDs[i] = collapsedVertices[i].m_collapsedIndex;
            }

            System.Array.Sort(collapsedVerticesIDs);

            //Traverse wedges (untouched and collapsed ones) and fill in the vertices array by shifting one vertex according to the previous sorted array
            cutVertices = new Vector3[m_data.Verts.Count];
            int maxID = -1;
            for (int i = 0; i != m_initialWedges.Count; i++)
            {
                Wedge wedge = m_initialWedges[i];
                for (int j = 0; j != wedge.Vertices.Count; j++)
                {
                    Vertex vertex = wedge.Vertices[j];
                    int id = GetShiftedID(collapsedVerticesIDs, vertex.ID);
                    if (id > maxID)
                        maxID = id;
                    cutVertices[id] = vertex.m_position;
                }
            }

            //use the maximum vertex index to crop the array
            System.Array.Resize(ref cutVertices, maxID);

            //now build the triangle list
            List<int> triangles = new List<int>();
            for (int i = 0; i != m_data.Tris.Count; i += 3)
            {
                int p0 = GetShiftedID(collapsedVerticesIDs, GetCollapseIDForID(collapsedVertices, m_data.Tris[i]));
                int p1 = GetShiftedID(collapsedVerticesIDs, GetCollapseIDForID(collapsedVertices, m_data.Tris[i + 1]));
                int p2 = GetShiftedID(collapsedVerticesIDs, GetCollapseIDForID(collapsedVertices, m_data.Tris[i + 2]));

                //one-dimensional (flat) triangle
                if (p0 == p1 || p1 == p2 || p2 == p0)
                    continue;

                triangles.Add(p0);
                triangles.Add(p1);
                triangles.Add(p2);
            }

            cutTriangles = triangles.ToArray();
        }

        private struct CollapsedVertex
        {
            public int m_initialIndex;
            public int m_collapsedIndex;
            public Vector3 m_position;
        }

        /**
        * Map the vertices of all the wedges that have been cut down beyond the maxWedges limit
        **/
        private List<CollapsedVertex> MapVertices(int maxWedges)
        {
            List<CollapsedVertex> collapsedVertices = new List<CollapsedVertex>();

            for (int i = m_initialWedges.Count - 1; i != maxWedges - 1; i--)
            {
                Wedge wedge = m_initialWedges[i];
                List<Vertex> wedgeVerticesCopy = new List<Vertex>(wedge.Vertices);
               
                Wedge collapseWedge = wedge;

                for (int v = 0; v != wedgeVerticesCopy.Count; v++)
                {
                    CollapsedVertex collapsedVertex = new CollapsedVertex();
                    Vertex vertex = wedgeVerticesCopy[v];
                    collapsedVertex.m_initialIndex = vertex.ID;

                    while (collapseWedge.ID >= maxWedges)
                    {
                        Vertex collapseVertex = vertex.CanCollapseOnWedge(wedge);
                        if (collapseVertex != null)
                        {
                            vertex = collapseVertex;
                            collapsedVertex.m_collapsedIndex = collapseVertex.ID;
                        }

                        collapseWedge = collapseWedge.m_collapse;
                    }

                    //set the vertex position as the position of the last wedge it collapsed to or been moved to
                    collapsedVertex.m_position = collapseWedge.m_position;

                    collapsedVertices.Add(collapsedVertex);
                }
            }

            return collapsedVertices;
        }

        /**
        * Shift the parameter 'id' according to the array of 'vertices' that collapsed
        **/
        private int GetShiftedID(int[] vertices, int id)
        {
            int shift = 0;
            for (int i = 0; i != vertices.Length; i++)
            {
                if (id >= vertices[i])
                {
                    shift = i;
                    break;
                }
            }
            return id - shift;
        }

        /**
        * Return the ID of the vertex on which the vertex of ID 'id' collapses
        **/
        private int GetCollapseIDForID(List<CollapsedVertex> vertices, int id)
        {
            for (int i = 0; i != vertices.Count; i++)
            {
                if (vertices[i].m_initialIndex == id)
                    return vertices[i].m_collapsedIndex;
            }

            //this vertex did not collapse
            return id;
        }       

        /**
        * Return the wedge at position 'position' if it exists
        **/
        private Wedge WedgeForPosition(Vector3 position)
        {
            for (int i = 0; i != m_wedges.Count; i++)
            {
                float sqrDistance = (m_wedges[i].m_position - position).sqrMagnitude;
                if (sqrDistance < 1E-07)
                    return m_wedges[i];
            }

            return null;
        }

        /**
        * Reorder the wedges according to the permutation array
        **/
        private void PermuteWedges(List<int> permutation)
        {
            if (permutation.Count != m_initialWedges.Count)
                throw new System.Exception("permutation list and initial wedges are not of the same size");

            // rearrange the wedge array 
            List<Wedge> tmpArray = new List<Wedge>(m_initialWedges);

            for (int i = 0; i < m_data.Verts.Count; i++)
            {
                m_initialWedges[permutation[i]] = tmpArray[i];
            }
        }

        /**
      * Compute the cost to collapse a specific edge defined by vertices u and v
      * **/
        private float ComputeEdgeCollapseCost(Wedge u, Wedge v)
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
                Triangle triangle = u.AdjacentTriangles[i];
                if (v.HasAdjacentTriangle(triangle)) //triangle is both adjacent to wedge u and v, so adjacent to edge [u-v]
                    sides.Add(u.AdjacentTriangles[i]);
                //if (u.AdjacentTriangles[i].HasVertex(v))
                //{
                //    sides.Add(u.AdjacentTriangles[i]);
                //}
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
        private void ComputeEdgeCostAtWedge(Wedge w)
        {
            // compute the edge collapse cost for all edges that start
            // from vertex v.  Since we are only interested in reducing
            // the object by selecting the min cost edge at each step, we
            // only cache the cost of the least cost edge at this vertex
            // (in member variable collapse) as well as the value of the 
            // cost (in member variable objdist).
            if (w.Neighbors.Count == 0)
            {
                // v doesn't have neighbors so it costs nothing to collapse
                w.m_collapse = null;
                w.m_cost = -0.01f;
                return;
            }
            w.m_cost = 1000000;
            w.m_collapse = null;
            // search all neighboring edges for "least cost" edge
            for (int i = 0; i < w.Neighbors.Count; i++)
            {
                float dist;
                dist = ComputeEdgeCollapseCost(w, w.Neighbors[i]);
                if (dist < w.m_cost)
                {
                    w.m_collapse = w.Neighbors[i];  // candidate for edge collapse
                    w.m_cost = dist;             // cost of the collapse
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
            for (int i = 0; i < m_wedges.Count; i++)
            {
                ComputeEdgeCostAtWedge(m_wedges[i]);
            }
        }

        /**
        * Collapse the wedge u onto the wedge v
        * **/
        //private void Collapse(Wedge u, Wedge v)
        //{
        //    // Collapse the edge uv by moving vertex u onto v
        //    // Actually remove tris on uv, then update tris that
        //    // have u to have v, and then remove u.
        //    if (v == null)
        //    {
        //        // u is a vertex all by itself so just delete it
        //        u.Delete();
        //        m_wedges.Remove(u);
        //        return;
        //    }

        //    List<Wedge> tmp = new List<Wedge>(u.Neighbors.Count);
        //    // make tmp a list of all the neighbors of u
        //    for (int i = 0; i < u.Neighbors.Count; i++)
        //    {
        //        tmp.Add(u.Neighbors[i]);
        //    }

        //    // delete triangles on edge uv:
        //    for (int i = u.AdjacentTriangles.Count - 1; i >= 0; i--)
        //    {
        //        Triangle adjacentTriangle = u.AdjacentTriangles[i];
        //        if (v.HasAdjacentTriangle(adjacentTriangle)) //triangle is both adjacent to wedge u and v, so adjacent to edge [u-v]
        //        {
        //            //m_triangles.Remove(adjacentTriangle);
        //            adjacentTriangle.Delete();
        //        }
        //        //if (adjacentTriangle.HasVertex(v))
        //        //{
        //        //    m_triangles.Remove(adjacentTriangle);
        //        //    adjacentTriangle.Delete();
        //        //}
        //    }

        //    // update remaining triangles to have v instead of u
        //    for (int i = u.AdjacentTriangles.Count - 1; i >= 0; i--)
        //    {
        //        u.AdjacentTriangles[i].ReplaceWedge(u, v);
        //    }

        //    u.Delete();
        //    m_wedges.Remove(u);

        //    // recompute the edge collapse costs for neighboring vertices
        //    for (int i = 0; i < tmp.Count; i++)
        //    {
        //        ComputeEdgeCostAtWedge(tmp[i]);
        //    }
        //}

        /**
        * Return the vertex with the 'least cost' to collapse
        * **/
        private Wedge MinimumCostEdge()
        {
            // Find the edge that when collapsed will affect model the least.
            // This funtion actually returns a Vertex, the second vertex
            // of the edge (collapse candidate) is stored in the vertex data.
            // Serious optimization opportunity here: this function currently
            // does a sequential search through an unsorted list :-(
            // Our algorithm could be O(n*lg(n)) instead of O(n*n)
            Wedge mn = m_wedges[0];
            for (int i = 0; i < m_wedges.Count; i++)
            {
                if (m_wedges[i].m_cost < mn.m_cost)
                {
                    mn = m_wedges[i];
                }
            }
            return mn;
        }

        /**
         * Build some data to test our algorithm
         * **/
        private ModelData GetDummyData()
        {
            //FILE SAMPLES
            //float[,] dummyVertices = RabbitData.rabbit_vertices;
            //int[,] dummyTriangles = RabbitData.rabbit_triangles;
            //float[,] dummyVertices = PlaneData.plane_vertices;
            //int[,] dummyTriangles = PlaneData.plane_triangles;

            //SAMPLE 1
            //float[,] dummyVertices = {
            //                            { -1, -1, 0},
            //                            { 1, -1, 0},
            //                            { -1, 1, 0},
            //                            { 1, -1, 0},
            //                            { 1, 1, 0},
            //                            { -1, 1, 0}
            //                         };

            //int[,] dummyTriangles = {
            //                            { 0, 2, 1},
            //                            { 3, 5, 4}
            //                         };

            //SAMPLE 2
            float[,] dummyVertices = {
                                        { 1, 0, 3 },
                                        { 0, -1, -1},
                                        { 2, 0.5f, 0},
                                        { 2.5f, -1, -1.5f},
                                        { 4, 0, 1},
                                        { 3.5f, 2, 1}
                                     };

            int[,] dummyTriangles = {
                                        { 0, 2, 1},
                                        { 1, 2, 3},
                                        { 2, 5, 3},
                                        { 3, 5, 4}
                                     };

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
        private void RefreshModel(Vector3[] vertices, int[] triangles)
        {
            m_mesh = new Mesh();
            this.GetComponent<MeshFilter>().sharedMesh = m_mesh;
            //m_mesh.Clear();

            m_mesh.vertices = vertices;
            m_mesh.triangles = triangles;
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
            System.IO.StreamWriter file = new System.IO.StreamWriter("F:\\Unity\\workspace\\PolyReduction\\Assets\\PlaneData.cs");
            file.WriteLine(strData);

            file.Close();
        }
    }
}
