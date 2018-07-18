using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WheelChairPose : RosComponent {

    RosSubscriber<ros.geometry_msgs.Pose> sub;

    triggerManager tmWheelChair;

    Vector3 pos;
    Quaternion quat;

    // Use this for initialization
    void Start () {

        Subscribe("wheelChairSub", "/wheelChairPose", 5, out sub);

        tmWheelChair = GetComponent<triggerManager>();

    }

    // Update is called once per frame
    void Update()
    {

        ros.geometry_msgs.Pose msg;

        if (Receive(sub, out msg))
        {
            pos.x = (float)msg.position.x;
            pos.y = 0.0f;
            pos.z = (float)msg.position.z;

            quat.x = (float)msg.orientation.x;
            quat.y = (float)msg.orientation.y;
            quat.z = (float)msg.orientation.z;
            quat.w = (float)msg.orientation.w;

            Source.Instance.savedRot = quat;

            tmWheelChair.moveToPos = pos;
            tmWheelChair.moveToRot = quat;
        }
    }
}
