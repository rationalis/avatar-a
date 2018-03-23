using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Timer : MonoBehaviour {

  public float elapsed_time { get; private set; }
  public float length;
  public bool finished { get { return elapsed_time > length; } }

	// Use this for initialization
	void Start () {
    elapsed_time = 0;
	}
	
	// Update is called once per frame
	void Update () {
    elapsed_time += Time.deltaTime;
	}

  public static Timer Add(MonoBehaviour mb)
  {
    return mb.gameObject.AddComponent<Timer>();
  }

  public static Timer Add(MonoBehaviour mb, float length)
  {
    Timer t = mb.gameObject.AddComponent<Timer>();
    t.length = length;
    return t;
  }
}
