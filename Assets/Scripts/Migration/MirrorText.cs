using System;
using UnityEngine;

public class MirrorText : RosComponent {

    RosSubscriber<ros.std_msgs.String> sub;

    string byteTextMirror;

    byte[] decodedBytesMirror;

    Texture2D texMirror;
    Renderer mirrorRenderer;

    // Use this for initialization
    void Start () {

        Subscribe("mirrorSub", "/mirrorText", 5, out sub);

        texMirror = new Texture2D(2, 2);

        mirrorRenderer = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {

        ros.std_msgs.String msg;

        if (Receive(sub, out msg))
        {
            if (msg.data.ToString().Length != 0)
            {
                byteTextMirror = msg.data.ToString();

                if (Source.Instance.frameCount % 2 == 1)
                {
                    decodedBytesMirror = Convert.FromBase64String(byteTextMirror.Substring(1, byteTextMirror.Length - 2));
                    texMirror.LoadImage(decodedBytesMirror);
                    mirrorRenderer.material.mainTexture = texMirror;
                }
            }
        }
    }
}
