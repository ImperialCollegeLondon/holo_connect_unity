//Here the connection and interaction between Unity/HL and ROS is made.

//Two different WebSockets libraries are used to establish a connection.
//WebSocketSharp provides a websocket client that allow us to connect Unity with ROS via RosBridge. 
//Since this library it is not available for Windows Store Apps (HL format), Windows.Networking.Socket is responsible
//for connecting HL and ROS.

//Pieces of code wrapped in #if UNITY_EDITOR are used only when Unity Play mode is running.
//Pieces of code wrapped in #if !UNITY_EDITOR are used only when the app is running on HL.

//Methodology for establishing an async Rosbridge connection during Unity play mode and to build a message responsible for accessing and 
//sending a ROS service message were given here: 
//github.com/2016UAVClass/Simulation-Unity3D @author Michael Jenkin, Robert Codd-Downey and Andrew Speers

using UnityEngine;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using SimpleJSON;
using HoloToolkit.Unity;


#if UNITY_EDITOR
using WebSocketSharp;
using System.Threading;
#endif


#if !UNITY_EDITOR
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Networking;
using Windows.Foundation;
using Windows.UI.Core;
using System.Threading.Tasks;

#endif

public class Source : Singleton<Source>
{
    public GameObject originObj;

    public GameObject wheelChairObj;
    public GameObject holoLens;

    public GameObject obsObj1;
    public Camera mainCam;
    public int helpType;
    public int numHelpTypes;

    private gripManager gripHandle;

    public TextToSpeech TextToSpeechObj;
    private bool shouldSpeak = false;
    private string speechText = "Write a string to me before playing";

    private bool isInit = false;

    private bool isPoints = false;
    private int curPoint = 0;
    private bool pointClicked = false;

    //private values for wheelchair offset 
    private Quaternion initialRot;

    //variables for clock sync 
    int rosSecs = 0;
    int rosNSecs = 0;

    public float lag = 0;

    double startSecsFilt = 0;
    double startNSecsFilt = 0;
    DateTime startTime;
    DateTime currTime;
    TimeSpan runTime;
    int mySecs = 0;
    int myNSecs = 0;

    public GameObject soundObj;
    soundMover sm;

    //Still needed for migration
    [HideInInspector]
    public int frameCount = 0, collisionWidth = 300;

    Vector3 Cube1Pos;

    void Update()
    {
        name = getObject(holoLens.transform.position, holoLens.transform.position + holoLens.transform.forward);
        Debug.Log(name);

        if (shouldSpeak)
        {
            shouldSpeak = false;
            var textToSpeechObj = this.GetComponent<TextToSpeech>();
            TextToSpeechObj.StartSpeaking(speechText);   
        }

        if (isPoints && pointClicked)
        {
            pointClicked = false;
            publishPointClicked(curPoint);
            curPoint++;
            if (curPoint == 8)
            {
                curPoint = 0;
                isPoints = false;
            }
        }

#if UNITY_EDITOR
        //Connecting in Unity play mode
        if (Input.GetKeyDown(KeyCode.C))
        {
            RosMessenger.Instance.Connect();
        }

        //Disconnecting in Unity play mode
        if (Input.GetKeyDown(KeyCode.E))
        {
            RosMessenger.Instance.Disconnect();
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            SendNext("next");
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            SendNext("clear");
        }

#endif
        if (RosMessenger.Instance.Con)
        {
            frameCount++;
            if (!isInit)
            {
                Initialise();
            }
            if (isInit && !RosMessenger.Instance.busy)
            {
                name = getObject(holoLens.transform.position, holoLens.transform.position + holoLens.transform.forward);
                Debug.Log(name);
                SendGaze(name, false);
                SendPose();
                if (frameCount % 4 == 0) sendObs(obsObj1, "/obs1");
            }
        }

    }//Update

    public void Start()
    {
        startTime = DateTime.Now;
        gripHandle = this.GetComponent<gripManager>();


        sm = soundObj.GetComponent<soundMover>();
    }

    //will need exhaustive list of all topics recieved here, mostly pose for now. 

