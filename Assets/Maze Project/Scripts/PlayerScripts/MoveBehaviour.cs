using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Net.Sockets;
using System.Linq;


// MoveBehaviour inherits from GenericBehaviour. This class corresponds to basic walk and run behaviour, it is the default behaviour.
public class MoveBehaviour : GenericBehaviour
{
    public volatile List<String> directions = new List<String>();
    public List<String> tempDirections = new List<String>();

    public float walkSpeed = 0.15f;                 // Default walk speed.
	public float runSpeed = 1.0f;                   // Default run speed.
	public float sprintSpeed = 2.0f;                // Default sprint speed.
	public float speedDampTime = 0.1f;              // Default damp time to change the animations based on current speed.
	public string jumpButton = "Jump";              // Default jump button.
	public float jumpHeight = 1.5f;                 // Default jump height.
	public float jumpIntertialForce = 10f;          // Default horizontal inertial force when jumping.

	private float speed, speedSeeker;               // Moving speed.
	private int jumpBool;                           // Animator variable related to jumping.
	private int groundedBool;                       // Animator variable related to whether or not the player is on ground.
	private bool jump;                              // Boolean to determine whether or not the player started a jump.
	private bool isColliding;                       // Boolean to determine if the player has collided with an obstacle.

	enum AdvanceSMStates {IDLE, GETOUTOFCROSSROADS, PROCESSINSTRUCTION, MOVEUNTILNEWSCENARIO, ASKFORINSTRUCTIONS, WAITFORINSTRUCTIONS};
	private AdvanceSMStates AdvanceSM = AdvanceSMStates.WAITFORINSTRUCTIONS;
	private int processinstruction_ctr = 0;


	//Socket
	public String host = "localhost";
    public Int32 port = 5000;

    internal Boolean socket_ready = false;
    internal String input_buffer = "";
    TcpClient tcp_socket;
    NetworkStream net_stream;

    StreamWriter socket_writer;
    StreamReader socket_reader;

    // Start is always called after any Awake functions.
    void Start() 
	{
        // Set up the references.
        jumpBool = Animator.StringToHash("Jump");
		groundedBool = Animator.StringToHash("Grounded");
		behaviourManager.GetAnim.SetBool (groundedBool, true);

		// Subscribe and register this behaviour as the default behaviour.
		behaviourManager.SubscribeBehaviour (this);
		behaviourManager.RegisterDefaultBehaviour (this.behaviourCode);
		speedSeeker = runSpeed;
		setupSocket();
	}

	IEnumerator Wait(float duration)
    {
         yield return new WaitForSeconds(duration);   //Wait
    }

    void OnApplicationQuit()
    {
        closeSocket();
    }

	// Update is used to set features regardless the active behaviour.
	void Update ()
	{

		// Process input data if available
        string received_data = readSocket();

        // Get jump input.
        /*if (!jump && Input.GetButtonDown(jumpButton) && behaviourManager.IsCurrentBehaviour(this.behaviourCode) && !behaviourManager.IsOverriding())
		{
			jump = true;
		}*/

        if (received_data != "")
        {
        	//Debug.Log(received_data);
			// Do something with the received data,
			// print it in the log for now
			if (AdvanceSM == AdvanceSMStates.WAITFORINSTRUCTIONS)
				UpdateMasterInstruction(received_data);
        }

		// Run Main state machine
		AdvanceStep();

		
	}

	public void UpdateMasterInstruction(string received_instruction)
	{
		/*if (received_instruction == ("up"))
		{
			behaviourManager.v = behaviourManager.GetV + 90f;
			MovementManagement(behaviourManager.h, behaviourManager.v);
		}
		if (received_instruction == ("down"))
		{
			behaviourManager.v = behaviourManager.v - 0.90f;
			MovementManagement(behaviourManager.h, behaviourManager.v);
			writeSocket("Hey from Unity!!!");
		}
		if (received_instruction == ("left"))
		{
			behaviourManager.h = behaviourManager.h - 0.90f;
			MovementManagement(behaviourManager.h, behaviourManager.v);
		}
		if (received_instruction == ("right"))
		{
			behaviourManager.h = behaviourManager.h + 0.90f;
			MovementManagement(behaviourManager.h, behaviourManager.v);

		}*/

		if (received_instruction == ("up"))
		{
			// Continue straight ahead
			// No changes needed in direction
		}
		if (received_instruction == ("back"))
		{
			behaviourManager.v = behaviourManager.GetV - .3f;
			MovementManagement(behaviourManager.h, behaviourManager.v);
		}
		if (received_instruction == ("left"))
		{
			behaviourManager.h = behaviourManager.h - 0.50f;
			MovementManagement(behaviourManager.h, behaviourManager.v);
		}
		if (received_instruction == ("right"))
		{
			behaviourManager.h = behaviourManager.h + 0.50f;
			MovementManagement(behaviourManager.h, behaviourManager.v);

		}

		Debug.Log("Instruction received: " + received_instruction.ToString());

		AdvanceSM = AdvanceSMStates.PROCESSINSTRUCTION;
	}

