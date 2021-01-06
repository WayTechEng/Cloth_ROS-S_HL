using RosSharp.RosBridgeClient.MessageTypes.Moveit;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
 *      private const uint IDLE = 1;
        private const uint EXECUTING = 2;
        private const uint TEMP_STOP = 3;
        private const uint MOVE_READY_STATE = 4;
        private const uint PREVIEW_SHOWING = 5;
        private const uint UNREACHABLE = 6;
 * 
 * 
 */

public class RobotStatusUpdate : MonoBehaviour
{
    [SerializeField]
    private TextMeshPro robotStatusText;

    [SerializeField]
    private TextMeshPro robotSystemCode;

    private int robotState;
    private GameObject personBarrier;
    private void Start()
    {
        personBarrier = GameObject.FindGameObjectWithTag("PersonBarrier");
    }

    void Update()
    {
        robotState = (int) RosSharp.RosBridgeClient.UnityStateSubscriber.robot_state;
        robotSystemCode.text = "System Code: " + robotState.ToString();
        switch (robotState)
        {
            case 1:
                robotStatusText.text = "IDLE";
                if (!personBarrier.GetComponent<BarrierPerson>().Hide)
                {
                    personBarrier.GetComponent<BarrierPerson>().Hide = true;
                }
                break;
            case 2:
                robotStatusText.text = "EXECUTING";
                if (personBarrier.GetComponent<BarrierPerson>().Hide)
                {
                    personBarrier.GetComponent<BarrierPerson>().Hide = false;
                }
                break;
            case 3:
                robotStatusText.text = "TEMP_STOP";
                if (personBarrier.GetComponent<BarrierPerson>().Hide)
                {
                    personBarrier.GetComponent<BarrierPerson>().Hide = false;
                }
                break;
            case 4:
                robotStatusText.text = "MOVE_READY_STATE";
                break;
            case 5:
                robotStatusText.text = "PREVIEW_SHOWING";
                break;
            case 6:
                robotStatusText.text = "UNREACHABLE";
                break;
            default:
                robotStatusText.text = "UNKNOWN";
                break;
        }
    }
}
