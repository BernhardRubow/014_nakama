using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;

using Nakama;

public class nvp_LoginManager_scr : MonoBehaviour
{

  // +++ inspector fields +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  [SerializeField] InputField _user;
  [SerializeField] InputField _password;




  // +++ exposed events +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  public event Action<object, object> OnLoginSuccessEvent;
  public event Action<object, object> OnLoginFailureEvent;
  public event Action<object, object> OnMatchMakeSuccessEvent;
  public event Action<object, object> OnMatchMakeFailureEvent;




  // +++ fields +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  INMatchmakeTicket _matchMakeTicket;
  string _cancelMatchTicket;
  string _msg;
  string _lastMsg;




  // +++ custom methods +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  public void Login()
  {
    Debug.Log("Login pressed");
    Debug.Log(_user.text);
    Debug.Log(_password.text);

    NakamaSessionManager
      .GetInstance()
      .SetUser(_user.text, _password.text)
      .SetConnectCallback(OnConnectSuccess, OnConnectFailure)
      .Connect();
  }

  private void MakeMatch(int numberOfPlayers)
  {
    NakamaSessionManager
      .GetInstance()
      .SubscribeOnMatchMakeMatch(OnMatchMakeMatched)
      .MatchMake(2, OnMatchMakeSuccess, OnMatchMakeFailure);
  }

  // +++ event handler ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  private void OnConnectSuccess()
  {
    Debug.Log("OnConnected custom");

    if (OnLoginSuccessEvent != null) OnLoginSuccessEvent(this, "success");

    // Request opponents
    MakeMatch(2);
  }
  private void OnConnectFailure()
  {
    Debug.Log("OnError custom");
    if (OnLoginFailureEvent != null) OnLoginFailureEvent(this, "login failure");
  }

  private void OnMatchMakeSuccess(INMatchmakeTicket matchTicket)
  {
    _matchMakeTicket = matchTicket;
    _cancelMatchTicket = _matchMakeTicket.Ticket;
    _msg = "Added user to matchmaker pool.";

    if (OnMatchMakeSuccessEvent != null) OnMatchMakeSuccessEvent(this, _msg);
  }

  private void OnMatchMakeFailure(INError err)
  {
    _msg = string.Format("Error: code '{0}' with '{1}'.", err.Code, err.Message);

    if (OnMatchMakeFailureEvent != null) OnMatchMakeFailureEvent(this, _msg);

  }

  private void OnMatchMakeMatched(INMatchmakeMatched matched)
  {
    // a match token is used to join the match.
    _msg = string.Format("Match token: '{0}'", matched.Token);
    Debug.LogFormat(_msg);

    // a list of users who've been matched as opponents.
    foreach (var presence in matched.Presence)
    {
      Debug.LogFormat("User id: '{0}'.", presence.UserId);
      Debug.LogFormat("User handle: '{0}'.", presence.Handle);
    }

    // list of all match properties
    foreach (var userProperty in matched.UserProperties)
    {
      foreach (KeyValuePair<string, object> entry in userProperty.Properties)
      {
        Debug.LogFormat("Property '{0}' for user '{1}' has value '{2}'.", entry.Key, userProperty.Id, entry.Value);
      }

      foreach (KeyValuePair<string, INMatchmakeFilter> entry in userProperty.Filters)
      {
        Debug.LogFormat("Filter '{0}' for user '{1}' has value '{2}'.", entry.Key, userProperty.Id, entry.Value.ToString());
      }
    }
  }
}