    public void parseMessage(string inString)
    {
        var N = JSON.Parse(inString);
        string ourTopic;
        getMove(out ourTopic, inString);

        switch (ourTopic)
        {
            case "\"/speech\"":
                speakMsg(inString);
                break;

            case "\"/pingOut\"":
                parseTime(inString);
                currTime = DateTime.Now;
                long ticks = currTime.Ticks - startTime.Ticks;
                runTime = new TimeSpan(ticks);
                mySecs = (int)(runTime.TotalSeconds);
                myNSecs = (int)(runTime.Ticks % 10000000L) * 100;
                sendBackTime(inString);
                if (lag != 0)
                {
                    calcBaseTime();
                }
                break;

            case "\"/lagOut\"":
                getLag(inString);
                break;

            case "\"/formatted_grid/intense_pixel\"":
                Debug.Log("got brightest pixel location");
                sm.x = N["msg"]["x"];
                sm.y = N["msg"]["y"];
                sm.width = collisionWidth;
                sm.intensity = N["msg"]["intensity"];
                break;

            case "fail":
                //if in doubt refresh the connection.
                break;
            default:
                Debug.Log("Recieved unexpected topic" + N["topic"].ToString());
                break;
        }
    }

    private void calcBaseTime()
    {
        int lagSecs = (int)lag;
        int lagNSecs = (int)((lag - lagSecs) / 1e9);
        int startSecs = rosSecs + lagSecs - mySecs;  //the time the program started on the ros clock is the time ros said plus the expected delay getting here minus the time we have been runnign
        int startNSecs = rosNSecs + lagNSecs - myNSecs;
        double ratio = 0.99;
        double error = (startSecsFilt - startSecs) + (startNSecsFilt - startNSecs) * 1e-9;

        if (startSecsFilt == 0 && startNSecsFilt == 0)
        {
            startSecsFilt = startSecs;
            startNSecsFilt = startNSecs;
        }

        if (Mathf.Abs((float)error) < lag)
        {
            startSecsFilt = ratio * startSecsFilt + (1 - ratio) * startSecs;
            startNSecsFilt = ratio * startNSecsFilt + (1 - ratio) * startNSecs;
        }

        //allocate fractional seconds. 
        double leftOverSec = startSecsFilt - (double)((int)startSecsFilt);
        startNSecsFilt = startNSecsFilt + 1e9 * leftOverSec;
        startSecsFilt = (double)((int)startSecsFilt);

        //roll over nSecs 
        if (startNSecsFilt > 1e9)
        {
            startNSecsFilt = startNSecsFilt - 1e9;
            startSecsFilt = startSecsFilt + 1;

        }

        if (startNSecsFilt < 0)
        {
            startNSecsFilt = startNSecsFilt + 1e9;
            startSecsFilt = startSecsFilt - 1;

        }
    }//calcBaseTime

    private void getLag(string inString)
    {
        var N = JSON.Parse(inString);
        lag = N["msg"]["data"];
    }

    private void parseTime(string inString)
    {
        var N = JSON.Parse(inString);
        rosNSecs = N["msg"]["data"]["nsecs"];
        rosSecs = N["msg"]["data"]["secs"];
    }

    private void sendBackTime(string inString)
    {
        var N = JSON.Parse(inString);
        N["topic"] = "/holoPing";
        RosMessenger.Instance.Send(N.ToString());
    }

    private Vector3 getPos(string inString, int num)
    {
        var N = JSON.Parse(inString);
        Vector3 ret;
        ret.x = N["msg"]["poses"][num]["position"]["x"];
        ret.y = N["msg"]["poses"][num]["position"]["y"];
        ret.z = N["msg"]["poses"][num]["position"]["z"];
        return ret;
    }

    private void speakMsg(string inString)
    {
        var N = JSON.Parse(inString);
        string toSay = N["msg"]["data"].ToString();
        speak(toSay);
    }

    public void speak(string toSay)
    {
        speechText = toSay;
        shouldSpeak = true;
    }

    private void getMove(out string topic, string inString)
    {
        //being careful with the string, could contain garbage
        try
        {
            var N = JSON.Parse(inString);
            topic = N["topic"].ToString();         
        }
        catch (System.ArgumentOutOfRangeException e)
        {
            topic = "fail";
            return;
        }

    }//getMove

    public void startPoints()
    {
        speak("started markers");
        isPoints = true;
    }

    public void click()
    {
        speak("click");
        SendNext("wandNext");
        if (isPoints)
        {
            pointClicked = true;
        }
    }

    public void clear()
    {
        speak("clear");
        SendNext("clear");
    }

    public void next()
    {
        Debug.Log("in source next()");
    }


    void OnApplicationQuit()
    {
        RosMessenger.Instance.Disconnect();
    }

