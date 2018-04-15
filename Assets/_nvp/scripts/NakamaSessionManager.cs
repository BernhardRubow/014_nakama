using Nakama;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NakamaSessionManager : MonoBehaviour {

  // +++ singelton ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  private static NakamaSessionManager _instance;
  public static NakamaSessionManager GetInstance(){
    return _instance;
  }




  // +++ inspector fields +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  [SerializeField] nvp_serverconfig_scr _config;




  // +++ fields +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  private INClient _client;
  private INSession _session;
  private Queue<IEnumerator> _executionQueue;
  private string _user;
  private string _password;

  private Action _onConnectCallback;
  private Action _onErrorCallback;




  // +++ constructor ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  public NakamaSessionManager() {
    _executionQueue = new Queue<IEnumerator>(1024);
  }




  // +++ unity lifecylcle +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  private void Awake() {

    // +++ singelton +++
    if(_instance != null) {
      Destroy(this.gameObject);
      return;
    } else {
      NakamaSessionManager._instance = this;
    }

    
  }

  private void Update() {
    lock (_executionQueue) {
      for (int i = 0, len = _executionQueue.Count; i < len; i++) {
        StartCoroutine(_executionQueue.Dequeue());
      }
    }
  }

  private void OnApplicationQuit() {
    if (_session != null) {
      _client.Disconnect();
    }
  }




  // +++ nakama methods +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  private void RestoreSessionAndConnect() {
    // Lets check if we can restore a cached session.
    var sessionString = PlayerPrefs.GetString("nk.session");
    if (string.IsNullOrEmpty(sessionString)) {
      return; // We have no session to restore.
    }

    var session = NSession.Restore(sessionString);
    if (session.HasExpired(DateTime.UtcNow)) {
      return; // We can't restore an expired session.
    }

    SessionHandler(session);
  }

  private void LoginOrRegister() {
    // See if we have a cached id in PlayerPrefs.
    var id = PlayerPrefs.GetString("nk.id");
    if (string.IsNullOrEmpty(id)) {
      // We'll use device ID for the user. See other authentication options.
      id = SystemInfo.deviceUniqueIdentifier;
      // Store the identifier for next game start.
      PlayerPrefs.SetString("nk.id", id);
    }

    // Use whichever one of the authentication options you want.
    var message = NAuthenticateMessage.Device(id);
    _client.Login(message, this.SessionHandler, (Action<INError>)((INError err) => {
      if (err.Code == ErrorCode.UserNotFound) {
        _client.Register(message, this.SessionHandler, ErrorHandler);
      } else {
        ErrorHandler(err);
        if(this._onErrorCallback != null) this._onErrorCallback();
      }
    }));
  }

  private void LoginOrRegisterEmail(){ 
    var message = NAuthenticateMessage.Email(_user, _password);
    _client.Login(message, this.SessionHandler, (Action<INError>)((INError err) => {
      if(err.Code == ErrorCode.UserNotFound) {
        _client.Register(message, this.SessionHandler, ErrorHandler);
      } else {
        ErrorHandler(err);
        if(this._onErrorCallback != null) this._onErrorCallback();
      }
    }));
  }

  private void SessionHandler(INSession session) {
    _session = session;
    Debug.LogFormat("Session: '{0}'.", session.Token);
    var userId = _session.Id;
    Debug.LogFormat("Session id '{0}' handle '{1}'.", userId, _session.Handle);
    Debug.LogFormat("Session expired: {0}", _session.HasExpired(DateTime.UtcNow));

    _client.Connect(_session, (Action<bool>)((bool done) => {
      // We enqueue callbacks which contain code which must be dispatched on
      // the Unity main thread.
      Enqueue((Action)(() => {
        Debug.Log("Session connected.");
        // Store session for quick reconnects.
        PlayerPrefs.SetString("nk.session", session.Token);
        this._onConnectCallback();
      }));
    }));
  }

  private void Enqueue(Action action) {
    lock (_executionQueue) {
      _executionQueue.Enqueue(ActionWrapper(action));
      if (_executionQueue.Count > 1024) {
        Debug.LogWarning("Queued actions not consumed fast enough.");
        _client.Disconnect();
      }
    }
  }

  private IEnumerator ActionWrapper(Action action) {
    action();
    yield return null;
  }

  private static void ErrorHandler(INError err) {
    Debug.LogErrorFormat("Error: code '{0}' with '{1}'.", err.Code, err.Message);
  }




  // +++ custom methods +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

  public INClient GetClient(){
    return _client;
  }

  public NakamaSessionManager SetUser(string user, string password){
      _user = user;
      _password = password;
      return _instance;
  }

  public NakamaSessionManager SetConnectCallback(Action onConnect, Action onError){
    _onConnectCallback = onConnect;
    _onErrorCallback = onError;
    return _instance;
  }

  public void Connect(){

    _client = new NClient.Builder("defaultkey")
			.Host(_config.serverUrl)
			.Port(7350)
			.SSL(false)
			.Build();
			
    RestoreSessionAndConnect();
    if (_session == null) {
      LoginOrRegisterEmail();
    }
  }
}