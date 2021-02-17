using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obi;

public class ClothTransformer : MonoBehaviour
{
    public ObiSolver solver;
    public GameObject robotFrame;
    public GameObject cloth_state_subscriber;
    // Start is called before the first frame update
    void Start()
    {
        //Debug.Log("Hello");
    }

    // Update is called once per frame
    void Update()
    {

        var solver_position_wrt_world = solver.transform.position;
        var solver_orientation_wrt_world = solver.transform.rotation;
        var solver_position_wrt_robot = robotFrame.transform.InverseTransformPoint(solver_position_wrt_world);
        //Debug.LogFormat("Position in robot frame (unity): {0}\nOrientation in robot frame (unity): {1}", solver_position_wrt_robot.ToString("F3"), solver_orientation_wrt_world.ToString("F3"));
    }

    public void Set_position()
    {
        var current_solver_position_wrt_world = solver.transform.position;
        var solver_orientation_wrt_world = solver.transform.rotation;
        Quaternion q = Quaternion.Euler(0.0F, 45.0F, 0.0F);
        Vector3 cloth_position = new Vector3(0.3F, 0.05F, 0.5F);
        var new_solver_position_wrt_world = robotFrame.transform.TransformPoint(cloth_position);
        new_solver_position_wrt_world.y = current_solver_position_wrt_world.y;
        solver.transform.SetPositionAndRotation(new_solver_position_wrt_world, q);
    }
}
