using UnityEngine;
using System.Collections;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using System;


/// <summary>
/// A Hero Knight ML Agent
/// </summary>
[Serializable]
public class HeroKnightAgent : Agent {

    [SerializeField] float      m_speed = 4.0f;
    [SerializeField] float      m_jumpForce = 7.5f;
    [SerializeField] float      m_rollForce = 6.0f;
    [SerializeField] bool       m_noBlood = false;
    [SerializeField] GameObject m_slideDust;

    private Animator            m_animator;
    private Rigidbody2D         m_body2d;
    private Sensor_HeroKnight   m_groundSensor;
    private Sensor_HeroKnight   m_wallSensorR1;
    private Sensor_HeroKnight   m_wallSensorR2;
    private Sensor_HeroKnight   m_wallSensorL1;
    private Sensor_HeroKnight   m_wallSensorL2;
    private bool                m_grounded = false;
    private bool                m_rolling = false;
    private int                 m_facingDirection = 1;
    private int                 m_currentAttack = 0;
    private float               m_timeSinceAttack = 0.0f;
    private float               m_delayToIdle = 0.0f;


    [SerializeField] bool isTrainingMode = false;
    [SerializeField] bool isFrozen = false;

    public override void Initialize()
    {
        if (isTrainingMode) MaxStep = 0;
    }

    public override void OnEpisodeBegin()
    {
        transform.position = Vector3.zero;
        
    }

