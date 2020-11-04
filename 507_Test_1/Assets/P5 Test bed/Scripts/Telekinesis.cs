﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Resources;
using Tobii.XR.Examples;
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
    private bool isGrabbed;
    public Transform telekineticTransform;
    
    public GameObject grabbedObject;
    private float moveConstant = 10;
    private float moveStep = 0.5f;
    private float distance = 0.0f;
    private Vector3 grabbedObjDir;
    private Vector3 latePos = Vector3.zero;
    private float throwStrength = 10000;

    [SerializeField] private Material seethrough;
    private Material originalMat;

    private Vector3 storedControllerPos;
    private Quaternion storedControllerRot;

    [SerializeField] private float controllerMoveStrength = 2.0f;

    [SerializeField] private GameObject particles;

    private float startingDistance;
    [SerializeField] private float setDistance = 1.0f;
    public enum TelekinesisMethod
    {
        Thumbstick,
        ZGesture,
        XYZGesture,
        PullTowards,
        Resetable
    }

    private bool window;
    public TelekinesisMethod Method;
    
    public SteamVR_Action_Boolean reset;
    public SteamVR_Input_Sources handType;
    private void Start()
    {
        eyeRaycast = GetComponent<EyeRaycast>();
        reset.AddOnStateDownListener(JoystickDown, handType);
    }
    
    public void JoystickDown(SteamVR_Action_Boolean fromAction, SteamVR_Input_Sources fromSource)
    {
        if (window) return;
        StartCoroutine(DoubleClickWindow());
    }
    private void Update()
    {
        
        if (window && reset.state)
        {
            //StoreVector();
            print("Yeah");
        }
        if (eyeRaycast.raycastHitObject && rightHand.grabPinchAction.state && !isGrabbed)
        {
            PickUp();
        }

        if (!rightHand.grabPinchAction.state && grabbedObject)
        {
            ReleaseObject();
        }

        if (!isGrabbed) return;

        switch (Method)
        {
            case TelekinesisMethod.Thumbstick:
            {
                ThumbstickUpdate();
                break;
            }
            case TelekinesisMethod.ZGesture:
            {
                ZGestureUpdate();
                break;
            }
            case TelekinesisMethod.XYZGesture:
            {
                XYZGestureUpdate();
                break;
            }
            
            
            case TelekinesisMethod.PullTowards:
            {
                PullTowardsUpdate();
                break;
            }
            
            
            case TelekinesisMethod.Resetable:
            {
                ResetableUpdate();
                break;
            }
        }
    }

    void CalculateDistance()
    {
        distance = Vector3.Distance(Camera.main.transform.position, grabbedObject.transform.position);
        distanceCalculated = true;
    }

    void PickUp()
    {
        grabbedObject = eyeRaycast.raycastHitObject;
        particles.SetActive(true);
        startingDistance = Vector3.Distance(transform.position, grabbedObject.transform.position);
        isGrabbed = true;
        originalMat = grabbedObject.GetComponent<Renderer>().material;
        grabbedObject.GetComponent<Renderer>().material = seethrough;
        CalculateDistance();
        StoreVector();
    }

    void ReleaseObject()
    {
        particles.SetActive(false);
        var rb = grabbedObject.GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.AddForce(grabbedObjDir * throwStrength, ForceMode.Force);
        grabbedObject.GetComponent<Renderer>().material = originalMat;
        grabbedObject = null;
        isGrabbed = false;
        distanceCalculated = false;
    }


    void XYZGestureUpdate()
    {
        grabbedObject.GetComponent<Rigidbody>().useGravity = false;

        UpdateParticles();
        var telekineticTransformDist =
            Vector3.Distance(telekineticTransform.position, grabbedObject.transform.position);


        moveStep = telekineticTransformDist / moveConstant;


        telekineticTransform.position = Camera.main.transform.position + eyeRaycast.eyeDirection * distance;


        telekineticTransform.position +=
            (rightHand.transform.position - storedControllerPos) * controllerMoveStrength;
        
        grabbedObject.transform.rotation = Quaternion.Lerp(grabbedObject.transform.rotation, rightHand.transform.rotation * Quaternion.Inverse(storedControllerRot), Time.deltaTime);

        grabbedObject.transform.position =
            Vector3.MoveTowards(grabbedObject.transform.position, telekineticTransform.position, moveStep);

        //Calculate direction of rigidbody for throwing on release
        grabbedObjDir = grabbedObject.transform.position - latePos;
        latePos = grabbedObject.transform.position;
    }

    void ZGestureUpdate()
    {
        grabbedObject.GetComponent<Rigidbody>().useGravity = false;

        var controllerPosition = rightHand.transform.position;
        float zMovement;
        if (Vector3.Distance(controllerPosition, Camera.main.transform.position) <
            Vector3.Distance(Camera.main.transform.position, storedControllerPos))
        {
            zMovement = -Vector3.Distance(controllerPosition, storedControllerPos);
        }
        else
        {
            zMovement = Vector3.Distance(controllerPosition, storedControllerPos);
        }


        if (distance > startingDistance / 5)
            distance += zMovement * controllerMoveStrength / 100;
        else distance = startingDistance / 5;

        if (distance < startingDistance * 2)
            distance += zMovement * controllerMoveStrength / 100;
        else distance = startingDistance * 2;

        UpdateParticles();

        var telekineticTransformDist =
            Vector3.Distance(telekineticTransform.position, grabbedObject.transform.position);


        moveStep = telekineticTransformDist / moveConstant;


        telekineticTransform.position = Camera.main.transform.position + eyeRaycast.eyeDirection * distance;


        grabbedObject.transform.position =
            Vector3.MoveTowards(grabbedObject.transform.position, telekineticTransform.position, moveStep);

        //Calculate direction of rigidbody for throwing on release
        grabbedObjDir = grabbedObject.transform.position - latePos;
        latePos = grabbedObject.transform.position;
    }

    void ThumbstickUpdate()
    {
        grabbedObject.GetComponent<Rigidbody>().useGravity = false;

        if (Input.GetAxis("right joystick vertical") != 0)
        {
            if (distance > startingDistance / 5)
                distance += Input.GetAxis("right joystick vertical") * controllerMoveStrength / 100;
            else distance = startingDistance / 5;

            if (distance < startingDistance * 2)
                distance += Input.GetAxis("right joystick vertical") * controllerMoveStrength / 100;
            else distance = startingDistance * 2;
        }

        UpdateParticles();

        var telekineticTransformDist =
            Vector3.Distance(telekineticTransform.position, grabbedObject.transform.position);


        moveStep = telekineticTransformDist / moveConstant;


        telekineticTransform.position = Camera.main.transform.position + eyeRaycast.eyeDirection * distance;

        grabbedObject.transform.position =
            Vector3.MoveTowards(grabbedObject.transform.position, telekineticTransform.position, moveStep);

        //Calculate direction of rigidbody for throwing on release
        grabbedObjDir = grabbedObject.transform.position - latePos;
        latePos = grabbedObject.transform.position;
    }

    void PullTowardsUpdate()
    {
        grabbedObject.GetComponent<Rigidbody>().useGravity = false;

        UpdateParticles();
        var telekineticTransformDist =
            Vector3.Distance(telekineticTransform.position, grabbedObject.transform.position);


        moveStep = telekineticTransformDist / moveConstant;


        telekineticTransform.position = Camera.main.transform.position + eyeRaycast.eyeDirection * setDistance;


        telekineticTransform.position +=
            (rightHand.transform.position - storedControllerPos) * controllerMoveStrength;
        
        grabbedObject.transform.rotation = Quaternion.Lerp(grabbedObject.transform.rotation, rightHand.transform.rotation * Quaternion.Inverse(storedControllerRot), Time.deltaTime);

        grabbedObject.transform.position =
            Vector3.MoveTowards(grabbedObject.transform.position, telekineticTransform.position, moveStep);

        //Calculate direction of rigidbody for throwing on release
        grabbedObjDir = grabbedObject.transform.position - latePos;
        latePos = grabbedObject.transform.position;
    }

    void ResetableUpdate()
    {
        
        
        grabbedObject.GetComponent<Rigidbody>().useGravity = false;

        UpdateParticles();
        var telekineticTransformDist =
            Vector3.Distance(telekineticTransform.position, grabbedObject.transform.position);


        moveStep = telekineticTransformDist / moveConstant;


        telekineticTransform.position = Camera.main.transform.position + eyeRaycast.eyeDirection * distance;


        telekineticTransform.position +=
            (rightHand.transform.position - storedControllerPos) * controllerMoveStrength;
        
        grabbedObject.transform.rotation = Quaternion.Lerp(grabbedObject.transform.rotation, rightHand.transform.rotation * Quaternion.Inverse(storedControllerRot), Time.deltaTime);

        grabbedObject.transform.position =
            Vector3.MoveTowards(grabbedObject.transform.position, telekineticTransform.position, moveStep);

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
        particles.transform.position =
            new Vector3(grabbedObject.transform.position.x, 0, grabbedObject.transform.position.z);

        var shapeModule = particles.GetComponent<ParticleSystem>().shape;
        shapeModule.length = grabbedObject.transform.position.y;
        shapeModule.radius = grabbedObject.GetComponent<Renderer>().bounds.extents.x;
    }        
    
    void ResetControllerPosition(){
        
        StoreVector();
        
    }

    IEnumerator DoubleClickWindow()
    {
        window = true;
        yield return new WaitForSeconds(0.5f);
        window = false;
    }
}


/*
print(Input.GetAxis("Vertical"));
print(Input.GetButtonDown("Fire1")); // B
print(Input.GetButtonDown("Fire2")); // A
print(Input.GetButtonDown("Fire3")); // Y
*/