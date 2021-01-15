using System;
using UnityEngine;
using Obi;


[RequireComponent(typeof(ObiActor))]
public class ObiControl : MonoBehaviour
{
	ObiActor actor;
	GameObject pick;
	GameObject end;
	public GameObject Speech_obj;
	Vector3 pick_orig_pos;
	Vector3 end_orig_pos;
	double threshold_distance = 0.01f;
    private DateTime startTime, tempTime;
    double time_threshold = 2000;

    public void Init()
    {
        pick_orig_pos = pick.transform.position;
        end_orig_pos = end.transform.position;
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

        // If points alread created then clear them
        var clones = GameObject.FindGameObjectsWithTag("clone");
        if (clones.Length != 0)
        {
            VC.ClearPoints();
        }

        VC.SetPointCustom(pickLocation, endLocation);

        // Calculate path (send to ROS)
        VC.LockPath();
        actor.GetComponent<ObiParticlePicker>().path_locked = true;


        //// "Grab" cloth when in "contact" with pick location
        //double dist = double.MaxValue;
        //while (dist >= threshold_distance)
        //{
        //	Vector3 EE_pos = actor.GetComponent<ObiParticlePicker>().EE.transform.position;
        //	Vector3 delta = EE_pos - pickLocation;
        //	dist = delta.magnitude;
        //	Debug.Log("Stuck in loop......");
        //	if(Input.GetKey(KeyCode.Escape))
        //          {
        //		dist = 0;
        //          }
        //}
        //// Start the grab
        //actor.GetComponent<ObiParticlePicker>().init_grab_cloth = 1;
        //actor.GetComponent<ObiParticlePicker>().continue_grab_cloth = 1;
    }

    private void Update()
	{
		//if (Input.GetKey(KeyCode.RightShift))
		//{
		//	Reset_all();
		//}
		int a;
	}
}
