using System;
using UnityEngine;

public class Projectile : Entity
{
    private CharacterBase shooter;
    private SkillData skillData;

    private UnitDetectable unitDetectable;
    private UnitDetectable excludeHitDetectable;

    [Header("Physic")]
    public float gravity = -9.81f;
    [SerializeField] private float terminateGravity = -60f;
    private Vector3 velocity;

    private bool enableHit = false;
    public event Action onHitCompleted;

    public bool debugMode = false;

    protected override void Start()
    {
        base.Start();
        unitDetectable = GetComponent<UnitDetectable>();
    }
    private void Update()
    {
        CalculateVelocity();
        transform.position += velocity * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(velocity);

        if (enableHit)
            OnHit();
    }

    private void OnHit()
    {
        UnitDetectable[] unitDetectables = unitDetectable.OverlapSelfRange();
        foreach (var unitDetectable in unitDetectables)
        {
            if (unitDetectable == excludeHitDetectable) { continue; }

            if (unitDetectable != null)
            {
                if (unitDetectable.GetComponent<CharacterBase>() == null) { continue; }
                if (debugMode)
                    Debug.Log($"Hit {unitDetectable.name}");

                DoDamage(unitDetectable);
                CameraController.instance.ChangeFollowTarget(shooter.transform);
                Destroy(gameObject);
                onHitCompleted.Invoke();
                return;
            }
        }
        if (CheckWorldRightUpForward() || CheckWorldRightDownForward()
            || CheckWorldLeftUpForward() || CheckWorldLeftDownForward()
            || CheckWorldRightUpBackward() || CheckWorldRightDownBackward()
            || CheckWorldLeftUpBackward() || CheckWorldLeftDownBackward())
        {
            Debug.Log("Hit World");
            onHitCompleted.Invoke();
            Destroy(gameObject);
        }
    }

    private void DoDamage(UnitDetectable target)
    {
        CharacterBase targetCharacter = target.GetComponent<CharacterBase>();
        if (targetCharacter == null) { return; }
        int damage = skillData.damageAmount;
        targetCharacter.TakeDamage(damage);
    }

    private void Launch(Vector3 direction, float speed)
    {
        enableHit = true;
        velocity = direction.normalized * speed;
        transform.rotation = Quaternion.LookRotation(velocity);
    }
    private void LaunchToTarget(Vector3 start, Vector3 end, int elevationAngle)
    {
        Vector3 displacementXZ = new Vector3(end.x - start.x, 0, end.z - start.z);
        float distanceXZ = displacementXZ.magnitude;
        float heightDifference = end.y - start.y;

        float g = -gravity;
        float angleRad = elevationAngle * Mathf.Deg2Rad;

        float numerator = g * distanceXZ * distanceXZ;
        float denominator = 2f * Mathf.Cos(angleRad) * Mathf.Cos(angleRad) * (distanceXZ * Mathf.Tan(angleRad) - heightDifference);

        if (denominator <= 0f)
        {
            Debug.LogWarning("Invalid parameters: Target is too close or below trajectory path.");
            return;
        }

        float velocity = Mathf.Sqrt(numerator / denominator);

        float rotationY = Mathf.Atan2(displacementXZ.x, displacementXZ.z) * Mathf.Rad2Deg;
        Vector3 forwardDir = Quaternion.Euler(0, rotationY, 0) * Vector3.forward;

        transform.position = start;
        Launch(Quaternion.Euler(0, rotationY, 0) * Quaternion.Euler(-elevationAngle, 0, 0) * Vector3.forward, velocity);
    }
    public void LaunchToTarget(CharacterBase shooter, SkillData skillData,
        Vector3 start, Vector3 end)
    {
        this.shooter = shooter;
        this.skillData = skillData;
        excludeHitDetectable = shooter.unitDetectable;
        int angle = skillData.initialElevationAngle;
        LaunchToTarget(start, end, angle);
    }

