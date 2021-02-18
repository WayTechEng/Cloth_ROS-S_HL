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
        Vector3 p = sphere.transform.position;
        //Debug.Log(p);
        sphere.transform.position = new Vector3(0.46f,p.y,1.7f);
        //sphere.GetComponent<Rigidbody>().AddForce(new Vector3(1,0,0));
    }
    private void OnTriggerStay(Collider other)
    {
        Debug.Log("Stay");
        Vector3 p = sphere.transform.position;
        sphere.transform.position = new Vector3(0.46f, p.y, 1.7f);
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
