using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Written by Steven Hoang 2020
 * RobotCommandPublisher controls the robot's action (execute, stop, get back to Ready State)
 * Previously, there are three different publishers, each dedicated for each task. This publisher is the combination of the three
 * Please get the latest version of virtual_barrier_ros on ROS to use this publisher
 */
namespace RosSharp.RosBridgeClient
{
    public class RobotCommandPublisher : UnityPublisher<MessageTypes.Std.UInt32>
    {
        private List<MessageTypes.Std.UInt32> message_queue;
        public const uint EXECUTE_TRIGGER = 1;
        public const uint STOP_TRIGGER = 2;
        public const uint READY_STATE_TRIGGER = 3;
        public const uint GET_CLOTH_STATE = 10;
        public const uint PICK_NOT_ON_CLOTH = 11;

        protected override void Start()
        {
            base.Start();
            InitialisedMessage();
        }
        private void InitialisedMessage()
        {
            message_queue = new List<MessageTypes.Std.UInt32>();
        }
        private void Update()
        {
            // If there is message in the queue
            if (message_queue.Count > 0)
            {
                // Publish the first message from queue
                Publish(message_queue[0]);
                // Then remove it
                message_queue.RemoveAt(0);
            }
        }
        public void SendCommand(uint data)
        {
            Debug.Log("Command Added to Queue");
            message_queue.Add(new MessageTypes.Std.UInt32(data));
        }
    }
}
