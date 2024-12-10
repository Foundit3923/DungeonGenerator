using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

public class CubeProperties : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    //public Vector3 dimensions;

    public bool isHallway;
    public int roomId;
    public int roomCollectionId;
    public Vector2Int position;
    public Vector2Int max;
    public Vector2Int min;
    public Vector2Int amax;
    public Vector2Int amin;
    public float xAdjust;
    public float zAdjust;
    public GameObject attachementPoint;
    public string type;
    void Start()
    {
        //dimensions = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
