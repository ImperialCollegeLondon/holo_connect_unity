using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class twistArrowControler : MonoBehaviour {


    public float linear = 0;
    public float angular = 0;
    public float linScale = 1;
    public float angScale = 1;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        float magnitude =Mathf.Sqrt( Mathf.Pow(linear * linScale, 2) + Mathf.Pow(angular * angScale, 2));
        float angle = Mathf.Atan2(angular * angScale, linear * linScale);
        transform.localScale = new Vector3(magnitude,1,1);
        transform.localRotation = Quaternion.EulerAngles(0,angle,0);

    }
}
