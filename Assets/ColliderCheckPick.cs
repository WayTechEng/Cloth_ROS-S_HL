using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderCheckPick : MonoBehaviour
{
    public GameObject pick_2;
    public GameObject place_2;
    public GameObject place_1;
    public bool entered = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        entered = true;
        Debug.Log("Entered");
        if (place_1.GetComponent<ColliderCheckPlace>().entered)
        {            
            pick_2.SetActive(true);
            place_2.SetActive(true);
        }
        
    }

    private void OnTriggerExit(Collider other)
    {
        entered = false;
        Debug.Log("Leaving");
        pick_2.SetActive(false);
        place_2.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //col.
        //var a = GetComponent<Collision>();
        //Debug.Log(a);
    }
}
