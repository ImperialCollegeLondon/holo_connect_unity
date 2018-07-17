using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//AllViz requires the GameObject to have a Renderer component
[RequireComponent(typeof(Renderer))]

public class AllViz : RosComponent
{

    RosSubscriber<ros.std_msgs.String> sub;

    Renderer _renderer;

    // Use this for initialization
    void Start()
    {
        Subscribe("allVizSub", "/allViz", 5, out sub);

        _renderer = GetComponent<Renderer>();

    }//Start

    // Update is called once per frame
    void Update()
    {

        ros.std_msgs.String msg;

        if (Receive(sub, out msg))
        {
            if (msg.data.ToString().Contains("true"))
            {
                _renderer.enabled = true;
            }
            else
            {
                _renderer.enabled = false;

            }
        }

    }//Update
}
