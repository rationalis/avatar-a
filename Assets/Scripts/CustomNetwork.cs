using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

public class CustomNetwork : NetworkManager {
  Timer joystickTimer;

  public Button defaultSelected;
  public Button selected_button;
  public Text inputIP;

  public GameObject CombatUI;
  public GameObject NetworkUI;

  public static bool gamestart = false;
  public void Host()
  {
    CombatUI.SetActive(true);
    NetworkUI.SetActive(false);
    SetPort();
    NetworkManager.singleton.StartHost();
    gamestart = true;
  }
  public void Join()
  {
    CombatUI.SetActive(true);
    NetworkUI.SetActive(false);
    SetIPAddress();
    SetPort();
    NetworkManager.singleton.StartClient();
    gamestart = true;
  }
 

  void SetIPAddress()
  {
    NetworkManager.singleton.networkAddress = inputIP.text;
  }

  void SetPort()
  {
    NetworkManager.singleton.networkPort = 7777;
  }
  private void Start()
  {
    CombatUI.SetActive(false);
  }
  private void Update()
  {
    if (!gamestart)
    {
      Vector2 leftJoystickInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

      if (leftJoystickInput.magnitude > 0.7f && (joystickTimer == null || joystickTimer.finished))
      {
        if (selected_button == null)
        {
          selectButton(defaultSelected);
        }
        else
        {
          navigateMenu(leftJoystickInput);
        }
        setJoystickTimer(0.2f);
      }
      else if (leftJoystickInput.magnitude < 0.1f  && joystickTimer != null)
      {
        Destroy(joystickTimer);
      }

      if (selected_button != null && OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger))
      {
        clickSelectedButton();
      }
    }
  }

  void navigateMenu(Vector2 leftJoystickInput)
  {
    float inputX = leftJoystickInput.x;
    float inputY = leftJoystickInput.y;
    if (Mathf.Abs(inputX) > Mathf.Abs(inputY))
    {
      if (inputX > 0)
      {
        selectButton(selected_button.GetComponent<ButtonController>().right);
      }
      else if (inputY < 0)
      {
        selectButton(selected_button.GetComponent<ButtonController>().left);
      }
    }
    else
    {
      if (inputY > 0)
      {
        selectButton(selected_button.GetComponent<ButtonController>().up);
      }
      else if (inputY < 0)
      {
        selectButton(selected_button.GetComponent<ButtonController>().down);
      }
    }
  }
  void clickSelectedButton()
  {
    string button_name = selected_button.name;
    if (button_name == "Host")
    {
      Host();
    }
    else if (button_name == "Join")
    {
      Join();
    }
    else if (button_name == "Delete")
    {
      int inputlen = inputIP.text.Length;
      if (inputlen > 0)
      {
        inputIP.text = inputIP.text.Remove(inputlen - 1);
      }
    }
    else
    {
      inputIP.text += button_name;
    }
  }

  void selectButton(Button current)
  {
    if (current != null)
    {
      if (selected_button != current)
      {
        if (selected_button != null)
        {
          selected_button.OnDeselect(null);
        }
        current.OnSelect(null);
      }

      selected_button = current;
    }
  }

  void setJoystickTimer(float t)
  {
    if (joystickTimer != null)
    {
      Destroy(joystickTimer);
    }
    joystickTimer = Timer.Add(this, t);
  }

}
