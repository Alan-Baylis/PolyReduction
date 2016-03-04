using UnityEngine;
using System.Collections.Generic;

namespace PolyReduction
{
    public class PolyReducer
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

        /**
         * Init the model and perform collapse operation on it
         * **/
        public void InitModel()
        {
            List<int> permutation;
            m_data = GetDummyData();
            ProgressiveMesh(m_data.Verts, m_data.Tris, out m_collapseMap, out permutation);

            PermuteVertices(permutation);
            //model_position = Vector(0, 0, -3);
            //Quaternion yaw(Vector(0, 1, 0), -3.14f / 4);    // 45 degrees
            //Quaternion pitch(Vector(1, 0, 0), 3.14f / 12);  // 15 degrees 
            //model_orientation = pitch * yaw;
        }

        /**
         * Core function of the algorithm
         * **/
        public void ProgressiveMesh(List<Vector3> verts, List<int> tris, out List<int> map, out List<int> permutation)
        {
            PrepareMeshData(verts, tris);

            float cost = ComputeEdgeCollapseCost(m_vertices[0], m_vertices[1]);

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
            // The caller of this function should reorder their vertices
            // according to the returned "permutation".
        }

        /**
         * Transform the inital mesh data (verts and tris) to one which is more appropriate to our algorithm (with more info in it
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

        void PermuteVertices(List<int> permutation)
        {
            if (permutation.Count != m_data.Verts.Count)
                throw new System.Exception("permutation list and initial vertices are not of the same size");

            // rearrange the vertex Array 
            List<Vector3> tmpArray = new List<Vector3>(m_data.Verts);
            //List<Vector3> tmpArray = new List<Vector3>(permutation.Count);

            //for (int i = 0; i < m_data.Verts.Count; i++)
            //{
            //    tmpArray.Add(m_data.Verts[i]);
            //}


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
        public float ComputeEdgeCollapseCost(Vertex u, Vertex v)
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
        public void ComputeEdgeCostAtVertex(Vertex v)
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
        public void ComputeAllEdgeCollapseCosts()
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
        public void Collapse(Vertex u, Vertex v)
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
        public Vertex MinimumCostEdge()
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
            List<Vector3> verts = new List<Vector3>(4);
            verts.Add(new Vector3(0, 0, 0));
            verts.Add(new Vector3(10, 0, 0));
            verts.Add(new Vector3(0, 5, 0));
            verts.Add(new Vector3(3, 8, 5));

            List<int> tris = new List<int>(12);
            tris.Add(0);
            tris.Add(2);
            tris.Add(1);

            tris.Add(0);
            tris.Add(1);
            tris.Add(3);

            tris.Add(1);
            tris.Add(2);
            tris.Add(3);

            tris.Add(2);
            tris.Add(0);
            tris.Add(3);

            ModelData dummyData = new ModelData(verts, tris);
            return dummyData;
        }
    }
}
