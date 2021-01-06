using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    public class MoveItTestPublisher : UnityPublisher<MessageTypes.Moveit.MoveItErrorCodes>
    {
        private MessageTypes.Moveit.MoveItErrorCodes message;
        protected override void Start()
        {
            base.Start();
            InitializeMessage();
        }

        private void FixedUpdate()
        {
            UpdateMessage();
        }

        private void InitializeMessage()
        {
            message = new MessageTypes.Moveit.MoveItErrorCodes();
        }
        private void UpdateMessage()
        {
            message.val = MessageTypes.Moveit.MoveItErrorCodes.SUCCESS;
            Publish(message);
        }
    }
}
