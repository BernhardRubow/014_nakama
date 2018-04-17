using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class nvp_DebugText_scr : MonoBehaviour {

	public Text _debugText;
	private static nvp_DebugText_scr _instance;
	public static nvp_DebugText_scr GetInstance(){
		return _instance;
	}
	
	void Awake()
	{
			if(nvp_DebugText_scr._instance != null) 
				Destroy(this.gameObject);
			else 			
				_instance = this;
	}

	void Start(){
		_debugText = this.GetComponent<Text>();
	}

	public void ChangeDebugText(string newText){
		_debugText.text = newText;
	}

}
