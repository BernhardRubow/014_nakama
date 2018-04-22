using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class nvp_DebugText_scr : MonoBehaviour {

	public Text _debugText;
	public InputField email;
	public InputField password;
	public Button login;
	public Button join;
	public Button cancel;


	public nvp_LoginManager_scr loginManager;

	void Awake()
	{

	}

	void Start(){
		loginManager.OnLoginSuccessEvent += OnLoginSuccess;
		loginManager.OnLoginFailureEvent += (s,e) => ChangeDebugText(e);
	
		NakamaSessionManager.GetInstance().OnShowDebugMessage += (e) => ChangeDebugText(e);
		loginManager.OnShowDebugMessage += (e) => ChangeDebugText(e);

	}

	void OnLoginSuccess(object sender, object eventArgs){
		ChangeDebugText(eventArgs);
		email.gameObject.SetActive(false);
		password.gameObject.SetActive(false);
		login.gameObject.SetActive(false);
		join.gameObject.SetActive(true);
		cancel.gameObject.SetActive(true);
	}

	public void ChangeDebugText(object newText){
		Debug.Log(newText);
		UnityMainThreadDispatcher.Instance().Enqueue(
			() => { _debugText.text += "\n" + newText.ToString(); }
		);
	}
}
