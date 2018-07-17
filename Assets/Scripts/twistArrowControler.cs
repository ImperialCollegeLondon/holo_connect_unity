﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class twistArrowControler : MonoBehaviour {

    [HideInInspector]
    public float linear = 0, angular = 0, linScale = 1, angScale = 1;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {

        float magnitude =Mathf.Sqrt( Mathf.Pow(linear * linScale, 2) + Mathf.Pow(angular * angScale, 2));
        float angle = Mathf.Atan2(angular * angScale, linear * linScale);
        transform.localScale = new Vector3(magnitude,1,1);
        transform.localRotation = Quaternion.Euler(0,angle,0);

    }
}
