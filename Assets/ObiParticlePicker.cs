using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Obi
{
    public class ObiParticlePicker : MonoBehaviour
    {

        public class ParticlePickEventArgs : EventArgs
        {

            public int particleIndex;
            public Vector3 worldPosition;

            public ParticlePickEventArgs(int particleIndex, Vector3 worldPosition)
            {
                this.particleIndex = particleIndex;
                this.worldPosition = worldPosition;
            }
        }

        [Serializable]
        public class ParticlePickUnityEvent : UnityEvent<ParticlePickEventArgs> { }

        public ObiSolver solver;
        public ObiActor actor;
        public float radiusScale = 1;

        public ParticlePickUnityEvent OnParticlePicked;
        public ParticlePickUnityEvent OnParticleHeld;
        public ParticlePickUnityEvent OnParticleDragged;
        public ParticlePickUnityEvent OnParticleReleased;

        public GameObject speech_obj;

        private Vector3 lastMousePos = Vector3.zero;
        private int pickedParticleIndex = -1;
        private float pickedParticleDepth = 0;
        private double time_threshold = 500;

        private Vector3 pickLocation;  // Mouse pick location
        private Vector3 trailingLocation;
        private Vector3 endLocation;

        private int counter = 0;
        private DateTime startTime, endTime;
        private int steps = 0;
        private float movement = 0;
        private int caseswitch = 0;
        private int dt = 35;
        double threshold_distance = 0.015f;
        double threshold_distance_drop = 0.010f;

        private float[] arrx = new float[50];
        private float[] arry = new float[50];
        private float[] arrz = new float[50];

        public Vector3 pick;
        public Vector3 end;

        private Vector3 EE_pos;
        private Vector3 last_EE_pos = Vector3.zero;
        private Vector3 EE_pos_last = Vector3.zero;


        public bool path_locked = false;
        public bool executing = false;
        private bool hide_the_cloth = false;
        private DateTime hide_cloth_timer;

        public bool init_grab_cloth = false;
        public bool continue_grab_cloth = false;

        GameObject pick_obj;
        GameObject end_obj;

        public GameObject EE;
        //private GameObject ee;
        // Find the left finger game object
        //ee = GameObject.Find("Node-EE_To_Cloth");

        void Awake()
        {
            lastMousePos = Input.mousePosition;
            last_EE_pos = EE.transform.position;
            pick_obj = GameObject.Find("Pick");
            end_obj = GameObject.Find("End");
        }

        void LateUpdate()
        {
            Move_by_robot();
            //Theirs();
            //Mine();
            //Updater();
            lastMousePos = Input.mousePosition;
            last_EE_pos = EE.transform.position;
            if (hide_the_cloth == true)
            {
                DateTime T = DateTime.Now;
                double elapsed = ((TimeSpan)(T - hide_cloth_timer)).TotalMilliseconds;
                if ((elapsed > 1000) && (solver.GetComponent<ObiSolver>().enabled == true))
                {
                    solver.GetComponent<ObiSolver>().enabled = false;
                    Debug.Log("Stopping physics!");
                }
           
                if ((elapsed > 2000) && (actor.GetComponent<ObiCloth>().enabled == true))
                {
                    actor.GetComponent<ObiCloth>().enabled = false;
                    Debug.Log("Hiding the cloth!");
                }
            }
        }

        public void Move_by_robot()
        {
            if (solver != null)
            {
                pickLocation = pick_obj.transform.position;
                endLocation = end_obj.transform.position;

                if (executing)
                {
                    hide_the_cloth = false;
                    Vector3 delta_start = last_EE_pos - pickLocation;
                    if ((delta_start.magnitude <= threshold_distance) && !continue_grab_cloth)
                    {
                        // Start the grab
                        init_grab_cloth = true;
                        continue_grab_cloth = true;
                        executing = false;
                        path_locked = false;
                    }
                }

                // Attach cloth to EE                
                if (init_grab_cloth)
                {
                    Debug.Log("Grab the cloth!");
                    init_grab_cloth = false;
                    //Debug.Log("Looking for Closest point on cloth");
                    pickedParticleIndex = -1;

                    // EE position
                    EE_pos = EE.transform.position;
                    //Debug.LogFormat("Left_EE position: {0}", EE_pos.ToString("F3"));

                    // Init transform
                    Matrix4x4 solver2World = solver.transform.localToWorldMatrix;

                    // Find the closest particle hit by the ray:
                    double smallest = float.MaxValue;
                    for (int i = 0; i < solver.renderablePositions.count; ++i)
                    {
                        Vector3 worldPos = solver2World.MultiplyPoint3x4(solver.renderablePositions[i]);
                        double dx = EE_pos.x - worldPos.x;
                        double dz = EE_pos.z - worldPos.z;
                        double dist = Math.Sqrt(dx * dx + dz * dz);

                        if (dist < smallest)
                        {
                            smallest = dist;
                            pickedParticleIndex = i;
                        }
                    }

                    // Check that a particle has been found..
                    if (pickedParticleIndex >= 0)
                    {
                        if (OnParticlePicked != null)
                        {
                            OnParticlePicked.Invoke(new ParticlePickEventArgs(pickedParticleIndex, EE_pos));
                            startTime = DateTime.Now;
                        }
                    }

                } // End input check
                else if (pickedParticleIndex >= 0)
                {
                    EE_pos = EE.transform.position;
                    //Debug.LogFormat("Particle index: {0}", pickedParticleIndex);
                    // Drag:
                    Vector3 EE_delta = EE_pos - last_EE_pos;
                    if (EE_delta.magnitude > 0.001f && OnParticleDragged != null)
                    {
                        OnParticleDragged.Invoke(new ParticlePickEventArgs(pickedParticleIndex, EE_pos));
                        //Debug.Log("Dragging");
                        //Debug.LogFormat("Left_EE position: {0}", EE_pos.ToString("F3"));
                    }
                    // Hold:
                    else if (OnParticleHeld != null)
                    {
                        OnParticleHeld.Invoke(new ParticlePickEventArgs(pickedParticleIndex, EE_pos));
                        //Debug.Log("Holding");
                    }
                    // Release:
                    Vector3 delta_end = EE_pos - endLocation;
                    Vector2 delta_end_xz = new Vector2(delta_end.x, delta_end.z);
                    
                    if (delta_end_xz.magnitude <= threshold_distance_drop)
                    {
                        double dd = EE_pos.y - EE_pos_last.y;
                        Debug.Log(dd);
                        if (dd > 0)
                        {
                            release_cloth();
                        }
                    }
                    EE_pos_last = EE_pos;
                } // End drag event.
            }// End Solver check.
        }

        public void release_cloth()
        {
            // Stop showing cloth and stop the physics on cloth
            hide_the_cloth = true;
            hide_cloth_timer = DateTime.Now;

            continue_grab_cloth = false;
            executing = false;

            if (OnParticleReleased != null)
            {
                OnParticleReleased.Invoke(new ParticlePickEventArgs(pickedParticleIndex, EE_pos));
            }
            pickedParticleIndex = -1;
        }

        void Move_by_robot_manual()
        {
            if (solver != null)
            {
                // Attach cloth to EE

                if (Input.GetKeyDown(KeyCode.Return))
                {
                    Debug.Log("Return-key pressed");
                    //Debug.Log("Looking for Closest point on cloth");
                    pickedParticleIndex = -1;

                    // EE position
                    EE_pos = EE.transform.position;
                    //Debug.LogFormat("Left_EE position: {0}", EE_pos.ToString("F3"));

                    // Init transform
                    Matrix4x4 solver2World = solver.transform.localToWorldMatrix;

                    // Find the closest particle hit by the ray:
                    double smallest = float.MaxValue;
                    for (int i = 0; i < solver.renderablePositions.count; ++i)
                    {
                        Vector3 worldPos = solver2World.MultiplyPoint3x4(solver.renderablePositions[i]);
                        double dx = EE_pos.x - worldPos.x;
                        double dz = EE_pos.z - worldPos.z;
                        double dist = Math.Sqrt(dx * dx + dz * dz);

                        if (dist < smallest)
                        {
                            smallest = dist;
                            pickedParticleIndex = i;
                        }
                    }

                    // Check that a particle has been found..
                    if (pickedParticleIndex >= 0)
                    {
                        if (OnParticlePicked != null)
                        {
                            OnParticlePicked.Invoke(new ParticlePickEventArgs(pickedParticleIndex, EE_pos));
                            startTime = DateTime.Now;
                        }
                    }

                } // End input check
                else if (pickedParticleIndex >= 0)
                {
                    EE_pos = EE.transform.position;
                    Debug.LogFormat("Particle index: {0}", pickedParticleIndex);
                    // Drag:
                    Vector3 EE_delta = EE_pos - last_EE_pos;
                    if (EE_delta.magnitude > 0.001f && OnParticleDragged != null)
                    {
                        OnParticleDragged.Invoke(new ParticlePickEventArgs(pickedParticleIndex, EE_pos));
                        //Debug.Log("Dragging");
                        Debug.LogFormat("Left_EE position: {0}", EE_pos.ToString("F3"));
                    }
                    // Hold:
                    else if (OnParticleHeld != null)
                    {
                        OnParticleHeld.Invoke(new ParticlePickEventArgs(pickedParticleIndex, EE_pos));
                        //Debug.Log("Holding");
                    }
                    // Release:				
                    if (Input.GetKeyUp(KeyCode.Return))
                    {
                        if (OnParticleReleased != null)
                        {
                            OnParticleReleased.Invoke(new ParticlePickEventArgs(pickedParticleIndex, EE_pos));
                        }
                        pickedParticleIndex = -1;
                    }
                } // End drag event.
            }// End Solver check.
        }

        void Theirs()
        {
            if (solver != null)
            {
                // Click:
                if (Input.GetMouseButtonDown(0))
                {

                    pickedParticleIndex = -1;

                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    float closestMu = float.MaxValue;
                    float closestDistance = float.MaxValue;

                    Matrix4x4 solver2World = solver.transform.localToWorldMatrix;

                    // Find the closest particle hit by the ray:
                    for (int i = 0; i < solver.renderablePositions.count; ++i)
                    {

                        Vector3 worldPos = solver2World.MultiplyPoint3x4(solver.renderablePositions[i]);

                        float mu;
                        Vector3 projected = ObiUtils.ProjectPointLine(worldPos, ray.origin, ray.origin + ray.direction, out mu, false);
                        float distanceToRay = Vector3.SqrMagnitude(worldPos - projected);

                        // Disregard particles behind the camera:
                        mu = Mathf.Max(0, mu);

                        float radius = solver.principalRadii[i][0] * radiusScale;

                        if (distanceToRay <= radius * radius && distanceToRay < closestDistance && mu < closestMu)
                        {
                            closestMu = mu;
                            closestDistance = distanceToRay;
                            pickedParticleIndex = i;
                        }
                    }
                    print("---------------");
                    print(solver.renderablePositions[pickedParticleIndex]);
                    print(pickedParticleIndex);

                    if (pickedParticleIndex >= 0)
                    {
                        pickedParticleDepth = Camera.main.transform.InverseTransformVector(solver2World.MultiplyPoint3x4(solver.renderablePositions[pickedParticleIndex]) - Camera.main.transform.position).z;

                        if (OnParticlePicked != null)
                        {
                            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, pickedParticleDepth));
                            OnParticlePicked.Invoke(new ParticlePickEventArgs(pickedParticleIndex, worldPosition));
                            print(worldPosition);
                        }
                    }

                }
                else if (pickedParticleIndex >= 0)
                {

                    // Drag:
                    Vector3 mouseDelta = Input.mousePosition - lastMousePos;
                    if (mouseDelta.magnitude > 0.01f && OnParticleDragged != null)
                    {
                        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, pickedParticleDepth));
                        OnParticleDragged.Invoke(new ParticlePickEventArgs(pickedParticleIndex, worldPosition));
                        Debug.Log("Dragging");
                    }
                    else if (OnParticleHeld != null)
                    {
                        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, pickedParticleDepth));
                        OnParticleHeld.Invoke(new ParticlePickEventArgs(pickedParticleIndex, worldPosition));
                        Debug.Log("Holding");
                    }

                    // Release:				
                    if (Input.GetMouseButtonUp(0))
                    {
                        if (OnParticleReleased != null)
                        {
                            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, pickedParticleDepth));
                            OnParticleReleased.Invoke(new ParticlePickEventArgs(pickedParticleIndex, worldPosition));
                        }
                        pickedParticleIndex = -1;
                    }
                }
            }
        }
        void Mine()
        {
            //print("RUnning...");
            if (solver != null)
            {
                //print("RUnning...");
                var pick = GameObject.Find("Pick").transform.position;
                var end = GameObject.Find("End").transform.position;
                pickLocation = new Vector3(pick.x, pick.y, pick.z);
                endLocation = new Vector3(end.x, end.y + 1, end.z);
                //endLocation = new Vector3(pick.x + 2, pick.y + 2, pick.z);

                //var obj = GameObject.Find("SpeechInputHandler").GetComponent<VoiceCommands>();
                //speech_obj.GetComponent<Voice>
                //speech_obj.GetComponent<>


                // Click:
                if ((Input.GetKey("return")) && (pickedParticleIndex < 0))
                {
                    pickedParticleIndex = -1;

                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    float closestMu = float.MaxValue;
                    float closestDistance = float.MaxValue;

                    Matrix4x4 solver2World = solver.transform.localToWorldMatrix;

                    // Find the closest particle hit by the ray:                    
                    print("Pick: ");
                    print(pick);
                    double smallest = float.MaxValue;
                    for (int i = 0; i < solver.renderablePositions.count; ++i)
                    {
                        Vector3 worldPos = solver2World.MultiplyPoint3x4(solver.renderablePositions[i]);
                        double dx = pick.x - worldPos.x;
                        double dz = pick.z - worldPos.z;
                        double dist = Math.Sqrt(dx * dx + dz * dz);

                        if (dist < smallest)
                        {
                            smallest = dist;
                            pickedParticleIndex = i;
                        }
                    }

                    if (pickedParticleIndex >= 0)
                    {
                        if (OnParticlePicked != null)
                        {
                            trailingLocation = new Vector3(pick.x, pick.y, pick.z);

                            OnParticlePicked.Invoke(new ParticlePickEventArgs(pickedParticleIndex, pickLocation));
                            PathGen();

                            startTime = DateTime.Now;
                        }
                    }
                }
                else if (pickedParticleIndex >= 0)
                {
                    counter++;
                    int len = arrx.Length;
                    // Implement switch case to move in y direction first...
                    // 0 .... move up
                    // 1 .... move across
                    // 2 .... move down
                    //if (counter >= 10)
                    endTime = DateTime.Now;
                    double elapsed = ((TimeSpan)(endTime - startTime)).TotalMilliseconds;
                    if (elapsed > 25)
                    {
                        switch (caseswitch)
                        {
                            case 0: // Move up
                                trailingLocation = new Vector3(pick.x, arry[steps], pick.z);
                                break;
                            case 1: // Move lateral
                                trailingLocation = new Vector3(arrx[steps], arry[len - 1], arrz[steps]);
                                //print("Lateral");
                                break;
                            case 2: // Move down
                                trailingLocation = new Vector3(arrx[len - 1], arry[len - 1 - steps], arrz[len - 1]);
                                //print("Down");
                                break;
                        }
                        OnParticleDragged.Invoke(new ParticlePickEventArgs(pickedParticleIndex, trailingLocation));
                        counter = 0;
                        steps++;
                        startTime = DateTime.Now;
                    }
                    else
                    {
                        OnParticleDragged.Invoke(new ParticlePickEventArgs(pickedParticleIndex, trailingLocation));
                    }

                    if (steps >= len - 1)
                    {
                        Vector3 trailingLocation = new Vector3(arrx[steps], arry[steps], arrz[steps]);
                        OnParticleReleased.Invoke(new ParticlePickEventArgs(pickedParticleIndex, trailingLocation));
                        //print("Moving");
                        steps = 0;
                        movement = 0;
                        caseswitch++;
                        if (caseswitch > 2)
                        {
                            caseswitch = 0;
                            pickedParticleIndex = -1;
                        }
                    }

                }
            }
        }

        // Mine - callable
        public void MoveInit()
        {
            if (solver != null)
            {
                print("Move initialised");
                pick = GameObject.Find("Pick").transform.position;
                end = GameObject.Find("End").transform.position;
                pickLocation = new Vector3(pick.x, pick.y, pick.z);
                endLocation = new Vector3(end.x, end.y + 0.3f, end.z);
                //endLocation = new Vector3(pick.x + 2, pick.y + 2, pick.z);

                // Click:
                if (pickedParticleIndex < 0)
                {
                    pickedParticleIndex = -1;

                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    float closestMu = float.MaxValue;
                    float closestDistance = float.MaxValue;

                    Matrix4x4 solver2World = solver.transform.localToWorldMatrix;

                    // Find the closest particle hit by the ray:                    
                    //print("Pick: ");
                    //print(pick);
                    double smallest = float.MaxValue;
                    for (int i = 0; i < solver.renderablePositions.count; ++i)
                    {
                        Vector3 worldPos = solver2World.MultiplyPoint3x4(solver.renderablePositions[i]);
                        double dx = pick.x - worldPos.x;
                        double dz = pick.z - worldPos.z;
                        double dist = Math.Sqrt(dx * dx + dz * dz);

                        if (dist < smallest)
                        {
                            smallest = dist;
                            pickedParticleIndex = i;
                        }
                    }

                    if (pickedParticleIndex >= 0)
                    {
                        if (OnParticlePicked != null)
                        {
                            trailingLocation = new Vector3(pick.x, pick.y, pick.z);

                            OnParticlePicked.Invoke(new ParticlePickEventArgs(pickedParticleIndex, pickLocation));
                            PathGen();

                            startTime = DateTime.Now;
                        }
                    }
                }
            }
        }
        public void Updater()
        {
            if (solver != null)
            {
                if (pickedParticleIndex >= 0)
                {
                    counter++;
                    int len = arrx.Length;
                    // Implement switch case to move in y direction first...
                    // 0 .... move up
                    // 1 .... move across
                    // 2 .... move down
                    //if (counter >= 10)
                    endTime = DateTime.Now;
                    double elapsed = ((TimeSpan)(endTime - startTime)).TotalMilliseconds;
                    if (elapsed > dt)
                    {
                        switch (caseswitch)
                        {
                            case 0: // Move up
                                trailingLocation = new Vector3(pick.x, arry[steps], pick.z);
                                break;
                            case 1: // Move lateral
                                trailingLocation = new Vector3(arrx[steps], arry[len - 1], arrz[steps]);
                                //print("Lateral");
                                break;
                            case 2: // Move down
                                trailingLocation = new Vector3(arrx[len - 1], arry[len - 1 - steps], arrz[len - 1]);
                                //print("Down");
                                break;
                        }
                        OnParticleDragged.Invoke(new ParticlePickEventArgs(pickedParticleIndex, trailingLocation));
                        counter = 0;
                        steps++;
                        startTime = DateTime.Now;
                    }
                    else
                    {
                        OnParticleDragged.Invoke(new ParticlePickEventArgs(pickedParticleIndex, trailingLocation));
                    }

                    if (steps >= len - 1)
                    {
                        Vector3 trailingLocation = new Vector3(arrx[steps], arry[steps], arrz[steps]);
                        OnParticleReleased.Invoke(new ParticlePickEventArgs(pickedParticleIndex, trailingLocation));
                        //print("Moving");
                        steps = 0;
                        movement = 0;
                        caseswitch++;
                        if (caseswitch > 2)
                        {
                            caseswitch = 0;
                            pickedParticleIndex = -1;
                        }
                    }

                }
            }
        }

        public void MineCall()
        {
            //print("RUnning...");
            if (solver != null)
            {
                print("RUnning...");
                var pick = GameObject.Find("Pick").transform.position;
                var end = GameObject.Find("End").transform.position;
                pickLocation = new Vector3(pick.x, pick.y, pick.z);
                endLocation = new Vector3(end.x, end.y + 1, end.z);
                //endLocation = new Vector3(pick.x + 2, pick.y + 2, pick.z);

                // Click:
                if (pickedParticleIndex < 0)
                {
                    pickedParticleIndex = -1;

                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    float closestMu = float.MaxValue;
                    float closestDistance = float.MaxValue;

                    Matrix4x4 solver2World = solver.transform.localToWorldMatrix;

                    // Find the closest particle hit by the ray:                    
                    //print("Pick: ");
                    //print(pick);
                    double smallest = float.MaxValue;
                    for (int i = 0; i < solver.renderablePositions.count; ++i)
                    {
                        Vector3 worldPos = solver2World.MultiplyPoint3x4(solver.renderablePositions[i]);
                        double dx = pick.x - worldPos.x;
                        double dz = pick.z - worldPos.z;
                        double dist = Math.Sqrt(dx * dx + dz * dz);

                        if (dist < smallest)
                        {
                            smallest = dist;
                            pickedParticleIndex = i;
                        }
                    }

                    if (pickedParticleIndex >= 0)
                    {
                        if (OnParticlePicked != null)
                        {
                            trailingLocation = new Vector3(pick.x, pick.y, pick.z);

                            OnParticlePicked.Invoke(new ParticlePickEventArgs(pickedParticleIndex, pickLocation));
                            PathGen();

                            startTime = DateTime.Now;
                        }
                    }
                }
                else if (pickedParticleIndex >= 0)
                {
                    counter++;
                    int len = arrx.Length;
                    // Implement switch case to move in y direction first...
                    // 0 .... move up
                    // 1 .... move across
                    // 2 .... move down
                    //if (counter >= 10)
                    endTime = DateTime.Now;
                    double elapsed = ((TimeSpan)(endTime - startTime)).TotalMilliseconds;
                    if (elapsed > 25)
                    {
                        switch (caseswitch)
                        {
                            case 0: // Move up
                                trailingLocation = new Vector3(pick.x, arry[steps], pick.z);
                                break;
                            case 1: // Move lateral
                                trailingLocation = new Vector3(arrx[steps], arry[len - 1], arrz[steps]);
                                print("Lateral");
                                break;
                            case 2: // Move down
                                trailingLocation = new Vector3(arrx[len - 1], arry[len - 1 - steps], arrz[len - 1]);
                                print("Down");
                                break;
                        }
                        OnParticleDragged.Invoke(new ParticlePickEventArgs(pickedParticleIndex, trailingLocation));
                        counter = 0;
                        steps++;
                        startTime = DateTime.Now;
                    }
                    else
                    {
                        OnParticleDragged.Invoke(new ParticlePickEventArgs(pickedParticleIndex, trailingLocation));
                    }

                    if (steps >= len - 1)
                    {
                        Vector3 trailingLocation = new Vector3(arrx[steps], arry[steps], arrz[steps]);
                        OnParticleReleased.Invoke(new ParticlePickEventArgs(pickedParticleIndex, trailingLocation));
                        //print("Moving");
                        steps = 0;
                        movement = 0;
                        caseswitch++;
                        if (caseswitch > 2)
                        {
                            caseswitch = 0;
                            pickedParticleIndex = -1;
                        }
                    }

                }
            }
        }

        // Path generation function
        void PathGen()
        {
            // sizes
            float len = arrx.Length;
            float x = endLocation.x - pickLocation.x;
            float y = endLocation.y - pickLocation.y;
            float z = endLocation.z - pickLocation.z;
            // Steps
            float xStep = x / len;
            float yStep = y / len;
            float zStep = z / len;

            for (int i = 0; i < len; i++)
            {
                arrx[i] = pickLocation.x + xStep * (i + 1);
                arry[i] = pickLocation.y + yStep * (i + 1);
                arrz[i] = pickLocation.z + zStep * (i + 1);
                //print(arry[i]);
            }
            print("Path Generated!");
        }
    }
}































