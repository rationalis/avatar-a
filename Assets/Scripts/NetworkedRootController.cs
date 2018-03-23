using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent (typeof(NetworkIdentity))]

// Tracks the Oculus coords with the character model
public class NetworkedRootController : NetworkBehaviour
{
	protected Animator animator;
	public GameObject knight_eyes;
	public Player player;
  public GameObject ovr_parent;
  public GameObject joystick_pos;
  public GameObject eye_anchor;
  public Terrain ground;

  public AudioSource thwacker;

  [SyncVar]
	public int conn_id;

	bool calibrated = false;
  bool calibrated2 = false;
	float spawn_time;

  [SyncVar]
  public float true_y;
  [SyncVar]
  public string animation_state;

  public float joystick_radius = 0.2f;
  public float movementSpeed = 7f;

  // Use this for initialization
  void Start () {
    ground = GameObject.Find("Terrain").GetComponent<Terrain>();
    spawn_time = Time.realtimeSinceStartup;
		animator = GetComponent<Animator> ();
    CmdUpdateAnimation("idle");
  }

  public override void OnStartAuthority() {
    ovr_parent = GameObject.Find("OVRParent");
    joystick_pos = GameObject.Find("CenterJoystick");
    eye_anchor = GameObject.Find("CenterEyeAnchor");

    //disableKnightMesh();
  }

  void OnAnimatorIK ()
	{
		if (player != null && animator != null) {
			Vector3 lookat_p = player.data.camera_p + player.data.camera_f;
			Vector3 left_p = player.data.left_wrist_p;
			Vector3 right_p = player.data.right_wrist_p;

			if (lookat_p != null) {
				animator.SetLookAtWeight (1);
				animator.SetLookAtPosition (lookat_p);
			}
			if (left_p != null) {
				animator.SetIKPositionWeight (AvatarIKGoal.LeftHand, 1);
				animator.SetIKPosition (AvatarIKGoal.LeftHand, left_p);
			}
			if (right_p != null) {
				animator.SetIKPositionWeight (AvatarIKGoal.RightHand, 1);
				animator.SetIKPosition (AvatarIKGoal.RightHand, right_p);
			}
		}
	}
	
	// Update is called once per frame
	void Update ()
	{
		if (player != null) {
      if (calibrated) {
        track();
        animate();
      }
      

      if (calibrated && hasAuthority)
      {
        if (!calibrated2)
        {
          resetJoystick();
          calibrated2 = true;
        }
        else
        {

          Vector3 player_pos = new Vector3(player.data.camera_p.x, 0, player.data.camera_p.z);
          Vector3 joystic_center = new Vector3(joystick_pos.transform.position.x, 0, joystick_pos.transform.position.z);
          Vector3 joystick = player_pos - joystic_center;

          if (joystick.magnitude > joystick_radius)
          {
            ovr_parent.transform.position += joystick.normalized * joystick.magnitude * movementSpeed * Time.deltaTime;
          }

          setAnimation(joystick);

          joystick_pos.transform.localPosition = new Vector3(joystick_pos.transform.localPosition.x, player.data.camera_p.y - 0.5f, joystick_pos.transform.localPosition.z);

        }
      }

      if (!calibrated && Time.realtimeSinceStartup - spawn_time > 4f)
      {
        calibrate();
      }
    } else
    {
      foreach (Player p in GameObject.FindObjectsOfType<Player>())
      {
        if (p.pid == conn_id)
        {
          player = p;
          player.myPlayerObject = this.gameObject;
        }
      }
    }
	}

	void track ()
	{
    //Vector3 xz_delta = player.data.camera_p - knight_eyes.transform.position;
    //xz_delta.y = Mathf.Max(0, xz_delta.y);
    Vector3 pos = player.data.camera_p;
    float currentheight = Terrain.activeTerrain.SampleHeight(pos);

    pos.y = Mathf.Max(currentheight, pos.y - true_y);
    //this.transform.position += xz_delta;
    this.transform.position = pos;

		this.transform.eulerAngles = new Vector3 (0, player.data.camera_ey, 0);
	}

  void setAnimation(Vector3 joystick_dir) {
    if (joystick_dir.magnitude > joystick_radius) {
      float forwardAngle = Vector3.Angle(joystick_dir, eye_anchor.transform.forward);
      float leftAngle = Vector3.Angle(joystick_dir, -eye_anchor.transform.right);
      float rightAngle = Vector3.Angle(joystick_dir, eye_anchor.transform.right);
      if (forwardAngle < 60) {
        CmdUpdateAnimation("run");
      }
      else if (leftAngle < 30) {
        CmdUpdateAnimation("left");
      }
      else if (rightAngle < 30) {
        CmdUpdateAnimation("right");
      }
      else if (forwardAngle > 120) {
        CmdUpdateAnimation("back");
      }
    }
    else{
      CmdUpdateAnimation("idle");
    }
  }

  void animate() {
    if (animation_state.Equals("idle")) {
      animator.SetBool("idle", true);
    }
    else {
      animator.SetBool("idle", false);
      animator.Play(animation_state);
    }
  }

	public void calibrate () {
    if (hasAuthority) {
      disableKnightMesh();
      ovr_parent.transform.position = new Vector3((conn_id == 1 ? 0 : 5), 0, 0);
      ovr_parent.transform.rotation *= Quaternion.Euler(0, (conn_id == 1 ? 0 : 180), 0);

      //CmdUpdateHeight(player.data.camera_p.y);
    }
    true_y = player.data.camera_p.y;
    float knight_y = knight_eyes.transform.position.y;
		float scale_factor = true_y / knight_y;

		this.transform.localScale = new Vector3 (scale_factor, scale_factor, scale_factor);

		this.transform.position += player.data.camera_p - knight_eyes.transform.position;

		this.transform.eulerAngles = new Vector3 (0, player.data.camera_ey, 0);

		calibrated = true;
	}

  [Command]
  public void CmdUpdateHeight(float height)
  {
    true_y = height;
  }

  [Command]
  public void CmdUpdateAnimation (string anim)
  {
    animation_state = anim;
  }

  public void resetJoystick()
  {
    joystick_pos.transform.position = new Vector3(player.data.camera_p.x, player.data.camera_p.y - 0.5f, player.data.camera_p.z);
  }
  void disableKnightMesh(){
    foreach (SkinnedMeshRenderer r in this.GetComponentsInChildren<SkinnedMeshRenderer>())
    {
      r.enabled = false;
    }
  }

  void FootL()
  {

  }

  void FootR()
  {

  }
}
