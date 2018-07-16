using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class gripManager : MonoBehaviour {

	public GameObject grippedObject;
	private GameObject theParent;
	public bool gripping = false;
	public Vector3 gripVec;
	public Quaternion gripRot;

	private Vector3 origionalVec;
	private Quaternion origionalRot;

	private Vector3 startVec;

	private triggerManager tmObj;

	// Use this for initialization
	void Start () {
		tmObj = grippedObject.GetComponent<triggerManager>();
		origionalVec = grippedObject.transform.localPosition;
		origionalRot = grippedObject.transform.localRotation;
		//theParent.transform = grippedObject.transform.parent;
		gripping = false;
	}
	
	// Update is called once per frame

	void Update () {
		if(gripping){
		//get grip vector and rot from the controller
			//gripVec = theParent.transform.localPosition - startVec;
			//gripRot = theParent.transform.localRotation;
			//tmObj.moveToPos = origionalVec + gripVec;
			//Quaternion temp = Quaternion.Inverse(gripRot);
			//temp.y = -temp.y;
			//tmObj.moveToRot = temp;

		}
		else{
			//startVec = theParent.transform.localPosition;
		}
	}

	void startGrip(){
		gripping = true;
		grippedObject.transform.parent = null;

	}
	void endGrip(){
		grippedObject.transform.parent = theParent.transform;
		tmObj.moveToPos = gripVec;
		tmObj.moveToRot = gripRot;
		gripping = false;
	}
}
