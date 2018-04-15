using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Nakama;
using System;

public class TestScript : MonoBehaviour
{

  // Use this for initialization
  void Start()
  {
		UpdateUser();
    FetchSelf();
  }

  private void UpdateUser()
  {
    var msg = new NSelfUpdateMessage.Builder()
			.Fullname("Bernhard Rubow")
			.Location("Paderborn")
			.Build();
			
		var client = NakamaSessionManager.GetInstance().GetClient();
		client.Send(
			msg, 
			(bool done) => Debug.Log("Successfully updated yourself."),
			(INError err) => Debug.LogErrorFormat("Error: code '{0}' with '{1}'.", err.Code, err.Message)
		);
  }

  private static void FetchSelf()
  {
    var client = NakamaSessionManager.GetInstance().GetClient();
    var msg = NSelfFetchMessage.Default();
    client.Send(msg, (INSelf self) =>
    {
      Debug.LogFormat("User has id '{0}' and handle '{1}'.", self.Id, self.Handle);
      Debug.LogFormat("User has JSON metadata '{0}'.", self.Metadata);
    }, (INError err) =>
    {
      Debug.LogErrorFormat("Error: code '{0}' with '{1}'.", err.Code, err.Message);
    });
  }

  // Update is called once per frame
  void Update()
  {

  }
}
