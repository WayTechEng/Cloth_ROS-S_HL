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
    class UnityStateSubscriber : UnitySubscriber<MessageTypes.Std.UInt32>
    {
        // Six states being reported from the ROS side
        private const uint IDLE = 1;
        private const uint EXECUTING = 2;
        private const uint TEMP_STOP = 3;
        private const uint MOVE_READY_STATE = 4;
        private const uint PREVIEW_SHOWING = 5;
        private const uint UNREACHABLE = 6;
        private const uint SIMULATE = 11;
        // RobotState with public set and private set
        public static uint robot_state { get; private set; }
        protected override void ReceiveMessage(MessageTypes.Std.UInt32 message)
        {
            robot_state = message.data;
            Debug.LogFormat("Unity state subscriber: {0}", message);
        }
    }
}
