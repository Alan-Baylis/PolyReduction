using UnityEngine;
using System.Collections.Generic;

namespace PolyReduction
{
    [ExecuteInEditMode]
    public class PolyReducer : MonoBehaviour
    {
        private List<Wedge> m_initialWedges;
        private List<Wedge> m_wedges;
        //private List<WedgeTriangle> m_wedgeTriangles;

        private int[] m_collapseMap; // to which neighbor each wedge collapses

        private ModelData m_data;

        private Mesh m_mesh;

        //Set a value of a maximum vertex count to render
        //public int m_renderedVerticesCount;
        //private int m_prevRenderedVerticesCount;

        //Set a value of a maximum wedge count to render
        public int m_renderedWedgesCount;
        private int m_prevRenderedWedgesCount;

        //the number of wedges able to collapse
        private int m_minRendereWedgesCount;

        public void Start()
        {
            PrepareModel();
        }

        /**
        * Prepare the data from the model attached to this component
        * Once this step is done, we can cut down the vertices on this model and create low-polygon models
        **/
        public void PrepareModel()
        {
            //Debug.Log("PrepareModel");

            int[] permutation;
            m_data = GetMeshData();
            //WriteModelDataToFile(100.0f);
            //m_data = GetDummyData();
            //m_data = MeshTrianglesSeparator.SeparateTrianglesInMesh(m_data);
            ProgressiveMesh(m_data, out m_collapseMap, out permutation);
            PermuteWedges(permutation);

            m_renderedWedgesCount = m_initialWedges.Count;
            m_prevRenderedWedgesCount = m_renderedWedgesCount;

            Debug.Log("model setup with %i:" + m_renderedWedgesCount + " vertices");
        }

        /**
        * Core function of the algorithm
        * **/
        private void ProgressiveMesh(ModelData data, out int[] map, out int[] permutation)
        {
            PrepareMeshData(data);

            ComputeAllEdgeCollapseCosts();

            //int[] mapArray = new int[m_wedges.Count]; // allocate space
            //int[] permutationArray = new int[m_wedges.Count]; // allocate space
            map = new int[m_wedges.Count];
            permutation = new int[m_wedges.Count];

            // reduce the object down to nothing
            while (m_wedges.Count > 0)
            {
                // get the next vertex to collapse
                Wedge mn = MinimumCostEdge();

                //if (mn.m_collapse != null)
                //    Debug.Log("collapsing wedge " + mn.ID + " on wedge " + mn.m_collapse.ID + " with cost " + mn.m_cost);

                // keep track of this vertex, i.e. the collapse ordering
                permutation[mn.ID] = m_wedges.Count - 1;
                // keep track of vertex to which we collapse to
                map[m_wedges.Count - 1] = (mn.m_collapse != null) ? mn.m_collapse.ID : -1;              
                // Collapse this edge
                Collapse(mn, mn.m_collapse);
                //copy mapped, displaced and deleted vertices to the wedge that will remain alive
                Wedge persistentWedge = WedgeForID(m_initialWedges, mn.ID);
                persistentWedge.m_collapsedVertices = mn.m_collapsedVertices;
                persistentWedge.m_displacedVertices = mn.m_displacedVertices;
                persistentWedge.m_deletedVertices = mn.m_deletedVertices;
            }

            // reorder the map list based on the collapse ordering
            for (int i = 0; i < map.Length; i++)
            {
                //map[i] = (map[i] == -1) ? 0 : permutation[map[i]];
                if (map[i] >= 0)
                    map[i] = permutation[map[i]];
            }
        }

        /**
         * Transform the inital mesh data (verts and tris) to one which is more appropriate to our algorithm (with more info in it)
         * **/
        private void PrepareMeshData(ModelData data)
        {
            m_wedges = new List<Wedge>();
            m_initialWedges = new List<Wedge>(); //as m_wedges will be consumed, store here a copy of this list

            BuildWedges(m_wedges, data);
            BuildWedges(m_initialWedges, data);
        }