//using System;
//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Events;

//namespace Obi
//{

//    public class ObiParticlePicker : MonoBehaviour
//    {

//        public class ParticlePickEventArgs : EventArgs
//        {

//            public int particleIndex;
//            public Vector3 worldPosition;

//            public ParticlePickEventArgs(int particleIndex, Vector3 worldPosition)
//            {
//                this.particleIndex = particleIndex;
//                this.worldPosition = worldPosition;
//            }
//        }

//        [Serializable]
//        public class ParticlePickUnityEvent : UnityEvent<ParticlePickEventArgs> { }

//        public ObiSolver solver;
//        public float radiusScale = 1;

//        public ParticlePickUnityEvent OnParticlePicked;
//        public ParticlePickUnityEvent OnParticleHeld;
//        public ParticlePickUnityEvent OnParticleDragged;
//        public ParticlePickUnityEvent OnParticleReleased;

//        private Vector3 lastMousePos = Vector3.zero;
//        private int pickedParticleIndex = -1;
//        private float pickedParticleDepth = 0;

//        void Awake()
//        {
//            lastMousePos = Input.mousePosition;
//        }

//        void LateUpdate()
//        {

//            if (solver != null)
//            {

//                // Click:
//                if (Input.GetMouseButtonDown(0))
//                {

