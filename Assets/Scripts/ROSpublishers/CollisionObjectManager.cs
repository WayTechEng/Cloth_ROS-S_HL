using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/*
 *  Written by Steven Hoang 2020
 *  CollisionObjectManager update the planning scene of ROS Planner on ROS side
 */
namespace RosSharp.RosBridgeClient
{
    class CollisionObjectManager : UnityPublisher<MessageTypes.Moveit.PlanningSceneWorld>
    {
        private List<MessageTypes.Moveit.PlanningSceneWorld> message_queue;
        protected override void Start()
        {
            base.Start();
            InitialiseMessage();
        }
        private void InitialiseMessage()
        {
            message_queue = new List<MessageTypes.Moveit.PlanningSceneWorld>();
        }
        private void Update()
        {
            // If there is message in the queue
            if (message_queue.Count > 0)
            {
                // Publish the first message from queue. Also check if null before publishing (this cause null ref error at the start)
                this?.Publish(message_queue[0]);
                // Then remove it
                message_queue.RemoveAt(0);
            }
        }
        // Update PlanningScene on ROS planner
        public void UpdateScene(MessageTypes.Moveit.PlanningSceneWorld new_scene)
        {
            // Check if null before adding. (This is called from SceneWatcher when init, get null object ref error at the start)
            message_queue?.Add(new_scene);
        }
    }
}
