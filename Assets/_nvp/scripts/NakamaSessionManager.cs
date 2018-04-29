using Nakama;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using newvisionsproject.managers.events;

public class NakamaSessionManager : MonoBehaviour
{

  // +++ singelton ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  private static NakamaSessionManager _instance;
  public static NakamaSessionManager GetInstance()
  {
    return _instance;
  }




  // +++ inspector fields +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  [SerializeField] nvp_serverconfig_scr _config;





  // +++ fields +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  private INClient _client;
  private INSession _session;
  INMatchmakeTicket _ticket;
  string _cancelticket;
  string _matchId;
  List<INUserPresence> _connectedOpponents = new List<INUserPresence>();
  private Queue<IEnumerator> _executionQueue;
  private string _user;
  private string _password;





  // +++ constructor ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  public NakamaSessionManager()
  {
    _executionQueue = new Queue<IEnumerator>(1024);
  }




  // +++ unity lifecylcle +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  private void Awake()
  {
    // +++ singelton +++
    if (_instance != null)
    {
      Destroy(this.gameObject);
      return;
    }
    else
    {
      NakamaSessionManager._instance = this;
    }
  }

  private void Start()
  {
    // subscribe to events
    nvp_EventManager_scr.INSTANCE.SubscribeToEvent(GameEvents.onUiLoginClicked, onLogin);
    nvp_EventManager_scr.INSTANCE.SubscribeToEvent(GameEvents.onUiMakeMatchClicked, onMakeMatch);
    nvp_EventManager_scr.INSTANCE.SubscribeToEvent(GameEvents.onUiCreateMatchClicked, onCreateMatch);
    nvp_EventManager_scr.INSTANCE.SubscribeToEvent(GameEvents.onUiJoinMatchClicked, onJoinMatch);
  }

  private void Update()
  {
    lock (_executionQueue)
    {
      for (int i = 0, len = _executionQueue.Count; i < len; i++)
      {
        StartCoroutine(_executionQueue.Dequeue());
      }
    }
  }

  private void OnApplicationQuit()
  {
    if (_session != null)
    {
      _client.Disconnect();
    }
  }




  // +++ nakama methods +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  private void RestoreSessionAndConnect()
  {
    // Lets check if we can restore a cached session.
    var sessionString = PlayerPrefs.GetString("nk.session");
    if (string.IsNullOrEmpty(sessionString))
    {
      return; // We have no session to restore.
    }

    var session = NSession.Restore(sessionString);
    if (session.HasExpired(DateTime.UtcNow))
    {
      return; // We can't restore an expired session.
    }

    SessionHandler(session);
  }

  private void Nakama_LoginEmail()
  {
    // build the authentication message
    var msg = NAuthenticateMessage.Email(_user, _password);
    // login by email
    _client.Login(msg, this.SessionHandler, onLoginFailure);
  }

  private void Nakama_RegisterEmail()
  {
    // build the authentication message
    var msg = NAuthenticateMessage.Email(_user, _password);
    // login by email
    _client.Register(msg, this.SessionHandler, onLoginFailure);
  }

