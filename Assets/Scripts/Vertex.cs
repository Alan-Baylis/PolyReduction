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


        //private List<Vertex> m_neighbors; // adjacent vertices
        //public List<Vertex> Neighbors
        //{
        //    get
        //    {
        //        return m_neighbors;
        //    }
        //}

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

        //public Wedge m_parentWedge { get; set; }

        //public override bool Equals(object obj)
        //{
        //    if (!(obj is Vertex))
        //        return false;

        //    Vertex other = (Vertex)obj;
        //    return m_iID == other.m_iID;
        //}

        //public override int GetHashCode() { return Mathf.RoundToInt(m_position.x) ^ Mathf.RoundToInt(m_position.y) ^ Mathf.RoundToInt(m_position.z); }

        public Vertex(Vector3 v, int _id)
        {
            m_position = v;
            m_iID = _id;

            //m_neighbors = new List<Vertex>(3);
            m_adjacentTriangles = new List<Triangle>(3);
        }

        //public Vertex(Vertex other)
        //{
        //    m_position = other.m_position;
        //    m_iID = other.m_iID;

        //    //m_adjacentTriangles = new List<Triangle>(other.m_adjacentTriangles.Count);
        //    //for (int i = 0; i != other.m_adjacentTriangles.Count; i++)
        //    //{
        //    //    m_adjacentTriangles.Add(new Triangle(other.m_adjacentTriangles[i]));
        //    //}
        //}

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
            return m_adjacentTriangles.Contains(triangle);
        }

        //public void AddNeighbor(Vertex neighbor)
        //{
        //    if (!HasNeighbor(neighbor))
        //        m_neighbors.Add(neighbor);
        //}

        //public void RemoveNeighbor(Vertex neighbor)
        //{
        //    if (HasNeighbor(neighbor))
        //        m_neighbors.Remove(neighbor);            
        //}

        //public bool HasNeighbor(Vertex neighbor)
        //{
        //    for (int i = 0; i != m_neighbors.Count; i++)
        //    {
        //        if (neighbor == m_neighbors[i])
        //            return true;
        //    }

        //    return false;
        //}

        /**
         * Removes v from neighbor list if v isn't a neighbor.
         * **/
        //public void RemoveIfNonNeighbor(Vertex v)
        //{
        //    if (!HasNeighbor(v)) 
        //        return;

        //    for (int i = 0; i < m_adjacentTriangles.Count; i++)
        //    {
        //        if (m_adjacentTriangles[i].HasVertex(v)) 
        //            return;
        //    }

        //    RemoveNeighbor(v);
        //}

        /**
        * Do this vertex and another one share one or more adjacent triangles
        */
        public List<Triangle> GetSharedTriangles(Vertex vertex)
        {
            List<Triangle> sharedTriangles = new List<Triangle>();
            for (int i = 0; i < m_adjacentTriangles.Count; i++)
            {
                if (m_adjacentTriangles[i].HasVertex(vertex))
                    sharedTriangles.Add(m_adjacentTriangles[i]);
            }

            return sharedTriangles;
        }

        public bool ShareTriangleWithVertex(Vertex vertex)
        {
            for (int i = 0; i < m_adjacentTriangles.Count; i++)
            {
                if (m_adjacentTriangles[i].HasVertex(vertex))
                    return true;
            }

            return false;
        }

        /**
        * Tell if this vertex can collapse on one of the vertices of the parameter 'wedge'
        * Return the Vertex on which this vertex can collapse on
        **/
        public Vertex FindVertexToCollapseOn(Wedge wedge)
        {
            if (wedge == null)
                return null;

            for (int i = 0; i != wedge.Vertices.Count; i++)
            {
                if (wedge.Vertices[i].ShareTriangleWithVertex(this))
                    return wedge.Vertices[i];
            }

            return null;
        }

        /**
        * Call this to perform the action of vertex collapsing on a wedge
        * Vertex can either collapse on another vertex or on nothing
        * In the first case, copy adjacent triangles to new vertex
        * In the second case, simply update the position of the vertex and move it to the new wedge vertex list
        **/
        public void CollapseOnWedge(Wedge collapseWedge)
        {
            Vertex collapseVertex = FindVertexToCollapseOn(collapseWedge); //the vertex it will collapse on if it exists

            if (collapseVertex != null)
            {
                //move the vertex
                this.m_position = collapseWedge.m_position;

                //copy adjacent triangles to collapseVertex and recompute normal
                for (int i = 0; i != m_adjacentTriangles.Count; i++)
                {
                    Triangle triangle = m_adjacentTriangles[i];

                    triangle.ReplaceVertex(this, collapseVertex);                    
                }                
            }
            else
            {
                //m_position = collapseWedge.m_position;
                //collapseWedge.AddVertex(this);
            }
        }

        public void Delete()
        {
            if (m_adjacentTriangles.Count > 0)
                throw new System.Exception("Vertex still references one or more adjacent triangles");

            //for each neighbor of this vertex remove it from the neighbors list
            //for (int i = 0; i != m_neighbors.Count;i++)
            //{
            //    m_neighbors[i].m_neighbors.Remove(this);
            //}
        }
    }
}