using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public struct PlayerData
{
  public Vector3 camera_p;
  public Vector3 camera_f; // forward
  public Vector3 left_wrist_p;
  public Vector3 right_wrist_p;
  public Vector3 left_p;
  public Vector3 right_p;
  public Vector3 parent_p;
  public Quaternion camera_r;
  public float camera_ey; // euler y
  public float hp;
  public int update_id;
}

[RequireComponent(typeof(NetworkIdentity))]
public class Player : NetworkBehaviour
{
  const int max_hp = 100;
  const float SmoothingFactor = 15;
  const float wrist_forward_offset = 0.09f;
  const float wrist_up_offset = -0.05f;
  const float wrist_right_offset = 0.15f;

  const float skill_cooldown = 1f;
  const float p1_x = 0;
  const float p2_x = 5;

  public GameObject KnightPrefab;
  public GameObject eye_anchor;
  public GameObject FancyFireball;
  public GameObject EarthWave;
  public GameObject AirCounter;
  public OvrAvatar avatar;

  public GameObject myPlayerObject;

  public PlayerData data { get; private set; }
  private PlayerData true_data;
  
  float hp;
  float display_hp;
  private Text hpText;
  private Image hpImg;

  Timer cooldown;

  //GestureManager gesture_manager;

  [SyncVar]
  int conn_id;

  public int pid { get { return conn_id; } }
  int update_id;
  float lastReceivedUpdateTime;

  // Use this for initialization
  void Start()
  {
    hp = 1;
    display_hp = 1;

    if (!isLocalPlayer)
    {
      hpText = GameObject.Find("OtherText").GetComponent<Text>();
      hpImg = GameObject.Find("OtherHP").GetComponent<Image>();
      gameObject.GetComponent<GestureManager>().enabled = false;
      return;
    }

    eye_anchor = GameObject.Find("CenterEyeAnchor");
    avatar = GameObject.Find("LocalAvatar").GetComponent<OvrAvatar>();
    
    hpText = GameObject.Find("LocalText").GetComponent<Text>();
    hpImg = GameObject.Find("LocalHP").GetComponent<Image>();

    this.GetComponent<GestureManager>().player = this;
    //gesture_manager = gameObject.AddComponent<GestureManager>();
    //gesture_manager.player = this;

    CmdSpawnPlayer();
  }

  public void TakeDamage(float dmg)
  {
    hp -= dmg;
  }

  void FixedUpdate()
  {
    if (hasAuthority) {
      PlayerData updated_data = GetCurrentPlayerData();
      this.update_id++;
      data = updated_data;
      CmdUpdatePlayer(updated_data);
    }
    else {
      // Interpolate
      float t = Time.deltaTime * SmoothingFactor;
      data = new PlayerData {
        camera_p = Vector3.Lerp(data.camera_p, true_data.camera_p, t),
        camera_f = Vector3.Lerp(data.camera_f, true_data.camera_f, t),
        left_wrist_p = Vector3.Lerp(data.left_wrist_p, true_data.left_wrist_p, t),
        right_wrist_p = Vector3.Lerp(data.right_wrist_p, true_data.right_wrist_p, t),
        parent_p = true_data.parent_p,
        camera_r = Quaternion.Lerp(data.camera_r, true_data.camera_r, t),
        camera_ey = Mathf.Lerp(data.camera_ey, true_data.camera_ey, t),
        hp = true_data.hp,
      };
    }
  }

  public PlayerData GetCurrentPlayerData()
  {
    Transform left = avatar.GetHandTransform(OvrAvatar.HandType.Left, OvrAvatar.HandJoint.HandBase);
    Transform right = avatar.GetHandTransform(OvrAvatar.HandType.Right, OvrAvatar.HandJoint.HandBase);
    GameObject parent = myPlayerObject.GetComponent<NetworkedRootController>().ovr_parent;
    PlayerData updated_data = new PlayerData {
      camera_p = eye_anchor.transform.position,
      camera_f = eye_anchor.transform.forward,
      left_wrist_p = left.position + (wrist_forward_offset * left.forward) + (wrist_up_offset * left.up) - (wrist_right_offset * left.right),
      right_wrist_p = right.position + (wrist_forward_offset * right.forward) + (wrist_up_offset * right.up) + (wrist_right_offset * right.right),
      left_p = left.position,
      right_p = right.position,
      parent_p = parent == null ? Vector3.zero : parent.transform.position,
      camera_r = eye_anchor.transform.rotation,
      camera_ey = eye_anchor.transform.eulerAngles.y,
      hp = this.hp,
      update_id = this.update_id,
    };
    return updated_data;
  }

