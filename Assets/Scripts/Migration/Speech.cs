using HoloToolkit.Unity;

public class Speech : RosComponent {

    RosSubscriber<ros.std_msgs.String> sub;

    TextToSpeech TextToSpeechObj;

    // Use this for initialization
    void Start () {

        Subscribe("speechSub", "/speech", 5, out sub);

        TextToSpeechObj = GetComponent<TextToSpeech>();

    }
	
	// Update is called once per frame
	void Update () {

        ros.std_msgs.String msg;

        if (Receive(sub, out msg))
        {
            TextToSpeechObj.StartSpeaking(msg.data);
        }

    }
}