	public void AdvanceStep()
	{
		//Debug.Log(AdvanceSM.ToString());
		switch (AdvanceSM)
		{
			case AdvanceSMStates.IDLE:
				break;
			case AdvanceSMStates.ASKFORINSTRUCTIONS:
				AskForInstructions();
				break;
			case AdvanceSMStates.WAITFORINSTRUCTIONS:
				break;
			case AdvanceSMStates.PROCESSINSTRUCTION:
				processinstruction_ctr++;
				if(processinstruction_ctr >= 100)
				{
					processinstruction_ctr = 0;
					AdvanceSM = AdvanceSMStates.GETOUTOFCROSSROADS;
				}
				break;
			case AdvanceSMStates.GETOUTOFCROSSROADS:
				// We just received the instructions, get into move out of the asked crossroad
				if (directions.Contains("right") && directions.Contains("left"))
				{
					AdvanceSM = AdvanceSMStates.MOVEUNTILNEWSCENARIO;
				}
				else
				{
					// Move until pass the current crossroad 

					// Main movement logic
					if (directions.Contains("front_left"))
					{
						behaviourManager.h = behaviourManager.h + 0.010f;
						MovementManagement(behaviourManager.h, behaviourManager.v);
					}
					else if (directions.Contains("front_right"))
					{
						behaviourManager.h = behaviourManager.h - 0.010f;
						MovementManagement(behaviourManager.h, behaviourManager.v);
					}
					//else
					{
						// Move to front
						 behaviourManager.v = behaviourManager.GetV + .3f;
						 MovementManagement(behaviourManager.h, behaviourManager.v);
					}
				}
				break;
			case AdvanceSMStates.MOVEUNTILNEWSCENARIO:
				if (directions.Contains("front") || !directions.Contains("left") || !directions.Contains("right"))
					AdvanceSM = AdvanceSMStates.ASKFORINSTRUCTIONS;
				else
				{
					// Main movement logic
					if (directions.Contains("front_left"))
					{
						behaviourManager.h = behaviourManager.h + 0.010f;
						MovementManagement(behaviourManager.h, behaviourManager.v);
					}
					else if (directions.Contains("front_right"))
					{
						behaviourManager.h = behaviourManager.h - 0.010f;
						MovementManagement(behaviourManager.h, behaviourManager.v);
					}
					//else
					{
						// Move to front
						behaviourManager.v = behaviourManager.GetV + .3f;
						MovementManagement(behaviourManager.h, behaviourManager.v);
					}
				}
				break;
			default:
				break;
		}
	}

	public void AskForInstructions()
	{
		// Ask for instructions
		String data = "";
		foreach (String direction in directions)
		{
			if (direction.Equals("left_front"))
				data += "left,";
			else if (direction.Equals("right_front"))
				data += "right,";
			else if (direction.Equals("front_tip"))
				data += "front,";
			else if (direction.Equals("back"))
				data += "back,";
		}
		if (data.Length > 0)
		{
			data = data.Remove(data.Length - 1);
		}
		writeSocket(data);
		Debug.Log("Sent data to app js: " + data);

		AdvanceSM = AdvanceSMStates.WAITFORINSTRUCTIONS;
	}

	// LocalFixedUpdate overrides the virtual function of the base class.
	public override void LocalFixedUpdate()
	{
		// Call the basic movement manager.
		MovementManagement(behaviourManager.GetH, behaviourManager.GetV);
		//Debug.Log("H: "+behaviourManager.GetH);
		//Debug.Log("V: "+behaviourManager.GetV);
	}

