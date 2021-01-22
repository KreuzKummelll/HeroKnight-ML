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
    [SerializeField] Transform spawnPoint;

    public override void Initialize()
    {
        if (!isTrainingMode) MaxStep = 0;
    }

    public override void OnEpisodeBegin()
    {
        transform.position = spawnPoint.position;
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
        float blockUp = vectorAction[3];
        float jump = vectorAction[4];
        float roll = vectorAction[5];
        CustomUpdate(move, attack, block, blockUp, jump, roll);
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
        actionsOut[0] = Input.GetAxis("Horizontal"); // Move
        actionsOut[1] = Input.GetMouseButtonDown(0) ? 1 : 0; // Attack
        actionsOut[2] = Input.GetMouseButtonDown(1) ? 1 : 0; // Block
        actionsOut[3] = Input.GetMouseButtonUp(1) ? 1 : 0; // Block Release
        actionsOut[4] = Input.GetKeyDown(KeyCode.Space) ? 1 : 0; // Jump
        actionsOut[5] = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) ? 1 : 0; // Roll
    }

    // Update is called once per frame
    void CustomUpdate (float move, float attack, float block, float blockUp, float jump, float roll)
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
        float inputX = move;
        bool leftMouseDown = attack > 0.5f;
        bool rightMouseDown = block > 0.5f;
        bool rightMouseUp = blockUp > 0.5f;
        bool spaceKeyDown = jump > 0.5f;
        bool shiftDown = roll > 0.5f;

        // Swap direction of sprite depending on walk direction
        if (inputX > 0)
        {
            GetComponent<SpriteRenderer>().flipX = false;
            m_facingDirection = 1;
        }

        else if (inputX < 0)
        {
            GetComponent<SpriteRenderer>().flipX = true;
            m_facingDirection = -1;
        }

        // Move
        if (!m_rolling)
        {
            m_body2d.velocity = new Vector2(inputX * m_speed, m_body2d.velocity.y);
            Debug.Log("Velocity " + m_body2d.velocity);
            Debug.Log("X Horizontal" + inputX);
        }

        //Set AirSpeed in animator
        m_animator.SetFloat("AirSpeedY", m_body2d.velocity.y);

        // -- Handle Animations --
        //Wall Slide
        m_animator.SetBool("WallSlide", (m_wallSensorR1.State() && m_wallSensorR2.State()) || (m_wallSensorL1.State() && m_wallSensorL2.State()));

        //Death
        if (Input.GetKeyDown("e"))
        {
            m_animator.SetBool("noBlood", m_noBlood);
            m_animator.SetTrigger("Death");
        }

        //Hurt
        else if (Input.GetKeyDown("q"))
            m_animator.SetTrigger("Hurt");

        //Attack
        else if (leftMouseDown && m_timeSinceAttack > 0.25f)
        {
            m_currentAttack++;

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

        // Block
        else if (rightMouseDown)
        {
            m_animator.SetTrigger("Block");
            m_animator.SetBool("IdleBlock", true);
        }

        else if (rightMouseUp)
            m_animator.SetBool("IdleBlock", false);

        // Roll
        else if (shiftDown && !m_rolling)
        {
            m_rolling = true;
            m_animator.SetTrigger("Roll");
            m_body2d.velocity = new Vector2(m_facingDirection * m_rollForce, m_body2d.velocity.y);
        }


        //Jump
        else if (spaceKeyDown && m_grounded)
        {
            m_animator.SetTrigger("Jump");
            m_grounded = false;
            m_animator.SetBool("Grounded", m_grounded);
            m_body2d.velocity = new Vector2(m_body2d.velocity.x, m_jumpForce);
            m_groundSensor.Disable(0.2f);
        }

        //Run
        else if (Mathf.Abs(inputX) > Mathf.Epsilon)
        {
            // Reset timer
            m_delayToIdle = 0.05f;
            m_animator.SetInteger("AnimState", 1);
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
        if (m_body2d.velocity.x > 0.5f || m_body2d.velocity.x < -0.5f)
        {
            AddReward(1 / MaxStep);
        }
    }
   
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // goals
        if (collision.transform.CompareTag("goal"))
        {
            AddReward(2f/MaxStep);
            if (collision.gameObject.name == "goal-short")
            {
                AddReward(1);
            }
            else if (collision.gameObject.name == "goal-mid-low")
            {
                AddReward(2);
            }
            else if (collision.gameObject.name == "goal-mid-high")
            {
                AddReward(3);
            }
            else if (collision.gameObject.name == "goal-long-low")
            {
                AddReward(4);
            }
            else if (collision.gameObject.name == "goal-long-high")
            {
                AddReward(5);
            }
            EndEpisode();
        }
    }
    
}
