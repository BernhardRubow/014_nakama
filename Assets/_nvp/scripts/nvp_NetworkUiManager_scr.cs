using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using newvisionsproject.managers.events;



public class nvp_NetworkUiManager_scr : MonoBehaviour
{

  public InputField email;
  public InputField passwd;
  public Button login;
  public Text networkLog;
  public Button matchMaker;
  public Button createMatch;
  public Button joinMatch;
  public InputField matchIdToJoin;
  public Button leaveMatch;
	public InputField message;
	public Button sendMessage;


  void Start()
  {
    // subscribe to events
    nvp_EventManager_scr.INSTANCE.SubscribeToEvent(GameEvents.onNakamaConnected, onNakamaConnected);
    nvp_EventManager_scr.INSTANCE.SubscribeToEvent(GameEvents.onNakamaAddedToMatchMakerPool, onNakamaAddedToMatchMakerPool);
    nvp_EventManager_scr.INSTANCE.SubscribeToEvent(GameEvents.onAddLogMessage, onAppendToLogMessage);
    nvp_EventManager_scr.INSTANCE.SubscribeToEvent(GameEvents.onNakamaMatchCreated, onMatchCreated);
		nvp_EventManager_scr.INSTANCE.SubscribeToEvent(GameEvents.onNakamaMatchJoined, onMatchJoined);
  }



  // +++ event handler ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
  private void onNakamaConnected(object sender, object eventArgs)
  {
    DisplaySessionInfo(eventArgs);
    DisableLoginUI();
    EnableMatchMakerUI();
  }

  private void onNakamaAddedToMatchMakerPool(object sender, object eventArgs)
  {
    UnityMainThreadDispatcher.Instance().Enqueue(DisableLoginUI);
  }

  private void onAppendToLogMessage(object sender, object eventArgs)
  {
    AppendToNetworkLog(eventArgs);
  }

  private void onMatchCreated(object sender, object EventArgs)
  {
    matchIdToJoin.text = EventArgs.ToString();
  }

	private void onMatchJoined (object sender, object eventArgs){
		UnityMainThreadDispatcher.Instance().Enqueue(
			() => AppendToNetworkLog("Successfully joined a match.")
		);

    DisableMatchMakeUI();
    EnableGameUI();
	}


  // +++ public exposed methods for UI callbacks ++++++++++++++++++++++++++++++++++++++++++++++++++

  public void Login()
  {
    Debug.Log("UiManager: Login clicked");
    if (email.text == "1")
      nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.onUiLoginClicked, this, new string[] { "user1@nvp.de", "test1234#" });
    else if (email.text == "2")
      nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.onUiLoginClicked, this, new string[] { "user2@nvp.de", "test1234#" });
    else
      nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.onUiLoginClicked, this, new string[] { email.text, passwd.text });
  }

  public void MakeMatch()
  {
    Debug.Log("UiManager: MakeMatch clicked");
    nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.onUiMakeMatchClicked, this, null);
  }

  public void CreateMatch()
  {
    Debug.Log("UiManager: CreateMatch click");
    nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.onUiCreateMatchClicked, this, null);
  }

  public void JoinMatch()
  {
    string matchId = matchIdToJoin.text;

    nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.onUiJoinMatchClicked, this, matchId);
  }

	public void SendRealtimeMessage(){
		string msg = message.text;
    nvp_EventManager_scr.INSTANCE.InvokeEvent(GameEvents.onSendRealtimeMessageClicked, this, msg);
	}

  // +++ functions ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

  private void AppendToNetworkLog(object text)
  {
    Debug.Log(text);
    UnityMainThreadDispatcher.Instance().Enqueue(() => { networkLog.text += "\n" + text; });
  }

  private void DisableLoginUI()
  {
    email.gameObject.SetActive(false);
    passwd.gameObject.SetActive(false);
    login.gameObject.SetActive(false);
  }

  private void DisplaySessionInfo(object eventArgs)
  {
    var session = (Nakama.INSession)eventArgs;
    var userId = session.Id;

    AppendToNetworkLog(string.Format("Session: {0}", session.Token));
    AppendToNetworkLog(string.Format("Session id '{0}'.", userId));
    AppendToNetworkLog(string.Format("Session handle '{0}' .", session.Handle));
    AppendToNetworkLog(string.Format("Session expired: {0}", session.HasExpired(System.DateTime.UtcNow)));
  }

  void EnableMatchMakerUI()
  {
    matchMaker.gameObject.SetActive(true);
		createMatch.gameObject.SetActive(true);
		joinMatch.gameObject.SetActive(true);
		matchIdToJoin.gameObject.SetActive(true);
  }

  void DisableMatchMakeUI()
  {
    matchMaker.gameObject.SetActive(false);
		createMatch.gameObject.SetActive(false);
		joinMatch.gameObject.SetActive(false);
		//matchIdToJoin.gameObject.SetActive(false);
  }

	void EnableGameUI(){
		leaveMatch.gameObject.SetActive(true);
		message.gameObject.SetActive(true);
		sendMessage.gameObject.SetActive(true);
	}

	void DisableGameUI(){
		leaveMatch.gameObject.SetActive(false);
		message.gameObject.SetActive(false);
		sendMessage.gameObject.SetActive(false);
	}
}