//                    pickedParticleIndex = -1;

//                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

//                    float closestMu = float.MaxValue;
//                    float closestDistance = float.MaxValue;

//                    Matrix4x4 solver2World = solver.transform.localToWorldMatrix;

//                    // Find the closest particle hit by the ray:
//                    for (int i = 0; i < solver.renderablePositions.count; ++i)
//                    {

//                        Vector3 worldPos = solver2World.MultiplyPoint3x4(solver.renderablePositions[i]);

//                        float mu;
//                        Vector3 projected = ObiUtils.ProjectPointLine(worldPos, ray.origin, ray.origin + ray.direction, out mu, false);
//                        float distanceToRay = Vector3.SqrMagnitude(worldPos - projected);

//                        // Disregard particles behind the camera:
//                        mu = Mathf.Max(0, mu);

//                        float radius = solver.principalRadii[i][0] * radiusScale;

//                        if (distanceToRay <= radius * radius && distanceToRay < closestDistance && mu < closestMu)
//                        {
//                            closestMu = mu;
//                            closestDistance = distanceToRay;
//                            pickedParticleIndex = i;
//                        }
//                    }

//                    if (pickedParticleIndex >= 0)
//                    {

//                        pickedParticleDepth = Camera.main.transform.InverseTransformVector(solver2World.MultiplyPoint3x4(solver.renderablePositions[pickedParticleIndex]) - Camera.main.transform.position).z;

