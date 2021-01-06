using Microsoft.MixedReality.Toolkit;
using RosSharp;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions.Must;

/* Written by Steven Hoang 2020
 * The SceneWatcher acts as a periodic publisher, which advertises the position and dimensions of all the barriers presented in the scene. 
 * This publisher advertises this information on rosbridge and it would be captured by the planner running on the ROS side.
 * Modified on 18/07/2020 by Steven Hoang with the new ROS-sharp integration
 */
namespace RosSharp.RosBridgeClient
{
	public class SceneWatcher : MonoBehaviour
	{
		// Rate at which collision objects are published to ROS
		[SerializeField]
		public float ROSUpdateRate = 0.5f;

		private GameObject ROSConnector;

		private GameObject[] barriers;
		
		private GameObject personBarrier;
		private MessageTypes.Moveit.CollisionObject pb_message;
		// private GameObject table;
		private MessageTypes.Moveit.CollisionObject workspace;

		private GameObject RobotModel;
		private Transform ros_world_coord_frame;

		private MessageTypes.Moveit.PlanningSceneWorld planning_scene;
		private List<MessageTypes.Moveit.CollisionObject> barrier_msgs;

		void Start()
		{
			personBarrier = GameObject.FindWithTag("PersonBarrier");
			// table = GameObject.Find("Table");
			// Connect with ROS
			ROSConnector = GameObject.Find("ROS Connector");

			// Find the Coordinate System of ROS
			RobotModel = GameObject.FindGameObjectWithTag("Robot");
			ros_world_coord_frame = RobotModel.transform.Find("world");
			//// Initialise ROS messages
			planning_scene = new MessageTypes.Moveit.PlanningSceneWorld();
			#region Generic Barrier
			barrier_msgs = new List<MessageTypes.Moveit.CollisionObject>();
            #endregion
            #region Person Barrier
            pb_message = new MessageTypes.Moveit.CollisionObject();
			pb_message.header.frame_id = "world";
			pb_message.id = "Person";
			pb_message.primitives = new MessageTypes.Shape.SolidPrimitive[] // CYLINDER needs height and radius
							{new MessageTypes.Shape.SolidPrimitive(MessageTypes.Shape.SolidPrimitive.CYLINDER, new double[2]) };
			pb_message.primitive_poses = new MessageTypes.Geometry.Pose[] { new MessageTypes.Geometry.Pose() };
			pb_message.primitive_poses[0].orientation = new MessageTypes.Geometry.Quaternion(0, 0, 0, 1);
            #endregion
            #region Workspace
            workspace = new MessageTypes.Moveit.CollisionObject();
			workspace.header.frame_id = "world";
			workspace.id = "Workspace";
			workspace.primitives = new MessageTypes.Shape.SolidPrimitive[] // Only one mesh, with (x, y, z) dimensions listed below
				{new MessageTypes.Shape.SolidPrimitive(MessageTypes.Shape.SolidPrimitive.BOX, new double[3] { 1.80f, 1.25f, 0.02f }) }; 
			workspace.primitive_poses = new MessageTypes.Geometry.Pose[] { new MessageTypes.Geometry.Pose() };
			workspace.primitive_poses[0].position = new MessageTypes.Geometry.Point(-0.1, 0.3, -0.01);
			workspace.primitive_poses[0].orientation = new MessageTypes.Geometry.Quaternion(0, 0, 0, 1);
            #endregion
			// Start updating the planningScene to ROS
            StartCoroutine(RosBarrierUpdate());
		}

		void LateUpdate()
		{
			//Debug.Log(personBarrier.transform.position);
		}

