using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(NetworkIdentity))]
public class EarthWaveController : NetworkBehaviour
{
  [SyncVar]
  public int pid;

  Timer global_timer;
  Timer timer;

  public ParticleSystem fog_particles;
  public ParticleSystem rock_particles;
  public ParticleSystem debris_particles;
  const float lifespan = 2.2f;
  const float animation_lifespan = 3f;
  const float dmg = 0.1f;
  const float velocity = 14;

  public AudioSource earth_sound;
  public AudioSource thwack;
  public AudioSource thwack_high;

  float start_y;

  // Use this for initialization
  void Start () {
    start_y = transform.position.y;

    //global_timer = Timer.Add(this, lifespan);
    this.GetComponent<Rigidbody>().velocity = this.transform.forward * velocity;
    global_timer = Timer.Add(this, lifespan);
  }
	
	// Update is called once per frame
	void Update () {
    Vector3 pos = transform.position;
    float y_increment = Terrain.activeTerrain.SampleHeight(transform.position);
    pos.y = start_y + y_increment;
    transform.position = pos;

    if ((global_timer.finished))//global_timer.finished)
    {
      if (timer == null)
      {
        timer = Timer.Add(this, animation_lifespan);
        this.GetComponent<Rigidbody>().isKinematic = true;
        ParticleSystem.EmissionModule fog_em = fog_particles.emission;
        fog_em.enabled = false;
        ParticleSystem.EmissionModule rock_em = rock_particles.emission;
        rock_em.rateOverTime = 0;
        ParticleSystem.EmissionModule debris_em = debris_particles.emission;
        debris_em.rateOverTime = 0;
      }
      else if (!timer.finished)
      {
        earth_sound.volume = 1 - timer.elapsed_time/2;
      }
      else
      {
        Destroy(this.gameObject);
      }
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
        NetworkedRootController otherNRC = other.transform.root.GetComponent<NetworkedRootController>();
        if (otherNRC != null)
        {
          int otherID = otherNRC.conn_id;
          if (otherID != pid)
          {
            otherNRC.player.TakeDamage(dmg);
            if (hasAuthority)
            {
              thwack_high.Play();
            }
            else
            {
              thwack.Play();
            }
            this.GetComponent<Collider>().enabled = false;
          }
        }
      }
    }
  }
}