    private float CheckGrounded(float downSpeed)
    {
        Vector3 half = unitDetectable.size * 0.5f;
        Vector3[] localCorners = new Vector3[]
        {
            new Vector3(-half.x, -half.y, -half.z),
            new Vector3( half.x, -half.y, -half.z),
            new Vector3(-half.x, -half.y,  half.z),
            new Vector3( half.x, -half.y,  half.z),
        };

        float checkYOffset = downSpeed * Time.deltaTime;

        foreach (var localCorner in localCorners)
        {
            Vector3 worldCorner = transform.TransformPoint(unitDetectable.center + localCorner);
            float checkY = worldCorner.y + checkYOffset;
            if (world.CheckSolidNode(worldCorner.x, checkY, worldCorner.z))
            {
                return 0;
            }
        }
        return downSpeed;
    }
    public void CalculateVelocity()
    {
        velocity.y = Mathf.Max(velocity.y + gravity * Time.deltaTime, terminateGravity);

        if (velocity.z > 0 && CheckWorldBottomForward() || 
            velocity.z < 0 && CheckWorldBottomBackward())
            velocity.z = 0;
        if (velocity.x > 0 && CheckWorldBottomRight() || 
            velocity.x < 0 && CheckWorldBottomLeft())
            velocity.x = 0;

        if (velocity.y < 0)
            velocity.y = CheckGrounded(velocity.y);
        else if (velocity.y > 0 || CheckWorldUp())
            velocity.y = CheckGrounded(velocity.y);
    }
    public bool CheckWorldUp()
    {
        Vector3 half = unitDetectable.size * 0.5f;

        Vector3 localOffset = new Vector3(0, half.y, 0);
        Vector3 worldPoint = transform.TransformPoint(unitDetectable.center + localOffset);

        if (world.CheckSolidNode(worldPoint))
        {
            velocity = Vector3.zero;
            return true;
        }
        else
            return false;
    }
    public bool CheckWorldBottomForward()
    {
        Vector3 half = unitDetectable.size * 0.5f;

        Vector3 localOffset = new Vector3(0, -half.y, half.z);
        Vector3 worldPoint = transform.TransformPoint(unitDetectable.center + localOffset);

        if (world.CheckSolidNode(worldPoint))
        {
            velocity = Vector3.zero;
            return true;
        }
        else
            return false;
    
    }
    public bool CheckWorldBottomBackward()
    {
        Vector3 half = unitDetectable.size * 0.5f;

        Vector3 localOffset = new Vector3(0, -half.y, -half.z);
        Vector3 worldPoint = transform.TransformPoint(unitDetectable.center + localOffset);

        if (world.CheckSolidNode(worldPoint))
        {
            velocity = Vector3.zero;
            return true;
        }
        else
            return false;
    }
    public bool CheckWorldBottomRight()
    {
        Vector3 half = unitDetectable.size * 0.5f;

        Vector3 localOffset = new Vector3(half.x, -half.y, 0);
        Vector3 worldPoint = transform.TransformPoint(unitDetectable.center + localOffset);

        if (world.CheckSolidNode(worldPoint))
        {
            velocity = Vector3.zero;
            return true;
        }
        else
            return false;
    }
    public bool CheckWorldBottomLeft()
    {
        Vector3 half = unitDetectable.size * 0.5f;

        Vector3 localOffset = new Vector3(-half.x, -half.y, 0);
        Vector3 worldPoint = transform.TransformPoint(unitDetectable.center + localOffset);

        if (world.CheckSolidNode(worldPoint))
        {
            velocity = Vector3.zero;
            return true;
        }
        else
            return false;
    }
    public bool CheckWorldRightUpForward()
    {
        Vector3 half = unitDetectable.size * 0.5f;

        Vector3 localOffset = new Vector3(half.x, half.y, half.z);
        Vector3 worldPoint = transform.TransformPoint(unitDetectable.center + localOffset);

        if (world.CheckSolidNode(worldPoint))
        {
            velocity = Vector3.zero;
            return true;
        }
        else
            return false;
    }
    public bool CheckWorldRightDownForward()
    {
        Vector3 half = unitDetectable.size * 0.5f;

        Vector3 localOffset = new Vector3(half.x, -half.y, half.z);
        Vector3 worldPoint = transform.TransformPoint(unitDetectable.center + localOffset);

        if (world.CheckSolidNode(worldPoint))
        {
            velocity = Vector3.zero;
            return true;
        }
        else
            return false;
    }
    public bool CheckWorldLeftUpForward()
    {
        Vector3 half = unitDetectable.size * 0.5f;

        Vector3 localOffset = new Vector3(-half.x, half.y, half.z);
        Vector3 worldPoint = transform.TransformPoint(unitDetectable.center + localOffset);

        if (world.CheckSolidNode(worldPoint))
        {
            velocity = Vector3.zero;
            return true;
        }
        else
            return false;
    }
    public bool CheckWorldLeftDownForward()
    {
        Vector3 half = unitDetectable.size * 0.5f;

        Vector3 localOffset = new Vector3(-half.x, -half.y, half.z);
        Vector3 worldPoint = transform.TransformPoint(unitDetectable.center + localOffset);

        if (world.CheckSolidNode(worldPoint))
        {
            velocity = Vector3.zero;
            return true;
        }
        else
            return false;
    }
    public bool CheckWorldRightUpBackward()
    {
        Vector3 half = unitDetectable.size * 0.5f;

        Vector3 localOffset = new Vector3(half.x, half.y, -half.z);
        Vector3 worldPoint = transform.TransformPoint(unitDetectable.center + localOffset);

        if (world.CheckSolidNode(worldPoint))
        {
            velocity = Vector3.zero;
            return true;
        }
        else
            return false;
    }
    public bool CheckWorldRightDownBackward()
    {
        Vector3 half = unitDetectable.size * 0.5f;

        Vector3 localOffset = new Vector3(half.x, -half.y, -half.z);
        Vector3 worldPoint = transform.TransformPoint(unitDetectable.center + localOffset);

        if (world.CheckSolidNode(worldPoint))
        {
            velocity = Vector3.zero;
            return true;
        }
        else
            return false;
    }
    public bool CheckWorldLeftUpBackward()
    {
        Vector3 half = unitDetectable.size * 0.5f;

        Vector3 localOffset = new Vector3(-half.x, half.y, -half.z);
        Vector3 worldPoint = transform.TransformPoint(unitDetectable.center + localOffset);

        if (world.CheckSolidNode(worldPoint))
        {
            velocity = Vector3.zero;
            return true;
        }
        else
            return false;
    }
    public bool CheckWorldLeftDownBackward()
    {
        Vector3 half = unitDetectable.size * 0.5f;

        Vector3 localOffset = new Vector3(-half.x, -half.y, -half.z);
        Vector3 worldPoint = transform.TransformPoint(unitDetectable.center + localOffset);

        if (world.CheckSolidNode(worldPoint))
        {
            velocity = Vector3.zero;
            return true;
        }
        else
            return false;
    }

