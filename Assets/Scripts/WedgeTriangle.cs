//using UnityEngine;

//namespace PolyReduction
//{
//    public class WedgeTriangle
//    {
//        private Wedge[] m_wedges;// the 3 wedges that make this triangle
//        public Vector3 m_normal { get; set; } // orthogonal unit vector

//        public WedgeTriangle(Wedge w0, Wedge w1, Wedge w2)
//        {
//            m_wedges = new Wedge[3];
//            m_wedges[0] = w0;
//            m_wedges[1] = w1;
//            m_wedges[2] = w2;

//            ComputeNormal();
//        }
//        public void ComputeNormal()
//        {
//            Vector3 v0 = m_wedges[0].m_position;
//            Vector3 v1 = m_wedges[1].m_position;
//            Vector3 v2 = m_wedges[2].m_position;

//            Vector3 u = v1 - v0;
//            Vector3 v = v2 - v0;

//            Vector3 crossProduct = new Vector3(u.y * v.z - u.z * v.y,
//                                               u.z * v.x - u.x * v.z,
//                                               u.x * v.y - u.y * v.x);

//            crossProduct.Normalize();

//            m_normal = crossProduct;
//        }

//        //public void ReplaceWedge(Wedge vOld, Wedge vNew)
//        //{
//        //    for (int i = 0; i != 3; i++)
//        //    {
//        //        if (vOld == m_vertices[i])
//        //            m_vertices[i] = vNew;
//        //    }

//        //    vOld.RemoveAdjacentTriangle(this);
//        //    vNew.AddAdjacentTriangle(this);
//        //    for (int i = 0; i < 3; i++)
//        //    {
//        //        vOld.RemoveIfNonNeighbor(m_vertices[i]);
//        //        m_vertices[i].RemoveIfNonNeighbor(vOld);
//        //    }
//        //    for (int i = 0; i < 3; i++)
//        //    {
//        //        if (m_vertices[i].HasAdjacentTriangle(this))
//        //        {
//        //            for (int j = 0; j < 3; j++)
//        //            {
//        //                if (i != j)
//        //                    m_vertices[i].AddNeighbor(m_vertices[j]);
//        //            }
//        //        }
//        //    }

//        //    ComputeNormal();
//        //}

//        public bool HasWedge(Wedge w)
//        {
//            return w == m_wedges[0] || w == m_wedges[1] || w == m_wedges[2];
//        }

//        //public void Delete()
//        //{
//        //    for (int i = 0; i < 3; i++)
//        //    {
//        //        if (m_vertices[i] != null)
//        //            m_vertices[i].AdjacentTriangles.Remove(this);
//        //    }

//        //    for (int i = 0; i < 3; i++)
//        //    {
//        //        int i2 = (i + 1) % 3;
//        //        if (m_vertices[i] == null || m_vertices[i2] == null)
//        //            continue;

//        //        m_vertices[i].RemoveIfNonNeighbor(m_vertices[i2]);
//        //        m_vertices[i2].RemoveIfNonNeighbor(m_vertices[i]);
//        //    }
//        //}
//    }
//}
