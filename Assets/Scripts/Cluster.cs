using UnityEngine;
using System.Collections.Generic;

namespace PolyReduction
{
    /**
    * A cluster holds one or more vertices that share the same 3D position but not necessarily the same triangle index, UV coordinates or normal
    **/
    public class Cluster
    {
        private Vector3 m_position;
        private List<Vertex> m_clusteredVertices; //vertices that share the same 3D position

        private List<Cluster> m_sharedNeighbors; //adjacent vertices to every vertex in this cluster
        public List<Cluster> SharedNeighbors
        {
            get
            {
                return m_sharedNeighbors;
            }
        }

        private List<Triangle> m_sharedAdjacentTriangles; //adjacent triangles
        public List<Triangle> SharedAdjacentTriangles
        {
            get
            {
                return m_sharedAdjacentTriangles;
            }
        }

        public void Init(List<Vertex> vertices)
        {
            m_clusteredVertices = vertices;

        }
    }
}
