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

        public override bool Equals(object obj)
        {
            if (!(obj is Vertex))
                return false;

            Vertex other = (Vertex)obj;

            if (m_position != other.m_position)
                return false;

            return m_position == other.m_position;
        }

        public override int GetHashCode() { return Mathf.RoundToInt(m_position.x) ^ Mathf.RoundToInt(m_position.y) ^ Mathf.RoundToInt(m_position.z); }

        public Wedge(Vector3 v, int id)
        {
            m_position = v;
            m_iID = id;

            m_vertices = new List<Vertex>();
            m_neighbors = new List<Wedge>(3);
            m_adjacentTriangles = new List<Triangle>(3);
        }

        public void AddVertex(Vertex vertex)
        {
            m_vertices.Add(vertex);

            //set this wedge as parent wedge
            vertex.m_parentWedge = this;

            //add all triangles to wedge
            for (int i = 0; i != vertex.AdjacentTriangles.Count; i++)
            {
                AddAdjacentTriangle(vertex.AdjacentTriangles[i]);
            }
        }

        public void RemoveVertex(Vertex vertex)
        {
            m_vertices.Add(vertex);
            if (HasVertex(vertex))
                m_vertices.Remove(vertex);
        }

        public bool HasVertex(Vertex vertex)
        {
            for (int i = 0; i != m_vertices.Count; i++)
            {
                if (vertex == m_vertices[i])
                {
                    return true;
                }
            }

            return false;
        }

        public void AddAdjacentTriangle(Triangle triangle)
        {
            if (!HasAdjacentTriangle(triangle))
                m_adjacentTriangles.Add(triangle);
        }

        public void RemoveAdjacentTriangle(Triangle triangle)
        {
            if (HasAdjacentTriangle(triangle))
                m_adjacentTriangles.Remove(triangle);
        }

        public bool HasAdjacentTriangle(Triangle triangle)
        {
            for (int i = 0; i != m_adjacentTriangles.Count; i++)
            {
                if (triangle == m_adjacentTriangles[i])
                    return true;
            }

            return false;
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
            for (int i = 0; i != m_neighbors.Count; i++)
            {
                if (neighbor == m_neighbors[i])
                    return true;
            }

            return false;
        }

        public void RemoveIfNonNeighbor(Wedge w)
        {
            // removes v from neighbor list if w isn't a neighbor.
            if (!HasNeighbor(w))
                return;

            for (int i = 0; i != m_vertices.Count; i++)
            {
                m_vertices[i].RemoveIfNonNeighbor(m_vertices[i]);
            }

            RemoveNeighbor(w);
        }

        public void Delete()
        {
            if (m_adjacentTriangles.Count > 0)
                throw new System.Exception("Vertex still references one or more adjacent triangles");

            //for each neighbor of this vertex remove it from the neighbors list
            for (int i = 0; i != m_neighbors.Count; i++)
            {
                m_neighbors[i].m_neighbors.Remove(this);
            }
        }
    }
}