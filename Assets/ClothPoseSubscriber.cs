using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Obi;

namespace RosSharp.RosBridgeClient
{
    public class ClothPoseSubscriber : UnitySubscriber<MessageTypes.Geometry.Pose>
    {
        public ObiSolver solver;
        public ObiActor actor;
        public GameObject robotFrame;
        public Vector3 position = new Vector3(0.0F, 0.0F, 0.0F);
        public Quaternion orientation = new Quaternion(0.0F, 0.0F, 0.0F, 0.0F);
        public int counter = 0;
        public int psuedo_counter = 0;

        protected override void ReceiveMessage(MessageTypes.Geometry.Pose message)
        {
            Debug.Log("Updating cloth state");
            var pos = message.position;
            var ori = message.orientation;
            //Debug.Log(pos);
            //Debug.Log(ori);
            Vector3 tmp_position = new Vector3((float)pos.x, (float)pos.y, (float)pos.z);
            Quaternion tmp_orientation = new Quaternion((float)ori.x, (float)ori.y, (float)ori.z, (float)ori.w);
            position.x = tmp_position.x;
            position.y = tmp_position.z;
            position.z = tmp_position.y;
            orientation.x = tmp_orientation.x;
            orientation.y = tmp_orientation.y;
            orientation.z = tmp_orientation.z;
            orientation.w = tmp_orientation.w;
            psuedo_counter++;
            if(psuedo_counter < 5)
            {
                counter++;
            }
            else
            {
                //psuedo_counter = 0;
                counter = 0;
            }

            if (psuedo_counter == 10)
            {
                psuedo_counter = 0;
            }
            Debug.Log(psuedo_counter);
            Debug.Log(counter);
            

            //actor = GetComponent<ObiActor>();
            //actor.GetComponent<ObiControl>().Set_cloth_state();
        }
    }
}
