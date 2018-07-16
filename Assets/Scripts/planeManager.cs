using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class planeManager : MonoBehaviour {
    public float A = 0,B = 1,C = 0,D = 0;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        Vector3 normal = new Vector3(A, B, C);
        float Distance = -D / normal.magnitude;
        normal.Normalize();
        Plane ourPlane = new Plane(normal,100000000*Distance);
        this.transform.localPosition =1f* Distance * normal;
        
        this.transform.localRotation = Quaternion.FromToRotation(new Vector3(0, 1, 0), normal); ;

        
	}
}
