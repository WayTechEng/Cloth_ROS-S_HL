using System;
using UnityEngine;
using Obi;
using RosSharp.RosBridgeClient;
using RosSharp;


[RequireComponent(typeof(ObiActor))]
public class ObiControl : MonoBehaviour
{
	ObiActor actor;
	GameObject pick;
	GameObject end;
    public GameObject robotFrame; 
    public GameObject Speech_obj;
    public ObiSolver solver;
    Vector3 pick_orig_pos;
	Vector3 end_orig_pos;
	double threshold_distance = 0.01f;
    private DateTime startTime, tempTime;
    double time_threshold = 2000;

    // ROS Connector to communicate with ROS
    private GameObject ROSConnector;

    private void Start()
    {
        ROSConnector = GameObject.Find("ROS Connector");
        pick = GameObject.Find("Pick");
        end = GameObject.Find("End");

        Get_cloth_state();
        pick_orig_pos = pick.transform.position;
        end_orig_pos = end.transform.position;
    }

    public void Get_cloth_state()
    {
        ROSConnector.GetComponent<RobotCommandPublisher>().SendCommand(RobotCommandPublisher.GET_CLOTH_STATE);
        Debug.Log("Requested to get cloth state");
    }    

    public void Set_cloth_state()
    {
        var pos = ROSConnector.GetComponent<ClothPoseSubscriber>().position;
        var ori = ROSConnector.GetComponent<ClothPoseSubscriber>().orientation;
        Debug.Log(pos);

        var current_solver_position_wrt_world = solver.transform.position;
        var current_solver_orientation_wrt_world = solver.transform.rotation;

        var new_solver_position_wrt_world = robotFrame.transform.TransformPoint(pos);
        new_solver_position_wrt_world.y = current_solver_position_wrt_world.y;
        //new_solver_position_wrt_world.z = current_solver_position_wrt_world.z;

        solver.transform.SetPositionAndRotation(new_solver_position_wrt_world, current_solver_orientation_wrt_world);
    }

    public void Reset_all()
	{
		if (actor == null || !actor.isLoaded)
		{
			return;
		}
		actor.ResetParticles();
		pick.transform.position = pick_orig_pos;
		end.transform.position = end_orig_pos;
		print("Reset All!");		
	}
	public void Reset_cloth()
	{
		if (actor == null || !actor.isLoaded)
		{
			return;
		}
		actor.ResetParticles();
		print("Reset Cloth!");
	}

	public void Fold()
    {
        var VC = Speech_obj.GetComponent<VoiceCommands>();
        VC.ExecuteCustom();
    }

    public void VisualiseMoveit()
    {
        Debug.Log("setting points...");
        // Set the pick and place
        actor = GetComponent<ObiActor>();
        

        var pickLocation = pick.transform.position;
        var endLocation = end.transform.position;
        var VC = Speech_obj.GetComponent<VoiceCommands>();

        // If points alread created then clear them
        var clones = GameObject.FindGameObjectsWithTag("clone");
        if (clones.Length != 0)
        {
            VC.ClearPoints();
        }
        // Send message to unity first!
        VC.SetPointCustom(pickLocation, endLocation);
        VC.LockPathMoveit();
        actor.GetComponent<ObiParticlePicker>().executing = true;
    }


