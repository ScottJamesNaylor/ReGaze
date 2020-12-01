﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tobii.XR;
using UnityEngine.VFX;

public class EyeRaycast : MonoBehaviour
{
    public GameObject lightObject;

    public float eyeSpeed = 2.0f;

    public GameObject raycastHitObject;

    public Vector3 targetPos;
    public RaycastHit raycastHit;

    public bool hasHit = false;

    public TobiiXR_EyeTrackingData eyeTrackingData;

    public Vector3 eyeOrigin;
    public Vector3 eyeDirection;

    [SerializeField] private float lightHeight = 0.05f;

    [SerializeField] private float lightMoveConstant = 0.2f;

    [SerializeField] private Transform jumpTransform;
    [SerializeField] private GameObject eyeSignifierPrefab;
    private GameObject eyeSignifier;

    private GameObject lastHitObject;

    [SerializeField] private float eyeSignifierSize;

    [SerializeField] private GameObject vrCamera;

    private void Start()
    {
        eyeSignifier = Instantiate(eyeSignifierPrefab, null) as GameObject;
        eyeSignifier.SetActive(false);
    }

    private void Update()
    {
        switch (Testing.Instance.eyeTracking)
        {
            case Testing.EyeTracking.Quest:
            {
                eyeOrigin = Camera.main.transform.position;
                eyeDirection = Camera.main.transform.forward;
                break;
            }

            case Testing.EyeTracking.HTC:
            {
                eyeTrackingData = TobiiXR.GetEyeTrackingData(TobiiXR_TrackingSpace.World);
                if (eyeTrackingData.GazeRay.IsValid)
                {
                    //eyeOrigin = Camera.main.transform.position;
                    eyeOrigin = eyeTrackingData.GazeRay.Origin;
                    eyeDirection = Vector3.Normalize(eyeTrackingData.GazeRay.Direction);
                }

                break;
            }
            case Testing.EyeTracking.Controllers: 
            {
                if(!Testing.Instance.rightHand) break;
                eyeOrigin = Testing.Instance.rightHand.transform.position;
                eyeDirection = Testing.Instance.rightHand.transform.forward;
                break;
            }
        }

        GazeCast(eyeOrigin, eyeDirection);
    }


    public void GazeCast(Vector3 startPoint, Vector3 direction)
    {
        RaycastHit hit;

        int layerMask = 1 << 0; // this would cast rays only against colliders in layer 0 --> the default one
        layerMask = ~layerMask; // invert the layer mask
        if (Physics.Raycast(startPoint, direction, out hit, Mathf.Infinity, layerMask))
        {
            print(hit.collider.gameObject.name);
            Debug.DrawRay(startPoint, direction * hit.distance, Color.yellow);
            // if ground, turn on target pos and so on...
            if (hit.collider.gameObject.layer == 8) // if ground                
            {
                targetPos = hit.point;
                targetPos += new Vector3(vrCamera.transform.position.x - transform.position.x, 0, vrCamera.transform.position.z - transform.position.z);
                raycastHit = hit;
                var distance = Vector3.Distance(eyeOrigin, targetPos);
                if (distance > 5) eyeSignifier.transform.GetChild(0).GetComponent<Light>().intensity = distance;

                eyeSignifier.SetActive(true);
                //lightObject.transform.localScale = new Vector3(distance, distance,distance);
                //lightObject.SetActive(true);
                hasHit = true;
                MoveLight(hit.point);
            }
            else
            {
                eyeSignifier.SetActive(false);
                hasHit = false;
                //lightObject.SetActive(false);
            }
        }
    }

    void MoveLight(Vector3 hitPoint)
    {
        jumpTransform.position = hitPoint + new Vector3(0, lightHeight, 0);
        var lightToTargetDistance = Vector3.Distance(eyeSignifier.transform.position, jumpTransform.position);
        var moveStep = lightToTargetDistance / lightMoveConstant;
        eyeSignifier.transform.position =
            Vector3.MoveTowards(eyeSignifier.transform.position, jumpTransform.position, moveStep);
    }
}