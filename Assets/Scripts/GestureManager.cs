using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct TimedPlayerData
{
  public PlayerData data;
  public Vector3 left_f;
  public Vector3 right_f;
  public float time;
}

public class GestureManager : MonoBehaviour {
  const int max_size = 100;
  const int frame_delta_fast = 8;
  const int frame_delta_slow = 16;

  const int f = 5;

  const float fire_cd = 0.4f;
  const float earth_cd = 2f;
  const float air_cd = 2f;
  const float waist_frac = 0.7f;

  public Player player;

  List<TimedPlayerData> buffer;
  Timer left_fire;
  Timer right_fire;
  Timer earth;
  Timer right_air;

  public GameObject sphere;
  public GameObject sphere1;
  public GameObject sphere2;
  public GameObject sphere3;
  public GameObject sphere4;
  public GameObject sphere5;
  public GameObject sphere6;

  public float true_y;

  bool left_fire_off_cd { get { return left_fire == null || left_fire.finished; } }
  bool right_fire_off_cd { get { return right_fire == null || right_fire.finished; } }
  bool earth_off_cd { get { return earth == null || earth.finished; } }

  float current_terrain_height {
    get {
      return Terrain.activeTerrain.SampleHeight(buffer[buffer.Count - 1].data.camera_p); } }

  // Use this for initialization
  void Start () {
    buffer = new List<TimedPlayerData>();
    true_y = 0;

    sphere = GameObject.Find("Sphere");
    sphere1 = GameObject.Find("Sphere (1)");
    sphere2 = GameObject.Find("Sphere (2)");
    sphere3 = GameObject.Find("Sphere (3)");
    sphere4 = GameObject.Find("Sphere (4)");
    sphere5 = GameObject.Find("Sphere (5)");
    sphere6 = GameObject.Find("Sphere (6)");
  }
	
	// Update is called once per frame
	void Update ()
  {
    if (true_y == 0)
    {
      foreach (var nrc in GameObject.FindObjectsOfType<NetworkedRootController>())
      {
        if (nrc.hasAuthority)
        {
          true_y = nrc.true_y;
        }
      }
    }

    Transform left = player.avatar.GetHandTransform(OvrAvatar.HandType.Left, OvrAvatar.HandJoint.HandBase);
    Transform right = player.avatar.GetHandTransform(OvrAvatar.HandType.Right, OvrAvatar.HandJoint.HandBase);
    TimedPlayerData tpd = new TimedPlayerData {
      data = player.GetCurrentPlayerData(),
      time = Time.realtimeSinceStartup,
      left_f = left.forward,
      right_f = right.forward };

    buffer.Add(tpd);
    if (buffer.Count > max_size)
    {
      buffer.RemoveAt(0);
    }

    if (buffer.Count > f*frame_delta_slow)
    {
      var keyframe_slow = keyframes(frame_delta_slow);
      var keyframe_fast = keyframes(frame_delta_fast);

      EarthWave(keyframe_fast);
      LeftFireball(keyframe_fast);
      RightFireball(keyframe_fast);

      RightAir(keyframe_fast);
      RightAir(keyframe_slow);
    }
  }

  List<TimedPlayerData> keyframes(int delta)
  {
    int len = buffer.Count;
    var keyframe = new List<TimedPlayerData>();
    for (int i = 0; i < f; i++)
    {
      keyframe.Add(buffer[len - 1 - (i * delta)]);
    }
    return keyframe;
  }

  bool left_above_waist(TimedPlayerData tpd) { return tpd.data.left_p.y >= waist_frac * true_y + current_terrain_height; }
  bool right_above_waist(TimedPlayerData tpd) { return tpd.data.right_p.y >= waist_frac * true_y + current_terrain_height; }

  bool almost_orthogonal(Vector3 u, Vector3 v)
  {
    float angle = Vector3.Angle(u, v);
    return angle > 60 && angle < 120;
  }


  void RightAir(List<TimedPlayerData> keyframe)
  {
    List<Vector3> p = new List<Vector3>();
    for (int i = 0; i < 5; i++)
    {
      p.Add(keyframe[i].data.right_p - keyframe[i].data.parent_p);
    }

    Vector3 first_third = p[4] - p[2];
    Vector3 second_fourth = p[1] - p[3];
    bool angle_match = almost_orthogonal(first_third, second_fourth);

    float a = Mathf.Max(first_third.magnitude, second_fourth.magnitude);
    float b = Mathf.Min(first_third.magnitude, second_fourth.magnitude);
    bool length_match = a/b < 2;
    bool length_threshold = b > 0.06f;

    Vector3 midpt1_rel = ((p[4] + p[2]) / 2);
    Vector3 midpt2_rel = ((p[1] + p[3]) / 2);
    bool center_match = (midpt1_rel - midpt2_rel).magnitude < 0.5f * b;
    bool looped = (p[0] - p[4]).magnitude < 0.2f;
    bool off_cd = right_air == null || right_air.finished;

    Vector3 first_second = p[4] - p[3];
    Vector3 first_fourth = p[4] - p[1];
    float c = Mathf.Max(first_second.magnitude, first_fourth.magnitude);
    float d = Mathf.Min(first_second.magnitude, first_fourth.magnitude);
    bool not_a_wave = c / d < 2f;

    //

    sphere.transform.position = p[0] + keyframe[0].data.parent_p;
    sphere1.transform.position = p[1] + keyframe[0].data.parent_p;
    sphere2.transform.position = p[2] + keyframe[0].data.parent_p;
    sphere3.transform.position = p[3] + keyframe[0].data.parent_p;
    sphere4.transform.position = p[4] + keyframe[0].data.parent_p;
    sphere5.transform.position = midpt1_rel + keyframe[0].data.parent_p;
    sphere6.transform.position = midpt2_rel + keyframe[0].data.parent_p;

    //

    if (length_threshold)
    {
      //print("center match: " + center_match + ", looped: " + looped + ", length_match: " + length_match);
    }

    Vector3 dir = Vector3.Cross(first_third, second_fourth).normalized;
    float normal_angle = Vector3.Angle(dir, keyframe[0].data.camera_f);
    bool angle_match2 = normal_angle < 30f || normal_angle > 150f;

    if (angle_match && length_match && length_threshold && center_match && looped && off_cd && not_a_wave && angle_match2)
    {
      Vector3 pos = keyframe[0].data.parent_p + (midpt1_rel + midpt2_rel) / 2;
      Quaternion rot = Quaternion.LookRotation(dir);

      player.CmdAirCounter(pos + dir*0.2f, rot, player.pid);

      Destroy(right_air);
      right_air = Timer.Add(this, air_cd);
    }
  }

