using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LerpMovement : MonoBehaviour
{
    [SerializeField] GameObject t1, t2;
    [Range(0, 1000f)] public float speed;
    float index;
    // Start is called before the first frame update
    void Start()
    {
        index = 0;
    }

    // Update is called once per frame
    void Update()
    {
        index += Time.deltaTime/speed;
        transform.position = Vector3.Lerp(t1.transform.position, t2.transform.position, index); 
    }
}
