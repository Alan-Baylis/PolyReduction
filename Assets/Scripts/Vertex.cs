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

            set
            {
                m_iID = value;
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

        //properties of the vertex
        public Color m_color { get; set; }
        public Vector2 m_uv { get; set; }
        //public Vector3 m_normal { get; set; }
        //private Vector3 m_accumulationNormal; //the normal obtained through accumulation of normals from adjacent triangles during polygon reduction process

        public Vertex()
        {

        }

        public Vertex(Vector3 v, int _id)
        {
            m_position = v;
            m_iID = _id;
            
            m_adjacentTriangles = new List<Triangle>(3);
        }

        public Vertex(Vertex other)
        {
            m_position = other.m_position;
            m_iID = other.m_iID;

            m_color = other.m_color;
            m_uv = other.m_uv;
            //m_normal = other.m_normal;
            //m_accumulationNormal = other.m_accumulationNormal;
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
            return m_adjacentTriangles.Contains(triangle);
        }

        /**
        * Add a triangle normal to this vertex m_accumulationNormal variable
        **/
        //public void AccumulateNormalFromTriangle(Vector3 triangleNormal)
        //{
        //    m_accumulationNormal += triangleNormal;
        //}

        /**
        * Return the normalized normal at this vertex after accumulating normals from all adjacent triangles
        **/
        //public Vector3 GetAccumulationNormal()
        //{
        //    m_accumulationNormal.Normalize();
        //    return m_accumulationNormal;
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
        * Vertex can either collapse on another vertex or on itself (i.e it did not find another vertex to collapse on)
        * In the first case, copy adjacent triangles to new vertex
        * In the second case, simply update the position of the vertex and move it to the new wedge vertex list
        **/
        public void CollapseOnWedgeVertex(Vertex collapseVertex)
        {
            //move the vertex
            this.m_position = collapseVertex.m_position;

            //copy adjacent triangles to collapseVertex
            for (int i = 0; i != m_adjacentTriangles.Count; i++)
            {
                Triangle triangle = m_adjacentTriangles[i];

                triangle.ReplaceVertex(this, collapseVertex);
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




    /**
    * Use this class to keep information on a vertex that collapsed on another
    **/
    public struct CollapsedVertex
    {
        public int m_initialIndex;
        public int m_collapsedIndex;

        public CollapsedVertex(int initialIndex, int collapsedIndex)
        {
            m_initialIndex = initialIndex;
            m_collapsedIndex = collapsedIndex;
        }

        public override string ToString()
        {
            return "vertex " + m_initialIndex + " collapsed on vertex " + m_collapsedIndex;
        }
    }

    public struct DisplacedVertex
    {
        public int m_index;
        public Vector3 m_targetPosition; //the position where to displace this vertex

        public override int GetHashCode()
        {
            return m_index;
        }
    }
}