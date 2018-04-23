using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace newvisionsproject.managers.events
{
  public enum GameEvents
  {
		onNakamaLoginSuccess,
		onNakamaLoginFailure,
		onNakamaConnectSuccess,
		onNakamaConnectFailure,
		onNakamaMakeMatchSuccess,
		onNakamaMakeMatchFailure,
		onNakamaJoinMatchSuccess,
		onNakamaJoinMatchFailure,
    onUiLoginClicked,
    onUiMakeMatchClicked,
    onNakamaAddedToMatchMakerPool,
    onAddLogMessage,
    onUiCreateMatchClicked,
    onUiJoinMatchClicked
  }

  public class nvp_EventManager_scr : MonoBehaviour
  {

    public static nvp_EventManager_scr INSTANCE;

    private Dictionary<GameEvents, List<Action<object, object>>> eventCallbacks;


    void Awake()
    {      
      if (nvp_EventManager_scr.INSTANCE != null)
      {
        Destroy(this.gameObject);
      }
      else
      {
        nvp_EventManager_scr.INSTANCE = this;
        eventCallbacks = new Dictionary<GameEvents, List<Action<object, object>>>();
      }
    }

    public void Test(int i)
    {

    }

    public void SubscribeToEvent(GameEvents e, Action<object, object> callback)
    {
      if (!eventCallbacks.ContainsKey(e))
      {
        eventCallbacks[e] = new List<Action<object, object>>();
      }

      eventCallbacks[e].Add(callback);
    }


    public void UnsubscribeFromEvent(GameEvents e, Action<object, object> observer)
    {
      if (!eventCallbacks.ContainsKey(e)) return;

      if (!eventCallbacks[e].Contains(observer)) return;

      eventCallbacks[e].Remove(observer);
    }

    public void InvokeEvent(GameEvents e, object sender, object eventArgs)
    {
      if (!eventCallbacks.ContainsKey(e)) return;
      
      foreach (var observer in eventCallbacks[e])
        observer(sender, eventArgs);
    }

    public void Reset()
    {
      eventCallbacks = new Dictionary<GameEvents, List<Action<object, object>>>();
    }
  }
}