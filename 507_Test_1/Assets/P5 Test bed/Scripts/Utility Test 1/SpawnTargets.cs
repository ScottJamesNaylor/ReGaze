﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tobii.XR.Examples;
using Tobii.XR;

public class SpawnTargets : MonoBehaviour
{
    public GameObject[] blinkTargets;
    public float height = 1.5f;
    public int blinkTargetDist = 8;
    private float numTargets = 10;
    


    // Start is called before the first frame update
    void Start()
    {

        for (int i = 1; i < blinkTargets.Length; i++) //deactivate all targets except the first one
        {
            blinkTargets[i].SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {

        checkPosition();
        Debug.Log(checkPosition());

        blinkTargets[checkPosition()].SetActive(true);
        
        
    }

    private int checkPosition()
    {
        int pos=0;

        if (Vector3.Distance(blinkTargets[0].transform.position, transform.GetChild(0).position) < blinkTargetDist)
        {
            pos = 1;
        }
        if (Vector3.Distance(blinkTargets[1].transform.position, transform.GetChild(0).position) < blinkTargetDist)
        {
            pos = 2;
        }
        if (Vector3.Distance(blinkTargets[2].transform.position, transform.GetChild(0).position) < blinkTargetDist)
        {
            pos = 3;
        }
        if (Vector3.Distance(blinkTargets[3].transform.position, transform.GetChild(0).position) < blinkTargetDist)
        {
            pos = 4;
        }
        if (Vector3.Distance(blinkTargets[4].transform.position, transform.GetChild(0).position) < blinkTargetDist)
        {
            pos = 5;
        }
        if (Vector3.Distance(blinkTargets[5].transform.position, transform.GetChild(0).position) < blinkTargetDist)
        {
            pos = 6;
        }
        if (Vector3.Distance(blinkTargets[6].transform.position, transform.GetChild(0).position) < blinkTargetDist)
        {
            pos = 7;
        }
        if (Vector3.Distance(blinkTargets[7].transform.position, transform.GetChild(0).position) < blinkTargetDist)
        {
            pos = 8;
        }
        if (Vector3.Distance(blinkTargets[8].transform.position, transform.GetChild(0).position) < blinkTargetDist)
        {
            pos = 9;
        }
        if (Vector3.Distance(blinkTargets[9].transform.position, transform.GetChild(0).position) < blinkTargetDist)
        {
            pos = 10;
        }

        return pos;
    }
}
