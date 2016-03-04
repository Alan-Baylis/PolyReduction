using UnityEngine;
using System.Collections.Generic;

namespace PolyReduction
{
    public class Vertex
    {
        public Vector3 m_position { get; set; } // location of this point

        private int m_iID; // place of vertex in original list
        public int ID
        {
            get
            {
                return m_iID;
            }
        }


        private List<Vertex> m_neighbors; // adjacent vertices
        public List<Vertex> Neighbors
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
        public Vertex m_collapse { get; set; } // candidate vertex for collapse

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

        public Vertex(Vector3 v, int _id)
        {
            m_position = v;
            m_iID = _id;

            m_neighbors = new List<Vertex>(3);
            m_adjacentTriangles = new List<Triangle>(3);
        }

        public void AddAdjacentTriangle(Triangle triangle)
        {
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

        public void AddNeighbor(Vertex neighbor)
        {
            if (!HasNeighbor(neighbor))
                m_neighbors.Add(neighbor);
        }

        public void RemoveNeighbor(Vertex neighbor)
        {
            if (HasNeighbor(neighbor))
                m_neighbors.Remove(neighbor);            
        }

        public bool HasNeighbor(Vertex neighbor)
        {
            for (int i = 0; i != m_neighbors.Count; i++)
            {
                if (neighbor == m_neighbors[i])
                    return true;
            }

            return false;
        }

        public void RemoveIfNonNeighbor(Vertex v)
        {
            // removes v from neighbor list if v isn't a neighbor.
            if (!HasNeighbor(v)) 
                return;

            for (int i = 0; i < m_adjacentTriangles.Count; i++)
            {
                if (m_adjacentTriangles[i].HasVertex(v)) 
                    return;
            }

            RemoveNeighbor(v);
        }

        public void Delete()
        {
            if (m_adjacentTriangles.Count > 0)
                throw new System.Exception("Vertex still references one or more adjacent triangles");

            //for each neighbor of this vertex remove it from the neighbors list
            for (int i = 0; i != m_neighbors.Count;i++)
            {
                m_neighbors[i].m_neighbors.Remove(this);
            }
        }
    }
}