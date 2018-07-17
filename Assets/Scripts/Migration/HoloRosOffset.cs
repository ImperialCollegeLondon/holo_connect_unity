using UnityEngine;

public class HoloRosOffset : RosComponent
{

    RosSubscriber<ros.geometry_msgs.Pose> sub;

    triggerManager tmHoloWorld;

    Vector3 pos;
    Quaternion quat;

    // Use this for initialization
    void Start()
    {

        Subscribe("holoRosOffset", "/holoRosOffset", 5, out sub);

        tmHoloWorld = GetComponent<triggerManager>();
    }

    // Update is called once per frame
    void Update()
    {

        ros.geometry_msgs.Pose msg;

        if (Receive(sub, out msg))
        {
            pos.x = (float)msg.position.x;
            pos.y = (float)msg.position.y;
            pos.z = (float)msg.position.z;

            quat.x = (float)msg.orientation.x;
            quat.y = (float)msg.orientation.y;
            quat.z = (float)msg.orientation.z;
            quat.w = (float)msg.orientation.w;

            Vector3 swappedPos = new Vector3(pos.x, pos.y, pos.z);
            Quaternion swappedQuaternion = new Quaternion(quat.x, quat.y, quat.z, quat.w);

            tmHoloWorld.moveToPos = swappedPos;
            tmHoloWorld.moveToPos.y = Cube1.Instance.transform.position.y;
            tmHoloWorld.moveToRot = (swappedQuaternion);
        }
    }
}