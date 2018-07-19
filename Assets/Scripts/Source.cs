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
    public GameObject holoWorldObj;
    public GameObject wheelChairObj;
    public GameObject holoLens;
    public GameObject wheelchairHolder;
    public Material errorMaterial;
    public GameObject collisionVizPlane;
    public GameObject collisionHolder;
    public GameObject userArrow;
    public GameObject correctedArrow;
    public GameObject obsObj1;
    public GameObject bestPlane;
    public GameObject mirrorPlane;
    public GameObject cube3;
    public GameObject cube2;
    public GameObject cube1;
    public Camera mainCam;
    public GameObject rearViewCamPlaneOverlay;
    public int helpType;
    public int numHelpTypes;

    public RenderTexture mapTex;

    private gripManager gripHandle;

    public TextToSpeech TextToSpeechObj;
    private bool shouldSpeak = false;
    private string speechText = "Write a string to me before playing";

    private bool isInit = false;

    private bool isPoints = false;
    private int curPoint = 0;
    private bool pointClicked = false;
    private Vector3 cube1Pos, cube2Pos, cube3Pos;

    //Value that will be send to teleport_absolute
    public float tx, ty;

    //private values for wheelchair offset 
    private Quaternion initialRot;
    private Quaternion savedRot;
    private Vector3 savedPos;
    private bool firstChair = true;
    private float errorMetric = 0;

    string byteText;
    string byteTextMap;
    string byteTextJoystickViz;
    string byteTextCollisionViz;
    string byteTextMirror;
    byte[] decodedBytesCollisionViz;
    byte[] decodedBytesJoystickViz;
    byte[] decodedBytesMirror;
    Texture2D texCollisionViz;
    Texture2D texMirror;
    bool mirrorNeedUpdate = false;
    bool collisionVizNeedUpdate = false;
    int collisionWidth = 300;

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

    private triggerManager tmHoloWorld;
    private triggerManager tmWheelChair;

    planeManager planeM;

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
            initialRot = holoLens.transform.rotation;
            //change the initial rot to be the closest rotation with only a Y component. 
            Vector3 inVect = new Vector3(1, 0, 0);
            Vector3 outVect = inVect.RotateAround(new Vector3(0, 0, 0), initialRot);
            outVect.y = 0;
            Vector2 inVect2;
            inVect2.x = inVect.x;
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
        texCollisionViz = new Texture2D(2, 2);
        texMirror = new Texture2D(2, 2);
        gripHandle = this.GetComponent<gripManager>();
        tmHoloWorld = holoWorldObj.GetComponent<triggerManager>();
        tmWheelChair = wheelchairHolder.GetComponent<triggerManager>();
        userArrowController = userArrow.GetComponent<twistArrowControler>();
        correctedArrowController = correctedArrow.GetComponent<twistArrowControler>();
        planeM = bestPlane.GetComponent<planeManager>();
        sm = soundObj.GetComponent<soundMover>();
    }

    //will need exhaustive list of all topics recieved here, mostly pose for now. 

    public void parseMessage(string inString)
    {
        var N = JSON.Parse(inString);
        Vector3 pos;
        Quaternion rot;
        string ourTopic;
        getMove(out ourTopic, inString, out pos, out rot);

        switch (ourTopic)
        {
            case "\"/holoWorld\"":
                tmHoloWorld.moveToPos = pos;
                tmHoloWorld.moveToRot = rot;
                break;

            case "\"/errorMetric\"":
                errorMetric = N["msg"]["data"];
                break;

            case "\"/wheelChairPose\"":
                savedPos = pos;
                pos.y = 0;
                savedRot = rot; //have to save them so that they can be accessed in update, where I am allowed to access to the other transforms.
                tmWheelChair.moveToPos = pos;
                tmWheelChair.moveToRot = rot;
                break;

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

                }
                break;

            case "\"/mirrorText\"":
                if (N["msg"]["data"].ToString().Length != 0)
                {
                    mirrorNeedUpdate = true;
                    byteTextMirror = N["msg"]["data"].ToString();
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
                publishTriangle(cube1Pos, cube2Pos, cube3Pos, inString);
                break;

            case "\"/holoRosOffset\"":
                Debug.Log("got hololens/ros offset");
                Vector3 swappedPos = new Vector3(pos.x, pos.y, pos.z);
                Quaternion swappedQuaternion = new Quaternion(rot.x, rot.y, rot.z, rot.w);
                tmHoloWorld.moveToPos = swappedPos;
                tmHoloWorld.moveToPos.y = cube1Pos.y;
                tmHoloWorld.moveToRot = (swappedQuaternion);
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
        N["msg"]["p3"]["y"] = 0.0f;
        N["msg"]["p3"]["z"] = p3.z;
        Debug.Log(N.ToString());
        RosMessenger.Instance.Send(N.ToString());
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
        Debug.Log("in source next()");
    }


    void OnApplicationQuit()
    {
        RosMessenger.Instance.Disconnect();
    }

    public void Initialise()
    {
        isInit = true;

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
        RosMessenger.Instance.Send(allVizSub);
        Debug.Log(headGazeSub);
        RosMessenger.Instance.Send(headGazeSub);
        Debug.Log(holoRosOffset);
        RosMessenger.Instance.Send(holoRosOffset);
        Debug.Log(trianglePointsPub);
        RosMessenger.Instance.Send(trianglePointsPub);
        Debug.Log(trianglePointsSub);
        RosMessenger.Instance.Send(trianglePointsSub);
        Debug.Log(mirrorSub);
        RosMessenger.Instance.Send(mirrorSub);
        Debug.Log(intensePixelSub);
        RosMessenger.Instance.Send(intensePixelSub);
        Debug.Log(bestPlaneSub);
        RosMessenger.Instance.Send(bestPlaneSub);
        Debug.Log(userTwistSub);
        RosMessenger.Instance.Send(obs1pub);
        Debug.Log(obs1pub);
        RosMessenger.Instance.Send(userTwistSub);
        Debug.Log(correctedTwistSub);
        RosMessenger.Instance.Send(correctedTwistSub);
        Debug.Log(collisionVizSub);
        RosMessenger.Instance.Send(collisionVizSub);
        Debug.Log(joystickVizSub);
        RosMessenger.Instance.Send(joystickVizSub);
        Debug.Log(errorMetricSub);
        RosMessenger.Instance.Send(errorMetricSub);
        Debug.Log(wheelChairSub);
        RosMessenger.Instance.Send(wheelChairSub);
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
        Debug.Log(holoWorldSub);
        RosMessenger.Instance.Send(holoWorldSub);
        Debug.Log(originSub);
        RosMessenger.Instance.Send(originSub);
        Debug.Log(advReCal);
        RosMessenger.Instance.Send(advReCal);
        RosMessenger.Instance.Send(speechSub);
        Debug.Log(arraySub);
        RosMessenger.Instance.Send(arraySub);
        Debug.Log(imTextSub);
        RosMessenger.Instance.Send(imTextSub);
        Debug.Log(qual);
        RosMessenger.Instance.Send(qual);
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
            RosMessenger.Instance.Send(tosend);
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
            RosMessenger.Instance.Send(N);
        }
        catch (System.ArgumentOutOfRangeException e)
        {
            return;
        }

    }//sendMapRaw

}//Source