  void EarthWave(List<TimedPlayerData> keyframe)
  {
    if (!earth_off_cd) return;

    if (!(left_above_waist(keyframe[0]) && right_above_waist(keyframe[0]))) return;

    Vector3 last_left_p = keyframe[0].data.left_p - keyframe[0].data.parent_p;
    Vector3 prev_left_p = keyframe[f - 1].data.left_p - keyframe[f - 1].data.parent_p;
    Vector3 left_delta = last_left_p - prev_left_p;

    Vector3 last_right_p = keyframe[0].data.right_p - keyframe[0].data.parent_p;
    Vector3 prev_right_p = keyframe[f - 1].data.right_p - keyframe[f - 1].data.parent_p;
    Vector3 right_delta = last_right_p - prev_right_p;

    if (left_delta.magnitude > 0.7f && Vector3.Angle(Vector3.up, left_delta) < 35f
      && right_delta.magnitude > 0.7f && Vector3.Angle(Vector3.up, right_delta) < 35f)
    {
      Vector3 dir = keyframe[0].left_f + keyframe[0].right_f;
      dir.y = 0;
      dir = dir.normalized;

      Vector3 pos = keyframe[0].data.camera_p + 5f * dir;
      pos.y = 0;

      Quaternion rot = Quaternion.LookRotation(dir);

      player.CmdEarthWave(pos, rot, player.pid);

      Destroy(earth);
      earth = Timer.Add(this, earth_cd);
    }
  }

  void LeftFireball(List<TimedPlayerData> keyframe)
  {
    if (left_fire_off_cd && left_above_waist(keyframe[f - 1]))
    {
      Vector3 last_p = keyframe[0].data.left_p - keyframe[0].data.camera_p;
      Vector3 prev_p = keyframe[f - 1].data.left_p - keyframe[f - 1].data.camera_p;
      Vector3 delta = last_p - prev_p;
      Vector3 player_f = Quaternion.Euler(new Vector3(0, keyframe[f - 1].data.camera_ey, 0)) * Vector3.forward;

      if (delta.magnitude > 0.33f && Vector3.Angle(player_f, delta) < 35f)
      {
        Vector3 numerator = Vector3.zero;

        foreach (TimedPlayerData prev in keyframe)
        {
          numerator += prev.left_f;
        }
        Vector3 direction = numerator.normalized;

        Quaternion rot = Quaternion.LookRotation(direction, Vector3.up);

        if (Vector3.Angle(player_f, keyframe[0].left_f) < 35f)
        {
          player.CmdFancyFireball(keyframe[0].data.left_p + direction * 0.3f, rot, player.pid);
          Destroy(left_fire);
          left_fire = Timer.Add(this, fire_cd);
        }
      }
    }
  }

  void RightFireball(List<TimedPlayerData> keyframe)
  {
    if (right_fire_off_cd && right_above_waist(keyframe[f - 1]))
    {
      Vector3 last_p = keyframe[0].data.right_p - keyframe[0].data.camera_p;
      Vector3 prev_p = keyframe[f - 1].data.right_p - keyframe[f - 1].data.camera_p;
      Vector3 delta = last_p - prev_p;
      Vector3 player_f = Quaternion.Euler(new Vector3(0, keyframe[f - 1].data.camera_ey, 0)) * Vector3.forward;
      
      if (delta.magnitude > 0.33f && Vector3.Angle(player_f, delta) < 35f)
      {
        Vector3 numerator = Vector3.zero;

        foreach (TimedPlayerData prev in keyframe)
        {
          numerator += prev.right_f;
        }
        Vector3 direction = numerator.normalized;

        Quaternion rot = Quaternion.LookRotation(direction, Vector3.up);

        if (Vector3.Angle(player_f, keyframe[0].right_f) < 35f)
        {
          player.CmdFancyFireball(keyframe[0].data.right_p + direction * 0.3f, rot, player.pid);
          Destroy(right_fire);
          right_fire = Timer.Add(this, fire_cd);
        }
      }
    }
  }
}