	// Deal with the basic player movement
	void MovementManagement(float horizontal, float vertical)
	{
		// On ground, obey gravity.
		if (behaviourManager.IsGrounded())
			behaviourManager.GetRigidBody.useGravity = true;
 
		// Call function that deals with player orientation.
		Rotating(horizontal, vertical);

		// Set proper speed.
		Vector2 dir = new Vector2(horizontal, vertical);
		speed = Vector2.ClampMagnitude(dir, 1f).magnitude;
		// This is for PC only, gamepads control speed via analog stick.
		speedSeeker += Input.GetAxis("Mouse ScrollWheel");
		speedSeeker = Mathf.Clamp(speedSeeker, walkSpeed, runSpeed);
		speed *= speedSeeker;
		if (behaviourManager.IsSprinting())
		{
			speed = sprintSpeed;
		}

		behaviourManager.GetAnim.SetFloat(speedFloat, speed, speedDampTime, Time.deltaTime);
	}

	// Rotate the player to match correct orientation, according to camera and key pressed.
	Vector3 Rotating(float horizontal, float vertical)
	{
		// Get camera forward direction, without vertical component.
		Vector3 forward = behaviourManager.playerCamera.TransformDirection(Vector3.forward);

		// Player is moving on ground, Y component of camera facing is not relevant.
		forward.y = 0.0f;
		forward = forward.normalized;

		// Calculate target direction based on camera forward and direction key.
		Vector3 right = new Vector3(forward.z, 0, -forward.x);
		Vector3 targetDirection;
		targetDirection = forward * vertical + right * horizontal;

		// Lerp current direction to calculated target direction.
		if((behaviourManager.IsMoving() && targetDirection != Vector3.zero))
		{
			Quaternion targetRotation = Quaternion.LookRotation (targetDirection);

			Quaternion newRotation = Quaternion.Slerp(behaviourManager.GetRigidBody.rotation, targetRotation, behaviourManager.turnSmoothing);
			behaviourManager.GetRigidBody.MoveRotation (newRotation);
			behaviourManager.SetLastDirection(targetDirection);
		}
		// If idle, Ignore current camera facing and consider last moving direction.
		if(!(Mathf.Abs(horizontal) > 0.9 || Mathf.Abs(vertical) > 0.9))
		{
			behaviourManager.Repositioning();
		}

		return targetDirection;
	}

    // Collision detection.

    private void OnCollisionStay(Collision collision)
	{
		isColliding = true;
    }
	private void OnCollisionExit(Collision collision)
	{
		isColliding = false;
	}

    public void OnTriggerEnter(Collider collider)
    {
        //colliders.Add(collider);
    }

    /*public void printColision(String direction)
    {
        Debug.Log(direction);
        writeSocket(direction);
    }*/

    public void addCollision(String direction)
    {
        if (!directions.Contains(direction))
        {
            directions.Add(direction);
			//Debug.Log(direction.ToString() +" added");
		}
    }

    public void removeCollision(String direction)
    {
        if (directions.Contains(direction)) {
            directions.Remove(direction);
			//Debug.Log(direction.ToString() + " removed");
		}
    }

    //Socket functions ########################################################################################################
    public void setupSocket()
    {
        try
        {
            tcp_socket = new TcpClient(host, port);

            net_stream = tcp_socket.GetStream();
            socket_writer = new StreamWriter(net_stream);
            socket_reader = new StreamReader(net_stream);

            socket_ready = true;
        }
        catch (Exception e)
        {
        	// Something went wrong
            Debug.Log("Socket error: " + e);
        }
    }

    public void writeSocket(string line)
    {
        if (!socket_ready)
            return;
            
        line = line + "\r\n";
        socket_writer.Write(line);
        socket_writer.Flush();
    }

    public String readSocket()
    {
        if (!socket_ready)
            return "";

        if (net_stream.DataAvailable)
            return socket_reader.ReadLine();

        return "";
    }

    public void closeSocket()
    {
        if (!socket_ready)
            return;

        socket_writer.Close();
        socket_reader.Close();
        tcp_socket.Close();
        socket_ready = false;
    }
}
