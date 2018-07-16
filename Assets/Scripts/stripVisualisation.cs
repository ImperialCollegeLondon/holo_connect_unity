using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class stripVisualisation : MonoBehaviour {

    public Vector3 position = new Vector3(0,0,0);
    public Quaternion rotation = new Quaternion(0,0,0,1);
    public float midLength = 1.0f;
    public float startLength = 0.2f;
    public float endLength= 0.2f;
    public float height = 0.15f;
    public float speed = 0.2f;
    public float stripW = 0.02f;
    public float stripSep = 0.1f;
    public Material stripMat;

    private GameObject startZone;
    private GameObject endZone;
    private GameObject midZone;
    private GameObject strip;
    private float accumilatedTime;

    private int numStrips=0;
    int neededStrips;
    private GameObject[] stripArr;

    // Use this for initialization
    void Start () {
        startZone = this.transform.Find("startZone").gameObject;
        endZone = this.transform.Find("endZone").gameObject;
        midZone = this.transform.Find("midZone").gameObject;
        strip = this.transform.Find("strip").gameObject;
        stripArr = new GameObject[1];
        

    }
	
	// Update is called once per frame
	void Update () {
        startZone.transform.localScale= new Vector3(startLength, 0.01f, height);
        endZone.transform.localScale = new Vector3(endLength, 0.01f, height);
        midZone.transform.localScale = new Vector3(midLength, 0.01f, height);
        strip.transform.localScale = new Vector3(stripW, 0.03f, height);
        endZone.transform.localPosition = new Vector3(0.5f * (midLength + endLength), 0, 0);
        startZone.transform.localPosition = new Vector3(-0.5f * (midLength + startLength), 0, 0);
        strip.transform.localPosition = new Vector3(0, 0, 0);
        strip.SetActive(false);

        neededStrips = (int)((midLength + startLength + endLength) / stripSep);
        // there needs to be more strips

        if (numStrips != neededStrips)
        {
            for (int i = 0; i < stripArr.Length; i++)
            {
                Destroy(stripArr[i]);
            }
                stripArr = new GameObject[neededStrips+1];
            for (int i = 0; i <= neededStrips; i++)
            {
                GameObject newStrip = Instantiate(strip,this.transform,false) as GameObject;
                newStrip.SetActive(true);
                MeshRenderer gameObjectRenderer = newStrip.GetComponent<MeshRenderer>();
                Material newMaterial = new Material(Shader.Find("Unlit/unlitTransparentScalable"));
                newMaterial.color = new Color(0.3f,0.1f,0.7f,1.0f);
                gameObjectRenderer.material = newMaterial;
                stripArr[i] = newStrip;

            }
            numStrips = neededStrips;
        }
        accumilatedTime += Time.deltaTime;

            for (int i = 0; i < stripArr.Length; i++)
            {
            stripArr[i].SetActive(true);
            float distance = accumilatedTime * speed + i * stripSep;
            distance = distance - (int)(distance / (midLength + startLength + endLength)) * (midLength + startLength + endLength);
                 stripArr[i].transform.localPosition = new Vector3(-0.5f*midLength - startLength +distance,0.021f, 0);
            if (stripArr[i].transform.localPosition.x > 0.5 * midLength + endLength)
            {
                Debug.Log("overflow");
                stripArr[i].transform.localPosition.Set(stripArr[i].transform.localPosition.x - (midLength + startLength + endLength), 0, 0);
            }
            MeshRenderer gameObjectRenderer = stripArr[i].GetComponent<MeshRenderer>();
            gameObjectRenderer.material.SetFloat("_Transparency", (1.0f) * 0.75f);
            if (stripArr[i].transform.localPosition.x < 0.5 * midLength && stripArr[i].transform.localPosition.x > 0.5 * midLength)
            {
                gameObjectRenderer.material.SetFloat("_Transparency",( 1.0f) * 0.75f);
            }
            if( stripArr[i].transform.localPosition.x > 0.5 * midLength){
                float value = (stripArr[i].transform.localPosition.x - 0.5f * midLength) / endLength;
                value = (1 - value) * 0.75f;
                gameObjectRenderer.material.SetFloat("_Transparency",value);
                if(value < 0.25) stripArr[i].SetActive(false);
            }
            if (stripArr[i].transform.localPosition.x < -0.5 * midLength)
            {
                float value = (stripArr[i].transform.localPosition.x + 0.5f * midLength) / startLength;
                value = (value + 1) * 0.75f;
                gameObjectRenderer.material.SetFloat("_Transparency",  value);
                if (value < 0.25) stripArr[i].SetActive(false);
            }
        }
    }
}