//                        if (OnParticlePicked != null)
//                        {
//                            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, pickedParticleDepth));
//                            OnParticlePicked.Invoke(new ParticlePickEventArgs(pickedParticleIndex, worldPosition));
//                        }
//                    }

//                }
//                else if (pickedParticleIndex >= 0)
//                {

//                    // Drag:
//                    Vector3 mouseDelta = Input.mousePosition - lastMousePos;
//                    if (mouseDelta.magnitude > 0.01f && OnParticleDragged != null)
//                    {

//                        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, pickedParticleDepth));
//                        OnParticleDragged.Invoke(new ParticlePickEventArgs(pickedParticleIndex, worldPosition));

//                    }
//                    else if (OnParticleHeld != null)
//                    {

//                        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, pickedParticleDepth));
//                        OnParticleHeld.Invoke(new ParticlePickEventArgs(pickedParticleIndex, worldPosition));

//                    }

//                    // Release:				
//                    if (Input.GetMouseButtonUp(0))
//                    {

//                        if (OnParticleReleased != null)
//                        {
//                            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, pickedParticleDepth));
//                            OnParticleReleased.Invoke(new ParticlePickEventArgs(pickedParticleIndex, worldPosition));
//                        }

//                        pickedParticleIndex = -1;

//                    }
//                }
//            }

//            lastMousePos = Input.mousePosition;
//        }
//    }
//}