  private void Update() {
    if (hasAuthority) {
      Transform right = avatar.GetHandTransform(OvrAvatar.HandType.Right, OvrAvatar.HandJoint.HandBase);

      if (OVRInput.GetDown(OVRInput.Button.One) && (cooldown == null || cooldown.finished)) {
        //CmdFancyFireball(right.position + right.forward/2, right.rotation, conn_id);

        /*
        Vector3 dir = right.forward;
        dir.y = 0;
        dir = dir.normalized;
        Vector3 pos = data.camera_p + 5f*dir;
        pos.y = 0;
        float y_rot = right.rotation.eulerAngles.y;
        Quaternion rot = Quaternion.Euler(new Vector3(0,y_rot,0));
        CmdEarthWave(pos, rot, conn_id);
        */
        Destroy(cooldown);
        cooldown = Timer.Add(this, skill_cooldown);
      }

      if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger)) {
        myPlayerObject.GetComponent<NetworkedRootController>().resetJoystick();
      }
      
    }
    
    display_hp = Mathf.Lerp(display_hp, data.hp, Time.deltaTime * SmoothingFactor);
    hpImg.fillAmount = Mathf.Max(0,display_hp);
    hpText.text = Mathf.Max(0 , ((int)Mathf.Round(data.hp * max_hp))).ToString();
  }

  [Command]
  public void CmdAirCounter(Vector3 position, Quaternion rotation, int spawner_id)
  {
    GameObject air = Instantiate(AirCounter, position, rotation);
    air.GetComponent<AirCounterController>().pid = spawner_id;
    NetworkServer.SpawnWithClientAuthority(air, connectionToClient);
  }

  [Command]
  public void CmdEarthWave(Vector3 position, Quaternion rotation, int spawner_id)
  {
    GameObject earth = Instantiate(EarthWave, position, rotation);
    earth.GetComponent<EarthWaveController>().pid = spawner_id;
    NetworkServer.SpawnWithClientAuthority(earth, connectionToClient);
  }

  [Command]
  public void CmdFancyFireball(Vector3 position, Quaternion rotation, int spawner_id)
  {
    GameObject fire = Instantiate(FancyFireball, position, rotation);
    fire.GetComponent<FancyFireballController>().pid = spawner_id;
    NetworkServer.SpawnWithClientAuthority(fire, connectionToClient);
  }

  //Command Server to spawn player
  [Command]
  void CmdSpawnPlayer()
  {
    // id == 1 for host, id == 2 for p2
    conn_id = NetworkServer.connections.Count;

    Vector3 init_pos = new Vector3((conn_id == 1 ? p1_x : p2_x), 0, 0);
    myPlayerObject = Instantiate(KnightPrefab, init_pos, Quaternion.identity);
    NetworkedRootController nrc = myPlayerObject.GetComponent<NetworkedRootController>();
    nrc.conn_id = conn_id;
    NetworkServer.SpawnWithClientAuthority(myPlayerObject, connectionToClient);
  }

  [Command]
  void CmdUpdatePlayer(PlayerData updated_data)
  {
    data = updated_data;

    RpcUpdatePlayer(updated_data);
  }

  [ClientRpc]
  void RpcUpdatePlayer(PlayerData updated_data)
  {
    //Client that owns the player
    if (hasAuthority)
    {
      return;
    }

    if (lastReceivedUpdateTime > 0)
    {
      //print(Time.realtimeSinceStartup - lastReceivedUpdateTime);
      //print(updated_data.update_id);
    }
    lastReceivedUpdateTime = Time.realtimeSinceStartup;

    //Other clients updated by server
    true_data = updated_data;
  }
}
