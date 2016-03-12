using UnityEngine;
using PolyReduction;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
    public GameObject m_rabbitPfb;

    public void Start()
    {
        //TestAlgorithmOnFileData();
    }

    /**
    * Test algorithm on data contained in a .cs file (rabbit)
    **/
    private void TestAlgorithmOnFileData()
    {
        //Test the algorithm on the rabbit data
        Debug.Log("Start");

        GameObject rabbitObject = (GameObject)Instantiate(m_rabbitPfb);
        MeshFilter meshFilter = rabbitObject.GetComponent<MeshFilter>();

        PolyReducer polyReducer = rabbitObject.GetComponent<PolyReducer>();
        polyReducer.PrepareModel();

        List<Vector3> cutVertices;
        List<int> cutTriangles;
        polyReducer.CutMeshPolygons(out cutVertices, out cutTriangles, 453);
    }

    /**
    * Test algorithm on 3ds max exported simple model
    **/
    private void TestAlgorithmOn3DSimpleExportedModel()
    {

    }

    /**
    * Test algorithm on 3ds max exported model with more than 2 submeshes
    **/
    private void TestAlgorithmOn3DComplexExportedModel()
    {

    }
}