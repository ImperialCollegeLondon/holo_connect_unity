using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavigationMainJsCmdVel : RosComponent {

    RosSubscriber<ros.geometry_msgs.Twist> sub;

    twistArrowControler userArrowController;

    // Use this for initialization
    void Start () {

        Subscribe("userTwistSub", "/navigation/main_js_cmd_vel", 5, out sub);

        userArrowController = GetComponent<twistArrowControler>();

    }
	
	// Update is called once per frame
	void Update () {

        ros.geometry_msgs.Twist msg;

        if (Receive(sub, out msg))
        {
            userArrowController.angular = (float)msg.angular.z;
            userArrowController.linear = (float)msg.linear.x;
        }
		
	}
}