    public void OnDrawGizmos()
    {
        if (unitDetectable == null) { return; }
        Vector3 half = unitDetectable.size * 0.5f;
        Vector3 localCenter = unitDetectable.center;

        Vector3[] corners = new Vector3[]
        {
            new Vector3(+half.x, +half.y, +half.z),
            new Vector3(+half.x, -half.y, +half.z),
            new Vector3(-half.x, +half.y, +half.z),
            new Vector3(-half.x, -half.y, +half.z),
            new Vector3(+half.x, +half.y, -half.z),
            new Vector3(+half.x, -half.y, -half.z),
            new Vector3(-half.x, +half.y, -half.z),
            new Vector3(-half.x, -half.y, -half.z),
        };

        Gizmos.color = Color.red;
        foreach (var corner in corners)
        {
            Vector3 worldPoint = transform.TransformPoint(localCenter + corner);
            Gizmos.DrawSphere(worldPoint, 0.1f);
        }

        Vector3[] groundChecks = new Vector3[]
        {
            new Vector3(-half.x, -half.y, -half.z),
            new Vector3(half.x, -half.y, -half.z),
            new Vector3(-half.x, -half.y, half.z),
            new Vector3(half.x, -half.y, half.z),
        };

        float checkYOffset = velocity.y * Time.deltaTime;

        Gizmos.color = new Color(1, 1, 0, 0.5f);
        foreach (var groundCheck in groundChecks)
        {
            Vector3 worldPoint = transform.TransformPoint(localCenter + groundCheck);
            Vector3 nextPos = new Vector3(worldPoint.x, worldPoint.y + checkYOffset, worldPoint.z);

            Gizmos.DrawSphere(nextPos, 0.2f);
        }

    }
}