    public void Visualise()
    {
        Debug.Log("setting points...");
        // Set the pick and place
        actor = GetComponent<ObiActor>();
        pick = GameObject.Find("Pick");
        end = GameObject.Find("End");

        var pickLocation = pick.transform.position;
        var endLocation = end.transform.position;
        var VC = Speech_obj.GetComponent<VoiceCommands>();

        // Transform object to position relative to cloth frame
        //Debug.LogFormat("Pick position in world frame:\n{0}\n", pickLocation.ToString("F3"));
        var pickLocationCloth = actor.transform.InverseTransformPoint(pickLocation);
        var placeLocationCloth = actor.transform.InverseTransformPoint(endLocation);
        Debug.LogFormat("Pick position W.R.T. Cloth:\n{0}\n", pickLocationCloth.ToString("F3"));
        Debug.LogFormat("Place position W.R.T. Cloth:\n{0}\n", placeLocationCloth.ToString("F3"));

        //////////////////////////////
        /// Need to normalise position WRT cloth frame to pass to Kinect program
        //////////////////////////////
        // Find the relative size of of the cloth..
        Matrix4x4 solver2World = solver.transform.localToWorldMatrix;
        float minx = float.MaxValue;
        float minz = float.MaxValue;
        float maxx = float.MinValue;
        float maxz = float.MinValue;
        for (int i = 0; i < solver.renderablePositions.count; ++i)
        {
            Vector3 worldPos = solver2World.MultiplyPoint3x4(solver.renderablePositions[i]);
            float x = worldPos.x;
            float z  = worldPos.z;

            if (x > maxx)
            {
                maxx = x;
            }
            if(z > maxz)
            {
                maxz = z;
            }
            if (x < minx)
            {
                minx = x;
            }
            if (z < minz)
            {
                minz = z;
            }
        }
        float size_x = Math.Abs(maxx - minx);
        float size_z = Math.Abs(maxz - minz);
        Debug.LogFormat("SIZE OF X x Z:   {0}  x {1} ", size_x.ToString("F3"), size_z.ToString("F3"));

        // Check for pick/place bounding conditions
        if (pickLocationCloth.x < -size_x / 2) pickLocationCloth.x = -size_x / 2;
        if (pickLocationCloth.x > size_x / 2) pickLocationCloth.x = size_x / 2;
        if (pickLocationCloth.y < -size_z / 2) pickLocationCloth.y = -size_z / 2;
        if (pickLocationCloth.y > size_z / 2) pickLocationCloth.y = size_z / 2;
        if (placeLocationCloth.x < -size_x / 2) placeLocationCloth.x = -size_x / 2;
        if (placeLocationCloth.x > size_x / 2)  placeLocationCloth.x = size_x / 2;
        if (placeLocationCloth.y < -size_z / 2) placeLocationCloth.y = -size_z / 2;
        if (placeLocationCloth.y > size_z / 2)  placeLocationCloth.y = size_z / 2;

        Debug.LogFormat("Pick position BOUNDING:\n{0}\n", pickLocationCloth.ToString("F3"));
        Debug.LogFormat("Place position BOUNDING:\n{0}\n", placeLocationCloth.ToString("F3"));



        // Normalise pick and place positions in the cloths frame
        // Arbitrarily set the zero position at x = -(size/2) , z = +(size/2) ..... Top left corner when looking from game view
        // It might be better in future to change this when the position of the kinect sensor is confirmed
        // While we are here, may as well normalise to x and y as well...

        // We now want the zero position at x = +(size/2), z = +(size/2) ...... Top right corner when looking from game corner
        // We also want x pointing in the negative z, and y pointing in the negative x ... to match the cameras frame!
        Vector3 pick_norm_cloth = new Vector3(0, 0, 0);
        Vector3 place_norm_cloth = new Vector3(0, 0, 0);

        //////// Zero at bottom left corner (Game view)
        //pick_norm_cloth.x = pickLocationCloth.x + size_x/2;
        ////pick_norm_cloth.z = -pickLocationCloth.z + size_z/2;
        //pick_norm_cloth.z = pickLocationCloth.z + size_z/2;
        //place_norm_cloth.x = placeLocationCloth.x + size_x/2;
        ////place_norm_cloth.z = -placeLocationCloth.z + size_z/2;
        //place_norm_cloth.z = placeLocationCloth.z + size_z/2;

        ///////// Zero at top right corner (game view)
        pick_norm_cloth.x = -pickLocationCloth.x + size_x / 2;
        //pick_norm_cloth.z = -pickLocationCloth.y + size_z / 2;
        pick_norm_cloth.z = pickLocationCloth.y + size_z / 2;
        place_norm_cloth.x = -placeLocationCloth.x + size_x / 2;
        //place_norm_cloth.z = -placeLocationCloth.y + size_z / 2;
        place_norm_cloth.z = placeLocationCloth.y + size_z / 2;

        Debug.LogFormat("Pick..... X x Z:   {0}  x {1} ", pick_norm_cloth.x.ToString("F3"), pick_norm_cloth.z.ToString("F3"));
        Debug.LogFormat("Place..... X x Z:   {0}  x {1} ", place_norm_cloth.x.ToString("F3"), place_norm_cloth.z.ToString("F3"));

        pick_norm_cloth.x = pick_norm_cloth.x / size_x;
        pick_norm_cloth.z = pick_norm_cloth.z / size_z;
        place_norm_cloth.x = place_norm_cloth.x / size_x;
        place_norm_cloth.z = place_norm_cloth.z / size_z;

        Debug.LogFormat("Pick NORM..... X x Z:   {0}  x {1} ", pick_norm_cloth.x.ToString("F3"), pick_norm_cloth.z.ToString("F3"));
        Debug.LogFormat("Place NORM..... X x Z:   {0}  x {1} ", place_norm_cloth.x.ToString("F3"), place_norm_cloth.z.ToString("F3"));

        // If points alread created then clear them
        var clones = GameObject.FindGameObjectsWithTag("clone");
        if (clones.Length != 0)
        {
            VC.ClearPoints();
        }

        //VC.SetPointCustom(pickLocation, endLocation);
        VC.SetPointToKinect(pickLocation, endLocation, pick_norm_cloth, place_norm_cloth);

        // Calculate path (send to ROS)
        //VC.LockPath();
        VC.LockPathKinect();
        //actor.GetComponent<ObiParticlePicker>().path_locked = true;
        
    }

    private void Update()
	{
        int count = ROSConnector.GetComponent<ClothPoseSubscriber>().counter;
        if (count > 0)
        {
            Set_cloth_state();
        }

	}
}
