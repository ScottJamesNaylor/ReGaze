﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using Tobii.XR.Examples;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class Telekinesis : MonoBehaviour
{
    public Hand leftHand, rightHand;
    private EyeRaycast eyeRaycast;
    private const ControllerButton TriggerButton = ControllerButton.Trigger;
    private const ControllerButton Wheel = ControllerButton.Touchpad;
    private bool distanceCalculated = false;
    public bool isGrabbed;
    public Transform telekineticTransform;

    public GameObject grabbedObject;
    public float moveConstant = 10;
    private float moveStep = 0.5f;
    private float distance = 0.0f;
    private Vector3 grabbedObjDir;
    private Vector3 latePos = Vector3.zero;
    private float throwStrength = 10000;

    public Material seethrough;
    public Material outlineMaterial;
    private Material originalMat;

    private Vector3 storedControllerPos;
    private Quaternion storedControllerRot;

    [SerializeField] private float gestureControllerMoveStrength = 85.0f;
    [SerializeField] private float trackpadControllerMoveStrength = 25.0f;

    public GameObject particles;
    private ParticleSystem ps;

    private float startingDistance;
    [SerializeField] private float setDistance = 1.0f;

    public enum TelekinesisMethod
    {
        Thumbstick,
        ZGesture,
        XYZGesture,
        PullTowards,
        Resetable,
        Combined,
        GestureWithLock
    }

    private bool window;
    public TelekinesisMethod Method;

    public SteamVR_Action_Boolean reset;
    public SteamVR_Input_Sources handType;
    public SteamVR_Action_Vector2 trackpadPos;
    public GameObject focusedObject;

    private void Start()
    {
        eyeRaycast = GetComponent<EyeRaycast>();
        reset.AddOnStateDownListener(JoystickDown, handType);
        if (particles) ps = particles.transform.GetChild(0).GetComponent<ParticleSystem>();
        Testing.Instance.rightHand = rightHand;
    }


    public void JoystickDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (window) return;
        StartCoroutine(DoubleClickWindow());
    }

    private void Update()
    {
        if (focusedObject && (rightHand.grabPinchAction.state || Input.GetKey(KeyCode.X)) && !isGrabbed)
        {
            PickUp();
        }

        if ((!rightHand.grabPinchAction.state || Input.GetKey(KeyCode.C)) && grabbedObject)
        {
            ReleaseObject();
        }

        if (!isGrabbed) return;
        print(trackpadPos[SteamVR_Input_Sources.RightHand].axis);
        CombinedUpdate();
    }

    private void FixedUpdate()
    {
        // GetComponent<Rigidbody>().MovePosition(transform.position + transform.right * Time.fixedDeltaTime);
    }

    void CalculateDistance()
    {
        distance = Vector3.Distance(Camera.main.transform.position, grabbedObject.transform.position);
        distanceCalculated = true;
    }

    void PickUp()
    {
        print("picking up");
        grabbedObject = focusedObject;
        grabbedObject.GetComponent<Grabbable>().OnSelect();
        startingDistance = Vector3.Distance(transform.position, grabbedObject.transform.position);
        isGrabbed = true;
        // originalMat = grabbedObject.GetComponent<Renderer>().material;
        particles.SetActive(true);
        var rb = grabbedObject.GetComponent<Rigidbody>();
        rb.velocity = Vector3.zero;
        rb.isKinematic = false;
        rb.angularVelocity = Vector3.zero;
        CalculateDistance();
        StoreVector();
    }

    public void ReleaseObject()
    {
        particles.SetActive(false);
        grabbedObject.GetComponent<Grabbable>().Default();
        var rb = grabbedObject.GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.AddForce(grabbedObjDir * throwStrength, ForceMode.Force);
        grabbedObject = null;
        eyeRaycast.raycastHitObject = null;
        isGrabbed = false;
        distanceCalculated = false;
    }

    void CombinedUpdate()
    {
        var rb = grabbedObject.GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        UpdateParticles();

        // Make the grabbed object follow the eyes
        var telekineticTransformDist =
            Vector3.Distance(telekineticTransform.position, grabbedObject.transform.position);

        moveStep = telekineticTransformDist / moveConstant;

        telekineticTransform.position = Camera.main.transform.position + eyeRaycast.eyeDirection * distance;

        // Move and rotate the grabbed object proportional to the gestures of the controller in the XYZ
        if ((Testing.Instance.tutorialInteractionMethods == Testing.TutorialInteractionMethods.Gestures ||
             Testing.Instance.tutorialInteractionMethods == Testing.TutorialInteractionMethods.TouchPad) &&
            Testing.Instance.eyeTracking != Testing.EyeTracking.Controllers)
        {
            telekineticTransform.position +=
                (rightHand.transform.position - storedControllerPos) * gestureControllerMoveStrength;

            grabbedObject.transform.rotation = Quaternion.Lerp(grabbedObject.transform.rotation,
                rightHand.transform.rotation * Quaternion.Inverse(storedControllerRot), Time.deltaTime);
        }


        // grabbedObject.transform.position =Vector3.MoveTowards(grabbedObject.transform.position, telekineticTransform.position, moveStep);
        // Vector3 direction = (telekineticTransform.position - grabbedObject.transform.position).normalized;
        // rb.MovePosition(grabbedObject.transform.position + direction * (Time.fixedDeltaTime * moveConstant));

        Vector3 direction = telekineticTransform.transform.position - grabbedObject.transform.position;
        Ray ray = new Ray(grabbedObject.transform.position, direction);
        RaycastHit hit;
        if (!Physics.Raycast(ray, out hit, direction.magnitude))
            rb.MovePosition(Vector3.Lerp(grabbedObject.transform.position, telekineticTransform.position,
                moveStep / rb.mass));
        else
        {
            // if(grabbedObject.GetComponent<Grabbable>().canCollide) 
            rb.MovePosition(Vector3.Lerp(grabbedObject.transform.position, hit.point, moveStep / rb.mass));
        }
        // rb.MovePosition(hit.point);
        // 

        // Move the grabbed object also with the touchpad
        if (Testing.Instance.tutorialInteractionMethods == Testing.TutorialInteractionMethods.TouchPad)
        {
            var t = SteamVR_Actions.default_Trackpad.GetAxis(SteamVR_Input_Sources.Any);
            if (t.y != 0)
            {
                print("Touchpad enabled:: " + t.y);
                print(t.y);
                // add a constraint to how close the grabbed object can get to the player
                if (distance > startingDistance / 5)
                    distance += t.y * trackpadControllerMoveStrength / 75;
                else distance = startingDistance / 5;

                print(t.y * trackpadControllerMoveStrength / 75);
                distance += t.y * trackpadControllerMoveStrength / 75;
            }
        }


        if (reset.state)
        {
            print("print");
            StoreVector();
            telekineticTransform.position = eyeRaycast.eyeDirection * telekineticTransformDist;
        }


        //Calculate direction of rigidbody for throwing on release
        grabbedObjDir = grabbedObject.transform.position - latePos;
        latePos = grabbedObject.transform.position;
    }

    void StoreVector()
    {
        storedControllerPos = rightHand.transform.position;
        storedControllerRot = rightHand.transform.rotation;
    }

    void UpdateParticles()
    {
        // set the position, rotation, scale and the rate over time emission of particles in the particle system dependent and proportionate on the grabbed object
        // position
        particles.transform.position =
            new Vector3(grabbedObject.transform.position.x, grabbedObject.transform.position.y,
                grabbedObject.transform.position.z);


        var psShape = ps.shape;
        // scale
        // psShape.scale = new Vector3(grabbedObject.transform.localScale.x, grabbedObject.transform.localScale.z, 1);
        psShape.scale = grabbedObject.transform.GetComponent<Renderer>().bounds.extents;
        // rotation
        var eulerRotation1 = new Vector3(grabbedObject.transform.eulerAngles.x, grabbedObject.transform.eulerAngles.y,
            particles.transform.eulerAngles.z);
        particles.transform.rotation = Quaternion.Euler(eulerRotation1);
        var eulerRotation2 = new Vector3(grabbedObject.transform.eulerAngles.x - 90, 0, 0);
        psShape.rotation = eulerRotation2;

        // rate over time
        var e = ps.emission;
        e.rateOverTime = (grabbedObject.transform.localScale.x + grabbedObject.transform.localScale.z) / 2 * 30;
    }

    void ResetControllerPosition()
    {
        StoreVector();
    }

    IEnumerator DoubleClickWindow()
    {
        window = true;
        yield return new WaitForSeconds(0.5f);
        window = false;
    }
}