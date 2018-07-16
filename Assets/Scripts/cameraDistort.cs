using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraDistort : MonoBehaviour {

    Mesh deformingMesh;
    Vector3[] originalVertices, displacedVertices;
    public float k1 = 0;
    public float k2 = 0;
    public float k3 = 0;
    public float fx = 50;
    public float camVertRes = 480;
    public Camera rearCam;
    Vector3 min;
    Vector3 max ;
    Vector3 mid ;
    float rScale;

    void Start()
    {
        deformingMesh = GetComponent<MeshFilter>().mesh;
        min  = deformingMesh.bounds.min;
        max = deformingMesh.bounds.max;
        mid = deformingMesh.bounds.center;
        rScale = (mid - max).magnitude;
        rScale = (1.0f + k1 * rScale * rScale + k2 * rScale * rScale * rScale * rScale + k3 * rScale * rScale * rScale * rScale * rScale * rScale);
        
        originalVertices = deformingMesh.vertices;
        displacedVertices = new Vector3[originalVertices.Length];
        for (int i = 0; i < originalVertices.Length; i++)
        {
            float  r= (mid - originalVertices[i]).magnitude;
            displacedVertices[i] = originalVertices[i]*(1.0f+ k1*r*r+ k2*r*r*r*r + k3*r*r*r*r*r*r);
        }
        deformingMesh.vertices = displacedVertices;
        deformingMesh.RecalculateBounds();
        deformingMesh.RecalculateNormals();
    }

    void Update()
    {
        float FOV = 360 * Mathf.Atan(camVertRes / (2 * fx))/Mathf.PI;
        rearCam.fieldOfView = FOV;
        deformingMesh = GetComponent<MeshFilter>().mesh;
        displacedVertices = new Vector3[originalVertices.Length];
        rScale = (mid - max).magnitude;
        rScale =1/ (1.0f + k1 * rScale * rScale + k2 * rScale * rScale * rScale * rScale + k3 * rScale * rScale * rScale * rScale * rScale * rScale);
        for (int i = 0; i < originalVertices.Length; i++)
        {
            float r = (mid - originalVertices[i]).magnitude;
            displacedVertices[i] = originalVertices[i]  *((1.0f + k1 * r * r + k2 * r * r * r * r + k3 * r * r * r * r * r * r));
        }
        deformingMesh.vertices = displacedVertices;
        deformingMesh.RecalculateBounds();
        deformingMesh.RecalculateNormals();
    }
}
