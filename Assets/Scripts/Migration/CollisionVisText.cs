using System;
using UnityEngine;

public class CollisionVisText : RosComponent {

    public GameObject collisionVizPlane;
    public GameObject wheelchairHolder;
    public GameObject collisionHolder;

    RosSubscriber<ros.std_msgs.String> sub;

    byte[] decodedBytesCollisionViz;
    string byteTextCollisionViz;

    Vector3 thisPos;

    Texture2D texCollisionViz;
    Renderer collisionRenderer;

    // Use this for initialization
    void Start () {

        Subscribe("collisionVizSub", "/collisionVisText", 5, out sub);

        texCollisionViz = new Texture2D(2, 2);

        collisionRenderer = collisionVizPlane.GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update () {

        ros.std_msgs.String msg;

        if (Receive(sub, out msg))
        {
            if (msg.data.ToString().Length != 0)
            {
                byteTextCollisionViz = msg.data.ToString();

                if (Source.Instance.frameCount % 2 == 1)
                {
                    decodedBytesCollisionViz = Convert.FromBase64String(byteTextCollisionViz.Substring(1, byteTextCollisionViz.Length - 2));
                    texCollisionViz.LoadImage(decodedBytesCollisionViz);

                    Source.Instance.collisionWidth = texCollisionViz.width;
                    collisionRenderer.material.mainTexture = texCollisionViz;

                    thisPos = wheelchairHolder.transform.position;
                    thisPos.y = wheelchairHolder.transform.position.y;

                    collisionHolder.transform.position = thisPos;
                    collisionHolder.transform.rotation = (wheelchairHolder.transform.rotation);
                }
            }
        }
    }
}
