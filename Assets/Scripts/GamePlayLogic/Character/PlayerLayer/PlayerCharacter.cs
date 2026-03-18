using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;

public class PlayerCharacter : CharacterBase
{
    public int index { get; set; }

    #region self path
    public readonly List<Vector3> positionHistory = new List<Vector3>();
    public int historyLimit { get; set; }

    private float recordInterval = 0.05f;
    private float recordTimer = 0f;
    #endregion

    #region state
    public bool isLeader;
    public bool isLink;

    public bool isMoving;
    private bool isGrounded;
    private bool isStepClimb;
    #endregion

    public Vector3? targetPosition = null;

    public PlayerStateMachine stateMechine;
    private Animator anim;

    public PlayerDeploymentState deploymentState { get; private set; }
    public PlayerBattleState battleState { get; private set; }

    public PlayerIdleStateExplore idleStateExplore { get; private set; }
    public PlayerMoveStateExplore moveStateExplore { get; private set; }
    public PlayerMovePathStateExplore movePathStateExplore { get; private set; }

    [Header("Physic")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float terminateGravity = -60f;
    public float stepClimbHeight = 1f;
    private Vector3 velocity;
    private const float DASH_MAGNIFICATION = 1.5f;

    #region Step Climb
    private const float STEP_CLIMB_SPEED = 20f;
    private float stepProgress;
    private float targetStep;
    #endregion

    public bool debugMode = false;

    public Vector3 direction { get; private set; }

    private void Awake()
    {
        stateMechine = new PlayerStateMachine();

        deploymentState = new PlayerDeploymentState(stateMechine, this);
        battleState = new PlayerBattleState(stateMechine, this);

        idleStateExplore = new PlayerIdleStateExplore(stateMechine, this);
        moveStateExplore = new PlayerMoveStateExplore(stateMechine, this);
        movePathStateExplore = new PlayerMovePathStateExplore(stateMechine, this);
    }

    protected override void Start()
    {
        base.Start();
        anim = GetComponent<Animator>();
        stateMechine.Initialize(idleStateExplore);
        unitDetectable = GetComponent<UnitDetectable>();

        GameEvent.onDeploymentStart += () =>
        {
            stateMechine.ChangeState(deploymentState);
        };
    }

    protected override void Update()
    {
        base.Update();
        stateMechine.currentState.Update();
        transform.position += velocity * Time.deltaTime;
    }

    private void FixedUpdate()
    {
        stateMechine.currentState.FixedUpdate();
    }

    private void LateUpdate()
    {
        stateMechine.currentState.LateUpdate();
    }

    #region History
    public void UpdateHistory()
    {
        recordTimer += Time.deltaTime;
        if (recordTimer >= recordInterval)
        {
            recordTimer = 0f;

            if (positionHistory.Count >= historyLimit)
                positionHistory.RemoveAt(0);

            positionHistory.Add(this.transform.position);
        }
    }
    public void CleanAllHistory()
    {
        positionHistory.Clear();
    }
    #endregion

    public void SetMoveDirection(Vector3 direction)
    {
        this.direction = direction;
    }
    public void MovementInput(out Vector3 direction)
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        direction = new Vector3(x, 0, z);
        direction = CameraController.instance.GetCameraTransformDirection(direction);

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            moveSpeed = moveSpeed * DASH_MAGNIFICATION;
        }
        else if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            moveSpeed = moveSpeed / DASH_MAGNIFICATION;
        }
    }

    /// <summary>
    /// Moves the unit character based on the given input frequencies.
    /// </summary>
    public void Move(Vector3 direction)
    {
        if (direction == Vector3.zero)
        {
            //  No movement input
            isMoving = false;
        }

        SetOrientation(direction);
        isMoving = true;

        //  Check movement and return

        velocity.x = direction.x * moveSpeed;
        velocity.z = direction.z * moveSpeed;
        Vector3 targetPosition = transform.position + velocity * Time.deltaTime;

        if (!world.IsValidWorldRange(targetPosition))
        {
            velocity.x = 0;
            velocity.z = 0;
        }
    }
    public void CalculateVelocity()
    {
        if (!isStepClimb)
        {
            velocity.y = velocity.y + gravity * Time.fixedDeltaTime;

            if (velocity.y < terminateGravity)
                velocity.y = terminateGravity;

            if (velocity.z > 0 && unitDetectable.CheckBottomForward() ||
                velocity.z < 0 && unitDetectable.CheckBottomBackward())
                velocity.z = 0;
            if (velocity.x > 0 && unitDetectable.CheckBottomRight() || 
                velocity.x < 0 && unitDetectable.CheckBottomLeft())
                velocity.x = 0;

            if (velocity.x > 0 && unitDetectable.CheckCenterRightCorner() ||
                velocity.x < 0 && unitDetectable.CheckCenterLeftCorner())
                velocity.x = 0;
            if (velocity.z > 0 && unitDetectable.CheckCenterForwardBothCorner() ||
                velocity.z < 0 && unitDetectable.CheckCenterBackwardConer())
                velocity.z = 0;
        }

        if (velocity.y < 0)
            velocity.y = CheckGrounded(velocity.y);
        else if (velocity.y > 0 || unitDetectable.CheckUp())
            velocity.y = CheckGrounded(velocity.y);
    }
    private float CheckGrounded(float downSpeed)
    {
        Vector3 half = unitDetectable.size * 0.5f;
        Vector3 centerPos = transform.position + unitDetectable.center;

        Vector3 min = centerPos - half;
        Vector3 max = centerPos + half;

        float checkY = min.y + downSpeed * Time.fixedDeltaTime;

        if (world.CheckSolidNode(min.x, checkY, min.z) ||
            world.CheckSolidNode(max.x, checkY, min.z) ||
            world.CheckSolidNode(min.x, checkY, max.z) ||
            world.CheckSolidNode(max.x, checkY, max.z))
        {
            isGrounded = true;
            return 0;
        }
        else
        {
            isGrounded = false;
            return downSpeed;
        }
    }
    public void ForceStopVelocity()
    {
        direction = Vector3.zero;
        velocity = Vector3.zero;
    }

    #region StepClimb
    public void StepClimbUp(float x, float z, float height)
    {
        if (isStepClimb)
        {
            float stepDelta = CheckStepHeight();
            transform.position += new Vector3(0, stepDelta, 0);
        }

        if (isStepClimb) return;
        Vector3 direction = new Vector3(x, 0, z).normalized;
        if (direction == Vector3.zero) { return; }

        Vector3 half = unitDetectable.size * 0.5f;
        Vector3 centerPos = transform.position + unitDetectable.center;
        //  The collision positions of the unit box
        float bottom = centerPos.y - half.y;
        float forward = centerPos.z + half.z;
        float backward = centerPos.z - half.z;
        float right = centerPos.x + half.x;
        float left = centerPos.x - half.x;

        float stepHeight = bottom + height;
        float nodeHeightY = bottom + world.cellSize;
        if (stepHeight < nodeHeightY) { return; }

        if (unitDetectable.CheckUp() || unitDetectable.CheckUpForward() || unitDetectable.CheckUpBackward()
            || unitDetectable.CheckUpRight() || unitDetectable.CheckUpLeft()) { return; }

        if (unitDetectable.CheckBottomForward())
        {
            if (!world.CheckSolidNode(centerPos.x, nodeHeightY, forward))
            {
                isStepClimb = true;
                targetStep = world.cellSize;
                stepProgress = 0;
            }
        }
        else if (unitDetectable.CheckBottomLeft())
        {
            if (!world.CheckSolidNode(left, nodeHeightY, centerPos.z))
            {
                isStepClimb = true;
                targetStep = world.cellSize;
                stepProgress = 0;
            }
        }
        else if (unitDetectable.CheckBottomRight())
        {
            if (!world.CheckSolidNode(right, nodeHeightY, centerPos.z))
            {
                isStepClimb = true;
                targetStep = world.cellSize;
                stepProgress = 0;
            }
        }
        else if (unitDetectable.CheckBottomBackward())
        {
            if (!world.CheckSolidNode(centerPos.x, nodeHeightY, backward))
            {
                isStepClimb = true;
                targetStep = world.cellSize;
                stepProgress = 0;
            }
        }
    }
    private float CheckStepHeight()
    {
        if (!isStepClimb) return 0;

        float speed = targetStep * STEP_CLIMB_SPEED;
        float remaining = targetStep - stepProgress;
        float delta = Mathf.Min(speed * Time.deltaTime, remaining);

        stepProgress += delta;
        if (stepProgress >= targetStep)
        {
            isStepClimb = false;
        }
        return delta;
    }
    #endregion

    public void YCoordinateAllignment()
    {
        if (!isGrounded || isStepClimb) { return; }

        float cellSize = world.cellSize;
        float halfCell = cellSize / 2f;
        float targetY = GetCharacterTranformToNodePos().y + halfCell;
        velocity.y = 0;
        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
    }

    #region A * Target
    public override void SetAStarMovePos(Vector3 targetPosition)
    {
        Vector3Int targetPos = Utils.RoundXZFloorYInt(targetPosition);
        SetAStarMovePos(targetPos);
    }
    public override void SetAStarMovePos(Vector3Int targetPosition)
    {
        Vector3Int startPosition = currentNode.GetNodeVectorInt();
        PathRoute route = pathFinding.GetPathRoute(startPosition, targetPosition, this, 1, 1);
        List<Vector3> pathVectorList = route.pathNodeVectorList;
        if (pathVectorList.Count != 0)
        {
            PathRoute pathRoute = new PathRoute
            {
                pathFinder = this,
                targetPosition = targetPosition,
                pathNodeVectorList = pathVectorList,
                pathIndex = 0
            };
            SetPathRoute(pathRoute);
            ForceStopVelocity();
            stateMechine.ChangeState(movePathStateExplore);
        }
    }
    public IEnumerator MoveToPositionCoroutine(Vector3 targetPosition, Action onArrived = null)
    {
        SetAStarMovePos(targetPosition);

        while (pathRoute != null && pathRoute.pathIndex < pathRoute.pathNodeVectorList.Count)
        {
            yield return null;
        }

        onArrived?.Invoke();
    }
    #endregion

    public override void TeleportToNodeDeployble(GameNode targetNode)
    {
        if (targetNode != null)
        {
            SetSelfToNode(targetNode, 0.5f);
            stateMechine.ChangeState(deploymentState);
            ForceStopVelocity();
        }
    }
    public override void TeleportToNodeFree(GameNode targetNode)
    {
        if (targetNode != null)
        {
            SetSelfToNode(targetNode, 0.5f);
            stateMechine.ChangeState(idleStateExplore);
            ForceStopVelocity();
        }
    }

    public override void ReadyBattle()
    {
        ForceStopVelocity();
        stateMechine.ChangeState(battleState);
    }

    public override void ExitBattle()
    {
        stateMechine.ChangeState(idleStateExplore);
    }

    private void OnDrawGizmos()
    {
        if (unitDetectable == null) return;

        Vector3 half = unitDetectable.size * 0.5f;
        Vector3 localCenter = unitDetectable.center;

        Vector3 centerPos = transform.position + unitDetectable.center;

        Vector3 min = centerPos - half;
        Vector3 max = centerPos + half;

        Vector3 bottom1 = new Vector3(min.x, min.y, min.z);
        Vector3 bottom2 = new Vector3(max.x, min.y, min.z);
        Vector3 bottom3 = new Vector3(min.x, min.y, max.z);
        Vector3 bottom4 = new Vector3(max.x, min.y, max.z);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(bottom1, 0.1f);
        Gizmos.DrawSphere(bottom2, 0.1f);
        Gizmos.DrawSphere(bottom3, 0.1f);
        Gizmos.DrawSphere(bottom4, 0.1f);

        Vector3 bottomForwardPos = new Vector3(transform.position.x, centerPos.y - half.y, centerPos.z + half.z);
        Vector3 bottomBackwardPos = new Vector3(transform.position.x, centerPos.y - half.y, centerPos.z - half.z);
        Vector3 bottomRightPos = new Vector3(centerPos.x + half.x, centerPos.y - half.y, transform.position.z);
        Vector3 bottomLeftPos = new Vector3(centerPos.x - half.x, centerPos.y - half.y, transform.position.z);

        Vector3 centerForwardRightPos = new Vector3(centerPos.x + half.x, centerPos.y, centerPos.z + half.z);
        Vector3 centerForwardLeftPos = new Vector3(centerPos.x - half.x, centerPos.y, centerPos.z + half.z);
        Vector3 centerBackwardRightPos = new Vector3(centerPos.x + half.x, centerPos.y, centerPos.z - half.z);
        Vector3 centerBackwardLeftPos = new Vector3(centerPos.x - half.x, centerPos.y, centerPos.z - half.z);

        Vector3 upPos = new Vector3(transform.position.x, centerPos.y + half.y, transform.position.z);
        Vector3 upForwardPos = new Vector3(transform.position.x, centerPos.y + half.y, centerPos.z + half.z);
        Vector3 upBackwardPos = new Vector3(transform.position.x, centerPos.y + half.y, centerPos.z - half.z);
        Vector3 upRightPos = new Vector3(centerPos.x + half.x, centerPos.y + half.y, transform.position.z);
        Vector3 upLeftPos = new Vector3(centerPos.x - half.x, centerPos.y + half.y, transform.position.z);

        Gizmos.DrawSphere(bottomForwardPos, 0.1f);
        Gizmos.DrawSphere(bottomBackwardPos, 0.1f);
        Gizmos.DrawSphere(bottomRightPos, 0.1f);
        Gizmos.DrawSphere(bottomLeftPos, 0.1f);

        Gizmos.DrawSphere(centerForwardRightPos, 0.1f);
        Gizmos.DrawSphere(centerForwardLeftPos, 0.1f);
        Gizmos.DrawSphere(centerBackwardRightPos, 0.1f);
        Gizmos.DrawSphere(centerBackwardLeftPos, 0.1f);

        Gizmos.DrawSphere(upPos, 0.1f);
        Gizmos.DrawSphere(upForwardPos, 0.1f);
        Gizmos.DrawSphere(upBackwardPos, 0.1f);
        Gizmos.DrawSphere(upRightPos, 0.1f);
        Gizmos.DrawSphere(upLeftPos, 0.1f);
    }
}