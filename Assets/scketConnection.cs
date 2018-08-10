using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.NetworkSystem;

public class scketConnection : MonoBehaviour {
	NetworkClient myClient;
	// Use this for initialization
	void Start () {
		Debug.Log("Starting socket connection");
		SetupClient();
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void SetupClient() {
        myClient = new NetworkClient();
        myClient.Connect("localhost", 5000);
        myClient.RegisterHandler(MsgType.Connect, OnConnected);
        
     }

    public void OnConnected(NetworkMessage netMsg) {
        Debug.Log("Connected to server");
    }

}
