using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Obi;
using RosSharp.RosBridgeClient;
using RosSharp;


[RequireComponent(typeof(ObiActor))]
public class ObiControl : MonoBehaviour
{
	ObiActor actor;
	GameObject pick;
	GameObject end;
    public ObiActor reference_cloth;
    ObiActor reference_actor;
    public GameObject robotFrame; 
    public GameObject Speech_obj;
    public Material see_through;
    //public GameObject computer_subscriber;
    public RosSharp.RosBridgeClient.unityComputerPoints computer_subscriber;
    public ObiSolver solver;
    Vector3 pick_orig_pos;
	Vector3 end_orig_pos;
	double threshold_distance = 0.01f;
    private DateTime startTime, tempTime;
    double time_threshold = 2000;
    private List<Vector3> pick_list = new List<Vector3>();
    private List<Vector3> place_list = new List<Vector3>();
    private bool first_fold = false;
    private List<Vector4> saved_state = new List<Vector4>();
    private List<float> saved_masses = new List<float>();

    // ROS Connector to communicate with ROS
    private GameObject ROSConnector;

    private void Start()
    {
        ROSConnector = GameObject.Find("ROS Connector");
        pick = GameObject.Find("Pick");
        end = GameObject.Find("End");
        actor = GetComponent<ObiActor>();

        MaterialPropertyBlock props = new MaterialPropertyBlock();
        props.SetColor("_Color", Color.clear);
        //props.SetColor("")
        //reference_cloth.GetComponent<Renderer>().SetPropertyBlock(props);
        reference_cloth.GetComponent<MeshRenderer>().material = see_through;

        // Save the initial cloth state
        //init_bp = actor.GetComponent<ObiActorBlueprint>();

        // Disable solver at the begginning
        solver.GetComponent<ObiSolver>().enabled = false;

        computer_subscriber = ROSConnector.GetComponent<unityComputerPoints>();

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
        Debug.Log("Reset Cloth!");
        solver.GetComponent<ObiSolver>().enabled = false;
        actor.GetComponent<ObiCloth>().enabled = false;        
        Debug.Log("Hiding cloth");        
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

        var pickLocation = pick.transform.position;
        var endLocation = end.transform.position;
        pick_list.Add(pick.transform.position);
        place_list.Add(end.transform.position);
        var VC = Speech_obj.GetComponent<VoiceCommands>();

        // If points alread created then clear them. Also reset all the particles
        VC.ClearPoints();

        // Send message to unity first!
        VC.SetPointCustom(pickLocation, endLocation);
        VC.LockPathMoveit();
        actor.GetComponent<ObiParticlePicker>().executing = true;
        // Enabled solver physics and show the cloth
        solver.GetComponent<ObiSolver>().enabled = true;
        actor.GetComponent<ObiCloth>().enabled = true;
        // Reset particles
        //actor.ResetParticles();
    }

    public void VisualiseMultiFold()
    {
        //Debug.LogFormat("Length of pick_list is: {0}",pick_list.Count);
        //Debug.Log(place_list[0]);
        if (pick_list.Count == 2)
        {
            Debug.Log("Performing Multi fold");
            var VC = Speech_obj.GetComponent<VoiceCommands>();
            VC.SetPointCustomMulti(pick_list, place_list);
            VC.LockPathKinect();
        }
        else if(pick_list.Count < 2)
        {
            Debug.Log("Not enough points selected");
        }
        else if (pick_list.Count > 2)
        {
            Debug.Log("Too many points selected");
        }
    }

    public void Visualise()
    {
        Debug.Log("setting points...");
        // Set the pick and place
        reference_actor = reference_cloth.GetComponent<ObiActor>();
        pick = GameObject.Find("Pick");
        end = GameObject.Find("End");

        var pickLocation = pick.transform.position;
        var endLocation = end.transform.position;
        var VC = Speech_obj.GetComponent<VoiceCommands>();

        // Transform object to position relative to cloth frame
        //Debug.LogFormat("Pick position in world frame:\n{0}\n", pickLocation.ToString("F3"));
        var pickLocationCloth = reference_actor.transform.InverseTransformPoint(pickLocation);
        var placeLocationCloth = reference_actor.transform.InverseTransformPoint(endLocation);
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
        VC.ClearPoints();

        // Set points and lock path
        VC.SetPointCustom(pickLocation, endLocation);
        //VC.SetPointToKinect(pickLocation, endLocation, pick_norm_cloth, place_norm_cloth);
        VC.LockPath();
        //VC.LockPathKinect();

        Reset_cloth();
        solver.GetComponent<ObiSolver>().enabled = false;
        actor.GetComponent<ObiCloth>().enabled = false;
    }

    public void save_state()
    {
        Debug.Log("Saving state");
        if (saved_state.Count == 0)
        {
            for (int i = 0; i < solver.renderablePositions.count; ++i)
            {
                //Vector3 Pos = solver.renderablePositions[i];
                Vector4 Pos = solver.positions[i];
                float m = solver.invMasses[i];
                saved_masses.Add(m);
                saved_state.Add(Pos);
            }
        }
        else
        {
            for (int i = 0; i < solver.renderablePositions.count; ++i)
            {
                //Vector3 Pos = solver.renderablePositions[i];
                Vector4 Pos = solver.positions[i];
                float m = solver.invMasses[i];
                saved_masses.Add(m);
                saved_state[i] = Pos;
            }
        }
    }

    public void load_previous_state()
    {        
        Debug.Log("Loading previous state");
        for (int i = 0; i < solver.renderablePositions.count; ++i)
        {            
            solver.invMasses[i] = 0;
            solver.positions[i] = saved_state[i];
            //solver.renderablePositions[i] = saved_state[i];
        }
        // re-apply the inverse masses
        for (int i = 0; i < solver.renderablePositions.count; ++i)
        {
            solver.invMasses[i] = saved_masses[i];
        }
    }

    private void Update()
	{
        int count = ROSConnector.GetComponent<ClothPoseSubscriber>().counter;
        if (count > 0)
        {
            Set_cloth_state();
        }

        bool do_computer_sim = computer_subscriber.message_received;
        if (do_computer_sim == true)
        {
            Debug.Log("Simuating...");
            // Positions recieved are on the x-z plane at y=0
            var pick_rec = computer_subscriber.pick;
            var place_rec = computer_subscriber.place;
            var pick_world = robotFrame.transform.TransformPoint(pick_rec);
            var place_world = robotFrame.transform.TransformPoint(place_rec);
            // Set state of pick and place locations using the spheres
            pick.transform.position = pick_world;
            end.transform.position = place_world;

            // Visualise moveit
            VisualiseMoveit();

            // Reset the flag
            computer_subscriber.message_received = false;
        }


    }
}
