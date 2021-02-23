using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderScript : MonoBehaviour
{
    
    public GameObject pick_1;
    public GameObject place_1;
    public GameObject pick_2;
    public GameObject place_2;
    public GameObject cloth;
    List<string> names = new List<string>();

    private ObiControl control;
    // Start is called before the first frame update
    void Start()
    {
        control = cloth.GetComponent<ObiControl>();
        names.Add(pick_1.name);
        names.Add(place_1.name);
        names.Add(pick_2.name);
        names.Add(place_2.name);
    }
    private void OnTriggerEnter(Collider obj)
    {
        //Debug.Log("Entered");
        GameObject c = obj.gameObject;
        for(int i = 0; i < names.Count; i++)
        {
            if(c.name == names[i])
            {
                control.spheres_in[i] = true;
                if (!control.ENABLE_SIMULATION)
                {
                    ActivateIfSpheresIn();
                }
                Debug.LogFormat("Sphere in: {0}", c.name);
            }
        }
    }

    private void OnTriggerExit(Collider obj)
    {
        GameObject c = obj.gameObject;
        for (int i = 0; i < names.Count; i++)
        {
            if (c.name == names[i])
            {
                control.spheres_in[i] = false;
                if (!control.ENABLE_SIMULATION)
                {
                    ActivateIfSpheresIn();
                }
                Debug.LogFormat("Sphere out: {0}", c.name);
            }
        }
    }

    private void ActivateIfSpheresIn()
    {
        if ((control.spheres_in[0] == true) && (control.spheres_in[1] == true))
        {
            pick_2.SetActive(true);
            place_2.SetActive(true);
        }
        else
        {
            pick_2.SetActive(false);
            place_2.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {

        
        
    }
}
