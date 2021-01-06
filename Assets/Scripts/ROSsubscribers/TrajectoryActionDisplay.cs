using RosSharp.RosBridgeClient.MessageTypes.Moveit;
using RosSharp.RosBridgeClient.MessageTypes.Sensor;
using RosSharp.RosBridgeClient.MessageTypes.Trajectory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
 * Written by Steven Hoang 2020
 * Trajectory Action Display mimic the trajectory that the robot would execute so the user can have an overview of the robot action
 * before sending an execution command
 */
namespace RosSharp.RosBridgeClient
{
    public class TrajectoryActionDisplay : UnitySubscriber<MessageTypes.Moveit.DisplayTrajectory>
    {
        [System.ComponentModel.Description("Playback speed (in Hz) determines how fast the jointstates are reflected on the robot model. (Preset at 30 Hz)")]
        public float PlaybackSpeed = 30;
        private float prevTime;
        private JointStateSubscriber JointStateSubscriber;
        private List<JointStateWriter> JointStateWriters;
        private List<string> JointNames;
        private string[] PlanningGroup;
        private List<MessageTypes.Trajectory.JointTrajectoryPoint> points;
        private int waypoint_counter = 0;

        protected override void Start()
        {
            base.Start();
            prevTime = Time.realtimeSinceStartup;
            JointStateSubscriber = GameObject.Find("ROS Connector").GetComponent<JointStateSubscriber>();
            JointStateWriters = JointStateSubscriber.JointStateWriters;
            JointNames = JointStateSubscriber.JointNames;
            points = new List<JointTrajectoryPoint>();
        }

        protected override void ReceiveMessage(DisplayTrajectory message)
        {
            // If there is new trajectory
            if (message.trajectory.Length > 0)
            {
                // Refresh the list
                points.Clear();
                // Assume the robot doesn't use MultiDOFTrajectory
                PlanningGroup = message.trajectory[0].joint_trajectory.joint_names;
                for (int i = 0; i < message.trajectory.Length; i++)
                {
                    points.AddRange(message.trajectory[i].joint_trajectory.points.ToList<JointTrajectoryPoint>());
                }
            }
        }

        public void Update()
        {
            // Preview mode only runs with a preset speed (not rendered every frame)
            if (JointStateSubscriber.previewMode && Time.realtimeSinceStartup >= prevTime + 1 / PlaybackSpeed)
            {
                if (waypoint_counter < points.Count)
                {
                    int index;
                    for (int i = 0; i < PlanningGroup.Length; i++)
                    {
                        index = JointNames.IndexOf(PlanningGroup[i]);
                        if (index != -1)
                            JointStateWriters[index].Write((float)points[waypoint_counter].positions[i]);
                    }
                    waypoint_counter++;
                    prevTime = Time.realtimeSinceStartup;
                }
                else
                {
                    // Reset counter, finish the preview
                    waypoint_counter = 0;
                    PreviewStop();
                }
            }
        }

        public void PreviewStart()
        {
            JointStateSubscriber.previewMode = true;
        }
        public void PreviewStop()
        {
            JointStateSubscriber.previewMode = false;
        }
    }
}
