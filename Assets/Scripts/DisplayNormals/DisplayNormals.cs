using UnityEngine;

[ExecuteInEditMode]
public class DisplayNormals : MonoBehaviour
{
    public bool m_displayNormals;
    private bool m_prevDisplayNormals;

    //public NormalsGroup m_groupOfNormalsPfb;

    public void Start()
    {
        m_displayNormals = true;
        m_prevDisplayNormals = m_displayNormals;
        Show();
    }

    private void Show()
    {
        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
        Vector3[] meshVertices = mesh.vertices;
        Vector3[] meshNormals = mesh.normals;

        //split normals into groups in order to avoid the 65536 vertex count limit per mesh
        int maxVertexCountPerMesh = 8192; // 2^16/2^3 = 2^13
        int numGroups = meshVertices.Length / maxVertexCountPerMesh + 1;
        int lastGroupVertexCount = meshVertices.Length % maxVertexCountPerMesh;

        float avgEdgeLength = DetermineAverageEdgeLength();
        float normalBoxSize = 0.05f * avgEdgeLength;
        float normalBoxHeight = 8 * normalBoxSize;

        for (int i = 0; i != numGroups; i++)
        {
            int firstNormalIndex = i * maxVertexCountPerMesh;
            int normalsCount = (i == numGroups - 1) ? lastGroupVertexCount : maxVertexCountPerMesh;

            GameObject groupOfNormals = new GameObject();
            groupOfNormals.name = "SubmeshNormals";
            NormalsGroup group = groupOfNormals.AddComponent<NormalsGroup>();
            group.RenderGroupOfNormals(meshVertices, meshNormals, firstNormalIndex, normalsCount, normalBoxSize, normalBoxHeight, Color.red);

            groupOfNormals.transform.parent = this.transform;
            groupOfNormals.transform.localPosition = Vector3.zero;
            groupOfNormals.transform.localRotation = Quaternion.identity;
            groupOfNormals.transform.localScale = Vector3.one;
        }

        float averageEdgeLength = DetermineAverageEdgeLength(); //the average distance between two neighbouring vertices inside the model, this will help up scale the normal
    }

    private void Hide()
    {
        NormalsGroup[] childrenGroups = this.GetComponentsInChildren<NormalsGroup>();
        for (int i = 0; i != childrenGroups.Length; i++)
        {
            DestroyImmediate(childrenGroups[i].gameObject);
        }
    }

    private void ToggleDisplayNormals()
    {
        if (m_displayNormals)
            Show();
        else
            Hide();
    }

    private float DetermineAverageEdgeLength()
    {   
        Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
        Vector3[] meshVertices = mesh.vertices;
        int[] meshTriangles = mesh.triangles;

        float avgSquareEdgeLength = 0; //the average square length of an edge across the whole model
        for (int i = 0; i != meshTriangles.Length; i += 3)
        {
            avgSquareEdgeLength += (meshVertices[meshTriangles[i]] - meshVertices[meshTriangles[i + 1]]).sqrMagnitude;
            avgSquareEdgeLength += (meshVertices[meshTriangles[i + 1]] - meshVertices[meshTriangles[i + 2]]).sqrMagnitude;
            avgSquareEdgeLength += (meshVertices[meshTriangles[i + 2]] - meshVertices[meshTriangles[i]]).sqrMagnitude;
        }

        return Mathf.Sqrt(avgSquareEdgeLength / (float) meshTriangles.Length);
    }

    public void Update()
    {
        if (m_displayNormals != m_prevDisplayNormals)
        {
            m_prevDisplayNormals = m_displayNormals;
            ToggleDisplayNormals();
        }
    }
}
