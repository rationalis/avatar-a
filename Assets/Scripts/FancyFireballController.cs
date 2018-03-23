using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(NetworkIdentity))]
public class FancyFireballController : NetworkBehaviour
{
  [SyncVar]
  public int pid;

  Timer global_timer;
  Timer timer;
  const float lifespan = 5f;
  const float spark_lifespan = 2f;
  const float dmg = 0.15f;
  const float velocity = 23;
  const float g = -3f;

  public AudioSource fire_sound;
  public AudioSource thwack;
  public AudioSource thwack_high;

  ConstantForce gravity;
  // Use this for initialization
  void Start()
  {
    //global_timer = Timer.Add(this, lifespan);
    this.GetComponent<Rigidbody>().velocity = this.transform.forward * velocity;
    gravity = this.gameObject.AddComponent<ConstantForce>();
    gravity.force = new Vector3(0.0f, g, 0.0f);
  }

  // Update is called once per frame
  void Update()
  {
    if(timer != null){
      fire_sound.volume -= Time.deltaTime;
    }
    if ((timer != null && timer.finished) || this.transform.position.y < -1)//global_timer.finished)
    {
      Destroy(this.gameObject);
    }
  }

  private void OnTriggerEnter(Collider other)
  {
    string name = other.gameObject.name;
    print(name);
    if (name != "OVRPlayerController") {
      if(name != "Terrain") {
        NetworkedRootController otherNRC = other.transform.root.GetComponent<NetworkedRootController>();
        if (otherNRC != null) {
          int otherID = otherNRC.conn_id;
          if (otherID != pid) {
            otherNRC.player.TakeDamage(dmg);
            if (hasAuthority)
            {
              thwack_high.Play();
            } else
            {
              thwack.Play();
            }
            explode();
          }
        }
      }
      else {
        explode();
      }
    }
  }

  private void explode()
  {
    this.GetComponent<Rigidbody>().isKinematic = true;
    this.GetComponent<Collider>().enabled = false;
    timer = Timer.Add(this, spark_lifespan);
  }
}
