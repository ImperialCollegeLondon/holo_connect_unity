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
    public GameObject originObj;
    public GameObject holoWorldObj;
    public GameObject manMod;
    public GameObject boardCam;
    public GameObject mapPlane;
    public GameObject camOneMarker;
    public GameObject camTwoMarker;
    public GameObject camThreeMarker;
    public GameObject camFourMarker;
    public GameObject camFiveMarker;
    public GameObject camSixMarker;
    public GameObject camSevenMarker;
    public GameObject camEightMarker;
    public GameObject wheelChairObj;
    public GameObject holoLens;
    public GameObject wheelchairHolder;
    public Material errorMaterial;
    public GameObject joystickVizPlane;
    public GameObject collisionVizPlane;
    public GameObject collisionHolder;
    public GameObject userArrow;
    public GameObject correctedArrow;
    public GameObject obsObj1;
    public GameObject rosWorld;
    public GameObject bestPlane;
    public GameObject mirrorPlane;
    public GameObject cube3;
    public GameObject cube2;
    public GameObject cube1;
    public Camera mainCam;
    public GameObject cursor;
    public GameObject rearViewCamPlaneOverlay;
    public int helpType;
    public int numHelpTypes;

    public RenderTexture mapTex;
    private Texture2D texMapTemp;

    private gripManager gripHandle;

    public TextToSpeech TextToSpeechObj;
    private bool shouldSpeak = false;
    private string speechText = "Write a string to me before playing";
    //Default ROSBridge port
    private int port = 9090;
    public bool con = false;

    private bool isInit = false;
    private bool busy = false;
    private bool isPoints = false;
    private int curPoint = 0;
    private bool pointClicked = false;
    private Vector3 cube1Pos, cube2Pos, cube3Pos;
    //Value that will be send to teleport_absolute
    public float tx, ty;
    private float timeSince = 0;
    private float keepAlive = 0;

    //private values for wheelchair offset 
    private Quaternion wheelChairRotSaved;
    private Vector3 wheelChairPosSaved;
    private Quaternion initialRot;
    private Vector3 initialPos;
    private Quaternion savedRot;
    private Vector3 savedPos;
    private bool firstChair = true;
    private float errorMetric = 0;
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
    string byteText;
    string byteTextMap;
    string byteTextJoystickViz;
    string byteTextCollisionViz;
    string byteTextMirror;
    byte[] decodedBytes;
    byte[] decodedBytesCollisionViz;
    byte[] decodedBytesJoystickViz;
    byte[] decodedBytesMirror;
    string encodedBytes;
    Texture2D tex;
    Texture2D solvedMapTex;
    Texture2D texJoyStickViz;
    Texture2D texCollisionViz;
    Texture2D texMirror;
    int positionTex = 0;
    bool texNeedUpdate = false;
    bool mapTexNeedUpdate = false;
    bool mirrorNeedUpdate = false;
    bool joystickVizNeedUpdate = false;
    bool collisionVizNeedUpdate = false;
    int collisionWidth =300;

    //variables for clock sync 
    int rosSecs = 0;
    int rosNSecs = 0;
    float lag = 0;
    float myTime = 0;
    double startSecsFilt = 0;
    double startNSecsFilt = 0;
    DateTime startTime;
    DateTime currTime;
    TimeSpan runTime;
    int mySecs = 0;
    int myNSecs = 0;
    int frameCount = 0;

    twistArrowControler userArrowController;
    twistArrowControler correctedArrowController;
    float planeA;
    float planeB;
    float planeC;
    float planeD;

    public GameObject soundObj;
    soundMover sm;

    bool allViz = true;

    void Update()
    {
        name = getObject(holoLens.transform.position, holoLens.transform.position + holoLens.transform.forward);
        Debug.Log(name);
        cube1Pos = cube1.transform.position;
        cube2Pos = cube2.transform.position;
        cube3Pos = cube3.transform.position;
        errorMaterial.SetFloat("_Transparency", errorMetric);
        
        Renderer userArrowRen = userArrow.GetComponentsInChildren<Renderer>()[0];
        Renderer correctedArrowRen = correctedArrow.GetComponentsInChildren<Renderer>()[0];
        Renderer mirrorPlaneRen = mirrorPlane.GetComponent<Renderer>();
        Renderer collisionVizPlaneRen = collisionVizPlane.GetComponent<Renderer>();
        Renderer rearViewCamPlaneOverlayRen = rearViewCamPlaneOverlay.GetComponent<Renderer>();
        Renderer cube1Ren = cube1.GetComponent<Renderer>();
        Renderer cube2Ren = cube2.GetComponent<Renderer>();
        Renderer cube3Ren = cube3.GetComponent<Renderer>();

        if (allViz)
        {

            userArrowRen.enabled = true;
            correctedArrowRen.enabled = true;
            mirrorPlaneRen.enabled = true;
            collisionVizPlaneRen.enabled = true;
            rearViewCamPlaneOverlayRen.enabled = true;
            cube1Ren.enabled = true;
            cube2Ren.enabled = true;
            cube3Ren.enabled = true;

        }
        else
        {
            userArrowRen.enabled = false;
            correctedArrowRen.enabled = false;
            mirrorPlaneRen.enabled = false;
            collisionVizPlaneRen.enabled = false;
            rearViewCamPlaneOverlayRen.enabled = false;
            cube1Ren.enabled = false;
            cube2Ren.enabled = false;
            cube3Ren.enabled = false;
        }


        if (firstChair && !savedRot.Equals(new Quaternion(0, 0, 0, 0)))
        {
            firstChair = false;
            wheelChairPosSaved = savedPos;
            wheelChairRotSaved = savedRot;
            initialPos = holoLens.transform.position;
            initialRot = holoLens.transform.rotation;
            //change the initial rot to be the closest rotation with only a Y component. 
            Vector3 inVect = new Vector3(1, 0, 0);
            Vector3 outVect = inVect.RotateAround(new Vector3(0, 0, 0), initialRot);
            outVect.y = 0;
            Vector2 inVect2;
            inVect2.x = inVect.x;
          //  inVect2.y = inVect.z;
          //  float pheta = Vector2.Angle(new Vector2(1, 0), inVect2);// + (3.14f / 2f);
          //  initialRot = new Quaternion(0f, 0f, 0f, 1f);
          //  initialRot = Quaternion.AngleAxis(pheta, new Vector3(0, 1, 0));
         //   rosWorld.transform.position = -wheelChairPosSaved.RotateAround(new Vector3(0, 0, 0), wheelChairRotSaved);
       //     rosWorld.transform.rotation = Quaternion.Inverse(wheelChairRotSaved);

        }

        //Debug.Log(1/Time.deltaTime);
        //the loadIMage function is slow. It seems that uploading the images is the problem, using low resolution texture works well. maybe set pixels using sub images is workable. or go with a representational visualisation that is low res. 
        if (texNeedUpdate)
        {
            texNeedUpdate = false;

            decodeTexture(byteText);
            //float start = Time.realtimeSinceStartup;
            tex.LoadImage(decodedBytes);
            //Debug.Log(Time.realtimeSinceStartup -start);
            Renderer manRenderer = manMod.GetComponent<Renderer>();
            manRenderer.material.mainTexture = tex;



        }
        if (mapTexNeedUpdate)
        {
            mapTexNeedUpdate = false;
            decodedBytes = Convert.FromBase64String(byteTextMap.Substring(1, byteTextMap.Length - 2));
            solvedMapTex.LoadImage(decodedBytes);
            Renderer mapRenderer = mapPlane.GetComponent<Renderer>();
            mapRenderer.material.mainTexture = solvedMapTex;
        }
        if (joystickVizNeedUpdate && frameCount % 2 == 0)
        {
            joystickVizNeedUpdate = false;
            decodedBytesJoystickViz = Convert.FromBase64String(byteTextJoystickViz.Substring(1, byteTextJoystickViz.Length - 2));
            texJoyStickViz.LoadImage(decodedBytesJoystickViz);
            Renderer joystickRenderer = joystickVizPlane.GetComponent<Renderer>();
            joystickRenderer.material.mainTexture = texJoyStickViz;
        }
        if (collisionVizNeedUpdate && frameCount % 2 == 1)
        {
            collisionVizNeedUpdate = false;
            decodedBytesCollisionViz = Convert.FromBase64String(byteTextCollisionViz.Substring(1, byteTextCollisionViz.Length - 2));
            texCollisionViz.LoadImage(decodedBytesCollisionViz);
            Renderer collisionRenderer = collisionVizPlane.GetComponent<Renderer>();
            collisionWidth = texCollisionViz.width;
            collisionRenderer.material.mainTexture = texCollisionViz;
               Vector3 thisPos = wheelchairHolder.transform.position;
            thisPos.y = wheelchairHolder.transform.position.y;
            collisionHolder.transform.position = thisPos;
            collisionHolder.transform.rotation=(wheelchairHolder.transform.rotation);
        }

        if (mirrorNeedUpdate && frameCount % 2 == 1 && byteTextMirror.Length !=0)
        {
            mirrorNeedUpdate = false;
            decodedBytesMirror = Convert.FromBase64String(byteTextMirror.Substring(1, byteTextMirror.Length - 2));
            texMirror.LoadImage(decodedBytesMirror);
            Renderer mirrorRenderer = mirrorPlane.GetComponent<Renderer>();
            mirrorRenderer.material.mainTexture = texMirror;
        }


        if (shouldSpeak)
        {
            shouldSpeak = false;
            var textToSpeechObj = this.GetComponent<TextToSpeech>();
            //textToSpeechObj.SpeakSsml(speechText);
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

        //Reset in Unity Play mode
        if (Input.GetKeyDown(KeyCode.I))
        {
                SendCal();
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
         //BroadcastMessage("next");
        }
        if (Input.GetKeyDown(KeyCode.M))
        {
               
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
                if (frameCount % 60 == 0)
                {
                   // encodeTexture(mapTex);
                }
                if (frameCount % 60 == 1)
                {
                   // sendMapRaw(encodedBytes);
                }

            }
            //publish the head location. 

            //Accessing ROS service turtle1/teleport_absolute to update turtle position
            //SendService("/turtle1/teleport_absolute", "{\"x\": " + tx + ", \"y\": " + ty + ", \"theta\": 0}");
        }
        else
        {

        }


    }

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
    }

    //Successfull network connection handler on HL
