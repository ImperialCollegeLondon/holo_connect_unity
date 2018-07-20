using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AllViz : RosComponent
{

    RosSubscriber<ros.std_msgs.String> sub;

    public List<GameObject> RenderingList;

    // Use this for initialization
    void Start()
    {
        Subscribe("allVizSub", "/allViz", 5, out sub);

    }//Start

    // Update is called once per frame
    void Update()
    {

        ros.std_msgs.String msg;

        if (Receive(sub, out msg))
        {

            if (msg.data.ToString().Contains("true"))
            {
                foreach (GameObject obj in RenderingList)
                {
                    obj.GetComponent<Renderer>().enabled = true;
                }                
            }
            else
            {
                Debug.Log("Reaching this point " + msg.data);

                foreach (GameObject obj in RenderingList)
                {
                    obj.GetComponent<Renderer>().enabled = false;
                }
            }
        }

    }//Update
}
