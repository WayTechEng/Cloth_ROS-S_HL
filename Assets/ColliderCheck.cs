using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderCheck : MonoBehaviour
{
    public GameObject sphere;
    public GameObject excluder;
    private Collider col;
    // Start is called before the first frame update
    void Start()
    {
        col = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger");
    }
    private void OnTriggerStay(Collider other)
    {
        //Debug.Log("Stag");
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Colliding");
        Destroy(excluder);
    }

    // Update is called once per frame
    void Update()
    {
        //col.
        //var a = GetComponent<Collision>();
        //Debug.Log(a);
    }
}
