using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Obi;
using RosSharp.RosBridgeClient;
using RosSharp;
using TMPro;


[RequireComponent(typeof(ObiActor))]
public class ObiControl : MonoBehaviour
{
	ObiActor actor;
    ObiActor reference_actor;
    public bool ENABLE_SIMULATION;
    public bool COMPUTER_SIMULATION;
    public GameObject pick_1;
	public GameObject place_1;
	public GameObject pick_2;
	public GameObject place_2;
    public ObiActor reference_cloth;    
    public GameObject robotFrame;
    public GameObject Speech_obj;
    public GameObject surround;
    public Material see_through;
    //public GameObject computer_subscriber;
    public RosSharp.RosBridgeClient.unityComputerPoints computer_subscriber;
    public RosSharp.RosBridgeClient.UnityStateSubscriber unity_state_subscriber;
    public ObiSolver solver;
    private ObiParticlePicker pp;
    private VoiceCommands VC;
    DateTime? start_time = null;
    DateTime? collide_time = null;
    private List<List<Vector3>> pick_place_list = new List<List<Vector3>>();
    private List<List<Vector4>> saved_state = new List<List<Vector4>>();
    private List<List<Vector3>> saved_sphere_positions = new List<List<Vector3>>();
    private List<List<float>> saved_masses = new List<List<float>>();
    private List<GameObject> pointer_list = new List<GameObject>();
    private List<GameObject> spheres = new List<GameObject>();
    private List<Vector3> spheres_orig_pos = new List<Vector3>();
    public bool[] spheres_in = new bool[4];
    private int which_pick = 0;
    private int sim_number = 0;

    // ROS Connector to communicate with ROS
    private GameObject ROSConnector;

