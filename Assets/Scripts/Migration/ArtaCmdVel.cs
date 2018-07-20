using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArtaCmdVel : RosComponent {

    RosSubscriber<ros.geometry_msgs.Twist> sub;

    twistArrowControler correctedArrowController;

    // Use this for initialization
    void Start () {

        Subscribe("correctedTwistSub", "/arta/cmd_vel", 5, out sub);

        correctedArrowController = GetComponent<twistArrowControler>();
    }

    // Update is called once per frame
    void Update () {
        ros.geometry_msgs.Twist msg;

        if (Receive(sub, out msg))
        {
            correctedArrowController.angular = (float)msg.angular.z;
            correctedArrowController.linear = (float)msg.linear.x;
        }
    }
}