		IEnumerator RosBarrierUpdate()
		{
			while (true)
			{
				// Reset the barrier array for a new PlanningSceneWorld message
				barrier_msgs.Clear();
				// Always add the workspace as a barrier/collision object
				// Debug.Log(ros_world_coord_frame.InverseTransformPoint(table.transform.position).Unity2Ros());
				barrier_msgs.Add(workspace);
				// Person Barrier
				// If the barrier is activated
				if (!personBarrier.GetComponent<BarrierPerson>().Hide)
				{
					// Update the barrier message and added it to the barrier list
					// Dimensions
					pb_message.primitives[0].dimensions[MessageTypes.Shape.SolidPrimitive.CYLINDER_HEIGHT] = 1.75f; //TODO: The scale is hardcoded due to the mismatch in Unity Scale with the actual size
					pb_message.primitives[0].dimensions[MessageTypes.Shape.SolidPrimitive.CYLINDER_RADIUS] = personBarrier.transform.localScale.x/2;
					// Pose
					pb_message.primitive_poses[0].position = GetGeometryPoint(ros_world_coord_frame.InverseTransformPoint(personBarrier.transform.position).Unity2Ros());
					barrier_msgs.Add(pb_message);
				}
                // Other barriers
                barriers = GameObject.FindGameObjectsWithTag("Barrier");
                foreach (GameObject barrier in barriers)
                {
					MessageTypes.Moveit.CollisionObject barrier_msg = new MessageTypes.Moveit.CollisionObject();
					barrier_msg = new MessageTypes.Moveit.CollisionObject();
					barrier_msg.header.frame_id = "world";
					barrier_msg.primitive_poses = new MessageTypes.Geometry.Pose[] { new MessageTypes.Geometry.Pose() };
					barrier_msg.id = barrier.name;
					barrier_msg.primitive_poses[0].position = GetGeometryPoint(ros_world_coord_frame.InverseTransformPoint(barrier.transform.position).Unity2Ros());
                    barrier_msg.primitive_poses[0].orientation = GetGeometryQuaternion(barrier.transform.rotation.Unity2Ros());
                    // Check the type of barrier
                    if (barrier.GetComponent<MeshFilter>().sharedMesh.name == "Sphere")
                    {
                        // Dimension required is only the radius of the sphere
                        barrier_msg.primitives = new MessageTypes.Shape.SolidPrimitive[1]
                            {new MessageTypes.Shape.SolidPrimitive(MessageTypes.Shape.SolidPrimitive.SPHERE,
                                                    new double[]{ barrier.transform.localScale.z / 2.0f })}; // Calculate the radius
						barrier_msgs.Add(barrier_msg);
                    }
                    else if (barrier.GetComponent<MeshFilter>().sharedMesh.name == "Cube")
                    {
                        // Dimensions required are the 3 size of the cube
                        barrier_msg.primitives = new MessageTypes.Shape.SolidPrimitive[]
                            {new MessageTypes.Shape.SolidPrimitive(MessageTypes.Shape.SolidPrimitive.BOX,
                                                     barrier.transform.localScale.ToRoundedDoubleArray())};
						barrier_msgs.Add(barrier_msg);
                    }
                }
				// Update the message and publish to ROS
				planning_scene.collision_objects = barrier_msgs.ToArray();
				ROSConnector.GetComponent<CollisionObjectManager>().UpdateScene(planning_scene);

				yield return new WaitForSeconds(ROSUpdateRate);
			}
		}
		public MessageTypes.Geometry.Point GetGeometryPoint(Vector3 position)
		{
			MessageTypes.Geometry.Point geometryPoint = new MessageTypes.Geometry.Point();
			geometryPoint.x = position.x;
			geometryPoint.y = position.y;
			geometryPoint.z = position.z;
			return geometryPoint;
		}
		public MessageTypes.Geometry.Quaternion GetGeometryQuaternion(Quaternion quaternion)
		{
			MessageTypes.Geometry.Quaternion geometryQuaternion = new MessageTypes.Geometry.Quaternion();
			geometryQuaternion.x = quaternion.x;
			geometryQuaternion.y = quaternion.y;
			geometryQuaternion.z = quaternion.z;
			geometryQuaternion.w = quaternion.w;
			return geometryQuaternion;
		}
	}
}