        private void BuildWedges(List<Wedge> wedges, ModelData data)
        {
            Vertex[] vertices = new Vertex[data.Verts.Length];

            //First sort vertices into wedges
            for (int i = 0; i != data.Verts.Length; i++)
            {
                Vertex vertex = new Vertex(data.Verts[i], i);
                if (data.Colors != null && data.Colors.Length > 0)
                    vertex.m_color = data.Colors[i];
                if (data.UVs != null && data.UVs.Length > 0)
                    vertex.m_uv = data.UVs[i];
                vertices[i] = vertex;

                Wedge wedge = WedgeForPosition(wedges, data.Verts[i]);
                if (wedge != null)
                {
                    wedge.AddVertex(vertex);
                }
                else
                {
                    wedge = new Wedge(data.Verts[i], wedges.Count);
                    wedge.AddVertex(vertex);
                    wedges.Add(wedge);
                }
            }

            //Build neighbourly relations between vertices and wedges using the triangles of the model
            BuildNeighbourlyRelations(wedges, vertices, data.Tris);

            //Determine which triangles are adjacent to each wedge
            for (int i = 0; i != m_wedges.Count; i++)
            {
                m_wedges[i].InvalidateAdjacentTriangles();
            }
        }

        /**
        * Build neighbourly relations between vertices and wedges using the triangles of the model
        **/
        private void BuildNeighbourlyRelations(List<Wedge> wedges, Vertex[] vertices, int[] tris)
        {
            for (int i = 0; i != tris.Length; i += 3)
            {
                Vertex v0 = vertices[tris[i]];
                Vertex v1 = vertices[tris[i + 1]];
                Vertex v2 = vertices[tris[i + 2]];

                Triangle triangle = new Triangle(v0, v1, v2);

                //Set this triangle as an adjacent triangle for every vertex
                v0.AddAdjacentTriangle(triangle);
                v1.AddAdjacentTriangle(triangle);
                v2.AddAdjacentTriangle(triangle);             

                Wedge w0 = GetWedgeHoldingVertex(wedges, v0);
                Wedge w1 = GetWedgeHoldingVertex(wedges, v1);
                Wedge w2 = GetWedgeHoldingVertex(wedges, v2);
                w0.AddNeighbor(w1);
                w0.AddNeighbor(w2);
                w1.AddNeighbor(w0);
                w1.AddNeighbor(w2);
                w2.AddNeighbor(w0);
                w2.AddNeighbor(w1);
            }
        }

        /**
        * Return the wedge holding the vertex 'vertex' in its list
        **/
        private Wedge GetWedgeHoldingVertex(List<Wedge> wedges, Vertex vertex)
        {
            for (int i = 0; i != wedges.Count; i++)
            {
                if (wedges[i].HasVertex(vertex))
                    return wedges[i];
            }

            return null;
        }

        /**
        * Return the data contained inside the mesh as one single object
        **/
        private ModelData GetMeshData()
        {
            m_mesh = GetComponent<MeshFilter>().sharedMesh;

            if (m_mesh == null)
                throw new System.Exception("A mesh has to be added to this object MeshFilter component in order to perform a polygon reduction on it");

            ModelData meshData = new ModelData(m_mesh.vertices, m_mesh.triangles);
            meshData.Colors = m_mesh.colors;
            meshData.UVs = m_mesh.uv;

            return meshData;
        }

        /**
        * Render this model using exactly m_maxVertices
        **/
        public void RenderModel()
        {
            ModelData cutData;

            if (m_renderedWedgesCount == 3)
            {
                System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                CutMeshPolygons(out cutData, m_renderedWedgesCount, true);

                sw.Stop();
                Debug.Log("cutting down model to " + m_renderedWedgesCount + " wedges lasts " + sw.ElapsedMilliseconds + " ms"); //20ms to rabbit 3 vertices
            }
            else
                CutMeshPolygons(out cutData, m_renderedWedgesCount);

            RefreshModel(cutData);
        }