#if !UNITY_EDITOR
    public void NetworkConnectedHandler(IAsyncAction asyncInfo, AsyncStatus status)
    {
        // Status completed is successful.
        if (status == AsyncStatus.Completed)
        {
            //Guarenteed connection
            con = true;
            speak("connected");

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
        Socket.OnOpen += (sender,e) => {
            con = true;
            busy = false;
            Debug.Log("connected!");
        };
        Socket.Connect();

        //con = true;

        //Socket.OnMessage +=Editor_MessageRecieved;
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
private void Editor_MessageRecieved(object thing,MessageEventArgs e){
    string messageString = e.Data;
    parseMessage(messageString);
}
#endif

    //will need exhaustive list of all topics recieved here, mostly pose for now. 
    bool imTextDecoding = false;

    private void parseMessage(string inString)
    {
        var N = JSON.Parse(inString);
        //Debug.Log(inString);
        Vector3 pos;
        Quaternion rot;
        string ourTopic;
        getMove(out ourTopic, inString, out pos, out rot);
        switch (ourTopic)
        {
            case "\"/origin\"":

                keepAlive = 0;
                //Debug.Log(pos);
        //        tmOrigin.moveToPos = pos;
       //         tmOrigin.moveToRot = rot;
                break;
            case "\"/holoWorld\"":
                //Debug.Log(pos);
                tmHoloWorld.moveToPos = pos;
                tmHoloWorld.moveToRot = rot;
                
                // tmOrigin.moveToPos= pos;
                // tmOrigin.moveToRot= rot;
                break;
            case "\"/errorMetric\"":
                errorMetric = N["msg"]["data"];

                break;
            case "\"/wheelChairPose\"":
                savedPos = pos;
                pos.y = 0;
                savedRot = rot; //have to save them so that they can be accessed in update, where I am allowed to access to the other transforms.
                tmWheelChair.moveToPos =pos;
                tmWheelChair.moveToRot = rot;

                break;
            case "\"/speech\"":
                speakMsg(inString);
                break;
            case "\"/imText\"":


                texNeedUpdate = true;
                Debug.Log("decoding");
                if (N["msg"]["data"].ToString().Length != 0)
                {
                    byteText = N["msg"]["data"].ToString();
                }





                break;
            case "\"/cameraPosArr\"":

                parseArray(inString);
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
            case "\"/collisionVisText\"":
                if (N["msg"]["data"].ToString().Length != 0)
                {
                    Debug.Log("got colision Image");
                    collisionVizNeedUpdate = true;
                    byteTextCollisionViz = N["msg"]["data"].ToString();
                }

                break;
            case "\"/joystickVisText\"":
                if (N["msg"]["data"].ToString().Length != 0)
                {
                    // Debug.Log("got joystick im");
                   // joystickVizNeedUpdate = true;
                   // byteTextJoystickViz = N["msg"]["data"].ToString();
                    //Debug.Log(byteTextJoystickViz);
                }

                break;
            case "\"/mirrorText\"":
                if (N["msg"]["data"].ToString().Length != 0)
                {
                    // Debug.Log("got joystick im");
                    mirrorNeedUpdate = true;
                    byteTextMirror = N["msg"]["data"].ToString();
                    //Debug.Log(byteTextJoystickViz);
                }

                break;
            case "\"/navigation/main_js_cmd_vel\"":

                userArrowController.angular = N["msg"]["angular"]["z"];
                userArrowController.linear = N["msg"]["linear"]["x"];

                break;
            case "\"/arta/cmd_vel\"":
                Debug.Log("gotcorrected");
                correctedArrowController.angular = N["msg"]["angular"]["z"];
                correctedArrowController.linear = N["msg"]["linear"]["x"];
                break;
            case "\"bestPlane\"":
                Debug.Log("got best plane");
                planeM.A = N["msg"]["data"][0];
                planeM.B = N["msg"]["data"][1];
                planeM.C = N["msg"]["data"][2];
                planeM.D = N["msg"]["data"][3];


                Debug.Log(planeA.ToString() + " " + planeB.ToString() + " " + planeC.ToString() + " " + planeD.ToString());
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
                publishTriangle(cube1Pos, cube2Pos, cube3Pos,inString);
                break;

            case "\"/holoRosOffset\"":
                Debug.Log("got hololens/ros offset");
                Vector3 swappedPos = new Vector3(pos.x, pos.y, pos.z);
                Quaternion swappedQuaternion = new Quaternion(rot.x, rot.y, rot.z, rot.w);
                tmHoloWorld.moveToPos = swappedPos;
                tmHoloWorld.moveToPos.y = cube1Pos.y;
                tmHoloWorld.moveToRot =(swappedQuaternion);
                break;
            case "\"/allViz\"":
                if (N["msg"]["data"].ToString().Contains("true"))
                {
                    allViz = true;
                }
                else
                {
                    allViz = false;
                }
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
        N["msg"]["p3"]["y"] =0.0f;
        N["msg"]["p3"]["z"] = p3.z;
        Debug.Log(N.ToString());
        Send(N.ToString());
    }


    private void calcBaseTime()
    {


        int lagSecs = (int)lag;
        int lagNSecs = (int)((lag - lagSecs) / 1e9);
        int startSecs = rosSecs + lagSecs - mySecs;  //the time the program started on the ros clock is the time ros said plus the expected delay getting here minus the time we have been runnign
        int startNSecs = rosNSecs + lagNSecs - myNSecs;
        double ratio = 0.99;
        //Debug.Log("sec:" + mySecs + "  nsecs:" + myNSecs);
        double error = (startSecsFilt - startSecs) + (startNSecsFilt - startNSecs) * 1e-9;
        //Debug.Log(error);
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
    }

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
        Send(N.ToString());

    }

    private void decodeTexture(string inString)
    {


        try
        {
            // Debug.Log(inString.Length);
            decodedBytes = Convert.FromBase64String(inString.Substring(1, inString.Length - 2));
            // string decodedText = Encoding.UTF8.GetString (decodedBytes);
            // Debug.Log(decodedText);
            //Debug.Log(decodedBytes);
            //Debug.Log("decoded Bytes");


        }
        catch (System.FormatException e)
        {
            Debug.Log(e);

        }


    }

    private void encodeTexture(RenderTexture rtex)
    {


        //      try
        //        {
        //rtex = RenderTexture.active;
        RenderTexture.active = rtex;
        //if(tex)

        texMapTemp.ReadPixels(new UnityEngine.Rect(0, 0, texMapTemp.width, texMapTemp.height), 0, 0);
        byte[] pngTex = texMapTemp.EncodeToPNG();
        encodedBytes = Convert.ToBase64String(pngTex);
        //Convert.ToBase64CharArray(pngTex,0,pngTex.Length,encodedBytes,0);


        //}
        //  catch (System.FormatException e)
        //    {
        //          Debug.Log(e);

        //       }


    }

    private void parseArray(string inString)
    {
        tm1.moveToPos = getPos(inString, 0);
        tm2.moveToPos = getPos(inString, 1);
        tm3.moveToPos = getPos(inString, 2);
        tm4.moveToPos = getPos(inString, 3);
        tm5.moveToPos = getPos(inString, 4);
        //Debug.Log(tm5.moveToPos);
        tm6.moveToPos = getPos(inString, 5);
        tm7.moveToPos = getPos(inString, 6);
        tm8.moveToPos = getPos(inString, 7);
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

    private int sendFlag = 0;
    private triggerManager tmOrigin;
    private triggerManager tmHoloWorld;
    private triggerManager tmWheelChair;
    private triggerManager tm1;
    private triggerManager tm2;
    private triggerManager tm3;
    private triggerManager tm4;
    private triggerManager tm5;
    private triggerManager tm6;
    private triggerManager tm7;
    private triggerManager tm8;
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

        //Debug.Log(N["msg"]["position"]["x"].ToString());

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
    public void startGrip()
    {
        speak("Started Grip");
    }
    public void endGrip()
    {
        speak("ended Grip");
        SendGrip();
    }

    public void startPoints()
    {
        speak("started markers");
        isPoints = true;
    }
    public void click()
    {
        boardCam.SetActive(false);
        speak("click");
        SendNext("wandNext");
        if (isPoints)
        {
            pointClicked = true;
        }
    }

    public void highQual()
    {
        speak("High quality");
        sendQual(true);
    }

    public void lowQual()
    {
        speak("Low quality");
        sendQual(false);
    }
    public void clear()
    {
        speak("clear");
        SendNext("clear");
    }

    public void next()
    {
        //speak("next");
        Debug.Log("in source next()");
        ////SendNext("next");
        //helpType++;
        //helpType = helpType % numHelpTypes;

        //if(helpType == 0)
        //{
        //    //make sound
        //}
        //if(helpType == 1)
        //{
        //    //make arrow
        //}
        //if(helpType == 2)
        //{
        //    //make map
        //}
        //if(helpType == 3)
        //{
        //    //floor markings
        //}


    }
    planeManager planeM;
    public void Start()
    {
        texMapTemp = new Texture2D(256, 256);
        startTime = DateTime.Now;
        tex = new Texture2D(2, 2);
        solvedMapTex = new Texture2D(2, 2);
        texCollisionViz = new Texture2D(2, 2);
        texMirror = new Texture2D(2, 2);
        texJoyStickViz = new Texture2D(2, 2);
        gripHandle = this.GetComponent<gripManager>();
        tmOrigin = originObj.GetComponent<triggerManager>();
        tmHoloWorld = holoWorldObj.GetComponent<triggerManager>();
        tm1 = camOneMarker.GetComponent<triggerManager>();
        tm2 = camTwoMarker.GetComponent<triggerManager>();
        tm3 = camThreeMarker.GetComponent<triggerManager>();
        tm4 = camFourMarker.GetComponent<triggerManager>();
        tm5 = camFiveMarker.GetComponent<triggerManager>();
        tm6 = camSixMarker.GetComponent<triggerManager>();
        tm7 = camSevenMarker.GetComponent<triggerManager>();
        tm8 = camEightMarker.GetComponent<triggerManager>();
        tmWheelChair = wheelchairHolder.GetComponent<triggerManager>();
        userArrowController = userArrow.GetComponent<twistArrowControler>();
        correctedArrowController = correctedArrow.GetComponent<twistArrowControler>();
        planeM = bestPlane.GetComponent<planeManager>();
        sm = soundObj.GetComponent<soundMover>();


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
        speak("disconnected");
        return;
    }


    void OnApplicationQuit()
    {
        this.Discon();
    }



#if UNITY_EDITOR
    public void Send(string str){
        busy = true;

        if (Socket != null && con)
        {

            Socket.Send(str);
        }
            busy = false;
}
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
        string bestPlaneSub = subscribe("bestPlane", "std_msgs/Float32MultiArray");
        string obs1pub = advertise("/obs1", "geometry_msgs/PoseStamped");
        string userTwistSub = subscribe("/navigation/main_js_cmd_vel", "geometry_msgs/Twist");
        string correctedTwistSub = subscribe("/arta/cmd_vel", "geometry_msgs/Twist");
        string lagSub = subscribe("/lagOut", "std_msgs/Float32");
        string errorMetricSub = subscribe("/errorMetric", "std_msgs/Float32");
        string holoPingAdv = advertise("/holoPing", "std_msgs/Time");
        string pingOutSub = subscribe("/pingOut", "std_msgs/Time");
        string advStr = advertise("/holoPose", "geometry_msgs/PoseStamped");
        string advReCal = advertise("/reCalibrate", "std_msgs/String");
        string qual = advertise("/qual", "std_msgs/String");
        string advpointClicked = advertise("/pointClicked", "std_msgs/Int32");
        string originSub = subscribe("/origin", "geometry_msgs/Pose");
        string holoWorldSub = subscribe("/holoWorld", "geometry_msgs/Pose");
        string wheelChairSub = subscribe("/wheelChairPose", "geometry_msgs/Pose");
        string speechSub = subscribe("/speech", "std_msgs/String");
        string imTextSub = subscribe("/imText", "std_msgs/String");
        string arraySub = subscribe("/cameraPosArr", "geometry_msgs/PoseArray");
        string nextSub = advertise("/holoNext", "std_msgs/String");
        string mapPub = advertise("/mapRaw", "std_msgs/String");
        string collisionVizSub = subscribe("/collisionVisText", "std_msgs/String");
        string joystickVizSub = subscribe("/joystickVisText", "std_msgs/String");
        string intensePixelSub = subscribe("/formatted_grid/intense_pixel", "hololens_experiment/IntensePixel");
        string mirrorSub = subscribe("/mirrorText", "std_msgs/String");
        string trianglePointsSub = subscribe("/hololens_experiment/common_points", "/hololens_experiment/CommonPoints");
        string trianglePointsPub = advertise("/hololens/commonPoints", "/hololens_experiment/CommonPoints");
        string holoRosOffset = subscribe("/holoRosOffset", "geometry_msgs/Pose");
        string headGazeSub = advertise("/headGaze", "std_msgs/String");  
        string allVizSub = subscribe("/allViz", "std_msgs/String");

        Debug.Log(allVizSub);
        Send(allVizSub);
        Debug.Log(headGazeSub);
        Send(headGazeSub);
        Debug.Log(holoRosOffset);
        Send(holoRosOffset);
        Debug.Log(trianglePointsPub);
        Send(trianglePointsPub);
        Debug.Log(trianglePointsSub);
        Send(trianglePointsSub);
        Debug.Log(mirrorSub);
        Send(mirrorSub);
        Debug.Log(intensePixelSub);
        Send(intensePixelSub);
        Debug.Log(bestPlaneSub);
        Send(bestPlaneSub);
        Debug.Log(userTwistSub);
        Send(obs1pub);
        Debug.Log(obs1pub);
        Send(userTwistSub);
        Debug.Log(correctedTwistSub);
        Send(correctedTwistSub);
        Debug.Log(collisionVizSub);
        Send(collisionVizSub);
        Debug.Log(joystickVizSub);
        Send(joystickVizSub);
        Debug.Log(errorMetricSub);
        Send(errorMetricSub);
        Debug.Log(wheelChairSub);
        Send(wheelChairSub);
        Debug.Log(mapPub);
        Send(mapPub);
        Debug.Log(lagSub);
        Send(lagSub);
        Debug.Log(holoPingAdv);
        Send(holoPingAdv);
        Debug.Log(pingOutSub);
        Send(pingOutSub);
        Debug.Log(advpointClicked);
        Send(advpointClicked);
        Debug.Log(advStr);
        Send(advStr);
        Debug.Log(holoWorldSub);
        Send(holoWorldSub);
        Debug.Log(originSub);
        Send(originSub);
        Debug.Log(advReCal);
        Send(advReCal);
        Send(speechSub);
        Debug.Log(arraySub);
        Send(arraySub);
        Debug.Log(imTextSub);
        Send(imTextSub);
        Debug.Log(qual);
        Send(qual);
        Debug.Log(nextSub);
        Send(nextSub);
    }

    /*
    public void SendPose(){
        //head pose;
        var headPosition = Camera.main.transform.position;
        var headRotation = Camera.main.transform.rotation;
         var N = JSON.Parse("{\"op\": \"publish\", \"topic\": \"" + "/holoPose" +"\",\"type\": \"" + "geometry_msgs/Pose" + "\"}"); 
        N["op"] = "publish";
        N["topic"] = "/holoPose";
            number = number +1 ;
        N["msg"]["position"]["x"]= headPosition.x+ number;
        N["msg"]["position"]["y"]= headPosition.y;
        N["msg"]["position"]["z"]= headPosition.z;


        N["msg"]["orientation"]["x"]= headRotation.x;
        N["msg"]["orientation"]["y"]= headRotation.y;
        N["msg"]["orientation"]["z"]= headRotation.z;
        N["msg"]["orientation"]["w"]= headRotation.w;
       // Debug.Log(N.ToString());
        try{
            string x = "a";
            string y = "b";
            while ( x != y) {
                y = x;
                x = N.ToString();
            }

            Debug.Log(y);
            Send(y);

            //Debug.Log(N);
            // string tosend = String.Copy(N.ToString());
            //Debug.Log(tosend);
            //Send(tosend);
        }
        catch(System.ArgumentOutOfRangeException e){
                return;
        }
    }
    */
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
            //name = hitObj.name;
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
        //Debug.Log(N);
        Send(N);
    }
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
        //Debug.Log(N);
        Send(N);
    }
    public void SendGrip()
    {
        var gripPos = gripHandle.gripVec;
        var gripRot = gripHandle.gripRot;

        var N = "{ \"op\": \"publish\"" +
                ", \"topic\": \"" + "/grip" + "\"" +
                ", \"type\": \"" + "geometry_msgs/Pose" + "\"" +
                ", \"msg\": " +
                    "{ \"position\": " +
                        "{ \"x\": " + gripPos.x.ToString() +
                        ", \"y\": " + gripPos.y.ToString() +
                        ", \"z\": " + gripPos.z.ToString() +
                        "}" +
                    ", \"orientation\": " +
                        "{ \"x\": " + gripRot.x.ToString() +
                        ", \"y\": " + gripRot.y.ToString() +
                        ", \"z\": " + gripRot.z.ToString() +
                        ", \"w\": " + gripRot.w.ToString() +
                        "}" +
                    "}" +
                "}";
        Send(N);
    }

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
    }

    //publishes to the gaze topics, eyehead = 1 is eye gaze, =0 is head gaze.
    public void SendGaze(string obj, bool eyeHead)
    {
        if (con)
        {
            //var N1 = JSON.Parse("{\"op\": \"publish\", \"topic\": \"" + "\"/holoNext\"" + "\",\"type\": \"" + "std_msgs/String" + "\"}");
            //Debug.Log(N1.ToString());
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
    }
    public void publishPointClicked(int i)
    {

        var N = "{ \"op\": \"publish\"" +
                ", \"topic\": \"" + "/pointClicked" + "\"" +
                ", \"type\": \"" + "std_msgs/Int32" + "\"" +
                ",\"msg\":{\"data\": " + i.ToString() + "}" +
                "}";
        Send(N);

    }


    public void SendCal()
    {
        //enable visualisation of camera 
        boardCam.SetActive(true);
        //head pose;
        var headPosition = Camera.main.transform.position;
        var headRotation = Camera.main.transform.rotation;
        var N = JSON.Parse("{\"op\": \"publish\", \"topic\": \"" + "\"/reCalibrate\"" + "\",\"type\": \"" + "std_msgs/String" + "\"}");
        N["msg"]["data"] = "doCal!";
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
    }
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
    }


    public void sendMapRaw(string image)
    {

        //var N = JSON.Parse("{\"op\": \"publish\", \"topic\": \"" + "\"/mapRaw\"" + "\",\"type\": \"" + "std_msgs/String" + "\"}");
        string N = "{ \"op\": \"publish\"" +
        ", \"topic\": \"" + "/mapRaw" + "\"" +
        ", \"type\": \"" + "std_msgs/String" + "\"" +
        ", \"msg\":{\"data\": \"" + image + "\"}" +
        "}";

        //N["msg"]["data"] = image;

        // Debug.Log(N.ToString());
        try
        {
            //string tosend = N.ToString();
            Send(N);
        }
        catch (System.ArgumentOutOfRangeException e)
        {
            return;
        }
    }
}