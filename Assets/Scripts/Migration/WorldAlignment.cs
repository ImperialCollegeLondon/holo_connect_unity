using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldAlignment : RosComponent {

    RosSubscriber<ros.geometry_msgs.Pose> holoRosOffset;
    RosSubscriber<ros.geometry_msgs.Pose> holoWorldSub;
    RosSubscriber<ros.geometry_msgs.Pose> wheelChairSub;

    public GameObject holoWorldObj;
    public GameObject wheelchairHolder;

    [HideInInspector]
    public Vector3 pos;
    [HideInInspector]
    public Quaternion quat;

    private triggerManager tmHoloWorld;
    private triggerManager tmWheelChair;

    // Use this for initialization
    void Start () {

        Subscribe("holoRosOffset", "/holoRosOffset", 5, out holoRosOffset);
        Subscribe("holoWorldSub", "/holoWorld", 5, out holoWorldSub);
        Subscribe("wheelChairSub", "/wheelChairPose", 5, out wheelChairSub);

        tmHoloWorld = holoWorldObj.GetComponent<triggerManager>();
        tmWheelChair = wheelchairHolder.GetComponent<triggerManager>();

    }
	
	// Update is called once per frame
	void Update () {

        ros.geometry_msgs.Pose holoRosOffset_msg;
        ros.geometry_msgs.Pose holoWorldSub_msg;
        ros.geometry_msgs.Pose wheelChairSub_msg;

        if (Receive(holoRosOffset, out holoRosOffset_msg))
        {
            Debug.Log("got hololens/ros offset");
            getMove(holoRosOffset_msg);

            Vector3 swappedPos = new Vector3(pos.x, pos.y, pos.z);
            Quaternion swappedQuaternion = new Quaternion(quat.x, quat.y, quat.z, quat.w);
            tmHoloWorld.moveToPos = swappedPos;
            tmHoloWorld.moveToPos.y = Cube1.Instance.transform.position.y;
            tmHoloWorld.moveToRot = (swappedQuaternion);
        }

        if (Receive(holoWorldSub, out holoWorldSub_msg))
        {
            getMove(holoWorldSub_msg);
            tmHoloWorld.moveToPos = pos;
            tmHoloWorld.moveToRot = quat;
        }

        if (Receive(wheelChairSub, out wheelChairSub_msg))
        {
            getMove(wheelChairSub_msg);

            pos.y = 0;
            tmWheelChair.moveToPos = pos;
            tmWheelChair.moveToRot = quat;
        }

    }

    private void getMove(ros.geometry_msgs.Pose msg)
    {
        pos = new Vector3();
        quat = new Quaternion();

        pos.x = (float)msg.position.x;
        pos.y = (float)msg.position.y;
        pos.z = (float)msg.position.z;

        quat.x = (float)msg.orientation.x;
        quat.y = (float)msg.orientation.y;
        quat.z = (float)msg.orientation.z;
        quat.w = (float)msg.orientation.w;

        if (float.IsNaN(pos.x) || float.IsNaN(pos.y) || float.IsNaN(pos.z) || float.IsNaN(quat.x) || float.IsNaN(quat.y) || float.IsNaN(quat.z) || float.IsNaN(quat.w))
        {
            pos.Set(0, 0, 0);
            quat.Set(0, 0, 0, 1);
        }

        float mag = Mathf.Sqrt(Mathf.Pow(quat.x, 2) + Mathf.Pow(quat.y, 2) + Mathf.Pow(quat.z, 2) + Mathf.Pow(quat.w, 2));
        quat.x = quat.x / mag;
        quat.y = quat.y / mag;
        quat.z = quat.z / mag;
        quat.w = quat.w / mag;

    }
}