  private void SessionHandler(INSession session)
  {

    // store session
    _session = session;

    // connect the client to the session on the server
    _client.Connect(_session, (bool done) =>
    {
      // We enqueue callbacks which contain code which must be dispatched on
      // the Unity main thread.
      Enqueue(() =>
      {
        // Store session for quick reconnects.
        PlayerPrefs.SetString("nk.session", session.Token);
        // inform other components that the session is connected
        nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.onNakamaConnected, this, _session);
      });
    });
  }

  private void Enqueue(Action action)
  {
    lock (_executionQueue)
    {
      _executionQueue.Enqueue(ActionWrapper(action));
      if (_executionQueue.Count > 1024)
      {
        nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.onAddLogMessage, this, "Queued actions not consumed fast enough.");
        _client.Disconnect();
      }
    }
  }

  private IEnumerator ActionWrapper(Action action)
  {
    action();
    yield return null;
  }

  private void ErrorHandler(INError err)
  {
    nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.onAddLogMessage, this, string.Format("Error: code '{0}' with '{1}'.", err.Code, err.Message));
  }

  private void LogMatchMakeMatchedData(INMatchmakeMatched matched)
  {
    // a list of users who've been matched as opponents.
    foreach (var presence in matched.Presence)
    {
      Enqueue(() =>
      {
        nvp_EventManager_scr.INSTANCE.InvokeEvent(
          GameEvents.onAddLogMessage,
          this,
          string.Format("User id: '{0}'.", presence.UserId));
      });

      Enqueue(() =>
      {
        nvp_EventManager_scr.INSTANCE.InvokeEvent(
          GameEvents.onAddLogMessage,
          this,
          string.Format("User handle: '{0}'.", presence.Handle));
      });
    }

    // list of all match properties
    foreach (var userProperty in matched.UserProperties)
    {
      foreach (KeyValuePair<string, object> entry in userProperty.Properties)
      {
        Enqueue(() =>
        {
          nvp_EventManager_scr.INSTANCE.InvokeEvent(
            GameEvents.onAddLogMessage,
            this,
            string.Format("Property '{0}' for user '{1}' has value '{2}'.", entry.Key, userProperty.Id, entry.Value));
        });
      }

      foreach (KeyValuePair<string, INMatchmakeFilter> entry in userProperty.Filters)
      {
        Enqueue(() =>
        {
          nvp_EventManager_scr.INSTANCE.InvokeEvent(
            GameEvents.onAddLogMessage,
            this,
            string.Format("Filter '{0}' for user '{1}' has value '{2}'.", entry.Key, userProperty.Id, entry.Value.ToString()));
        });
      }
    }
  }

  private void LogJoinedMatchData(INResultSet<INMatch> matches)
  {
    Enqueue(() =>
          nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.onAddLogMessage, this, "Successfully joined match.")
        );

    _connectedOpponents = new List<INUserPresence>();
    // Add list of connected opponents.
    _connectedOpponents.AddRange(matches.Results[0].Presence);
    // Remove your own user from list.
    _connectedOpponents.Remove(matches.Results[0].Self);

    foreach (var presence in _connectedOpponents)
    {
      var userId = presence.UserId;
      var handle = presence.Handle;
      Enqueue(() =>
        nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.onAddLogMessage, this, "User id '" + userId + "' handle " + handle)
      );
    }
  }

  private void LogMatchPresencesData(INMatchPresence presences)
  {
    Enqueue(() =>
      nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.onAddLogMessage, this, "Connected Opponents changed:")
    );

    // Remove all users who left.
    foreach (var user in presences.Leave)
    {
      _connectedOpponents.Remove(user);
    }
    // Add all users who joined.
    _connectedOpponents.AddRange(presences.Join);

    foreach (var presence in _connectedOpponents)
    {
      var userId = presence.UserId;
      var handle = presence.Handle;
      Enqueue(() =>
        nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.onAddLogMessage, this, "User id '" + userId + "' handle " + handle)
      );
    }
  }




  // +++ nakama eventhandler ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  private void onLoginFailure(INError err)
  {
    if (err.Code == ErrorCode.UserNotFound)
    {
      nvp_EventManager_scr.INSTANCE.InvokeEvent(
        GameEvents.onNakamaLoginFailure,
        this,
        new ArrayList() { "User not found", err }
      );
      Enqueue(() => Debug.LogError(err.Message));

      // if user is not found register him  
      Nakama_RegisterEmail();
    }
    else
    {
      nvp_EventManager_scr.INSTANCE.InvokeEvent(
        GameEvents.onNakamaLoginFailure,
        this,
        new ArrayList() { "Failed to authenticate user", err });
      Debug.LogError(err.Message);
    }
  }

  private void onRegisterFailure(INError err)
  {
    nvp_EventManager_scr.INSTANCE.InvokeEvent(
      GameEvents.onNakamaLoginFailure,
      this,
      new ArrayList() { "Failed to authenticate user", err });
  }

  private void onMatchMake(INMatchmakeTicket ticket)
  {
    Debug.Log("here");
    _ticket = ticket;
    Enqueue(
      () => nvp_EventManager_scr.INSTANCE.InvokeEvent(
        GameEvents.onAddLogMessage,
        this,
        "Added to matchmaker-pool"
      )
    );
  }

  private void onMatchmakeMatched(INMatchmakeMatched matched)
  {
    // a match token is used to join the match.
    Enqueue(
      () => nvp_EventManager_scr.INSTANCE.InvokeEvent(
        GameEvents.onAddLogMessage,
        this,
        string.Format("Match token: '{0}'", matched.Token)
      )
    );

    LogMatchMakeMatchedData(matched);

    Enqueue(
      () => nvp_EventManager_scr.INSTANCE.InvokeEvent(
        GameEvents.onNakamaMakeMatchSuccess,
        this,
        matched
      )      
    );

    Enqueue(
      () => nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.onNakamaMatchJoined, this, null)
    );
  }

  private void onMatchCreated(INMatch match)
  {
    string id = match.Id;

    Enqueue(() =>
    {
      nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.onAddLogMessage, this, string.Format("Match created: Id {0}", id));
      nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.onNakamaMatchCreated, this, id);
      nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.onNakamaMatchJoined, this, null);
    });
  }

  private void onJoinedMatch(INResultSet<INMatch> matches)
  {
    LogJoinedMatchData(matches);
  }

  private void OnMatchPresenceChanged(INMatchPresence presences)
  {
    LogMatchPresencesData(presences);

    nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.OnNakamPresencesChanged, this, presences);
  }






  // +++ external event handler +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  private void onLogin(object sender, object eventArgs)
  {
    var data = (string[])eventArgs;
    string email = data[0];
    string passwd = data[1];

    LoginEmail(email, passwd);
  }

  private void onMakeMatch(object sender, object eventArgs)
  {
    MakeMatch();
  }

  private void onCreateMatch(object sender, object eventArgs)
  {
    CreateMatch();
  }

  private void onJoinMatch(object sender, object eventArgs)
  {
    string id = eventArgs.ToString();
    JoinMatch(id);
  }





  // +++ custom methods +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  private void LoginEmail(string email, string passwd)
  {
    _user = email;
    _password = passwd;

    // build the client
    _client = new NClient.Builder("defaultkey")
      .Host(_config.serverUrl)
      .Port(7350)
      .SSL(false)
      .Build();

    RestoreSessionAndConnect();
    if (_session == null)
    {
      Nakama_LoginEmail();
    }
  }

  private void MakeMatch()
  {
    _ticket = null;

    // register an even handler for events of possible opponents
    _client.OnMatchmakeMatched = onMatchmakeMatched;


    var msg = NMatchmakeAddMessage.Default(2);
    _client.Send(msg, onMatchMake, ErrorHandler);
  }

  private void CreateMatch()
  {
    var msg = NMatchCreateMessage.Default();

    _client.OnMatchPresence = OnMatchPresenceChanged;

    _client.Send(msg, onMatchCreated, ErrorHandler);
  }

  private void JoinMatch(string id)
  {
    var message = NMatchJoinMessage.Default(id);
    _client.Send(
      message,
      onJoinedMatch,
      ErrorHandler);
  }

}