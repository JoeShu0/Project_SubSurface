using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPanner : MonoBehaviour
{
    [Range(2.0f, 50.0f)]
    public float radius = 1.0f;
    [Range(2.0f, 50.0f)]
    public float Height = 1.0f;

    [Range(1.0f, 10.0f)]
    public float speed = 1.0f;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float X = Mathf.Sin(Time.time / speed) * radius;
        float Y = Mathf.Cos(Time.time / speed) * radius;

        transform.position = new Vector3(X, Height, Y);
    }
}