    private void Start()
    {
        ROSConnector = GameObject.Find("ROS Connector");
        var grid = GameObject.Find("Workspace");
        grid.GetComponent<MeshRenderer>().enabled = false;
        actor = GetComponent<ObiActor>();
        pp = actor.GetComponent<ObiParticlePicker>();
        VC = Speech_obj.GetComponent<VoiceCommands>();

        spheres.Add(pick_1);
        spheres.Add(place_1);
        spheres.Add(pick_2);
        spheres.Add(place_2);

        pointer_list.Add(GameObject.Find("pick_marker1"));
        pointer_list.Add(GameObject.Find("place_marker1"));
        pointer_list.Add(GameObject.Find("pick_marker2"));
        pointer_list.Add(GameObject.Find("place_marker2"));
        pointer_list.Add(GameObject.Find("pick_marker3"));
        pointer_list.Add(GameObject.Find("place_marker3"));
        ResetMarkers();        

        computer_subscriber = ROSConnector.GetComponent<unityComputerPoints>();
        //Get_cloth_state();
        spheres_orig_pos.Add(pick_1.transform.position );
        spheres_orig_pos.Add(place_1.transform.position);
        spheres_orig_pos.Add(pick_2.transform.position );
        spheres_orig_pos.Add(place_2.transform.position);

        // Initial enables/disables
        solver.GetComponent<ObiSolver>().enabled = false;
        actor.GetComponent<ObiCloth>().enabled = false;
        actor.GetComponent<LineRenderer>().enabled = false;
        surround.SetActive(true);
        pick_1.SetActive(true);
        place_1.SetActive(true);
        pick_2.SetActive(false);        
        place_2.SetActive(false);
        reference_cloth.enabled = true;
        reference_cloth.GetComponent<MeshRenderer>().material = see_through;
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

    public void ResetPickPlace()
    {
        for(int i = 0; i < 2; i++)
        {
            spheres[i].SetActive(true);
            spheres[i].transform.position = spheres_orig_pos[i];
        }
        for (int i = 2; i < 4; i++)
        {
            spheres[i].SetActive(false);
            spheres[i].transform.position = spheres_orig_pos[i];
        }
        Debug.Log("Reset pick and place");
    }

    public void Reset_all()
	{
        if (ENABLE_SIMULATION)
        {
            VC.ClearPoints();
            pp.Release_cloth();
            Reset_cloth();
            ResetPickPlace();
            pick_place_list = new List<List<Vector3>>();
            saved_state = new List<List<Vector4>>();
            saved_sphere_positions = new List<List<Vector3>>();
            saved_masses = new List<List<float>>();
            ResetMarkers();
            print("Reset All!");
            sim_number = 0;
        }
        else
        {
            VC.ClearPoints();
            ResetPickPlace();
            pick_place_list = new List<List<Vector3>>();
            ResetMarkers();
            print("Reset All! -- No Sim");
        }
	}

    public void ResetMarkers()
    {
        Debug.Log("Hiding Markers");
        int x = pointer_list.Count;
        for (int i = 0; i < x; i++)
        {
            pointer_list[i].SetActive(false);
        }
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
        VC.ExecuteCustom();
    }

    public void VisualiseMoveit()
    {
        if(ENABLE_SIMULATION)
        {
            if(pp.pickedParticleIndexs.Count > 0)
            {
                Debug.Log("Already executing a simulation!");
                return;
            }
            VC.ClearPoints();
            if((spheres_in[0] == true) && (spheres_in[1] == true))
            {
                which_pick = 0;
            }
            else if ((spheres_in[2] == true) && (spheres_in[3] == true))
            {
                which_pick = 2;
            }
            else
            {
                Debug.Log("Not enough spheres are in the region - two are needed!");
                return;
            }
            solver.GetComponent<ObiSolver>().enabled = true;
            actor.GetComponent<ObiCloth>().enabled = true;
            Debug.Log(solver.GetComponent<ObiSolver>().enabled);
            Debug.Log(actor.GetComponent<ObiCloth>().enabled);
            
            pp.pickLocation = spheres[which_pick].transform.position;
            pp.placeLocation = spheres[which_pick + 1].transform.position;
            pp.executing = true;
            Debug.Log(pp.pickLocation);
            for (int i = 0; i < 10000; i++)
            {
                int a = 1;
            }            
            bool found_particles = pp.Find_closest_particles(pp.pickLocation);
            if (found_particles == true)
            {
                VC.SetPointCustom(pp.pickLocation, pp.placeLocation);
                VC.LockPathMoveit();
                SaveState(spheres[which_pick], spheres[which_pick + 1]);
                VC.ClearPoints();
            }
            else
            {
                if (!COMPUTER_SIMULATION)
                {
                    solver.GetComponent<ObiSolver>().enabled = false;
                    actor.GetComponent<ObiCloth>().enabled = false;
                }
                sim_number--;                
                Debug.Log("Could not find particles near the pick location...");
            }
            Disable_spheres(which_pick);
            if (which_pick == 0)
            {
                spheres[which_pick + 2].SetActive(true);
                spheres[which_pick + 3].SetActive(true);
            }
            which_pick += 2;
        }
    }

    public void VisualiseMultiFold() 
    {
        if (ENABLE_SIMULATION)
        {
            if (pick_place_list.Count == 2)
            {
                Debug.Log("Performing Multi fold");
                pp.Release_cloth();
                actor.ResetParticles();
                solver.GetComponent<ObiSolver>().enabled = false;
                actor.GetComponent<ObiCloth>().enabled = false;
                start_time = DateTime.Now;

                VC.SetPointCustomMulti(pick_place_list);
                VC.LockPathKinect();
                AddVisualMarkers();
            }
            else
            {
                Debug.Log("Not enough points selected...");
            }

            // put arrows and text over the pick and place points
            VC.ClearPoints();
        }
        else
        {
            bool valid = true;
            for(int i = 0; i < spheres_in.Length; i++)
            {
                if(spheres_in[i] == false)
                {
                    valid = false;
                }
            }
            if (valid)
            {
                SaveState(spheres[0], spheres[1]);
                SaveState(spheres[2], spheres[3]);
                VC.SetPointCustomMulti(pick_place_list);
                VC.LockPathKinect();
                AddVisualMarkers();
                for(int i = 0; i < spheres.Count - 1; i+=2)
                {
                    Disable_spheres(i);
                }
                VC.ClearPoints();
            }
            else
            {
                Debug.Log("One or more spheres are in valid positions!");
            }
        }
    }

    public void Visualise()
    {
        Debug.Log("setting points...");
        // Set the pick and place
        reference_actor = reference_cloth.GetComponent<ObiActor>();

        var pickLocation = pick_1.transform.position;
        var endLocation = place_1.transform.position;
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

    public void SaveState(GameObject pick, GameObject place)
    {
        if (ENABLE_SIMULATION)
        {
            List<float> temp_mass_list = new List<float>();
            List<Vector4> temp_state_list = new List<Vector4>();
            if (saved_state.Count == 0)
            {
                Debug.Log("Saving initial state");
                for (int i = 0; i < solver.renderablePositions.count; ++i)
                {
                    //Vector3 Pos = solver.renderablePositions[i];
                    Vector4 Pos = solver.positions[i];
                    float m = solver.invMasses[i];
                    temp_mass_list.Add(m);
                    temp_state_list.Add(Pos);
                }
            }
            else
            {
                Debug.Log("Saving state");
                for (int i = 0; i < solver.renderablePositions.count; ++i)
                {
                    //Vector3 Pos = solver.renderablePositions[i];
                    Vector4 Pos = solver.positions[i];
                    float m = solver.invMasses[i];
                    temp_mass_list.Add(m);
                    temp_state_list.Add(Pos);
                }
            }
            saved_masses.Add(temp_mass_list);
            saved_state.Add(temp_state_list);
            // Also save the sphere positions
            List<Vector3> temp_sphere_list = new List<Vector3>();
            temp_sphere_list.Add(pick.transform.position);
            temp_sphere_list.Add(place.transform.position);
            saved_sphere_positions.Add(temp_sphere_list);
        }
        // Save positions to list for RV controller
        List<Vector3> temp_list = new List<Vector3>();
        temp_list.Add(pick.transform.position);
        temp_list.Add(place.transform.position);
        pick_place_list.Add(temp_list);
    }

    public void Disable_spheres(int i)
    {
        ResetSpheresToStart(i);
        spheres[i].SetActive(false);
        spheres[i + 1].SetActive(false);
    }
    
    public void ResetSpheresToStart(int i)
    {
        spheres[i].transform.position = spheres_orig_pos[i];
        spheres[i + 1].transform.position = spheres_orig_pos[i + 1];
        spheres_in[i] = false;
        spheres_in[i + 1] = false;
    }

    public void LoadSavedState()
    {
        if (ENABLE_SIMULATION)
        {
            which_pick -= 2;
            if (which_pick < 0)
            {
                which_pick = 0;
            }
            else if (which_pick == 0)
            {
                Disable_spheres(2);
            }
            GameObject pick = spheres[which_pick];
            GameObject place = spheres[which_pick + 1];
            int x = saved_state.Count;
            if (x > 0)
            {
                for (int i = 0; i < solver.renderablePositions.count; ++i)
                {
                    solver.invMasses[i] = 0;
                    solver.positions[i] = saved_state[x - 1][i];
                    //solver.renderablePositions[i] = saved_state[i];
                }
                // re-apply the inverse masses
                for (int i = 0; i < solver.renderablePositions.count; ++i)
                {
                    solver.invMasses[i] = saved_masses[x - 1][i];
                }
                pick.SetActive(true);
                place.SetActive(true);
                pick.transform.position = saved_sphere_positions[x - 1][0];
                place.transform.position = saved_sphere_positions[x - 1][1];
                saved_sphere_positions.RemoveAt(x - 1);
                pick_place_list.RemoveAt(x - 1);
                saved_state.RemoveAt(x - 1);
                saved_masses.RemoveAt(x - 1);
                Debug.Log("Sucessfully loaded previous state");
            }
            else
            {
                Debug.Log("Unable to load previous state.... Previous state does not exist?");
                ResetSpheresToStart(which_pick);
                actor.ResetParticles();
            }
            actor.GetComponent<ObiParticlePicker>().Release_cloth();
        }
    }

    public void LoadSavedStateComputer()
    {
        which_pick -= 2;
        if (which_pick < 0)
        {
            which_pick = 0;
        }
        else if (which_pick == 0)
        {
            Disable_spheres(2);
        }
        int x = saved_state.Count;
        if (x > 0)
        {
            for (int i = 0; i < solver.renderablePositions.count; ++i)
            {
                solver.invMasses[i] = 0;
                solver.positions[i] = saved_state[x - 1][i];
                //solver.renderablePositions[i] = saved_state[i];
            }
            // re-apply the inverse masses
            for (int i = 0; i < solver.renderablePositions.count; ++i)
            {
                solver.invMasses[i] = saved_masses[x - 1][i];
            }
            Debug.Log("Sucessfully loaded previous state");
            saved_state.RemoveAt(x - 1);
            saved_masses.RemoveAt(x - 1);
        }
        else
        {
            Debug.Log("Unable to load previous state.... Previous state does not exist?");
            ResetSpheresToStart(which_pick);
            actor.ResetParticles();
        }
        actor.GetComponent<ObiParticlePicker>().Release_cloth();
    }

    public void AddVisualMarkers()
    {
        int x = pick_place_list.Count * 2;
        int c = 0;
        Debug.Log("Setting positions");
        for (int i = 0; i < x; i+=2)
        {
            pointer_list[i].SetActive(true);
            pointer_list[i].transform.position = pick_place_list[c][0];
            pointer_list[i + 1].SetActive(true);
            pointer_list[i + 1].transform.position = pick_place_list[c][1];
            c++;
        }
    }

    public bool InWorkSpaceCheck(int a, int b)
    {        
        Debug.LogFormat("1: {0},   2: {1}", spheres_in[a], spheres_in[b]);
        bool result = spheres_in[a] && spheres_in[b];
        return result;
    }

    private void Update()
	{
        //int count = ROSConnector.GetComponent<ClothPoseSubscriber>().counter;
        //if (count > 0)
        //{
        //    Set_cloth_state();
        //}

        //if(start_time != null)
        //{
        //    if( (((TimeSpan)(DateTime.Now - start_time)).TotalMilliseconds) > 10*1000 )
        //    {
        //        pick.SetActive(true);
        //        place.SetActive(true);
        //        pick.transform.position = pick_orig_pos;
        //        place.transform.position = place_orig_pose;
        //        start_time = null;
        //    }
        //}
        //else
        //{
        //    //Debug.Log("Start time is null");
        //}
        if (COMPUTER_SIMULATION)
        {
            if (unity_state_subscriber.fold == true)
            {
                Reset_all();                
                unity_state_subscriber.fold = false;
            }
            else if (unity_state_subscriber.undo == true)
            {
                unity_state_subscriber.undo = false;
                sim_number--;
                LoadSavedStateComputer();
            }

            if (computer_subscriber.message_received == true)
            {
                computer_subscriber.message_received = false;                
                // Positions recieved are on the x-z plane at y=0
                var pick_rec = computer_subscriber.pick;
                var place_rec = computer_subscriber.place;
                var pick_world = robotFrame.transform.TransformPoint(pick_rec);
                var place_world = robotFrame.transform.TransformPoint(place_rec);
                pick_world.y = pick_1.transform.position.y;
                place_world.y = pick_1.transform.position.y;
                // Set state of pick and place locations using the spheres
                if ((computer_subscriber.sim_number == 1) && (sim_number < 1))
                {
                    collide_time = DateTime.Now;
                    spheres[0].SetActive(true);
                    spheres[1].SetActive(true);
                    spheres_in[2] = false;
                    spheres_in[3] = false;
                    pick_1.transform.position = pick_world;
                    place_1.transform.position = place_world;
                    sim_number = 1;
                }
                else if ((computer_subscriber.sim_number == 2) && (sim_number == 1))
                {
                    collide_time = DateTime.Now;
                    spheres[2].SetActive(true);
                    spheres[3].SetActive(true);
                    spheres_in[0] = false;
                    spheres_in[1] = false;
                    pick_2.transform.position = pick_world;
                    place_2.transform.position = place_world;
                    sim_number = 2;
                }
                else
                {
                    Debug.Log("Incorrect sequence!");
                    Debug.Log(computer_subscriber.sim_number);
                    Debug.Log(sim_number);
                    Debug.Log("");
                    return;
                }
                Debug.Log("Simuating...");
            }
            // Wait for collisions to occur
            if(collide_time != null)
            {
                double t = ((TimeSpan)(DateTime.Now - collide_time)).TotalMilliseconds;
                if(t > 500)
                {
                    collide_time = null;
                    bool check = false;
                    if (computer_subscriber.sim_number == 1)
                    {
                        check = InWorkSpaceCheck(0, 1);
                    }
                    else if (computer_subscriber.sim_number == 2)
                    {
                        check = InWorkSpaceCheck(2, 3);
                    }

                    if (check)
                    {
                        VisualiseMoveit();
                    }
                    else
                    {
                        Debug.Log("Invalid points!");
                    }
                }
            }
        }

    }
}
