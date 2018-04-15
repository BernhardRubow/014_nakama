using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;

public class nvp_LoginManager_scr : MonoBehaviour {

	
	[SerializeField] InputField _user;
	[SerializeField] InputField _password;

	


	public void Login(){
		Debug.Log("Login pressed");
		Debug.Log(_user.text);
		Debug.Log(_password.text);

		NakamaSessionManager
			.GetInstance()
			.SetUser(_user.text, _password.text)
			.SetConnectCallback(OnConnect, OnError)
			.Connect();			
	}

	private void OnConnect(){
		Debug.Log("OnConnected custom");
	}

	private void OnError(){
		Debug.Log("OnError custom");
	}



}
