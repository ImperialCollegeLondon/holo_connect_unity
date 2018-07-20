using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class MirrorText : RosComponent {

    RosSubscriber<ros.std_msgs.String> sub;

    private const String valuemap = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

    byte[] decodedBytesMirror;

    Renderer mirrorRenderer;

    // Use this for initialization
    void Start () {

        Subscribe("mirrorSub", "/mirrorText", 5, out sub);

        mirrorRenderer = GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {

        ros.std_msgs.String msg;

        if (Receive(sub, out msg))
        {
            if (Source.Instance.frameCount % 2 == 1)
            {
                String encoded = msg.data;
                byte[] image = DecodeString(encoded);

                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(image);
                mirrorRenderer.material.mainTexture = tex;
            }
        }
    }

    byte[] DecodeString(String str)
    {
        List<byte> buff = new List<byte>();
        int pad = str.Count(c => c == '=');

        String strip = str.Replace("=", "A");

        for (int i = 0; i < strip.Length; i += 4)
        {
            String chunk = strip.Substring(i, 4);
            byte[] base64 = new byte[4];

            for (int j = 0; j < 4; j++)
            {
                char c = chunk[j];
                base64[j] = (byte)valuemap.IndexOf(c);
            }

            buff.Add((byte)((base64[0] << 2) + (base64[1] >> 4)));
            buff.Add((byte)((base64[1] << 4) + (base64[2] >> 2)));
            buff.Add((byte)((base64[2] << 6) + (base64[3])));
        }
        for (int i = 0; i < pad; i++) buff.RemoveAt(buff.Count - 1);
        return buff.ToArray();
    }
}