    /// <summary>
    /// Collect vector observations from the environment
    /// </summary>
    /// <param name="sensor">the vector sensor</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(m_speed); // +1
        sensor.AddObservation(transform.localPosition.normalized);
        sensor.AddObservation(m_grounded); // +1
        sensor.AddObservation(m_rolling); // + 1
        sensor.AddObservation(m_facingDirection); // +1
        sensor.AddObservation(m_currentAttack); // +1
        sensor.AddObservation(m_timeSinceAttack); // +1
        sensor.AddObservation(m_delayToIdle); // +1
    }

    /// <summary>
    /// Called when an action is received
    /// from either the player input or the neural network.
    /// Index 0: move vector x (+1 = right, -1 = left)
    /// Index 1: attack (+ 1 = true, 0 = false)
    /// Index 2: block (+ 1 = true, 0 = false)
    /// Index 3: jump (+ 1 = true, 0 = false)
    /// </summary>
    /// <param name="vectorAction"></param>
    public override void OnActionReceived(float[] vectorAction)
    {
        // Don't take actions if frozen
        if (isFrozen) return;

        float move = vectorAction[0];
        float attack = vectorAction[1];
        float block = vectorAction[2];
        float jump = vectorAction[3];
        float roll = vectorAction[4];
        CustomUpdate(move, attack, block, jump, roll);
        // I will make sure to set the decision requester to 20
    }

    // Use this for initialization
    void Start ()
    {
        m_animator = GetComponent<Animator>();
        m_body2d = GetComponent<Rigidbody2D>();
        m_groundSensor = transform.Find("GroundSensor").GetComponent<Sensor_HeroKnight>();
        m_wallSensorR1 = transform.Find("WallSensor_R1").GetComponent<Sensor_HeroKnight>();
        m_wallSensorR2 = transform.Find("WallSensor_R2").GetComponent<Sensor_HeroKnight>();
        m_wallSensorL1 = transform.Find("WallSensor_L1").GetComponent<Sensor_HeroKnight>();
        m_wallSensorL2 = transform.Find("WallSensor_L2").GetComponent<Sensor_HeroKnight>();
    }

    public override void Heuristic(float[] actionsOut)
    {
        float move;
        float attack;
        float block;
        float jump;
        float roll;

        move = Input.GetAxis("Horizontal");
        attack = Input.GetMouseButtonDown(0) ? 1 : 0;
        block = Input.GetMouseButtonDown(1) ? 1 : 0;
        jump = Input.GetKeyDown("space") ? 1 : 0;
        roll = Input.GetKeyDown("left shift") ? 1 : 0;

        actionsOut[0] = move;
        actionsOut[1] = attack;
        actionsOut[2] = block;
        actionsOut[3] = jump;
        actionsOut[4] = roll;
    }

    // Update is called once per frame
    void CustomUpdate (float move, float attack, float block, float jump, float roll)
    {
        // Increase timer that controls attack combo
        m_timeSinceAttack += Time.deltaTime;

        //Check if character just landed on the ground
        if (!m_grounded && m_groundSensor.State())
        {
            m_grounded = true;
            m_animator.SetBool("Grounded", m_grounded);
        }

        //Check if character just started falling
        if (m_grounded && !m_groundSensor.State())
        {
            m_grounded = false;
            m_animator.SetBool("Grounded", m_grounded);
        }

        // -- Handle input and movement --
        //float inputX = Input.GetAxis("Horizontal"); //& vectorX +1
        float inputX = move;

        // Swap direction of sprite depending on walk direction
        if (inputX > 0)
        {
            GetComponent<SpriteRenderer>().flipX = false;
            m_facingDirection = 1;
            AddReward(1 / MaxStep);
        }
            
        else if (inputX < 0)
        {
            GetComponent<SpriteRenderer>().flipX = true;
            m_facingDirection = -1;
            AddReward(1 / MaxStep);
        }

        // Move
        if (!m_rolling)
        {
            m_body2d.velocity = new Vector2(inputX * m_speed, m_body2d.velocity.y);
            if (inputX > 1)
                AddReward(1f / MaxStep);
        }

        //Set AirSpeed in animator
        m_animator.SetFloat("AirSpeedY", m_body2d.velocity.y);

        // -- Handle Animations --
        //Wall Slide
        m_animator.SetBool("WallSlide", (m_wallSensorR1.State() && m_wallSensorR2.State()) || (m_wallSensorL1.State() && m_wallSensorL2.State()));


        //Attack  //& bool +1
        bool leftMouseDown = attack > 0.5f;
        bool rightMouseDown = block > 0.5f;
        bool spacebarDown = jump > 0.5f;
        bool shiftDown = roll > 0.5f;

        //Death 
        if (Input.GetKeyDown("e"))
        {
            m_animator.SetBool("noBlood", m_noBlood);
            m_animator.SetTrigger("Death");
            AddReward(-4);
        }

        //Hurt 
        else if (Input.GetKeyDown("q"))
        {
            m_animator.SetTrigger("Hurt");
            AddReward(-2);
        }


        //else if(Input.GetMouseButtonDown(0) && m_timeSinceAttack > 0.25f)
        else if (leftMouseDown && m_timeSinceAttack > 0.25f)
        {
            m_currentAttack++;
            AddReward(1 / MaxStep);

            // Loop back to one after third attack
            if (m_currentAttack > 3)
                m_currentAttack = 1;

            // Reset Attack combo if time since last attack is too large
            if (m_timeSinceAttack > 1.0f)
                m_currentAttack = 1;

            // Call one of three attack animations "Attack1", "Attack2", "Attack3"
            m_animator.SetTrigger("Attack" + m_currentAttack);

            // Reset timer
            m_timeSinceAttack = 0.0f;
        }

        // Block //& bool +1
        //else if (Input.GetMouseButtonDown(1))
        else if (rightMouseDown)
        {
            m_animator.SetTrigger("Block");
            m_animator.SetBool("IdleBlock", true);
            AddReward(1 / MaxStep);
        }

        else if (rightMouseDown)
            m_animator.SetBool("IdleBlock", false);

        // Roll //& bool +1
        else if (shiftDown && !m_rolling)
        {
            m_rolling = true;
            m_animator.SetTrigger("Roll");
            m_body2d.velocity = new Vector2(m_facingDirection * m_rollForce, m_body2d.velocity.y);
            AddReward(1 / MaxStep);
        }


        //Jump //& bool +1
        //else if (Input.GetKeyDown("space") && m_grounded)
        else if (spacebarDown && m_grounded)
        {
            m_animator.SetTrigger("Jump");
            m_grounded = false;
            m_animator.SetBool("Grounded", m_grounded);
            m_body2d.velocity = new Vector2(m_body2d.velocity.x, m_jumpForce);
            m_groundSensor.Disable(0.2f);
            AddReward(0.5f / MaxStep);
        }

        //Run
        else if (Mathf.Abs(inputX) > Mathf.Epsilon)
        {
            // Reset timer
            m_delayToIdle = 0.05f;
            m_animator.SetInteger("AnimState", 1);
            AddReward(1 / MaxStep);
        }

        //Idle
        else
        {
            // Prevents flickering transitions to idle
            m_delayToIdle -= Time.deltaTime;
            if (m_delayToIdle < 0)
                m_animator.SetInteger("AnimState", 0);
        }
    }

    // Animation Events
    // Called in end of roll animation.
    void AE_ResetRoll()
    {
        m_rolling = false;
    }

    // Called in slide animation.
    void AE_SlideDust()
    {
        Vector3 spawnPosition;

        if (m_facingDirection == 1)
            spawnPosition = m_wallSensorR2.transform.position;
        else
            spawnPosition = m_wallSensorL2.transform.position;

        if (m_slideDust != null)
        {
            // Set correct arrow spawn position
            GameObject dust = Instantiate(m_slideDust, spawnPosition, gameObject.transform.localRotation) as GameObject;
            // Turn arrow in correct direction
            dust.transform.localScale = new Vector3(m_facingDirection, 1, 1);
        }
    }


    private void FixedUpdate()
    {
        if (transform.position.y < -1.5)
        {
            AddReward(-4);
            EndEpisode();
        }      
    }
}