    public void Initialise()
    {
        isInit = true;

        string obs1pub = advertise("/obs1", "geometry_msgs/PoseStamped");
        string lagSub = subscribe("/lagOut", "std_msgs/Float32");
        string holoPingAdv = advertise("/holoPing", "std_msgs/Time");
        string pingOutSub = subscribe("/pingOut", "std_msgs/Time");
        string advStr = advertise("/holoPose", "geometry_msgs/PoseStamped");
        string advReCal = advertise("/reCalibrate", "std_msgs/String");
        string advpointClicked = advertise("/pointClicked", "std_msgs/Int32");
        string originSub = subscribe("/origin", "geometry_msgs/Pose");
        string speechSub = subscribe("/speech", "std_msgs/String");
        string arraySub = subscribe("/cameraPosArr", "geometry_msgs/PoseArray");
        string nextSub = advertise("/holoNext", "std_msgs/String");
        string mapPub = advertise("/mapRaw", "std_msgs/String");
        string intensePixelSub = subscribe("/formatted_grid/intense_pixel", "hololens_experiment/IntensePixel");
        string headGazeSub = advertise("/headGaze", "std_msgs/String");  

        Debug.Log(headGazeSub);
        RosMessenger.Instance.Send(headGazeSub);
        Debug.Log(intensePixelSub);
        RosMessenger.Instance.Send(intensePixelSub);
        Debug.Log(obs1pub);
        RosMessenger.Instance.Send(obs1pub);
        Debug.Log(mapPub);
        RosMessenger.Instance.Send(mapPub);
        Debug.Log(lagSub);
        RosMessenger.Instance.Send(lagSub);
        Debug.Log(holoPingAdv);
        RosMessenger.Instance.Send(holoPingAdv);
        Debug.Log(pingOutSub);
        RosMessenger.Instance.Send(pingOutSub);
        Debug.Log(advpointClicked);
        RosMessenger.Instance.Send(advpointClicked);
        Debug.Log(advStr);
        RosMessenger.Instance.Send(advStr);
        Debug.Log(originSub);
        RosMessenger.Instance.Send(originSub);
        Debug.Log(advReCal);
        RosMessenger.Instance.Send(advReCal);
        RosMessenger.Instance.Send(speechSub);
        Debug.Log(arraySub);
        RosMessenger.Instance.Send(arraySub);
        Debug.Log(nextSub);
        RosMessenger.Instance.Send(nextSub);

    }//Initialise

    public static string subscribe(string topic, string type)
    {
        return "{\"op\": \"subscribe\", \"topic\": \"" + topic + "\",\"type\": \"" + type + "\"}";
    }

    public static string advertise(string topic, string type)
    {
        return "{\"op\": \"advertise\", \"topic\": \"" + topic + "\",\"type\": \"" + type + "\"}";
    }

    string getObject(Vector3 start, Vector3 pointOnRay)
    {
        Vector3 dir = pointOnRay - start;
        RaycastHit info;
        string name = "none";
        bool ishit = Physics.Raycast(start, dir, out info, 20);
        if (ishit)
        {
            name = info.collider.gameObject.name;
        }
        return name;
    }

    public void SendPose()
    {
        var headPosition = Camera.main.transform.position;
        var headRotation = Camera.main.transform.rotation;
        currTime = DateTime.Now;
        long ticks = currTime.Ticks - startTime.Ticks;
        runTime = new TimeSpan(ticks);
        mySecs = (int)(runTime.TotalSeconds);
        myNSecs = (int)(runTime.Ticks % 10000000L) * 100;

        double leftOverSec = startSecsFilt - (double)((int)startSecsFilt);
        startNSecsFilt = startNSecsFilt + 1e9 * leftOverSec;
        startSecsFilt = (double)((int)startSecsFilt);

        int thisNSecs = (int)(startNSecsFilt) + myNSecs;
        int thisSecs = (int)(startSecsFilt) + mySecs;

        if (thisNSecs > 1000000000)
        {
            thisNSecs = thisNSecs - 1000000000;
            thisSecs = thisSecs + 1;
        }

        if (thisNSecs < 0)
        {
            thisNSecs = thisNSecs + 1000000000;
            thisSecs = thisSecs - 1;
        }

        var N = "{ \"op\": \"publish\"" +
                ", \"topic\": \"" + "/holoPose" + "\"" +
                ", \"type\": \"" + "geometry_msgs/PoseStamped" + "\"" +
                ", \"msg\": " +
                    "{ \"header\": " +
                        "{ \"seq\": 0 " +
                        ", \"frame_id\": \"holoLens\"" +
                        ", \"stamp\": " +
                            "{ \"secs\": " + thisSecs.ToString() +
                            ", \"nsecs\": " + thisNSecs.ToString() +
                            "}}" +
                    ", \"pose\": " +
                        "{ \"position\": " +
                            "{ \"x\": " + headPosition.x.ToString() +
                            ", \"y\": " + headPosition.y.ToString() +
                            ", \"z\": " + headPosition.z.ToString() +
                            "}" +
                        ", \"orientation\": " +
                            "{ \"x\": " + headRotation.x.ToString() +
                            ", \"y\": " + headRotation.y.ToString() +
                            ", \"z\": " + headRotation.z.ToString() +
                            ", \"w\": " + headRotation.w.ToString() +
                            "}" +
                        "}" +
                    "}" +
                "}";
        RosMessenger.Instance.Send(N);

    }//SendPose

