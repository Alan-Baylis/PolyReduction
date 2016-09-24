using UnityEngine;

public class ModelData
{
    private Vector3[] m_verts;
    public Vector3[] Verts
    {
        get
        {
            return m_verts;
        }
    }

    private int[] m_tris;
    public int[] Tris
    {
        get
        {
            return m_tris;
        }
    }

    private Color[] m_colors;
    public Color[] Colors
    {
        get
        {
            return m_colors;
        }

        set
        {
            m_colors = value;
        }
    }

    private Vector2[] m_UVs;
    public Vector2[] UVs
    {
        get
        {
            return m_UVs;
        }

        set
        {
            m_UVs = value;
        }
    }

    private Vector3[] m_normals;
    public Vector3[] Normals
    {
        get
        {
            return m_normals;
        }

        set
        {
            m_normals = value;
        }
    }

    public ModelData(Vector3[] verts, int[] tris)
    {
        m_verts = verts;
        m_tris = tris;
        m_colors = null;
        m_UVs = null;
        m_normals = null;
    }
}
