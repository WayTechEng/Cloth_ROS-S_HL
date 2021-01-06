using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using RosSharp.RosBridgeClient;
using Microsoft.MixedReality.Toolkit;

/* Written by Steven Hoang and Steven Lay, 2020.
 * Virtual Person Barrier related code.
 * Modified on 19/07/2020 by Steven Hoang with the new ROS-sharp integration
 */
public class BarrierPerson : MonoBehaviour 
{
    // Get Myo Band Controller to use Haptic Feedback functionality from ROS Connector
    private GameObject ROSConnector;

    // Flag to determine whether collision with Robot arm has occured
    // Steven H's note: preset a value here to make sure the if condition is satisfied in OnEnterTrigger callback
    // Assume the user starts at a no-collision position
    private bool collided = false;

    // Hide the Virtual Person Barrier when Robot is not executing
    private bool hide = false;

    private GameObject rings;
    private GameObject[] visualMarkers;

    public bool Hide {
        set
        {
            hide = value;
            SetHide(hide);
        }
        get
        {
            return hide;
        }
    }

    void Start()
    {
        // Find ROSConnector and set myoBand operating parameters
        ROSConnector = GameObject.Find("ROS Connector");
        ROSConnector.GetComponent<myoBandFeedback>().SetVibrationDurationAndInterval(0.15f, 1);

        rings = GameObject.Find("Rings");
        visualMarkers = GameObject.FindGameObjectsWithTag("BarrierMarker");
        // Hide the Virtual Person Barrier by default
        SetHide(true);
    }

    void LateUpdate()
    {
        transform.position = Camera.main.transform.position + new Vector3(0, -0.5f, 0);
    }

    // Enable/Disable MeshCollider of PersonBarrier and RobotModel
    private void SetHide(bool hide)
    {
        if (hide)
        {
            gameObject.GetComponent<Collider>().enabled = false;
            foreach (MeshCollider meshCollider in GameObject.FindGameObjectWithTag("Robot").GetComponentsInChildren<MeshCollider>())
            {
                meshCollider.enabled = false;
            }
            EnableVisualTrackers(false);
            EnableRings(false);
        }
        else
        {
            gameObject.GetComponent<Collider>().enabled = true;
            foreach (MeshCollider meshCollider in GameObject.FindGameObjectWithTag("Robot").GetComponentsInChildren<MeshCollider>())
            {
                meshCollider.enabled = true;
                meshCollider.tag = "Robot_arm";
            }
            EnableVisualTrackers(true);
            EnableRings(true);
        }
    }

    private void EnableVisualTrackers(bool enable)
    {
        try
        {
            foreach (var markers in visualMarkers)
            {
                markers.SetActive(enable);
            }
        } catch (Exception)
        {
            Debug.Log("No visual markers available to modify");
        }
    }

    private void EnableRings(bool enable)
    {
        try
        {
            rings.SetActive(enable);
        }
        catch (Exception)
        {
            Debug.Log("No visual rings available to modify");
        }
    }
    // Alert the user when the Virtual Person Barrier collides with the Robot via Haptic Feedback
    private void OnTriggerEnter(Collider col)
    {
        if (col.CompareTag("Robot_arm") && !collided)
        {
            collided = true;
            ROSConnector.GetComponent<myoBandFeedback>().VibrateBandRequest(3);
            Debug.Log("VPB-Robot collision occured");
        }
    }

    private void OnTriggerExit(Collider col)
    {
        if (collided)
        {
            collided = false;
            Debug.Log("VPB-Robot collision exit");
        }   
    }
    
}

