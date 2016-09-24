using UnityEngine;
using System.Collections.Generic;

namespace PolyReduction
{
    public class Wedge
    {
        public Vector3 m_position { get; set; } // location of this wedge

        private int m_iID; // place of wedge in original list
        public int ID
        {
            get
            {
                return m_iID;
            }

            set
            {
                m_iID = value;
            }
        }

        private List<Vertex> m_vertices;
        public List<Vertex> Vertices
        {
            get
            {
                return m_vertices;
            }
        }

        private List<Wedge> m_neighbors; // adjacent wedges
        public List<Wedge> Neighbors
        {
            get
            {
                return m_neighbors;
            }
        }

        private List<Triangle> m_adjacentTriangles; // adjacent triangles
        public List<Triangle> AdjacentTriangles
        {
            get
            {
                return m_adjacentTriangles;
            }
        }

        public float m_cost { get; set; } // cached cost of collapsing edge
        public Wedge m_collapse { get; set; } // candidate wedge for collapse        

        //public List<CollapsedVertex> m_collapsedVertices; //mapping between vertices that collapse
        public Dictionary<int, int> m_collapsedVertices; //mapping between vertices that collapse
        public List<DisplacedVertex> m_displacedVertices; //store here vertices that have been displaced after this wedge collapsed
        public List<int> m_deletedVertices; //store here vertices that no longer exist after this wedge collapsed
        

        public Wedge(Vector3 v, int id)
        {
            m_position = v;
            m_iID = id;

            m_vertices = new List<Vertex>();
            m_neighbors = new List<Wedge>(3);
            m_adjacentTriangles = new List<Triangle>(3);
            //m_collapsedVertices = new List<CollapsedVertex>();
            m_collapsedVertices = new Dictionary<int, int>();
            m_displacedVertices = new List<DisplacedVertex>();
            m_deletedVertices = new List<int>();
        }

        public void AddVertex(Vertex vertex)
        {
            m_vertices.Add(vertex);
        }

        public void RemoveVertex(Vertex vertex)
        {
            if (HasVertex(vertex))
                m_vertices.Remove(vertex);
        }

        public bool HasVertex(Vertex vertex)
        {
            return m_vertices.Contains(vertex);
        }

        public Vertex GetVertexForID(int id)
        {
            for (int i = 0; i != m_vertices.Count; i++)
            {
                if (m_vertices[i].ID == id)
                    return m_vertices[i];
            }

            return null;
        }

        /**
        * Recompute the list of adjacent triangles using the list of child vertices
        **/
        public void InvalidateAdjacentTriangles()
        {
            m_adjacentTriangles.Clear();

            for (int i = 0; i != m_vertices.Count; i++)
            {
                for (int j = 0; j != m_vertices[i].AdjacentTriangles.Count; j++)
                {
                    if (!HasAdjacentTriangle(m_vertices[i].AdjacentTriangles[j]))
                        m_adjacentTriangles.Add(m_vertices[i].AdjacentTriangles[j]);
                }
            }
        }

        public bool HasAdjacentTriangle(Triangle triangle)
        {
            return m_adjacentTriangles.Contains(triangle);
        }

        public void AddNeighbor(Wedge neighbor)
        {
            if (!HasNeighbor(neighbor))
                m_neighbors.Add(neighbor);
        }

        public void RemoveNeighbor(Wedge neighbor)
        {
            if (HasNeighbor(neighbor))
                m_neighbors.Remove(neighbor);
        }

        public bool HasNeighbor(Wedge neighbor)
        {
            return m_neighbors.Contains(neighbor);
        }

        /**
        * Return the triangles shared by two wedges.
        **/
        public List<Triangle> GetSharedTrianglesWithWedge(Wedge wedge)
        {
            List<Triangle> sharedTriangles = new List<Triangle>();

            for (int i = 0; i < m_adjacentTriangles.Count; i++)
            {
                for (int j = 0; j != wedge.m_adjacentTriangles.Count; j++)
                {
                    if (m_adjacentTriangles[i] == wedge.m_adjacentTriangles[j])
                        sharedTriangles.Add(m_adjacentTriangles[i]);
                }
            }

            return sharedTriangles;
        }

        /**
        * Collapse all vertices in this wedge using the m_mappedVertices list
        **/
        public void CollapseOnWedge(Wedge w)
        {
            //w = m_collapse;

            //collapsed vertices
            Vertex[] collapseVertices = new Vertex[m_collapsedVertices.Count];
            int collapseVertexIdx = 0;
            foreach (KeyValuePair<int, int> kvp in m_collapsedVertices)
            {
                Vertex vertex = GetVertexForID(kvp.Key);

                Vertex collapseVertex = w.GetVertexForID(kvp.Value);
                vertex.CollapseOnWedgeVertex(collapseVertex);

                collapseVertices[collapseVertexIdx] = collapseVertex;
                collapseVertexIdx++;     
            }

            for (int i = 0; i != collapseVertices.Length; i++)
            {
                Vertex collapseVertex = collapseVertices[i];

                //the collapse vertex does not have any adjacent triangles even after collapsing operation, delete it
                if (collapseVertex.AdjacentTriangles.Count == 0)
                {
                    w.RemoveVertex(collapseVertex);
                    this.m_deletedVertices.Add(collapseVertex.ID);
                }
            }

            //for (int i = 0; i != m_collapsedVertices.Count; i++)
            //{
            //    Vertex vertex = GetVertexForID(m_collapsedVertices[i].m_initialIndex);

            //    //if (vertex != null) //vertex has been deleted
            //    Vertex collapseVertex = w.GetVertexForID(m_collapsedVertices[i].m_collapsedIndex);
            //    vertex.CollapseOnWedgeVertex(collapseVertex);

            //    //the collapse vertex does not have any adjacent triangles even after collapsing operation, delete it
            //    if (collapseVertex.AdjacentTriangles.Count == 0)
            //    {
            //        w.RemoveVertex(collapseVertex);
            //        this.m_deletedVertices.Add(collapseVertex.ID);
            //    }
            //}

            //displaced vertices
            for (int i = 0; i != m_displacedVertices.Count; i++)
            {
                Vertex vertex = GetVertexForID(m_displacedVertices[i].m_index);

                //if (vertex != null)
                w.AddVertex(vertex);
                this.RemoveVertex(vertex);
            }
        }

        public Vertex MapVertex(Vertex vertex)
        {
            if (m_collapsedVertices == null)
                return null;

            //collapsed vertices
            int collapseVertexID;
            if (m_collapsedVertices.TryGetValue(vertex.ID, out collapseVertexID))
                return m_collapse.GetVertexForID(collapseVertexID);

            return null;

            //for (int i = 0; i != m_collapsedVertices.Count; i++)
            //{
            //    if (m_collapsedVertices[i].m_initialIndex == vertex.ID)
            //    {
            //        if (m_collapse != null)
            //            return m_collapse.GetVertexForID(m_collapsedVertices[i].m_collapsedIndex);
            //    }

            //}

            //return null;
        }

        public void Delete()
        {
            //for each neighbor of this wedge remove it from their neighbors list
            for (int i = 0; i != Neighbors.Count; i++)
            {
                Neighbors[i].RemoveNeighbor(this);
            }
        }
    }
}