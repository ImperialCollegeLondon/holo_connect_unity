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
    //VM IP adress used for connection
    private string host = "10.0.0.213";

    public GameObject wheelChairObj;
    public GameObject holoLens;
    public GameObject obsObj1;

    [HideInInspector]
    public bool con = false;

    [HideInInspector]
    public Quaternion savedRot;

    private gripManager gripHandle;

    //Default ROSBridge port
    private int port = 9090;
    private bool isInit = false;
    private bool busy = false;
    private bool isPoints = false;
    private int curPoint = 0;
    private bool pointClicked = false;

    //private values for wheelchair offset 
    private Quaternion initialRot;

    private bool firstChair = true;

#if UNITY_EDITOR
    //WebSocket client from WebSocketSharp
    private WebSocket Socket;
    private Thread runThread;
#endif

    //WebSocket client from Windows.Networking.Sockets
#if !UNITY_EDITOR
    private MessageWebSocket messageWebSocket;
    Uri server;
    DataWriter dataWriter;
#endif

    //variables for clock sync 
    int rosSecs = 0;
    int rosNSecs = 0;

    double startSecsFilt = 0;
    double startNSecsFilt = 0;
    DateTime startTime;
    DateTime currTime;
    TimeSpan runTime;
    int mySecs = 0;
    int myNSecs = 0;

    public GameObject soundObj;
    soundMover sm;

    //To maintain Speak functionality
    TextToSpeech TextToSpeechObj;

    //Still need it for Migration
    [HideInInspector]
    public float lag = 0;
    [HideInInspector]
    public int frameCount = 0, collisionWidth = 300;

    public void Start()
    {
        startTime = DateTime.Now;
        gripHandle = this.GetComponent<gripManager>();
        sm = soundObj.GetComponent<soundMover>();

        //To maintain Speak functionality
        TextToSpeechObj = GetComponent<TextToSpeech>();
    }

    void Update()
    {
        name = getObject(holoLens.transform.position, holoLens.transform.position + holoLens.transform.forward);

        if (firstChair && !savedRot.Equals(new Quaternion(0, 0, 0, 0)))
        {
            firstChair = false;
            initialRot = holoLens.transform.rotation;
            //change the initial rot to be the closest rotation with only a Y component. 
            Vector3 inVect = new Vector3(1, 0, 0);
            Vector3 outVect = inVect.RotateAround(new Vector3(0, 0, 0), initialRot);
            outVect.y = 0;
            Vector2 inVect2;
            inVect2.x = inVect.x;
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
            Connect(host);
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            Connect("10.42.0.97");
        }

        //Disconnecting in Unity play mode
        if (Input.GetKeyDown(KeyCode.E))
        {
            Discon();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            sendQual(true);
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            sendQual(false);
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
        if (con)
        {
            frameCount++;
            if (!isInit)
            {
                Initialise();
            }
            if (isInit && !busy)
            {
                name = getObject(holoLens.transform.position, holoLens.transform.position + holoLens.transform.forward);
                Debug.Log(name);
                SendGaze(name, false);
                SendPose();
                if (frameCount % 4 == 0) sendObs(obsObj1, "/obs1");
            }
        }

    }//Update

    //Tap Gesture on HL
#if !UNITY_EDITOR
    void OnSelect()
    {

    }
#endif

    public void Connect(string address)
    {
        host = address;
        //Async connection.
        if (!con && !busy)
        {
            busy = true;
            Debug.Log("connecting");
            Debug.Log(port);
#if UNITY_EDITOR
            runThread = new Thread(Run);
            runThread.Start();
#endif

# if !UNITY_EDITOR

            messageWebSocket = new MessageWebSocket();
            messageWebSocket.Control.MessageType = SocketMessageType.Utf8;
            messageWebSocket.MessageReceived += Win_MessageReceived;

            server = new Uri("ws://" + host + ":" + port.ToString());

            IAsyncAction outstandingAction = messageWebSocket.ConnectAsync(server);
            AsyncActionCompletedHandler aach = new AsyncActionCompletedHandler(NetworkConnectedHandler);
            outstandingAction.Completed = aach;

#endif
        }
    }//Connect

    //Successfull network connection handler on HL
#if !UNITY_EDITOR
    public void NetworkConnectedHandler(IAsyncAction asyncInfo, AsyncStatus status)
    {
        // Status completed is successful.
        if (status == AsyncStatus.Completed)
        {
            //Guarenteed connection
            con = true;
           TextToSpeechObj.StartSpeaking("connected");

            Debug.Log("connected!");
            busy = false;


            //Creating the writer that will be repsonsible to send a message through Rosbridge
            dataWriter = new DataWriter(messageWebSocket.OutputStream);

        }
        else
        {
            con = false;
        }
    }
#endif

    //Starting connection between Unity play mode and ROS.
    private void Run()
    {
#if UNITY_EDITOR
        Socket = new WebSocket("ws://" + host + ":" + port);

        Socket.OnOpen += (sender, e) => {
            con = true;
            busy = false;
            Debug.Log("connected!");
        };

        Socket.Connect();

        while (true)
        {
            Thread.Sleep(10000);
        }
#endif
    }

    //The MessageReceived event handler.
#if !UNITY_EDITOR
    private void Win_MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
    {
        try
        {
            DataReader messageReader = args.GetDataReader();
            messageReader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
            string messageString = messageReader.ReadString(messageReader.UnconsumedBufferLength);
            parseMessage(messageString);
        }
        catch (InvalidCastException e)
        {

        }

        //Add code here to do something with the string that is received.
    }
#endif

#if UNITY_EDITOR
    private void Editor_MessageRecieved(object thing, MessageEventArgs e)
    {
        string messageString = e.Data;
        parseMessage(messageString);
    }
#endif

    //will need exhaustive list of all topics recieved here, mostly pose for now. 

    private void parseMessage(string inString)
    {
        var N = JSON.Parse(inString);
        Vector3 pos;
        Quaternion rot;
        string ourTopic;
        getMove(out ourTopic, inString, out pos, out rot);

        switch (ourTopic)
        {

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

            case "\"/formatted_grid/intense_pixel\"":
                Debug.Log("got brightest pixel location");
                sm.x = N["msg"]["x"];
                sm.y = N["msg"]["y"];
                sm.width = collisionWidth;
                sm.intensity = N["msg"]["intensity"];
                break;

            case "\"/hololens_experiment/common_points\"":
                Debug.Log("got triangle");
                publishTriangle(Cube1.Instance.transform.position, Cube2.Instance.transform.position, Cube3.Instance.transform.position, inString);
                break;

            case "fail":
                //if in doubt refresh the connection.
                break;

            default:
                Debug.Log("Recieved unexpected topic" + N["topic"].ToString());
                break;
        }
    }

    private void publishTriangle(Vector3 p1, Vector3 p2, Vector3 p3, string inString)
    {
        var N = JSON.Parse(inString); // to get data from the request. 
        N["msg"]["frame_id"] = "hololens";
        N["topic"] = "/hololens/commonPoints";
        N["msg"]["p1"]["x"] = p1.x;
        N["msg"]["p1"]["y"] = 0.0f;
        N["msg"]["p1"]["z"] = p1.z;
        N["msg"]["p2"]["x"] = p2.x;
        N["msg"]["p2"]["y"] = 0.0f;
        N["msg"]["p2"]["z"] = p2.z;
        N["msg"]["p3"]["x"] = p3.x;
        N["msg"]["p3"]["y"] = 0.0f;
        N["msg"]["p3"]["z"] = p3.z;
        Debug.Log(N.ToString());
        Send(N.ToString());
    }//publishTriangle

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
        Send(N.ToString());
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

    private void getMove(out string topic, string inString, out Vector3 pos, out Quaternion quat)
    {
        //being careful with the string, could contain garbage
        try
        {
            var N = JSON.Parse(inString);
            topic = N["topic"].ToString();
            pos.x = N["msg"]["position"]["x"].AsFloat;
            pos.y = N["msg"]["position"]["y"].AsFloat;
            pos.z = N["msg"]["position"]["z"].AsFloat;
            quat.x = N["msg"]["orientation"]["x"].AsFloat;
            quat.y = N["msg"]["orientation"]["y"].AsFloat;
            quat.z = N["msg"]["orientation"]["z"].AsFloat;
            quat.w = N["msg"]["orientation"]["w"].AsFloat;
        }
        catch (System.ArgumentOutOfRangeException e)
        {
            topic = "fail";
            pos.x = 0;
            pos.y = 0;
            pos.z = 0;
            quat.x = 0;
            quat.y = 0;
            quat.z = 0;
            quat.w = 1;
            return;
        }

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

    }//getMove

    public void startPoints()
    {
        TextToSpeechObj.StartSpeaking("started markers");
        isPoints = true;
    }

    public void click()
    {
        TextToSpeechObj.StartSpeaking("click");
        SendNext("wandNext");
        if (isPoints)
        {
            pointClicked = true;
        }
    }

    public void highQual()
    {
        TextToSpeechObj.StartSpeaking("High quality");
        sendQual(true);
    }

    public void lowQual()
    {
        TextToSpeechObj.StartSpeaking("Low quality");
        sendQual(false);
    }

    public void clear()
    {
        TextToSpeechObj.StartSpeaking("clear");
        SendNext("clear");
    }

    public void next()
    {
        Debug.Log("in source next()");
    }

    public void Discon()
    {
        firstChair = false;
        savedRot = new Quaternion(0, 0, 0, 0);
        //Killing connection
#if UNITY_EDITOR
        runThread.Abort();
        Socket.Close();
        con = false;
#endif

#if !UNITY_EDITOR
        messageWebSocket.Dispose();
        messageWebSocket = null;
        con = false;
#endif
        isInit = false;
        TextToSpeechObj.StartSpeaking("disconnected");
        return;
    }

    void OnApplicationQuit()
    {
        this.Discon();
    }

#if UNITY_EDITOR
    public void Send(string str)
    {
        busy = true;

        if (Socket != null && con)
        {
            Socket.Send(str);
        }
        busy = false;

    }//Send
#endif

#if !UNITY_EDITOR
    public async Task Send(string str)
    {
        busy = true;
        await WebSock_SendMessage(messageWebSocket, str);
        busy = false;
    }
#endif

#if !UNITY_EDITOR
    private async Task WebSock_SendMessage(MessageWebSocket webSock, string message)
    {
        dataWriter.WriteString(message);
        await dataWriter.StoreAsync();
    }
#endif

    public void Initialise()
    {
        isInit = true;
#if UNITY_EDITOR
        Socket.OnMessage+= Editor_MessageRecieved;
#endif
        string obs1pub = advertise("/obs1", "geometry_msgs/PoseStamped");
        string holoPingAdv = advertise("/holoPing", "std_msgs/Time");
        string advStr = advertise("/holoPose", "geometry_msgs/PoseStamped");
        string qual = advertise("/qual", "std_msgs/String");
        string advpointClicked = advertise("/pointClicked", "std_msgs/Int32");
        string nextSub = advertise("/holoNext", "std_msgs/String");
        string mapPub = advertise("/mapRaw", "std_msgs/String");
        string trianglePointsPub = advertise("/hololens/commonPoints", "/hololens_experiment/CommonPoints");
        string headGazeSub = advertise("/headGaze", "std_msgs/String");

        string intensePixelSub = subscribe("/formatted_grid/intense_pixel", "hololens_experiment/IntensePixel");
        string trianglePointsSub = subscribe("/hololens_experiment/common_points", "/hololens_experiment/CommonPoints");
        string pingOutSub = subscribe("/pingOut", "std_msgs/Time");

        Debug.Log(headGazeSub);
        Send(headGazeSub);
        Debug.Log(trianglePointsPub);
        Send(trianglePointsPub);
        Debug.Log(trianglePointsSub);
        Send(trianglePointsSub);
        Debug.Log(intensePixelSub);
        Send(intensePixelSub);
        Debug.Log(obs1pub);
        Send(obs1pub);
        Debug.Log(mapPub);
        Send(mapPub);
        Debug.Log(holoPingAdv);
        Send(holoPingAdv);
        Debug.Log(pingOutSub);
        Send(pingOutSub);
        Debug.Log(advpointClicked);
        Send(advpointClicked);
        Debug.Log(advStr);
        Send(advStr);
        Debug.Log(qual);
        Send(qual);
        Debug.Log(nextSub);
        Send(nextSub);

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
        Send(N);

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
        Send(N);
    }//sendObs

    public void SendNext(string type)
    {
        var N = JSON.Parse("{\"op\": \"publish\", \"topic\": \"" + "\"/holoNext\"" + "\",\"type\": \"" + "std_msgs/String" + "\"}");
        N["msg"]["data"] = type;
        Debug.Log(N);
        try
        {
            string tosend = N.ToString();
            Send(tosend);
        }
        catch (System.ArgumentOutOfRangeException e)
        {
            return;
        }

    }//SendNext

    //publishes to the gaze topics, eyehead = 1 is eye gaze, =0 is head gaze.
    public void SendGaze(string obj, bool eyeHead)
    {
        if (con)
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
                Send(tosend);
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
        Send(N);

    }//publishPointClicked

    public void sendQual(bool highLow)
    {
        var N = JSON.Parse("{\"op\": \"publish\", \"topic\": \"" + "\"/qual\"" + "\",\"type\": \"" + "std_msgs/String" + "\"}");
        if (highLow)
        {
            N["msg"]["data"] = "high";
        }
        else
        {
            N["msg"]["data"] = "low";
        }
        // Debug.Log(N.ToString());
        try
        {
            string tosend = N.ToString();
            Send(tosend);
        }
        catch (System.ArgumentOutOfRangeException e)
        {
            return;
        }
    }//sendQual

    public void sendMapRaw(string image)
    {
        string N = "{ \"op\": \"publish\"" +
        ", \"topic\": \"" + "/mapRaw" + "\"" +
        ", \"type\": \"" + "std_msgs/String" + "\"" +
        ", \"msg\":{\"data\": \"" + image + "\"}" +
        "}";

        try
        {
            Send(N);
        }
        catch (System.ArgumentOutOfRangeException e)
        {
            return;
        }

    }//sendMapRaw

}//Source