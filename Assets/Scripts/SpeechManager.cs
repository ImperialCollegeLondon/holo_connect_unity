﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class SpeechManager : MonoBehaviour
{
    KeywordRecognizer keywordRecognizer = null;
    Dictionary<string, System.Action> keywords = new Dictionary<string, System.Action>();

    public GameObject camOneMarker;
    public GameObject camTwoMarker;
    public GameObject camThreeMarker;
    public GameObject camFourMarker;
    public GameObject camFiveMarker;
    public GameObject camSixMarker;
    public GameObject camSevenMarker;
    public GameObject camEightMarker;
    public GameObject wand;
    public GameObject sourceHandle;
    public GameObject mainCamera;

    // Use this for initialization
    void Start()
    {
        //sourceHandle = root.GetComponent<Source>();
        keywords.Add("Reset world", () =>
        {
            // Call the OnReset method on every descendant object.
            this.BroadcastMessage("OnReset");
        });

        keywords.Add("Drop Sphere", () =>
        {
            var focusObject = GazeGestureManager.Instance.FocusedObject;
            if (focusObject != null)
            {
                // Call the OnDrop method on just the focused object.
                focusObject.SendMessage("OnDrop");
            }
        });

        keywords.Add("Camera one", () =>
        {
            camOneMarker.SendMessage("OnTrigger");
        });
        keywords.Add("Camera two", () =>
        {
           	camTwoMarker.SendMessage("OnTrigger");
        });
        keywords.Add("Camera three", () =>
        {
       		camThreeMarker.SendMessage("OnTrigger");
        });
        keywords.Add("Camera four", () =>
        {
       		camFourMarker.SendMessage("OnTrigger");
        });
        keywords.Add("Camera five", () =>
        {
            camFiveMarker.SendMessage("OnTrigger");
        });
        keywords.Add("Camera six", () =>
        {
           	camSixMarker.SendMessage("OnTrigger");
        });
        keywords.Add("Camera seven", () =>
        {
            camSevenMarker.SendMessage("OnTrigger");
        });
        keywords.Add("Camera eight", () =>
        {
           	camEightMarker.SendMessage("OnTrigger");
        });
        keywords.Add("grip", () =>
        {
            this.BroadcastMessage("startGrip");
        });

        keywords.Add("end grip", () =>
        {
            this.BroadcastMessage("endGrip");
        });

        keywords.Add("markers", () =>
        {
            this.BroadcastMessage("startPoints");
        });
        keywords.Add("click", () =>
        {
            this.BroadcastMessage("click");
        });

        keywords.Add("Place", () =>
        {

                // Call the OnDrop method on just the focused object.
           	camOneMarker.SendMessage("OnPlace");
           	camTwoMarker.SendMessage("OnPlace");

        });

        keywords.Add("restart", () =>
        {

                // Call the OnDrop method on just the focused object.
           	camOneMarker.SendMessage("OnRestart");
           	camTwoMarker.SendMessage("OnRestart");

        });

        keywords.Add("connect", () =>
        {
            //sourceHandle.WantToCon = true;
            // Call the OnDrop method on just the focused object.

            this.BroadcastMessage("Connect", "10.0.0.213");
        });
        keywords.Add("disconnect", () =>
        {
                // Call the OnDrop method on just the focused object.
           	this.BroadcastMessage("Discon");
        });
        keywords.Add("chicken", () =>
        {
            // Call the OnDrop method on just the focused object.
            // this.BroadcastMessage("SendCal");
            mainCamera.BroadcastMessage("CalibratePupil");
        });
        keywords.Add("high", () =>
        {
                // Call the OnDrop method on just the focused object.
            this.BroadcastMessage("highQual");
        });
        keywords.Add("low", () =>
        {
                // Call the OnDrop method on just the focused object.
            this.BroadcastMessage("lowQual");
        });
        keywords.Add("racket", () =>
        {
                // Call the OnDrop method on just the focused object.
            this.BroadcastMessage("clear");
        });
        keywords.Add("next", () =>
        {
                // Call the OnDrop method on just the focused object.
            this.BroadcastMessage("next");
        });
        keywords.Add("all", () =>
        {
            // Call the OnDrop method on just the focused object.
            this.BroadcastMessage("all");
        });
        keywords.Add("debug", () =>
        {
            // Call the OnDrop method on just the focused object.
            sourceHandle.SendMessage("setPillVis", "on") ;
        });
        keywords.Add("remove", () =>
        {
            // Call the OnDrop method on just the focused object.
            sourceHandle.SendMessage("setPillVis", "off");
        });
        keywords.Add("increment", () =>
        {
            // Call the OnDrop method on just the focused object.
            this.BroadcastMessage("increment");
        });
        // Tell the KeywordRecognizer about our keywords.
        keywordRecognizer = new KeywordRecognizer(keywords.Keys.ToArray());

        // Register a callback for the KeywordRecognizer and start recognizing!
        keywordRecognizer.OnPhraseRecognized += KeywordRecognizer_OnPhraseRecognized;
        keywordRecognizer.Start();
    }

    private void KeywordRecognizer_OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        System.Action keywordAction;
        if (keywords.TryGetValue(args.text, out keywordAction))
        {
            keywordAction.Invoke();
        }
    }
}