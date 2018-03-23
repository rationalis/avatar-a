using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(NetworkIdentity))]
public class AirCounterController : NetworkBehaviour
{
  [SyncVar]
  public int pid;

  Timer global_timer;
  Timer timer;
  const float blocking = 2f;
  const float lifespan = 3f;

  // Use this for initialization
  void Start()
  {
    timer = Timer.Add(this, blocking);
    global_timer = Timer.Add(this, lifespan);
  }

  // Update is called once per frame
  void Update()
  {
    if (timer != null && timer.finished)
    {
      this.GetComponent<Collider>().enabled = false;
      Destroy(timer);
    }

    if (global_timer.finished)
    {
      Destroy(this.gameObject);
    }
  }

  private void OnTriggerEnter(Collider other)
  {
    string name = other.gameObject.name;
    print(name);
    if (name != "OVRPlayerController")
    {
      if (name != "Terrain")
      {
        //
      }
    }
  }
}
