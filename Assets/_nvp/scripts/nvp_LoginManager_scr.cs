using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;

using Nakama;

public class nvp_LoginManager_scr : MonoBehaviour {

	// +++ inspector fields +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
	[SerializeField] InputField _user;
	[SerializeField] InputField _password;




	// +++ fields +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
	INMatchmakeTicket _matchMakeTicket;
	string _cancelMatchTicket;




	
	// +++ custom methods +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
	public void Login(){
		Debug.Log("Login pressed");
		Debug.Log(_user.text);
		Debug.Log(_password.text);

		NakamaSessionManager
			.GetInstance()
			.SetUser(_user.text, _password.text)
			.SetConnectCallback(OnConnectSuccess, OnConnectFailure)
			.Connect();			
	}

	private void MakeMatch(int numberOfPlayers){
		NakamaSessionManager
			.GetInstance()
			.MatchMake(2, OnMatchMakeSuccess, OnMatchMakeFailure);
	}

	// +++ event handler ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		private void OnConnectSuccess(){
		Debug.Log("OnConnected custom");

		// Request opponents
		MakeMatch(2);
	}
	private void OnConnectFailure(){
		Debug.Log("OnError custom");
	}

	private void OnMatchMakeSuccess(INMatchmakeTicket matchTicket){
		_matchMakeTicket = matchTicket;
		_cancelMatchTicket = _matchMakeTicket.Ticket;
		Debug.Log("Added user to matchmaker pool.");
	}

	private void OnMatchMakeFailure(INError err){
		Debug.LogErrorFormat("Error: code '{0}' with '{1}'.", err.Code, err.Message);
	}





}
