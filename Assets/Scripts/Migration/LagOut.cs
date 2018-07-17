using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LagOut : RosComponent {

    RosSubscriber<ros.std_msgs.Float32> sub;

    // Use this for initialization
    void Start () {
        Subscribe("lagSub", "/lagOut", 5, out sub);
    }
	
	// Update is called once per frame
	void Update () {

        ros.std_msgs.Float32 msg;

        if (Receive(sub, out msg))
        {
            Source.Instance.lag = msg.data;
        }

    }
}