        /**
        * Return a list of vertices and indices where player has specified a maximum count of vertices for the initial model
        **/
        public void CutMeshPolygons(out ModelData cutData, int maxWedges, bool bStopWatch = false)
        {
            //no work to do here
            if (maxWedges >= m_initialWedges.Count)
            {
                cutData = m_data;
                return;
            }

            //list of vertices deleted during the process of collapsing
            //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            //sw.Start();

            //int counter = 0;
            //while (counter < 1E04)
            //{
            //   FindDeletedVertices(maxWedges);
            //    counter++;
            //}
            //sw.Stop();
            //Debug.Log("V1 took " + sw.ElapsedMilliseconds + " ms");

            //sw = new System.Diagnostics.Stopwatch();
            //sw.Start();

            //counter = 0;
            //while (counter < 1E04)
            //{
            //    FindDeletedVertices2(maxWedges);
            //    counter++;
            //}
            //sw.Stop();
            //Debug.Log("V2 took " + sw.ElapsedMilliseconds + " ms");

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            if (bStopWatch)
            {
                sw = new System.Diagnostics.Stopwatch();
                sw.Start();
            }

            //List<int> deletedVertices = FindDeletedVertices(maxWedges);
            int[] deletedVertices = FindDeletedVertices(maxWedges);

            //list of vertices displaced during the process of collapsing
            List<DisplacedVertex> displacedVertices = FindDisplacedVertices(maxWedges);

            //map vertices that have collapsed
            List<CollapsedVertex> collapsedVertices = MapVertices(maxWedges);

            //copy IDs of collapsed and deleted vertices into an array and sort it by ascending order
            int[] dismissedVerticesIDs = new int[collapsedVertices.Count + deletedVertices.Length];
            for (int i = 0; i != collapsedVertices.Count; i++)
            {
                dismissedVerticesIDs[i] = collapsedVertices[i].m_initialIndex;
            }

            for (int i = collapsedVertices.Count; i != dismissedVerticesIDs.Length; i++)
            {
                dismissedVerticesIDs[i] = deletedVertices[i - collapsedVertices.Count];
            }

            if (dismissedVerticesIDs.Length > 1)
                System.Array.Sort(dismissedVerticesIDs);

            if (bStopWatch)
            {
                sw.Stop();
                Debug.Log("STEP1 took " + sw.ElapsedMilliseconds + " ms");
            }

            if (bStopWatch)
            {
                sw = new System.Diagnostics.Stopwatch();
                sw.Start();
            }

            //Traverse wedges (untouched and collapsed ones) and fill in the vertices array by shifting one vertex according to the previous sorted array
            Vertex[] cutVertices = new Vertex[m_data.Verts.Length];
            int maxID = -1;
            for (int i = 0; i != m_initialWedges.Count; i++)
            {
                Wedge wedge = m_initialWedges[i];
                for (int j = 0; j != wedge.Vertices.Count; j++)
                {
                    Vertex vertex = wedge.Vertices[j];

                    //create a copy of this vertex to maintain intact the original one
                    Vertex copiedVertex = new Vertex();
                    copiedVertex.m_position = vertex.m_position;
                    copiedVertex.ID = vertex.ID;
                    copiedVertex.m_color = vertex.m_color;
                    copiedVertex.m_uv = vertex.m_uv;

                    //test if the vertex collapsed or has been deleted
                    bool bDismissVertex = false;
                    for (int p = 0; p != dismissedVerticesIDs.Length; p++)
                    {
                        if (dismissedVerticesIDs[p] == copiedVertex.ID)
                            bDismissVertex = true;
                    }

                    if (!bDismissVertex)
                    {
                        //test if it has been displaced and update its position accordingly
                        for (int p = 0; p != displacedVertices.Count; p++)
                        {
                            if (displacedVertices[p].m_index == copiedVertex.ID)
                            {
                                copiedVertex.m_position = displacedVertices[p].m_targetPosition;
                                break;
                            }
                        }

                        //compute the new id of this vertex and populate the new vertex array
                        int id = GetShiftedID(dismissedVerticesIDs, copiedVertex.ID);
                        if (id > maxID)
                            maxID = id;
                        if (cutVertices[id] == null)
                            cutVertices[id] = copiedVertex;
                    }
                }
            }

            //use the maximum vertex index to crop the array
            System.Array.Resize(ref cutVertices, maxID + 1);

            if (bStopWatch)
            {
                sw.Stop();
                Debug.Log("STEP2 took " + sw.ElapsedMilliseconds + " ms");
            }

            //now build the triangle list
            List<int> triangles = new List<int>();
            for (int i = 0; i != m_data.Tris.Length; i += 3)
            {
                int p0 = m_data.Tris[i];
                int p1 = m_data.Tris[i + 1];
                int p2 = m_data.Tris[i + 2];

                //first check if all triangle vertices still exist
                bool bDismissTriangle = false;
                for (int p = 0; p != deletedVertices.Length; p++)
                {
                    if (p0 == deletedVertices[p] || p1 == deletedVertices[p] || p2 == deletedVertices[p])
                    {
                        bDismissTriangle = true;
                        break;
                    }
                }

                if (bDismissTriangle)
                    continue;

                //if this triangle is valid, find the correct ID for each vertex
                p0 = GetShiftedID(dismissedVerticesIDs, GetCollapseIDForID(collapsedVertices, p0));
                p1 = GetShiftedID(dismissedVerticesIDs, GetCollapseIDForID(collapsedVertices, p1));
                p2 = GetShiftedID(dismissedVerticesIDs, GetCollapseIDForID(collapsedVertices, p2));

                //one-dimensional (flat) triangle, dismiss it
                if (p0 == p1 || p1 == p2 || p2 == p0)
                    continue;

                //accumulate triangle normal for each vertex
                Triangle triangle = new Triangle(cutVertices[p0], cutVertices[p1], cutVertices[p2]);
                //cutVertices[p0].AccumulateNormalFromTriangle(triangle.m_normal);
                //cutVertices[p1].AccumulateNormalFromTriangle(triangle.m_normal);
                //cutVertices[p2].AccumulateNormalFromTriangle(triangle.m_normal);

                triangles.Add(p0);
                triangles.Add(p1);
                triangles.Add(p2);
            }

            //extract data from the Vertex array
            Vector3[] outputVertices = new Vector3[cutVertices.Length];
            //Vector3[] outputNormals = new Vector3[cutVertices.Length];
            Color[] outputColors = (m_data.Colors != null) ? new Color[cutVertices.Length] : null;
            Vector2[] outputUVs = (m_data.UVs != null) ? new Vector2[cutVertices.Length] : null;
            for (int i = 0; i != outputVertices.Length; i++)
            {
                //vertices
                outputVertices[i] = cutVertices[i].m_position;
                //normals
                //outputNormals[i] = cutVertices[i].GetAccumulationNormal();
                //colors
                if (outputColors != null)
                    outputColors[i] = cutVertices[i].m_color;
                //uv
                if (outputUVs != null)
                    outputUVs[i] = cutVertices[i].m_uv;

            }

            cutData = new ModelData(outputVertices, triangles.ToArray());
            //cutData.Normals = outputNormals;
            cutData.Colors = outputColors;
            cutData.UVs = outputUVs;
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
                
                for (int v = 0; v != wedgeVerticesCopy.Count; v++)
                {
                    Wedge collapseWedge = wedge; //reset the wedge to the actual wedge holding this vertex

                    CollapsedVertex collapsedVertex = new CollapsedVertex(-1, -1);
                    Vertex vertex = wedgeVerticesCopy[v];
                    collapsedVertex.m_initialIndex = vertex.ID;

                    int stopIndex = 0;
                    while (collapseWedge.ID >= maxWedges && stopIndex < 100)
                    {
                        if (collapseWedge.m_collapse == null)
                            break;

                        //Vertex collapseVertex = vertex.FindVertexToCollapseOn(collapseWedge.m_collapse);
                        Vertex collapseVertex = collapseWedge.MapVertex(vertex);

                        if (collapseVertex != null)
                        {
                            vertex = collapseVertex;
                            collapsedVertex.m_collapsedIndex = collapseVertex.ID;
                        }

                        //collapseWedge = collapseWedge.m_collapse;
                        //int nextWedgeIndex = m_collapseMap[collapseWedge.ID];
                        //if (nextWedgeIndex >= 0)
                        //    collapseWedge = m_initialWedges[m_collapseMap[collapseWedge.ID]];
                        //else
                        //    break;

                        int nextWedgeIndex = m_collapseMap[collapseWedge.ID];
                        if (nextWedgeIndex == 0)
                            break;
                        else
                            collapseWedge = collapseWedge.m_collapse;


                        //if (collapseWedge != null)
                        //{
                        //    //set the vertex position as the position of the last wedge it collapsed to or been moved to
                        //    collapsedVertex.m_position = collapseWedge.m_position;
                        //}

                        stopIndex++;
                    }
                    if (stopIndex >= 99)
                    Debug.Log("stopIndex:" + stopIndex);

                    if (collapsedVertex.m_collapsedIndex >= 0)
                        collapsedVertices.Add(collapsedVertex);
                }
            }