    public void sendObs(GameObject obj, string topic)
    {
        // positions in world space. 
        var Position = obj.transform.position;
        var Rotation = obj.transform.rotation;
        Position = wheelChairObj.transform.InverseTransformPoint(Position);
        Rotation = Quaternion.Inverse(wheelChairObj.transform.rotation) * Rotation;
        currTime = DateTime.Now;
        long ticks = currTime.Ticks - startTime.Ticks;
        runTime = new TimeSpan(ticks);
        mySecs = (int)(runTime.TotalSeconds);
        myNSecs = (int)(runTime.Ticks % 10000000L) * 100;


        double leftOverSec = startSecsFilt - (double)((int)startSecsFilt);
        startNSecsFilt = startNSecsFilt + 1e9 * leftOverSec;
        startSecsFilt = (double)((int)startSecsFilt);

        int thisNSecs = (int)(startNSecsFilt) + myNSecs;
        int thisSecs = (int)(startSecsFilt) + mySecs;
        if (thisNSecs > 1000000000)
        {
            thisNSecs = thisNSecs - 1000000000;
            thisSecs = thisSecs + 1;

        }
        if (thisNSecs < 0)
        {

            thisNSecs = thisNSecs + 1000000000;
            thisSecs = thisSecs - 1;
        }
        var N = "{ \"op\": \"publish\"" +
                ", \"topic\": \"" + topic + "\"" +
                ", \"type\": \"" + "geometry_msgs/PoseStamped" + "\"" +
                ", \"msg\": " +
                    "{ \"header\": " +
                        "{ \"seq\": 0 " +
                        ", \"frame_id\": \"base_link\"" +
                        ", \"stamp\": " +
                            "{ \"secs\": " + thisSecs.ToString() +
                            ", \"nsecs\": " + thisNSecs.ToString() +
                            "}}" +
                    ", \"pose\": " +
                        "{ \"position\": " +
                            "{ \"x\": " + Position.x.ToString() +
                            ", \"y\": " + Position.y.ToString() +
                            ", \"z\": " + Position.z.ToString() +
                            "}" +
                        ", \"orientation\": " +
                            "{ \"x\": " + Rotation.x.ToString() +
                            ", \"y\": " + Rotation.y.ToString() +
                            ", \"z\": " + Rotation.z.ToString() +
                            ", \"w\": " + Rotation.w.ToString() +
                            "}" +
                        "}" +
                    "}" +
                "}";
        RosMessenger.Instance.Send(N);
    }//sendObs

    public void SendNext(string type)
    {
        var N = JSON.Parse("{\"op\": \"publish\", \"topic\": \"" + "\"/holoNext\"" + "\",\"type\": \"" + "std_msgs/String" + "\"}");
        N["msg"]["data"] = type;
        Debug.Log(N);
        try
        {
            string tosend = N.ToString();
            RosMessenger.Instance.Send(tosend);
        }
        catch (System.ArgumentOutOfRangeException e)
        {
            return;
        }

    }//SendNext

    //publishes to the gaze topics, eyehead = 1 is eye gaze, =0 is head gaze.
    public void SendGaze(string obj, bool eyeHead)
    {
        if (RosMessenger.Instance.Con)
        {
            string topic;

            if (eyeHead)
            {
                topic = "\"/eyeGaze\"";
            }
            else
            {
                topic = "\"/headGaze\"";
            }
            var N = "{ \"op\": \"publish\"" +
        ", \"topic\":" + topic +
        ", \"type\": \"" + "std_msgs/String" + "\"" +
        ",\"msg\":{\"data\": " +"\"" +obj.ToString() + "\"" + "}" +
        "}";

            try
            {
                string tosend = N.ToString();
                Debug.Log(tosend);
                RosMessenger.Instance.Send(tosend);
            }
            catch (System.ArgumentOutOfRangeException e)
            {
                return;
            }
        }

    }//SendGaze

    public void publishPointClicked(int i)
    {
        var N = "{ \"op\": \"publish\"" +
                ", \"topic\": \"" + "/pointClicked" + "\"" +
                ", \"type\": \"" + "std_msgs/Int32" + "\"" +
                ",\"msg\":{\"data\": " + i.ToString() + "}" +
                "}";
        RosMessenger.Instance.Send(N);

    }//publishPointClicked

}//Source