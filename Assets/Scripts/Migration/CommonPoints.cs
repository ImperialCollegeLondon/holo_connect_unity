using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommonPoints : RosComponent {

    RosSubscriber<ros.hololens_experiment.CommonPoints> sub;
    RosPublisher<ros.hololens_experiment.CommonPoints> pub;

    ros.geometry_msgs.Point p1;
    ros.geometry_msgs.Point p2;
    ros.geometry_msgs.Point p3;

    // Use this for initialization
    void Start () {

        Subscribe("trianglePointsSub", "/hololens_experiment/common_points", 5, out sub);
        Advertise("trianglePointsPub", "/hololens/commonPoints", 5, out pub);

        p1 = new ros.geometry_msgs.Point();
        p2 = new ros.geometry_msgs.Point();
        p3 = new ros.geometry_msgs.Point();

    }

    // Update is called once per frame
    void Update () {

        ros.hololens_experiment.CommonPoints msg;

        if (Receive(sub, out msg))
        {
            Debug.Log("got triangle");

            p1.x = Cube1.Instance.transform.position.x;
            p1.y = 0.0f;
            p1.z = Cube1.Instance.transform.position.z;

            p2.x = Cube2.Instance.transform.position.x;
            p2.y = 0.0f;
            p2.z = Cube2.Instance.transform.position.z;

            p3.x = Cube3.Instance.transform.position.x;
            p3.y = 0.0f;
            p3.z = Cube3.Instance.transform.position.z;

            ros.hololens_experiment.CommonPoints resp = new ros.hololens_experiment.CommonPoints(msg.secs, msg.nsecs, "hololens", p1, p2, p3);
            Publish(pub, resp);
        }	
	}
}
