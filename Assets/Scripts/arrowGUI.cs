using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class arrowGUI : MonoBehaviour {

    public GameObject objectToPoint;
    public GameObject arrow;
    public bool shouldPoint;
    private bool isInView;
    private bool isInFront;
    private float pheta;
	// Use this for initialization
	void Start () {
        shouldPoint = true;
        isInView = false;
        isInFront = false;
        pheta = 0;
	}
	
	// Update is called once per frame
	void Update () {
        // get object to point in the frame of the camera. 
        Vector3 relativePoint = transform.InverseTransformPoint(objectToPoint.transform.position);
        if(relativePoint.z > 0)
        {
            isInFront = true;
        }
        else
        {
            isInFront = false;
        }
        Vector2 flatPoint = new Vector2(relativePoint.x, relativePoint.y);
        flatPoint.Normalize();
        pheta = Vector2.Angle(new Vector2(1, 0), flatPoint);


        //calculate the angle in the xy plane

        // rotate the arrow about the centre of view accordingly
        //pheta = pheta + (float)0.01;
        float radiusy = (float)0.085;
        float radiusx = (float)0.06;
        arrow.transform.localPosition= new Vector3(radiusx * flatPoint.x,radiusy* flatPoint.y, (float)(0.6));
        if (relativePoint.y > 0)
        {
            pheta += 180;
            arrow.transform.localRotation = Quaternion.Euler(new Vector3(- pheta, 90, 90));
        }
        else
        {
            pheta += 180;
            arrow.transform.localRotation = Quaternion.Euler(new Vector3(+ pheta, 90, 90));
        }
        

    }
}
