using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetector : MonoBehaviour {

    public  MoveBehaviour moveBehaviour;

    // Use this for initialization
    void Start () {
       
    }

    // Update is called once per frame
    void Update () {
 
    }

    void OnTriggerStay(Collider other)
    {
        moveBehaviour.printColision("Coliding!!!!!!!!!!!!!!!!!!!!!!" + this.tag);
    }
}
