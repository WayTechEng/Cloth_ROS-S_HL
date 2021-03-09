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

        public GameObject speech_obj;
        public ObiControl obi_control;
        public GameObject EE;
        public GameObject pick_1_obj;
        public GameObject place_1_obj;
        public GameObject pick_2_obj;
        public GameObject place_2_obj;

        private int pickedParticleIndex = -1;
        public List<int> pickedParticleIndexs = new List<int>();

        //double threshold_distance = 0.015f;
        double threshold_distance_height = 0.05f;  // closeness before we can start grabbing the cloth
        double threshold_distance_drop = 0.013f;   // radial distance off EE to the drop position
        //double threshold_height = -0.395f;
        double threshold_retract_velocity = 2.0f;
        float search_radius = 0.015F;   // Define a search radius to detect particles within.

        //public Vector3 pick;
        //public Vector3 end;
        public Vector3 pickLocation;
        public Vector3 placeLocation;

        private Vector3 EE_pos;
        private Vector3 last_EE_pos = Vector3.zero;
        

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
            obi_control = actor.GetComponent<ObiControl>();
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
                    //Debug.Log("Not Stopping physics!");
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
                //pickLocation = pick_1_obj.transform.position;
                //endLocation = place_1_obj.transform.position;
                if (executing)
                {
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
                                OnParticlePicked.Invoke(new ParticlePickEventArgs(pickedParticleIndexs, EE_pos));
                            }
                            //solver.GetComponent<ObiSolver>().enabled = true;
                            continue_grab_cloth = true;
                            executing = false;
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
                        OnParticleDragged.Invoke(new ParticlePickEventArgs(pickedParticleIndexs, EE_pos));
                    }
                    // Hold:
                    else if (OnParticleHeld != null)
                    {
                        OnParticleHeld.Invoke(new ParticlePickEventArgs(pickedParticleIndexs, EE_pos));
                    }
                    // Release:
                    Vector3 delta_end = EE_pos - placeLocation;
                    Vector2 delta_end_xz = new Vector2(delta_end.x, delta_end.z);
                    
                    if (delta_end_xz.magnitude <= threshold_distance_drop)
                    {
                        if(first_time_in == true)
                        {
                            first_time_in = false;
                            drop_timer = DateTime.Now;
                            Debug.Log("First time in");
                        }
                        double dd = EE_pos.y - last_EE_pos.y;
                        DateTime Timerrr = DateTime.Now;
                        double drop_elapsed = ((TimeSpan)(Timerrr - drop_timer)).TotalMilliseconds;
                        //Debug.Log(dd);
                        if (dd > 0.0015)
                        {
                            Debug.LogFormat("Time taken to drop the cloth: {0}", drop_elapsed);
                            Release_cloth();
                            Debug.Log("Dropped because of distance requirement");
                        }
                        else if (drop_elapsed >= 5000)
                        {
                            Release_cloth();
                            Debug.Log("Dropped because of time constraint");
                        }
                    }
                } // End drag event.
            }// End Solver check.
        }

        public bool Find_closest_particles(Vector3 sphere_pos)
        {
            ///////////// Find the closest particles to pick sphere:
            // Find attachment particles based on (x, z), ignore y values for now.
            // Need to calculate the pick location first, not when robot is near the sphere.
            // We still activate the attachment based on closeness, but not don't do the calculation at the same time.
            bool found = false;
            if (found_particles_to_grab == false)
            {
                found_particles_to_grab = true;

                Matrix4x4 solver2World = solver.transform.localToWorldMatrix;
                for (int i = 0; i < solver.renderablePositions.count; ++i)
                {
                    Vector3 worldPos = solver2World.MultiplyPoint3x4(solver.renderablePositions[i]);
                    double dx = sphere_pos.x - worldPos.x;
                    double dz = sphere_pos.z - worldPos.z;
                    double dist = Math.Sqrt(dx * dx + dz * dz);
                    //if (dist <= search_radius*3)
                    //{
                        //Debug.Log(dist);
                    //}
                    if (dist <= search_radius)
                    {
                        pickedParticleIndexs.Add(i);
                        //AddSphereToPoint(worldPos);
                        //Debug.Log(i);
                        found = true;
                        
                    }
                }
            }
            return found;
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
            first_time_in = true;
            hide_the_cloth = true;

            OnParticleReleased.Invoke(new ParticlePickEventArgs(pickedParticleIndexs, EE_pos));
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
