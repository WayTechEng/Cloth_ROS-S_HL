using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
/*
* Writen by Steven Hoang 2020, based on Cole Shing 2017 and Steven Lay 2020
* Implement myoBand Control for Haptic Feedback
*/
namespace RosSharp.RosBridgeClient
{
    public class myoBandFeedback : UnityPublisher<MessageTypes.Std.UInt8>
    {
        MessageTypes.Std.UInt8 message;
        // Vibrating period (the time between the beginning of two vibration), default is 150ms
        [SerializeField]
        private float vibrating_period = 0.15f;
        // Vibrating duration can have a value from 1 to 3 (shortest to longest), hardcode to 1 for now
        [SerializeField]
        private byte vibrating_duration = 1;
        // Number of vibrations
        [SerializeField]
        private int vibration_count = 0;
        // Timing variables
        private float previousTime = 0;
        protected override void Start()
        {
            base.Start();
            InitialiseMessage();
        }
        private void Update()
        {
            // If the vibration request has not finished and the time interval reaches
            if (vibration_count > 0 && Time.realtimeSinceStartup >= previousTime + vibrating_period)
            {
                // Publish to the ROS to vibrate the myoBand
                message.data = vibrating_duration;
                Publish(message);
                previousTime = Time.realtimeSinceStartup;
                vibration_count--;
            }
        }
        private void InitialiseMessage()
        {
            message = new MessageTypes.Std.UInt8();
        }
        public void VibrateBandRequest(int number_of_vibration)
        {
            // If the vibrating request has not been done yet
            if(vibration_count > 0)
            {
                Debug.Log("Previous Request has not been done. Please try again later");
            }
            // Otherwise, update the request
            else
            {
                vibration_count = number_of_vibration;
            }
        }
        // Set the time between each intervals in seconds
        public void SetVibrationInterval(float interval_in_secs)
        {
            vibrating_period = interval_in_secs;
        }
        // Set the duration of vibration
        public void SetVibrationDuration(byte duration)
        {
            vibrating_duration = duration;
        }
        // Set both conditions above
        public void SetVibrationDurationAndInterval(float interval_in_sec, byte duration)
        {
            SetVibrationInterval(interval_in_sec);
            SetVibrationDuration(duration);
        }
    }
}
