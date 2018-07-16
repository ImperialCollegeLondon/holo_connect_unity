using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class cubeController : MonoBehaviour {
    public GameObject Cube0;
    public GameObject Cube1;
    public GameObject Cube2;
    public GameObject Cube3;
    public GameObject Cube4;
    public GameObject Cube5;
    public GameObject Cube6;
    public GameObject Cube7;
    public GameObject Cube8;
    public GameObject Cube9;
    public GameObject GOwithArrow;
    public GameObject arrow;
    public GameObject map;
    public GameObject floorBlocker;
    public int currentCube = 0;
    public int totalCubes = 10;
    public int helpType;
    public int numHelpTypes;
    private arrowGUI arrowGUIScript;
    GameObject getCube(int num)
    {
        if (num == 0) return Cube0;
        if (num == 1) return Cube1;
        if (num == 2) return Cube2;
        if (num == 3) return Cube3;
        if (num == 4) return Cube4;
        if (num == 5) return Cube5;
        if (num == 6) return Cube6;
        if (num == 7) return Cube7;
        if (num == 8) return Cube8;
        if (num == 9) return Cube9;
        return Cube0;
    }
	// Use this for initialization
	void Start () {
		for(int i = 0; i < totalCubes; i++)
        {
            GameObject thisCube = getCube(i);
            thisCube.SetActive(false);
        }
        arrowGUIScript = GOwithArrow.GetComponent<arrowGUI>();
        arrow.SetActive(false);

	}

    void all()
    {
        for (int i = 0; i < totalCubes; i++)
        {
            GameObject thisCube = getCube(i);
            thisCube.SetActive(true);
        }
    }
	
    void single()
    {
        for (int i = 0; i < totalCubes; i++)
        {
            GameObject thisCube = getCube(i);
            if(i == currentCube)
            {
                thisCube.SetActive(true);
            }
            else
            {
                thisCube.SetActive(false);
            }
            
        }
        arrow.SetActive(true);
    }
    void increment()
    {
        currentCube++;
        currentCube = currentCube % totalCubes;
        arrowGUIScript.objectToPoint = getCube(currentCube);
        single();
        setHelp(helpType);
    }

    void setHelp(int help)
    {
        var cube = getCube(currentCube);
        var sound = cube.GetComponent<AudioSource>();
        if (help == 0)
        {
            //make sound      
            BroadcastMessage("speak", "Stereo audio");
            sound.mute = false;
            arrow.SetActive(false);
            map.SetActive(false);
            floorBlocker.SetActive(true);
        }
        if (help == 1)
        {
            //make arrow
            BroadcastMessage("speak", "Heads up arrow");
            sound.mute = true;
            arrow.SetActive(true);
            map.SetActive(false);
            floorBlocker.SetActive(true);
        }
        if (help == 2)
        {
            //make map
            BroadcastMessage("speak", "floating map");
            sound.mute = true;
            arrow.SetActive(false);
            map.SetActive(true);
            floorBlocker.SetActive(true);
        }
        if (help == 3)
        {
            //floor markings
            BroadcastMessage("speak", "floor markings");
            sound.mute = true;
            arrow.SetActive(false);
            map.SetActive(false);
            floorBlocker.SetActive(false);
        }
    }

    public void next()
    {

        //SendNext("next");
        Debug.Log("incrementing to next help");
        helpType++;
        helpType = helpType % numHelpTypes;
        setHelp(helpType);


    }
    // Update is called once per frame
    void Update () {
		
	}
}
