using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveTowardsObj : MonoBehaviour
{
    [SerializeField] GameObject _target;
    public float speed;
    Rigidbody rb;
    // Start is called before the first frame update
    void Start()
    {
       rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        //rb.position = Vector3.MoveTowards(transform.position, _target.transform.position, Time.deltaTime * speed);
        //rb.MovePosition(_target.transform.position);
        rb.rotation = Quaternion.LookRotation(_target.transform.position - transform.position);
    }   
        /*Vector3 Diraction = (_target.transform.position-transform.position ).normalized;
        rb.velocity = Diraction * speed;*/
        //transform.position = Vector3.MoveTowards(transform.position, _target.transform.position, Time.deltaTime*speed);
    
}
