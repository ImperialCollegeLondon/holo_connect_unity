using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;

public class Speech : RosComponent {

    RosSubscriber<ros.std_msgs.String> sub;

    private bool shouldSpeak = false;
    private string speechText = "Write a string to me before playing";

    // Use this for initialization
    void Start () {
        Subscribe("speechSub", "/speech", 5, out sub);
	}
	
	// Update is called once per frame
	void Update () {

        ros.std_msgs.String msg;

        if (Receive(sub, out msg))
        {
            string toSay = msg.data.ToString();
            speak(toSay);
        }

        if (shouldSpeak)
        {
            shouldSpeak = false;
            GetComponent<TextToSpeech>().StartSpeaking(speechText);
        }

    }

    public void speak(string toSay)
    {
        speechText = toSay;
        shouldSpeak = true;
    }
}
