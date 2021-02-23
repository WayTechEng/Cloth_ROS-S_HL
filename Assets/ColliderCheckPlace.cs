using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderCheckPlace : MonoBehaviour
{
    public GameObject pick_2;
    public GameObject place_2;
    public GameObject pick_1;
    public bool entered = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        entered = true;
        Debug.Log("Entered");
        if (pick_1.GetComponent<ColliderCheckPick>().entered)
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
        Debug.Log("Colliding");
    }

    // Update is called once per frame
    void Update()
    {
        //col.
        //var a = GetComponent<Collision>();
        //Debug.Log(a);
    }
}
