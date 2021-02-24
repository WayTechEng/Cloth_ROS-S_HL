using RosSharp.Urdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using UnityEngine;


/*
 * Written by Steven Hoang 2020
 * UnityStateSubscriber updates the system state on ROS side to Unity
 * Steven H's note: Only the barebone has been implemented (call back function to obtain the states).
 */
namespace RosSharp.RosBridgeClient
{
    public class UnityStateSubscriber : UnitySubscriber<MessageTypes.Std.UInt32>
    {
        // States being reported from the ROS side
        private const uint IDLE = 1;
        private const uint EXECUTING = 2;
        private const uint TEMP_STOP = 3;
        private const uint MOVE_READY_STATE = 4;
        private const uint PREVIEW_SHOWING = 5;
        private const uint UNREACHABLE = 6;
        private const uint SIMULATE = 11;
        private const uint RESET_COMPUTER_SIM_COUNTER = 12;
        private const uint FOLD = 13;
        private const uint UNDO = 14;
        public bool reset_computer_sim_counter = false;
        public bool fold = false;
        public bool undo = false;
        // RobotState with public set and private set
        public static uint robot_state { get; private set; }
        protected override void ReceiveMessage(MessageTypes.Std.UInt32 message)
        {
            robot_state = message.data;
            //Debug.LogFormat("Unity state subscriber: {0}", message);
            if(robot_state == RESET_COMPUTER_SIM_COUNTER)
            {
                reset_computer_sim_counter = true;
                robot_state = 0;
            }
            else if(robot_state == FOLD)
            {
                fold = true;
                robot_state = 0;
            }
            else if(robot_state == UNDO)
            {
                undo = true;
                robot_state = 0;
            }
        }
    }
}
