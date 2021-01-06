using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * Written by Steven Hoang 2020
 * PathRequest send the goal point(s) requests to ROS for pathPlanning
 */
namespace RosSharp.RosBridgeClient
{
    public class PathRequest : UnityPublisher<MessageTypes.Geometry.PoseArray>
    {
        private List<MessageTypes.Geometry.PoseArray> message_queue;
        private MessageTypes.Std.Header std_header;
        protected override void Start()
        {
            base.Start();
            InitialiseMessage();
        }
        private void InitialiseMessage()
        {
            message_queue = new List<MessageTypes.Geometry.PoseArray>();
            std_header = new MessageTypes.Std.Header();
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
        public void SendRequest(List<MessageTypes.Geometry.Pose> goal_points)
        {
            message_queue.Add(new MessageTypes.Geometry.PoseArray(std_header, goal_points.ToArray()));
        }
    }
}
