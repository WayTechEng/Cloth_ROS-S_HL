using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Obi
{
    public class ObiParticlePicker : MonoBehaviour
    {

        //public class ParticlePickEventArgs : EventArgs
        //{

        //    public int particleIndex;
        //    public Vector3 worldPosition;

        //    public ParticlePickEventArgs(int particleIndex, Vector3 worldPosition)
        //    {
        //        this.particleIndex = particleIndex;
        //        this.worldPosition = worldPosition;
        //    }
        //}

        public class ParticlePickEventArgs : EventArgs
        {

            public List<int> particleIndexs;
            public Vector3 worldPosition;

            public ParticlePickEventArgs(List<int> particleIndexs, Vector3 worldPosition)
            {
                this.particleIndexs = particleIndexs;
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
        //public List<ParticlePickUnityEvent> elist = new List<ParticlePickUnityEvent>();
        //elist.Add(OnParticlePicked);
        //elist.Add(OnParticleHeld);
        //elist.Add(OnParticleDragged);
        //elist.Add(OnParticleReleased);

        public GameObject speech_obj;
        public ObiControl obi_control;
        public GameObject EE;
        GameObject pick_obj;
        GameObject end_obj;        

        private int pickedParticleIndex = -1;
        public List<int> pickedParticleIndexs = new List<int>();

        //double threshold_distance = 0.015f;
        double threshold_distance_height = 0.05f;
        double threshold_distance_drop = 0.013f;
        //double threshold_height = -0.395f;
        double threshold_retract_velocity = 2.0f;
        float search_radius = 0.015F;   // Define a search radius to detect particles within.

        public Vector3 pick;
        public Vector3 end;

        private Vector3 EE_pos;
        private Vector3 last_EE_pos = Vector3.zero;
        private Vector3 EE_pos_last = Vector3.zero;
        private Vector3 pickLocation;  // Mouse pick location
        private Vector3 endLocation;

        public bool path_locked = false;
        public bool executing = false;
        private bool hide_the_cloth = false;
        private bool found_particles_to_grab = false;
        public bool init_grab_cloth = false;
        public bool continue_grab_cloth = false;
        private bool first_time_in = true;

        private DateTime hide_cloth_timer;        
        private DateTime drop_timer;        

        void Awake()
        {
            EE_pos = EE.transform.position;
            last_EE_pos = EE.transform.position;
            pick_obj = GameObject.Find("Pick");
            end_obj = GameObject.Find("End");
            obi_control = actor.GetComponent<ObiControl>();
            //ParticlePickUnityEvent OnParticlePicked1 = new ParticlePickUnityEvent();
            //ParticlePickUnityEvent OnParticleHeld1 = new ParticlePickUnityEvent();
            //ParticlePickUnityEvent OnParticleDragged1 = new ParticlePickUnityEvent();
            //ParticlePickUnityEvent OnParticleReleased1 = new ParticlePickUnityEvent();
            //elist.Add(OnParticlePicked1);
            //elist.Add(OnParticleHeld1);
            //elist.Add(OnParticleDragged1);
            //elist.Add(OnParticleReleased1);
            //obi_control.SaveState();
        }

        void LateUpdate()
        {
            if (!(EE_pos.magnitude == EE.transform.position.magnitude))
            {
                last_EE_pos = EE_pos;
                EE_pos = EE.transform.position;
            }
            Move_by_robot();
            
            if (hide_the_cloth == true)
            {
                DateTime T = DateTime.Now;
                double elapsed = ((TimeSpan)(T - hide_cloth_timer)).TotalMilliseconds;
                if ((elapsed > 1000) && (solver.GetComponent<ObiSolver>().enabled == true))
                {
                    Debug.Log("Not Stopping physics!");
                    //solver.GetComponent<ObiSolver>().enabled = false;
                    solver.GetComponent<ObiSolver>().enabled = true;
                    //var xxx = solver.GetComponent<ObiSolver>().parameters;
                    //Debug.Log(xxx.sleepThreshold);
                    ////xxx.sleepThreshold = 1;
                    //Debug.Log(xxx.sleepThreshold);
                    //Debug.Log("Stopping physics!");
                }
           
                if ((elapsed > 2000) && (actor.GetComponent<ObiCloth>().enabled == true))
                {
                    //actor.GetComponent<ObiCloth>().enabled = false;
                    //Debug.Log("Hiding the cloth!");
                    Debug.Log("Not Hiding the cloth - ready for another simulated manipulation");
                    hide_the_cloth = false;
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
                    //Debug.Log("Executing");
                    //Debug.Log(pickedParticleIndexs.Count);
                    if (pickedParticleIndexs.Count == 0)
                    {
                        Find_closest_particles();
                    }
                    // A particle has been found..
                    if (pickedParticleIndexs.Count > 0)
                    {
                        found_particles_to_grab = false;
                        //Debug.Log("picked particles");
                        hide_the_cloth = false;
                        Vector3 delta_start = EE_pos - pickLocation;
                        Vector3 delta_EE = last_EE_pos - EE_pos;
                        float dy = delta_EE.y * 1000f;
                        //Debug.Log(dy);
                        //if ((delta_start.magnitude <= threshold_distance) && !continue_grab_cloth)
                        //if ((EE_pos.y < threshold_height) && !continue_grab_cloth)
                        if ((dy < threshold_retract_velocity) && (Math.Abs(delta_start.y) < threshold_distance_height) && !continue_grab_cloth)
                        {
                            // Start the grab
                            Debug.Log("Starting the grab...");
                            //solver.GetComponent<ObiSolver>().enabled = true;
                            //var xxx = solver.GetComponent<ObiSolver>().parameters;
                            //xxx.sleepThreshold = 0.0001f;
                            if (OnParticlePicked != null)
                            {
                                //for (int i = 0; i < pickedParticleIndexs.Count; i++)
                                //{
                                //    OnParticlePicked.Invoke(new ParticlePickEventArgs(pickedParticleIndexs[i], EE_pos));
                                //}
                                OnParticlePicked.Invoke(new ParticlePickEventArgs(pickedParticleIndexs, EE_pos));
                                //OnParticlePicked_1.Invoke(new ParticlePickEventArgs(150, EE_pos));
                                //OnParticlePicked_1.Invoke(new ParticlePickEventArgs(190, EE_pos));
                            }
                            //solver.GetComponent<ObiSolver>().enabled = true;
                            continue_grab_cloth = true;
                            executing = false;
                            path_locked = false;
                        }
                    }
                }
                else if ((pickedParticleIndexs.Count > 0) && continue_grab_cloth)
                {
                    //Debug.LogFormat("Particle index: {0}", pickedParticleIndex);
                    // Drag:
                    Vector3 EE_delta = EE_pos - last_EE_pos;
                    if (EE_delta.magnitude > 0.001f && OnParticleDragged != null)
                    {
                        //for (int i = 0; i < pickedParticleIndexs.Count; i++)
                        //{
                        //    OnParticleDragged.Invoke(new ParticlePickEventArgs(pickedParticleIndexs[i], EE_pos));
                        //}

                        OnParticleDragged.Invoke(new ParticlePickEventArgs(pickedParticleIndexs, EE_pos));
                        //OnParticleDragged_1.Invoke(new ParticlePickEventArgs(150, EE_pos));
                        //OnParticleDragged_1.Invoke(new ParticlePickEventArgs(190, EE_pos));

                        //Debug.Log("Dragging");
                        //Debug.LogFormat("Left_EE position: {0}", EE_pos.ToString("F3"));
                    }
                    // Hold:
                    else if (OnParticleHeld != null)
                    {
                        //for (int i = 0; i < pickedParticleIndexs.Count; i++)
                        //{
                        //OnParticleHeld.Invoke(new ParticlePickEventArgs(pickedParticleIndexs[i], EE_pos));
                        //}
                        OnParticleHeld.Invoke(new ParticlePickEventArgs(pickedParticleIndexs, EE_pos));
                        //OnParticleHeld_1.Invoke(new ParticlePickEventArgs(150, EE_pos));
                        //OnParticleHeld_1.Invoke(new ParticlePickEventArgs(190, EE_pos));
                        //Debug.Log("Holding");
                    }
                    // Release:
                    Vector3 delta_end = EE_pos - endLocation;
                    Vector2 delta_end_xz = new Vector2(delta_end.x, delta_end.z);
                    
                    if (delta_end_xz.magnitude <= threshold_distance_drop)
                    {
                        //Debug.Log(first_time_in);
                        if(first_time_in == true)
                        {
                            first_time_in = false;
                            drop_timer = DateTime.Now;
                            Debug.Log("First time in");
                        }
                        //Debug.Log(first_time_in);
                        //Debug.Log(drop_timer);
                        double dd = EE_pos.y - EE_pos_last.y;
                        DateTime Timerrr = DateTime.Now;
                        double drop_elapsed = ((TimeSpan)(Timerrr - drop_timer)).TotalMilliseconds;
                        //Debug.Log(Timerrr);
                        //Debug.Log(drop_elapsed);
                        //Debug.Log(dd);
                        if (dd > 0.1)
                        {
                            first_time_in = true;
                            hide_the_cloth = true;
                            Release_cloth();
                            Debug.Log("Dropped because of distance requirement");
                        }
                        else if (drop_elapsed >= 1200)
                        {
                            first_time_in = true;
                            hide_the_cloth = true;
                            Release_cloth();
                            Debug.Log("Dropped because of time constraint");
                        }
                    }
                } // End drag event.
            }// End Solver check.
        }

        private void Find_closest_particles()
        {
            ///////////// Find the closest particles to pick sphere:
            // Find attachment particles based on (x, z), ignore y values for now.
            // Need to calculate the pick location first, not when robot is near the sphere.
            // We still activate the attachment based on closeness, but not don't do the calculation at the same time.
            if (found_particles_to_grab == false)
            {
                found_particles_to_grab = true;

                Vector3 sphere_pos = pick_obj.transform.position;
                Matrix4x4 solver2World = solver.transform.localToWorldMatrix;
                for (int i = 0; i < solver.renderablePositions.count; ++i)
                {
                    Vector3 worldPos = solver2World.MultiplyPoint3x4(solver.renderablePositions[i]);
                    double dx = sphere_pos.x - worldPos.x;
                    double dz = sphere_pos.z - worldPos.z;
                    double dist = Math.Sqrt(dx * dx + dz * dz);

                    if (dist <= search_radius)
                    {
                        pickedParticleIndexs.Add(i);
                        //AddSphereToPoint(worldPos);
                        //Debug.Log(i);
                    }
                }
            }
        }

        private void Find_clostest_particle()
        {
            /////////////// Find the closest particle next to pick sphere:
            Debug.Log("Grab the cloth!");
            init_grab_cloth = false;
            EE_pos = EE.transform.position;
            pickedParticleIndex = -1;
            Matrix4x4 solver2World = solver.transform.localToWorldMatrix;
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
        }

        private void Pick_particles()
        {
            //for (int i = 0; i < pickedParticleIndexs.Count; i++)
            //{
            //    OnParticlePicked.Invoke(new ParticlePickEventArgs(pickedParticleIndexs[i], EE_pos));
            //}
        }

        private void Drag_particles()
        {
            for (int i = 0; i < pickedParticleIndexs.Count; i++)
            {
                OnParticleDragged.Invoke(new ParticlePickEventArgs(pickedParticleIndexs, EE_pos));
            }
        }

        public void Release_cloth()
        {
            // Stop showing cloth and stop the physics on cloth
            hide_cloth_timer = DateTime.Now;

            continue_grab_cloth = false;
            executing = false;

            //for (int i = 0; i < pickedParticleIndexs.Count; i++)
            //{
            //OnParticleReleased.Invoke(new ParticlePickEventArgs(pickedParticleIndexs[i], EE_pos));
            //}
            //if (OnParticleReleased != null)
            //{
            //OnParticleReleased_1.Invoke(new ParticlePickEventArgs(190, EE_pos));
            OnParticleReleased.Invoke(new ParticlePickEventArgs(pickedParticleIndexs, EE_pos));
            //}
            pickedParticleIndexs = new List<int>();
        }

        public void AddSphereToPoint(Vector3 p)
        {
            //Debug.Log("Adding sphere");
            //Transform spherePoint = null;
            //spherePoint.tag = "clone";
            //Transform child;
            //child = Instantiate(spherePoint, p, Quaternion.identity);
            GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            s.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            s.name = "Sphere1";
            //s.transform.
            s.transform.position = p;
            s.GetComponent<Renderer>().material.color = Color.blue;
        }

        /*void Move_by_robot_manual()
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
        }*/
    }
}
