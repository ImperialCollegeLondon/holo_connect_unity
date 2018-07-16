using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class triggerManager : MonoBehaviour {
	bool beingPlaced;
	bool gripped;
	Vector3 originalPosition;
	float trackingDist;
	public Vector3 moveToPos;
	public Quaternion moveToRot;
	public Vector3 gripVector;
	public Quaternion gripRot;
	public GameObject sourceObj;
	Vector3 currentPos;
	Quaternion currentRot;
	// Use this for initialization
	void Start () {
		beingPlaced = false;
		originalPosition = this.transform.localPosition;
		moveToPos = this.transform.localPosition;
		moveToRot = this.transform.localRotation;
		trackingDist = 2;
		currentPos = transform.localPosition;
		currentRot = transform.localRotation;

	}
	
	// Update is called once per frame
	void Update () {
		currentPos = transform.localPosition;
		currentRot = transform.localRotation;
		if(beingPlaced){
			var headPosition = Camera.main.transform.position;
	        var gazeDirection = Camera.main.transform.forward;
	        transform.position = headPosition + trackingDist *gazeDirection;
	        moveToPos = this.transform.localPosition;
			moveToRot = this.transform.localRotation;
	    }
	    else{

		    	transform.localPosition =moveToPos;
		    	transform.localRotation =Quaternion.Inverse(moveToRot);
		    
	    }
	}


	void OnTrigger() {
		beingPlaced = true;
		trackingDist = 2;

	}

	void OnPlace(){
		beingPlaced = false;
	}

	void OnRestart(){
		beingPlaced = false;
		transform.position = originalPosition;
	}
	void OnSelect(){
		var headPosition = Camera.main.transform.position;
		beingPlaced = !beingPlaced;
		trackingDist =(transform.position - headPosition).magnitude;
	}
	void MoveMe(object[] move){
		// transform.localPosition = (Vector3)move[0];
		// transform.localRotation = (Quaternion)(move[1]);
	}
}
