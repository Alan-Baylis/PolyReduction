using UnityEngine;

namespace PolyReduction
{
    public class Triangle
    {
        private Vertex[] m_vertices; // the 3 points that make this tri
        public Vertex[] Vertices
        {
            get
            {
                return m_vertices;
            }
        }

        public Vector3 m_normal { get; set; } // orthogonal unit vector

        public Triangle(Vertex v0, Vertex v1, Vertex v2)
        {
            m_vertices = new Vertex[3];
            m_vertices[0] = v0;
            m_vertices[1] = v1;
            m_vertices[2] = v2;

            ComputeNormal();
        }

        //public Triangle(Triangle other)
        //{
        //    m_vertices = new Vertex[3];
        //    m_vertices[0] = new Vertex(other.m_vertices[0]);
        //    m_vertices[1] = new Vertex(other.m_vertices[1]);
        //    m_vertices[2] = new Vertex(other.m_vertices[2]);

        //    m_normal = other.m_normal;
        //}

        public void ComputeNormal()
        {
            Vector3 v0 = m_vertices[0].m_position;
            Vector3 v1 = m_vertices[1].m_position;
            Vector3 v2 = m_vertices[2].m_position;

            Vector3 u = v1 - v0;
            Vector3 v = v2 - v0;

            Vector3 crossProduct = new Vector3(u.y * v.z - u.z * v.y,
                                               u.z * v.x - u.x * v.z,
                                               u.x * v.y - u.y * v.x);

            crossProduct.Normalize();

            m_normal = crossProduct;
        }

        public void ReplaceVertex(Vertex vOld, Vertex vNew)
        {
            for (int i = 0; i != 3; i++)
            {
                if (vOld == m_vertices[i])
                    m_vertices[i] = vNew;
            }

            vNew.AddAdjacentTriangle(this);
            ComputeNormal();

            //vOld.RemoveAdjacentTriangle(this);
            //vNew.AddAdjacentTriangle(this);
            //for (int i = 0; i < 3; i++)
            //{
            //    vOld.RemoveIfNonNeighbor(m_vertices[i]);
            //    m_vertices[i].RemoveIfNonNeighbor(vOld);
            //}
            //for (int i = 0; i < 3; i++)
            //{
            //    if (m_vertices[i].HasAdjacentTriangle(this))
            //    {
            //        for (int j = 0; j < 3; j++)
            //        {
            //            if (i != j)
            //                m_vertices[i].AddNeighbor(m_vertices[j]);
            //        }
            //    }
            //}

            //ComputeNormal();
        }

        public bool HasVertex(Vertex v)
        {
            return v == m_vertices[0] || v == m_vertices[1] || v == m_vertices[2];
        }

        public void Delete()
        {
            for (int i = 0; i < 3; i++)
            {
                Vertex vertex = m_vertices[i];
                if (vertex != null)
                    vertex.RemoveAdjacentTriangle(this);
            }

            //for (int i = 0; i < 3; i++)
            //{
            //    int i2 = (i + 1) % 3;
            //    if (m_vertices[i] == null || m_vertices[i2] == null)
            //        continue;

            //    //m_vertices[i].RemoveIfNonNeighbor(m_vertices[i2]);
            //    //m_vertices[i2].RemoveIfNonNeighbor(m_vertices[i]);
            //    m_vertices[i].RemoveNeighbor(m_vertices[i2]);
            //    m_vertices[i2].RemoveNeighbor(m_vertices[i]);
            //}
        }
    }
}
