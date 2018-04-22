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
  public event Action<string> OnShowDebugMessage;




  // +++ fields +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  INMatchmakeTicket _matchMakeTicket;
  INMatchmakeMatched _matched;
  string _cancelMatchTicket;
  string _msg;
  string _lastMsg;




  // +++ custom methods +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  public void Login()
  {
    OnShowDebugMessage("Login pressed");
    OnShowDebugMessage(_user.text);
    OnShowDebugMessage(_password.text);



    NakamaSessionManager
      .GetInstance()
      .SetUser(_user.text, _password.text)
      .SetConnectCallback(OnConnectSuccess, OnConnectFailure)
      .Connect();
  }

  private void MakeMatch(int numberOfPlayers)
  {
    // register event for match make
    NakamaSessionManager
      .GetInstance()
      .GetClient()
      .OnMatchmakeMatched = OnMatchMakeMatched;

    // send message to make match
    var msg = NMatchmakeAddMessage.Default(2);
    NakamaSessionManager
      .GetInstance()
      .GetClient()
      .Send(
        msg,
        OnMatchMakeSuccess,
        OnMatchMakeFailure);
  }

  public void JoinMatch(){
    OnShowDebugMessage("Join Match clicked");
    if(_matched == null){
      OnShowDebugMessage("Not matched");
      return;
    }
    var msg = NMatchJoinMessage.Default(_matched.Token);
    NakamaSessionManager
      .GetInstance()
      .GetClient()
      .Send(msg, OnJoinMatchSuccess, OnJoinMatchFailure);
  }

  public void CancelMatch(){
    OnShowDebugMessage("Cancel Match clicked");
  }

  // +++ event handler ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  private void OnConnectSuccess()
  {
    OnShowDebugMessage("OnConnected custom");

    if (OnLoginSuccessEvent != null) OnLoginSuccessEvent(this, "success");

    // Request opponents
    MakeMatch(2);
  }
  private void OnConnectFailure()
  {
    OnShowDebugMessage("OnError custom");
    if (OnLoginFailureEvent != null) OnLoginFailureEvent(this, "login failure");
  }

  private void OnMatchMakeSuccess(INMatchmakeTicket matchTicket)
  {
    OnShowDebugMessage("OnMatchMakeSuccess");    
    
    _matchMakeTicket = matchTicket;
    _cancelMatchTicket = _matchMakeTicket.Ticket;
    _msg = "Added user to matchmaker pool.";
    OnShowDebugMessage("Added user to matchmaker pool.");     

    // register eventhandler which is called the the server has found opponents
    // for the user
    NakamaSessionManager
      .GetInstance()
      .GetClient()
      .OnMatchmakeMatched = OnMatchMakeMatched;
  }

  private void OnMatchMakeFailure(INError err)
  {
    OnShowDebugMessage("OnMatchMakeFailure");
    OnShowDebugMessage(string.Format("Error: code '{0}' with '{1}'.", err.Code, err.Message));    
  }

  private void OnMatchMakeMatched(INMatchmakeMatched matched)
  {
    _matched = matched;
    // a match token is used to join the match.
    _msg = string.Format("Match token: '{0}'", matched.Token);
    OnShowDebugMessage(_msg);

    // a list of users who've been matched as opponents.
    foreach (var presence in matched.Presence)
    {
      OnShowDebugMessage(string.Format("User id: '{0}'.", presence.UserId));
      OnShowDebugMessage(string.Format("User handle: '{0}'.", presence.Handle));
    }

    // list of all match properties
    foreach (var userProperty in matched.UserProperties)
    {
      foreach (KeyValuePair<string, object> entry in userProperty.Properties)
      {
        OnShowDebugMessage(string.Format("Property '{0}' for user '{1}' has value '{2}'.", entry.Key, userProperty.Id, entry.Value));
      }

      foreach (KeyValuePair<string, INMatchmakeFilter> entry in userProperty.Filters)
      {
        OnShowDebugMessage(string.Format("Filter '{0}' for user '{1}' has value '{2}'.", entry.Key, userProperty.Id, entry.Value.ToString()));
      }
    }
  }

  private void OnJoinMatchSuccess(INResultSet<INMatch> matches){
    OnShowDebugMessage("Successfully joined match"); 
  }

  private void OnJoinMatchFailure(INError error){
    OnShowDebugMessage(string.Format("Error: code '{0}' with '{1}'.", error.Code, error.Message)); 
  }

}
