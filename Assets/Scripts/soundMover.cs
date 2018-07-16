using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class soundMover : MonoBehaviour {

    public float x = 0;
    public float y = 0;
    public int width = 300;
    public float intensity = 0;
    public GameObject plane;
    AudioSource obsSound;
	// Use this for initialization
	void Start () {
        obsSound = GetComponent<AudioSource>();
	}
	
	// Update is called once per frame
	void Update () {
        obsSound.volume = intensity;
        float scale = plane.transform.localScale.x;
        float xF = x / width;
        float yF = y / width;
        xF = (xF-0.5f) * 10f * scale/0.6f;
        yF = (yF - 0.5f) * 10f * scale/0.6f;
        obsSound.transform.localPosition = new Vector3(-xF, 0, yF);
	}
}
