using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetector : MonoBehaviour {

    public MoveBehaviour avatar;

    // Use this for initialization
    void Start () {
        avatar = gameObject.AddComponent(typeof(MoveBehaviour)) as MoveBehaviour;
    }
	
	// Update is called once per frame
	void Update () {
 
    }

    void OnTriggerStay(Collider other)
    {
        //Debug.Log("Coliding!!!!!!!!!!!!!!!!!!!!!!" + this.tag);
        avatar.printColision("Coliding!!!!!!!!!!!!!!!!!!!!!!" + this.tag);
    }
}