            return collapsedVertices;
        }

        //private List<int> FindDeletedVertices(int maxWedges)
        //{
        //    List<int> deletedVertices = new List<int>();
        //    for (int i = m_initialWedges.Count - 1; i != maxWedges - 1; i--)
        //    {
        //        deletedVertices.AddRange(m_initialWedges[i].m_deletedVertices);
        //    }

        //    return deletedVertices;
        //}

        private int[] FindDeletedVertices(int maxWedges)
        {
            int deletedVerticesCount = 0;
            for (int i = m_initialWedges.Count - 1; i != maxWedges - 1; i--)
            {
                deletedVerticesCount += m_initialWedges[i].m_deletedVertices.Count;
            }

            int[] deletedVertices = new int[deletedVerticesCount];
            int index = 0;
            for (int i = m_initialWedges.Count - 1; i != maxWedges - 1; i--)
            {
                for (int j = 0; j != m_initialWedges[i].m_deletedVertices.Count; j++)
                {
                    deletedVertices[index] = m_initialWedges[i].m_deletedVertices[j];
                    index++;
                }
            }

            return deletedVertices;
        }

        private List<DisplacedVertex> FindDisplacedVertices(int maxWedges)
        {
            List<DisplacedVertex> displacedVertices = new List<DisplacedVertex>();
            for (int i = m_initialWedges.Count - 1; i != maxWedges - 1; i--)
            {
                if (displacedVertices.Count == 0) //slight optim when list empty, no need to check if item already exists
                    displacedVertices.AddRange(m_initialWedges[i].m_displacedVertices);
                else
                {
                    for (int p = 0; p != m_initialWedges[i].m_displacedVertices.Count; p++)
                    {
                        DisplacedVertex vertex = m_initialWedges[i].m_displacedVertices[p];

                        //test if the vertex is already in the global list of displaced vertices
                        bool bVertexAlreadyExists = false;
                        for (int q = 0; q != displacedVertices.Count; q++)
                        {
                            if (displacedVertices[q].m_index == vertex.m_index)
                            {
                                bVertexAlreadyExists = true;
                                displacedVertices[q] = vertex; //update the vertex position
                                break;
                            }
                        }

                        if (!bVertexAlreadyExists)
                            displacedVertices.Add(vertex);
                    }
                }
            }

            return displacedVertices;
        }

