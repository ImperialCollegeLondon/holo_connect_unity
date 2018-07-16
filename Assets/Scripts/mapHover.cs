using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//This script is to make a plane float at a set hieght and distance from the user, in a similar way to tag along, though less agressive. Used for aligned maps, for user direction. 

public class mapHover : MonoBehaviour {

    public float hoverHeight = 1;
    public float hoverDistance = 1;

    public GameObject userCamera;
    public GameObject plane;
    public float filter;
    private Vector3 filtered;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        // get angle from head to plane. 
        var forward = userCamera.transform.forward;
        forward.y = 0;
        forward.Normalize();

        var position = userCamera.transform.position + hoverDistance * forward;
        position.y = hoverHeight;
        filtered = filter * position + (1 - filter) * filtered;
        plane.transform.SetPositionAndRotation(filtered, plane.transform.rotation);

        //if (angleBetween < allowableAngle)
        //{
        //    Debug.Log(angleBetween);
        //    var position = userCamera.transform.position + hoverDistance * Mathf.Cos(allowableAngle*3.14f/180f) * forward + hoverDistance * Mathf.Sin(allowableAngle * 3.14f / 180f) * side;
        //    position.y = -hoverHeight;
        //    plane.transform.SetPositionAndRotation(position, plane.transform.rotation);
        //}
        //else if (angleBetween < -allowableAngle)
        //{
        //    Debug.Log(angleBetween);
        //    var position = userCamera.transform.position + hoverDistance * Mathf.Cos(allowableAngle * 3.14f / 180f) * forward - hoverDistance * Mathf.Sin(allowableAngle * 3.14f / 180f) * side;
        //    position.y = -hoverHeight;
        //    plane.transform.SetPositionAndRotation(position, plane.transform.rotation);
        //}

        //else
        //{
        //    //var position = (1 / (targetDir.magnitude- hoverDistance)) * targetDir + userCamera.transform.position;
        //    //plane.transform.SetPositionAndRotation(position, plane.transform.rotation);
        //}

        //if angle is more than allowed set it to min/max. Also put it at correct distance.

    }
}
