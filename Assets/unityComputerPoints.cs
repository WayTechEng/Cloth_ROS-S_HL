using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Obi;

namespace RosSharp.RosBridgeClient
{
    public class unityComputerPoints : UnitySubscriber<MessageTypes.Geometry.PoseArray>
    {
        public ObiSolver solver;
        public ObiActor actor;
        public GameObject robotFrame;
        public Vector3 pick;
        public Vector3 place;
        public double sim_number = 0;

        public bool message_received = false;

        protected override void ReceiveMessage(MessageTypes.Geometry.PoseArray message)
        {
            // We are receiving the message wrt. the robots base frame
            // robot.x = unity_robot.z
            // robot.y = -unity_robot.x
            // unity_robot.z = robot.x 
            // unity_robot.x = -robot.y
            Debug.Log("Received message");
            var pick_rec = message.poses[0].position;
            var place_rec = message.poses[1].position;

            pick = new Vector3((float)-pick_rec.y, 0.01F, (float)pick_rec.x);
            place = new Vector3((float)-place_rec.y, 0.01F, (float)place_rec.x);
            sim_number = message.poses[0].orientation.x;
            //Debug.LogFormat("{0}", pick.ToString("F3"));

            message_received = true;
        }
    }
}
