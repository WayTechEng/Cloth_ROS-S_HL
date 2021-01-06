using RosSharp.RosBridgeClient.MessageTypes.Geometry;
using RosSharp.RosBridgeClient.MessageTypes.Visualization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
 * Written by Steven Hoang 2020
 * TrajectoryLine subscriber, display planned trajectory line from ROS, this line is identical to what RViz produces
 * Notes: This is a prime example of implementing a subscriber where callback function needs to process a large load
 */
namespace RosSharp.RosBridgeClient
{
    public class TrajectoryLineDisplay : UnitySubscriber<MessageTypes.Geometry.PoseArray>
    {
        private List<UnityEngine.Vector3> trajectory_points;
        private MessageTypes.Geometry.Pose[] trajectory_poses;
        private UnityEngine.Transform ros_world_coord_frame;
        private LineRenderer LineTracer;
        private bool isMessageReceived;
        protected override void Start()
        {
            base.Start();
            trajectory_points = new List<UnityEngine.Vector3>();
            // Find the Transform of ROS's world coord frame to do conversion
            ros_world_coord_frame = GameObject.FindGameObjectWithTag("Robot").transform.Find("world").transform;
            LineTracer = GameObject.Find("Trajectory Display").GetComponent<LineRenderer>();
            LineTracer.material = new Material(Shader.Find("Sprites/Default"));
            LineTracer.widthMultiplier = 0.005f;
            LineTracer.startColor = Color.green;
            LineTracer.endColor = Color.green;
            LineTracer.enabled = false;
            isMessageReceived = false;
        }
        protected override void ReceiveMessage(PoseArray message)
        {
            trajectory_poses = message.poses;
            isMessageReceived = true;
        }
        private void Update()
        {
            if (isMessageReceived)
                ProcessMessage();
        }
        void ProcessMessage()
        {
            if (trajectory_poses.Length > 0)
            {
                Debug.Log(trajectory_poses.Length);
                trajectory_points.Clear();
                // The point received is in ROS coordinate frame, need to convert back to Unity's world frame
                foreach (MessageTypes.Geometry.Pose pose in trajectory_poses)
                {
                    trajectory_points.Add(ros_world_coord_frame.TransformPoint(GetUnityPoint(pose.position).Ros2Unity()));
                }
                // Set properties from LineRender
                Debug.LogFormat("Message Received with Length {0}", trajectory_points.Count);
                LineTracer.enabled = true;
                LineTracer.positionCount = trajectory_points.Count;
                LineTracer.SetPositions(trajectory_points.ToArray());
                isMessageReceived = false;
            }
        }
        private UnityEngine.Vector3 GetUnityPoint(MessageTypes.Geometry.Point position)
        {
            return new UnityEngine.Vector3((float)position.x, (float)position.y, (float)position.z);
        }
        public void ClearTrajectoryLine()
        {
            trajectory_points.Clear();
            LineTracer.positionCount = trajectory_points.Count;
        }
    }
}
