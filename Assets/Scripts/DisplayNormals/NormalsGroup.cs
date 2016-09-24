using UnityEngine;

/**
* Holds a maximum of 8192 normals objects due to the 65536 mesh vertex count limi
**/
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class NormalsGroup : MonoBehaviour
{
    /**
    * Render the representations of all vertex normals of a submesh of the original mesh in one single mesh
    **/
    public void RenderGroupOfNormals(Vector3[] vertices, Vector3[] normals, int firstNormalIndex, int normalsCount, float boxSize, float boxHeight, Color color)
    {
        Vector3[] groupVertices = new Vector3[8 * normalsCount]; //one box (8 vertices) per vertex
        int[] groupTriangles = new int[36 * normalsCount];
        Color[] groupColors = new Color[8 * normalsCount];
        Vector3[] groupNormals = new Vector3[8 * normalsCount];

        for (int i = firstNormalIndex; i != firstNormalIndex + normalsCount; i++)
        {
            NormalMesh boxMesh = new NormalMesh();
            boxMesh.vertices = groupVertices;
            boxMesh.firstVertexIndex = 8 * i;
            boxMesh.triangles = groupTriangles;
            boxMesh.firstTriangleIndex = 36 * i;
            boxMesh.colors = groupColors;
            boxMesh.firstColorIndex = 8 * i;
            boxMesh.normals = groupNormals;
            boxMesh.firstNormalIndex = 8 * i;

            //build the actual box
            BuildBoxAtVertex(boxMesh, vertices[i], normals[i], boxSize, boxHeight, color);
        }

        Mesh groupMesh = new Mesh();
        groupMesh.name = "GroupOfNormals";
        groupMesh.vertices = groupVertices;
        groupMesh.triangles = groupTriangles;
        groupMesh.colors = groupColors;
        groupMesh.normals = groupNormals;

        groupMesh.RecalculateBounds();

        Debug.Log("groupTrianglesCount:" + groupMesh.triangles.Length);

        this.GetComponent<MeshFilter>().sharedMesh = groupMesh;
    }

    /**
    * Light struct that holds references to the parent group mesh
    **/
    public struct NormalMesh
    {
        public Vector3[] vertices;
        public int firstVertexIndex;
        public int[] triangles;
        public int firstTriangleIndex;
        public Color[] colors;
        public int firstColorIndex;
        public Vector3[] normals;
        public int firstNormalIndex;

    }

    public void BuildBoxAtVertex(NormalMesh boxMesh, Vector3 vertexPosition, Vector3 vertexNormal, float boxSize, float boxHeight, Color color)
    {
        Quaternion boxRotation = Quaternion.FromToRotation(Vector3.up, vertexNormal);

        //vertices
        boxMesh.vertices[boxMesh.firstVertexIndex] = boxRotation * (boxSize * new Vector3(-0.5f, 0, 0.5f)) + vertexPosition;
        boxMesh.vertices[boxMesh.firstVertexIndex + 1] = boxRotation * (boxSize * new Vector3(-0.5f, 0, -0.5f)) + vertexPosition;
        boxMesh.vertices[boxMesh.firstVertexIndex + 2] = boxRotation * (boxSize * new Vector3(0.5f, 0, -0.5f)) + vertexPosition;
        boxMesh.vertices[boxMesh.firstVertexIndex + 3] = boxRotation * (boxSize * new Vector3(0.5f, 0, 0.5f)) + vertexPosition;
        boxMesh.vertices[boxMesh.firstVertexIndex + 4] = boxRotation * new Vector3(-0.5f * boxSize, boxHeight, 0.5f * boxSize) + vertexPosition;
        boxMesh.vertices[boxMesh.firstVertexIndex + 5] = boxRotation * new Vector3(-0.5f * boxSize, boxHeight, -0.5f * boxSize) + vertexPosition;
        boxMesh.vertices[boxMesh.firstVertexIndex + 6] = boxRotation * new Vector3(0.5f * boxSize, boxHeight, -0.5f * boxSize) + vertexPosition;
        boxMesh.vertices[boxMesh.firstVertexIndex + 7] = boxRotation * new Vector3(0.5f * boxSize, boxHeight, 0.5f * boxSize) + vertexPosition;

        //triangles
        int[] triangles = new int[]
        {
            0,3,1,
            1,3,2,
            0,5,4,
            0,1,5,
            1,6,5,
            1,2,6,
            2,7,6,
            2,3,7,
            3,4,7,
            3,0,4,
            4,6,7,
            4,5,6
        };

        for (int i = 0; i != 36; i++)
        {
            boxMesh.triangles[i + boxMesh.firstTriangleIndex] = triangles[i] + boxMesh.firstVertexIndex;
        }

        //colors
        for (int i = boxMesh.firstColorIndex; i != boxMesh.firstColorIndex + 8; i++)
        {
            boxMesh.colors[i] = color;
        }
        
        //box mesh
        boxMesh.normals[boxMesh.firstNormalIndex] = 1 / 3.0f * new Vector3(-1, -1, 1);
        boxMesh.normals[boxMesh.firstNormalIndex + 1] = 1 / 3.0f * new Vector3(-1, -1, -1);
        boxMesh.normals[boxMesh.firstNormalIndex + 2] = 1 / 3.0f * new Vector3(1, -1, -1);
        boxMesh.normals[boxMesh.firstNormalIndex + 3] = 1 / 3.0f * new Vector3(1, -1, 1);
        boxMesh.normals[boxMesh.firstNormalIndex + 4] = 1 / 3.0f * new Vector3(-1, 1, 1);
        boxMesh.normals[boxMesh.firstNormalIndex + 5] = 1 / 3.0f * new Vector3(-1, 1, -1);
        boxMesh.normals[boxMesh.firstNormalIndex + 6] = 1 / 3.0f * new Vector3(1, 1, -1);
        boxMesh.normals[boxMesh.firstNormalIndex + 7] = 1 / 3.0f * new Vector3(1, 1, 1);
    }
}