        /**
        * Shift the parameter 'id' according to the array of 'vertices' that collapsed
        **/
        private int GetShiftedID(int[] vertices, int id)
        {
            int shift = 0;
            for (int i = 0; i != vertices.Length; i++)
            {
                if (id > vertices[i])
                    shift++;
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
        private Wedge WedgeForPosition(List<Wedge> wedges, Vector3 position)
        {
            for (int i = 0; i != wedges.Count; i++)
            {
                float sqrDistance = (wedges[i].m_position - position).sqrMagnitude;
                if (sqrDistance < 1E-07)
                    return wedges[i];
            }

            return null;
        }

        /**
        * Return the wedge with ID 'id'
        **/
        private Wedge WedgeForID(List<Wedge> wedges, int id)
        {
            for (int i = 0; i != wedges.Count; i++)
            {
                if (wedges[i].ID == id)
                    return wedges[i];
            }

            return null;
        }

        /**
        * Reorder the wedges according to the permutation array
        **/
        private void PermuteWedges(int[] permutation)
        {
            if (permutation.Length != m_initialWedges.Count)
                throw new System.Exception("permutation list and initial wedges are not of the same size");

            // rearrange the wedge array 
            List<Wedge> tmpArray = new List<Wedge>(m_initialWedges);

            for (int i = 0; i < m_initialWedges.Count; i++)
            {
                m_initialWedges[permutation[i]] = tmpArray[i];
                //m_initialWedges[permutation[i]].ID = permutation[i];
                //m_initialWedges[i].ID = i;                
            }

            //use the collapse map to attribute one collapse wedge or a null one to each wedge
            for (int i = 0; i < m_initialWedges.Count; i++)
            {
                m_initialWedges[i].m_collapse = (m_collapseMap[i] == -1) ? null : m_initialWedges[m_collapseMap[i]];

                //int collapseVertexIndex = m_collapseMap[i];
                //if (collapseVertexIndex > 0)
                //    m_initialWedges[i].m_collapse = m_initialWedges[collapseVertexIndex];
                //else
                //    m_initialWedges[i].m_collapse = null;
            }

            for (int i = 0; i < m_initialWedges.Count; i++)
            {
                m_initialWedges[permutation[i]].ID = permutation[i];
                //m_initialWedges[i].ID = i;                
            }
            
            int index = 0;
            for (int i = 0; i != m_collapseMap.Length; i++)
            {
                if (m_collapseMap[i] == -1)
                    index++;
                else
                    break;
            }

            m_minRendereWedgesCount = Mathf.Max(index, 3); //stop the collapsing process at the last vertex we can actually collapse or 3 to render at least one triangle

            Debug.Log(index + " vertices cannot collapse");
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

            //// find the "sides" triangles that are on the edge uv
            //List<Triangle> sides = new List<Triangle>();
            //for (i = 0; i < u.AdjacentTriangles.Count; i++)
            //{
            //    Triangle triangle = u.AdjacentTriangles[i];
            //    if (v.HasAdjacentTriangle(triangle)) //triangle is both adjacent to wedge u and v, so adjacent to edge [u-v]
            //        sides.Add(u.AdjacentTriangles[i]);
            //}

            // find the "sides" triangles that are on the edge uv
            List<Triangle> sides = u.GetSharedTrianglesWithWedge(v);

            if (sides.Count < 2) //wedge u cannot be collapsed on v because this edge is non manifold (i.e does not have 2 adjacent triangle)
                return -1; //return a negative/invalid cost

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
                w.m_cost = 0;
                return;
            }
            w.m_cost = 1000000;
            w.m_collapse = null;
            // search all neighboring edges for "least cost" edge
            for (int i = 0; i < w.Neighbors.Count; i++)
            {
                float dist;
                dist = ComputeEdgeCollapseCost(w, w.Neighbors[i]);
                if (dist < 0) //non-manifold edge
                {
                    w.m_collapse = null;
                    w.m_cost = 0;
                }
                else
                {
                    if (dist < w.m_cost)
                    {
                        w.m_collapse = w.Neighbors[i];  // candidate for edge collapse
                        w.m_cost = dist;             // cost of the collapse
                    }
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
        private void Collapse(Wedge u, Wedge v)
        {
            // Collapse the edge uv by moving vertex u onto v
            // Actually remove tris on uv, then update tris that
            // have u to have v, and then remove u.
            if (v == null)
            {
                // u is a vertex all by itself so just delete it
                u.Delete();
                m_wedges.Remove(u);
                return;
            }

            //Find vertices that will collapse or be displaced
            for (int i = 0; i != u.Vertices.Count; i++)
            {
                Vertex vertex = u.Vertices[i];
                Vertex collapseVertex = vertex.FindVertexToCollapseOn(v); //the vertex it will collapse on if it exists

                if (collapseVertex != null)
                {
                    u.m_collapsedVertices.Add(vertex.ID, collapseVertex.ID); //no exception will be thrown as unique IDs are inserted into that dictionary

                    //CollapsedVertex collapsedVertex = new CollapsedVertex(vertex.ID, collapseVertex.ID);

                    //u.m_collapsedVertices.Add(collapsedVertex);
                }
                else
                {
                    DisplacedVertex displacedVertex = new DisplacedVertex();
                    displacedVertex.m_index = vertex.ID;
                    displacedVertex.m_targetPosition = v.m_position;

                    u.m_displacedVertices.Add(displacedVertex);
                }
            }

            //delete triangles on edge[u - v]
            List<Triangle> sharedTriangles = u.GetSharedTrianglesWithWedge(v);
            for (int i = 0; i != sharedTriangles.Count; i++)
            {
                //delete the triangle
                sharedTriangles[i].Delete();

                for (int j = 0; j != sharedTriangles[i].Vertices.Length; j++)
                {
                    Vertex vertex = sharedTriangles[i].Vertices[j];

                    if (!u.HasVertex(vertex) && !v.HasVertex(vertex)) //the third wedge that is not u or v holds this vertex
                    {
                        if (vertex.AdjacentTriangles.Count == 0) //vertex is isolated, remove it
                        {
                            //find the wedge and remove the vertex from its internal list
                            GetWedgeHoldingVertex(m_wedges, vertex).RemoveVertex(vertex);
                            //add the vertex ID to the list of vertices deleted during the operation of collapsing u onto v
                            u.m_deletedVertices.Add(vertex.ID);
                        }
                    }
                }
            }

            //perform the actual collapse
            u.CollapseOnWedge(v);           

            //add u neighbors to v and add v as a new neighbor of u neighbors
            for (int i = 0; i != u.Neighbors.Count; i++)
            {
                if (u.Neighbors[i] != v)
                {
                    v.AddNeighbor(u.Neighbors[i]);
                    u.Neighbors[i].AddNeighbor(v);
                }
            }
            
            //neighbors of wedge u are likely to have lost one triangle through one of their vertex so invalidate them
            for (int i = 0; i != u.Neighbors.Count; i++)
            {
                u.Neighbors[i].InvalidateAdjacentTriangles();
            }
            
            //delete the wedge and remove it from global list
            u.Delete();
            m_wedges.Remove(u);

            // recompute the edge collapse costs for neighboring wedges
            for (int i = 0; i < u.Neighbors.Count; i++)
            {
                ComputeEdgeCostAtWedge(u.Neighbors[i]);
            }
        }

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
            Wedge mn = null;
            float minCost = float.MaxValue;
            for (int i = 0; i < m_wedges.Count; i++)
            {
                if (m_wedges[i].m_collapse == null)
                    continue;

                if (m_wedges[i].m_cost == 0) //zero cost, take the first wedge we encounter
                    return m_wedges[i];

                if (m_wedges[i].m_cost < minCost)
                {
                    mn = m_wedges[i];
                    minCost = m_wedges[i].m_cost;
                }
            }

            if (mn == null) //we spent all wedges that collapse, only wedges with null collapse remain so return the first one
                mn = m_wedges[0];

            return mn;
        }

        /**
         * Build some data to test our algorithm
         * **/
        private ModelData GetDummyData()
        {
            //FILE SAMPLES
            float[,] dummyVertices = RabbitData.rabbit_vertices;
            int[,] dummyTriangles = RabbitData.rabbit_triangles;
            //float[,] dummyVertices = PlaneData.plane_vertices;
            //int[,] dummyTriangles = PlaneData.plane_triangles;



            //SAMPLE 1
            //float[,] dummyVertices = {
            //                            { 1, 0, 3 },
            //                            { 0, -1, -1},
            //                            { 2, 0.5f, 0},
            //                            { 2.5f, -1, -1.5f},
            //                            { 4, 0, 1},
            //                            { 3.5f, 2, 1}
            //                         };

            //int[,] dummyTriangles = {
            //                            { 0, 2, 1},
            //                            { 1, 2, 3},
            //                            { 2, 5, 3},
            //                            { 3, 5, 4}
            //                         };

            //SAMPLE 2
            //float[,] dummyVertices = {
            //                            { -5, 2, 0 },
            //                            { -5, -3, 0},
            //                            { 0, 5, 0},
            //                            { 3, 5, 0},
            //                            { 5, -3, 1},
            //                            { 1, -5, 1},
            //                            { -1, 0, 1},
            //                            { 2, 0, 1}
            //                         };

            //int[,] dummyTriangles = {
            //                            { 0, 6, 1},
            //                            { 1, 6, 5},
            //                            { 6, 7, 5},
            //                            { 5, 7, 4},
            //                            { 4, 7, 3},
            //                            { 7, 2, 3},
            //                            { 6, 2, 7},
            //                            { 6, 0, 2}
            //                         };

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

            ModelData dummyData = new ModelData(verts.ToArray(), tris.ToArray());
            return dummyData;
        }

        /**
        * Reassign vertices and triangles to the model
        **/
        private void RefreshModel(ModelData data)
        {
            if (GetComponent<MeshFilter>().sharedMesh == null)
            {
                m_mesh = new Mesh();
                GetComponent<MeshFilter>().sharedMesh = m_mesh;
            }
            else
                m_mesh.Clear();

            //m_mesh = new Mesh();
            //this.GetComponent<MeshFilter>().sharedMesh = m_mesh;
            //m_mesh.Clear();

            m_mesh.vertices = data.Verts;
            m_mesh.triangles = data.Tris;
            m_mesh.colors = data.Colors;
            m_mesh.uv = data.UVs;

            m_mesh.RecalculateBounds();
            m_mesh.RecalculateNormals();
        }


        public void Update()
        {
            if (m_data != null)
            {
                if (m_renderedWedgesCount < m_minRendereWedgesCount)
                    m_renderedWedgesCount = m_minRendereWedgesCount;
                if (m_renderedWedgesCount > m_initialWedges.Count)
                    m_renderedWedgesCount = m_initialWedges.Count;

                if (m_renderedWedgesCount != m_prevRenderedWedgesCount)
                {
                    //Debug.Log("RenderModel:" + m_prevRenderedWedgesCount + " - " + m_renderedWedgesCount);
                    RenderModel();

                    m_prevRenderedWedgesCount = m_renderedWedgesCount;
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
            for (int i = 0; i != m_data.Verts.Length; i++)
            {
                Vector3 vertex = m_data.Verts[i];
                string strVertex = "\t\t\t{" + (vertex.x * scale) + "f," + (vertex.y * scale) + "f," + (vertex.z * scale) + "f}";
                strData += strVertex;
                if (i < m_data.Verts.Length - 1)
                    strData += ",";
                strData += "\n";
            }

            strData += "\t\t};\n\n";

            //triangles
            strData += "\t\tpublic static int[,] plane_triangles =\n";
            strData += "\t\t{\n";
            for (int i = 0; i != m_data.Tris.Length; i += 3)
            {
                string strTri = "\t\t\t{" + m_data.Tris[i] + "," + m_data.Tris[i + 1] + "," + m_data.Tris[i + 2] + "}";
                strData += strTri;
                if (i < m_data.Tris.Length - 1)
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
