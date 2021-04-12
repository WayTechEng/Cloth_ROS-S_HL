using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


/*
 * Written by Steven Hoang 2020
 * RobotCommandPublisher controls the robot's action (execute, stop, get back to Ready State)
 * Previously, there are three different publishers, each dedicated for each task. This publisher is the combination of the three
 * Please get the latest version of virtual_barrier_ros on ROS to use this publisher
 */
namespace RosSharp.RosBridgeClient
{
    public class CommandTracker : UnityPublisher<MessageTypes.Std.Header>
    {
        private List<MessageTypes.Std.Header> message_queue;
        public const uint UNDO = 1;
        public const uint SIMULATE = 2;
        public const uint FOLD = 3;

        protected override void Start()
        {
            base.Start();
            InitialisedMessage();
        }
        private void InitialisedMessage()
        {
            message_queue = new List<MessageTypes.Std.Header>();
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
            MessageTypes.Std.Time time = new MessageTypes.Std.Time();
            time.secs = (uint)DateTime.Now.Second;
            MessageTypes.Std.Header std_header;
            std_header = new MessageTypes.Std.Header();
            std_header.stamp = time;
            if (data == UNDO)
            {
                std_header.frame_id = "undo";
            }
            else if (data == SIMULATE)
            {
                std_header.frame_id = "simulate";
            }
            else if (data == FOLD)
            {
                std_header.frame_id = "fold";
            }
            Debug.Log("SENDINGGGG");
            message_queue.Add(std_header);
            
        }
    }
}
