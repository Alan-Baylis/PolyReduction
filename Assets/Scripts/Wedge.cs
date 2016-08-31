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

        public struct CollapsedVertex
        {
            public int m_initialIndex;
            public int m_collapsedIndex;
            public Vector3 m_position;

            public override string ToString()
            {
                return "vertex " + m_initialIndex + " collapsed on vertex " + m_collapsedIndex + " at position " + m_position;
            }
        }

        public List<CollapsedVertex> m_mappedVertices;

        //public override bool Equals(object obj)
        //{
        //    if (!(obj is Wedge))
        //        return false;

        //    Wedge other = (Wedge)obj;

        //    return m_iID == other.m_iID;
        //}

        //public override int GetHashCode() { return Mathf.RoundToInt(m_position.x) ^ Mathf.RoundToInt(m_position.y) ^ Mathf.RoundToInt(m_position.z); }

        public Wedge(Vector3 v, int id)
        {
            m_position = v;
            m_iID = id;

            m_vertices = new List<Vertex>();
            m_neighbors = new List<Wedge>(3);
            m_adjacentTriangles = new List<Triangle>(3);
        }

        //public Wedge(Wedge other)
        //{
        //    m_position = other.m_position;
        //    m_iID = other.m_iID;

        //    m_vertices = new List<Vertex>(other.m_vertices.Count);
        //    for (int i = 0; i != other.m_vertices.Count; i++)
        //    {
        //        m_vertices.Add(new Vertex(other.m_vertices[i]));
        //    }

        //    m_neighbors = new List<Wedge>(other.m_neighbors.Count);
        //    for (int i = 0; i != other.m_neighbors.Count; i++)
        //    {
        //        m_neighbors.Add(new Wedge(other.m_neighbors[i]));
        //    }

        //    m_adjacentTriangles = new List<Triangle>(other.m_adjacentTriangles.Count);
        //    for (int i = 0; i != other.m_adjacentTriangles.Count; i++)
        //    {
        //        m_adjacentTriangles.Add(new Triangle(other.m_adjacentTriangles[i]));
        //    }
        //}

        public void AddVertex(Vertex vertex)
        {
            m_vertices.Add(vertex);
        }

        public void RemoveVertex(Vertex vertex)
        {
            m_vertices.Add(vertex);
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
        //public List<Triangle> GetSharedTrianglesWithWedge(Wedge wedge)
        //{
        //    List<Triangle> sharedTriangles = new List<Triangle>();

        //    for (int i = 0; i < m_adjacentTriangles.Count; i++)
        //    {
        //        for (int j = 0; j != wedge.m_adjacentTriangles.Count; j++)
        //        {
        //            if (m_adjacentTriangles[i] == wedge.m_adjacentTriangles[j])
        //                sharedTriangles.Add(m_adjacentTriangles[i]);
        //        }
        //    }

        //    return sharedTriangles;
        //}

        public void MapVertices(Wedge v)
        {
            m_mappedVertices = new List<CollapsedVertex>();

            for (int i = 0; i != m_vertices.Count; i++)
            {
                CollapsedVertex collapsedVertex = new CollapsedVertex();
                Vertex vertex = m_vertices[i];
                collapsedVertex.m_initialIndex = vertex.ID;

                Vertex collapseVertex = vertex.FindVertexToCollapseOn(v);
                if (collapseVertex != null)
                {
                    vertex = collapseVertex;
                    collapsedVertex.m_collapsedIndex = collapseVertex.ID;
                    collapsedVertex.m_position = v.m_position;
                    m_mappedVertices.Add(collapsedVertex);
                }
            }
        }

        public Vertex MapVertex(Vertex vertex)
        {
            if (m_mappedVertices == null)
                return null;

            for (int i = 0; i != m_mappedVertices.Count; i++)
            {
                if (m_mappedVertices[i].m_initialIndex == vertex.ID)
                {
                    if (m_collapse != null)
                        return m_collapse.GetVertexForID(m_mappedVertices[i].m_collapsedIndex);
                }
                    
            }

            return null;
        }

        public void Delete()
        {
            //for each neighbor of this vertex remove it from the neighbors list
            for (int i = 0; i != m_neighbors.Count; i++)
            {
                m_neighbors[i].RemoveNeighbor(this);
            }
        }
    }
}