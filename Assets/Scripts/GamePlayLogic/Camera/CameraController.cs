using UnityEngine;
using Tactics.InputHelper;
using System;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float rotationSpeed = 10f;
    private Vector3 cameraCurrentTargetPos;

    [Header("Zoom")]
    public Vector3 defaultZoom = new Vector3(0, 10, -8);
    public Vector3 maximunZoomIn = new Vector3(0, 6, -4);
    public Vector3 maximunZoomOut = new Vector3(0, 13, -11);

    [Header("Camera Component")]
    public Transform pivotPoint;
    private Vector3 generalPivotPos;
    private Quaternion generalPivotRotate;
    public Camera cameraBody;
    private Vector3 generalBodyPos;
    private Quaternion generalBodyRotate;

    [Space]
    [Tooltip("Control camera align to the target transform")]
    public Transform followTarget;
    private bool isTargeting = false;

    private bool isResetting = false;

    public bool enableRotateAlignment = false;
    private Quaternion rotateAlignmentTarget;
    private bool isRotateAligmenting = false;

    public bool enableTacticalView = false;

    public bool debugMode = false;
    public static CameraController instance { get; private set; }
    private void Awake()
    {
        instance = this;
    }
    public void Start()
    {
        if (followTarget != null)
        {
            isTargeting = true;
        }
        GameEvent.onLeaderChanged += (CharacterBase newLeader) =>
        {
            ChangeFollowTarget(newLeader.transform);
            isTargeting = true;
        };

        RecordPosAndAngle();
    }

    public void Update()
    {
        if (InputKeyHelper.GetKeyDownSolo(KeyCode.P))
        {
            if (enableTacticalView) return;
            isTargeting = true;
            isResetting = true;
        }
        else if (InputKeyHelper.GetKeyDownSolo(KeyCode.Escape))
        {
            isTargeting = true;
        }
        else if (InputKeyHelper.GetKeyDownSolo(KeyCode.T))
        {
            enableTacticalView = !enableTacticalView;
            isTargeting = true;
            if (enableTacticalView)
            {
                RecordPosAndAngle();
            }
        }
        else if (InputKeyHelper.GetKeyCombo(KeyCode.LeftControl, KeyCode.T))
        { 
            enableRotateAlignment = !enableRotateAlignment;
        }
    }

    private void LateUpdate()
    {
        CameraResetAligment();
        MoveCamera();
        RotateCamera();
        RotateCameraAlignment();
        ZoomCamera();
        MoveCameraViewAlignment();
        TacticCameraViewAlignment();
    }

    private void RecordPosAndAngle()
    {
        cameraCurrentTargetPos = transform.position;
        generalPivotPos = pivotPoint.localPosition;
        generalPivotRotate = pivotPoint.localRotation;
        generalBodyPos = cameraBody.transform.localPosition;
        generalBodyRotate = cameraBody.transform.localRotation;
    }
    private void CameraResetAligment()
    {
        if (!isResetting || enableTacticalView) return;

        cameraBody.transform.localPosition = Vector3.Lerp(cameraBody.transform.localPosition, defaultZoom,
            Time.deltaTime * moveSpeed);
        generalBodyPos = cameraBody.transform.localPosition;
        Quaternion defaultPivotRotate = Quaternion.Euler(Vector3.zero);
        pivotPoint.localRotation = Quaternion.Lerp(pivotPoint.localRotation,
            defaultPivotRotate, Time.deltaTime * moveSpeed);
        generalPivotRotate = pivotPoint.localRotation;

        if (Vector3.Distance(cameraBody.transform.localPosition, defaultZoom) < 0.01f &&
    Quaternion.Angle(pivotPoint.localRotation, defaultPivotRotate) < 0.5f)
        {
            isResetting = false;
        }
    }
    private void MoveCamera()
    {
        Vector3 direction = Vector3.zero;

        if (InputKeyHelper.GetKeySolo(KeyCode.J))
        {
            isTargeting = false;
            direction = new Vector3(-1, 0, 0);
        }
        if (InputKeyHelper.GetKeySolo(KeyCode.L))
        {
            isTargeting = false;
            direction = new Vector3(1, 0, 0);
        }
        if (InputKeyHelper.GetKeySolo(KeyCode.I))
        {
            isTargeting = false;
            direction = new Vector3(0, 0, 1);
        }
        if (InputKeyHelper.GetKeySolo(KeyCode.K))
        {
            isTargeting = false;
            direction = new Vector3(0, 0, -1);
        }

        Vector3 rotatedDirection = pivotPoint.transform.TransformDirection(direction);
        rotatedDirection.y = 0;

        if (rotatedDirection != Vector3.zero)
            transform.position += rotatedDirection * Time.deltaTime * moveSpeed;
    }
    private void MoveCameraViewAlignment()
    {
        if (isTargeting && followTarget != null)
        {
            Vector3 targetPosition = followTarget.position;
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed);
        }
    }
    private void RotateCamera()
    {
        if (enableRotateAlignment || enableTacticalView) { return;}
        if (InputKeyHelper.GetKeySolo(KeyCode.U))
        {
            pivotPoint.localEulerAngles += new Vector3(0, 10, 0) * Time.deltaTime * rotationSpeed;
            generalPivotRotate = pivotPoint.localRotation;
        }
        else if (InputKeyHelper.GetKeySolo(KeyCode.O))
        {
            pivotPoint.localEulerAngles += new Vector3(0, -10, 0) * Time.deltaTime * rotationSpeed;
            generalPivotRotate = pivotPoint.localRotation;
        }
    }
    private void RotateCameraAlignment()
    {
        if (!enableRotateAlignment || enableTacticalView) { return; }

        float currentY = pivotPoint.localEulerAngles.y;
        currentY = Mathf.Repeat(currentY, 360f);

        float snappedY = Mathf.Round(currentY / 90f) * 90f;

        if (InputKeyHelper.GetKeyDownSolo(KeyCode.U))
        {
            isRotateAligmenting = true;
            rotateAlignmentTarget = Quaternion.Euler(0, snappedY + 90f, 0);
        }
        else if (InputKeyHelper.GetKeyDownSolo(KeyCode.O))
        {
            isRotateAligmenting = true;
            rotateAlignmentTarget = Quaternion.Euler(0, snappedY - 90f, 0);
        }

        if (isRotateAligmenting)
        {
            pivotPoint.localRotation = Quaternion.Lerp(pivotPoint.localRotation, 
                rotateAlignmentTarget, Time.deltaTime * moveSpeed);
            generalPivotRotate = pivotPoint.localRotation;
        }

        if (Quaternion.Angle(pivotPoint.localRotation, rotateAlignmentTarget) <= 0.5f)
        {
            isRotateAligmenting = false;
            pivotPoint.localRotation = rotateAlignmentTarget;
            generalPivotRotate = pivotPoint.localRotation;
        }
    }
    private void ZoomCamera()
    {
        if (enableTacticalView) { return; }

        if (InputKeyHelper.GetKeySolo(KeyCode.Equals))
        {
            if (cameraBody.transform.localPosition.y < maximunZoomIn.y || cameraBody.transform.localPosition.z > maximunZoomIn.z) { return; }
            cameraBody.transform.localPosition += new Vector3(0, -1, 1) * Time.deltaTime * moveSpeed;
            generalBodyPos = cameraBody.transform.localPosition;
        }
        if (InputKeyHelper.GetKeySolo(KeyCode.Minus))
        {
            if (cameraBody.transform.localPosition.y > maximunZoomOut.y || cameraBody.transform.localPosition.z < maximunZoomOut.z) { return; }
            cameraBody.transform.localPosition += new Vector3(0, 1, -1) * Time.deltaTime * moveSpeed;
            generalBodyPos = cameraBody.transform.localPosition;
        }
    }
    private void TacticCameraViewAlignment()
    {
        Quaternion currentPivotRotation = pivotPoint.localRotation;
        Vector3 currentBodyPosition = cameraBody.transform.localPosition;
        Quaternion currentBodyRotation = cameraBody.transform.localRotation;

        if (enableTacticalView)
        {
            Quaternion alignmentPivotRotation = Quaternion.Euler(Vector3.zero);
            pivotPoint.localRotation = Quaternion.Lerp(currentPivotRotation, 
                alignmentPivotRotation, Time.deltaTime * moveSpeed);

            Quaternion alignmentBodyRotation = Quaternion.Euler(new Vector3(90, 0, 0));
            Vector3 alignmentBodyPosition = new Vector3(0, maximunZoomOut.y, 0);
            cameraBody.transform.localPosition = Vector3.Lerp(currentBodyPosition, alignmentBodyPosition, 
                Time.deltaTime * moveSpeed);
            cameraBody.transform.localRotation = Quaternion.Lerp(currentBodyRotation, alignmentBodyRotation, 
                Time.deltaTime * moveSpeed);
        }
        else
        {
            pivotPoint.localRotation = Quaternion.Lerp(currentPivotRotation, 
                generalPivotRotate, Time.deltaTime * moveSpeed);

            cameraBody.transform.localPosition = Vector3.Lerp(currentBodyPosition, generalBodyPos, Time.deltaTime * moveSpeed);
            cameraBody.transform.localRotation = Quaternion.Lerp(currentBodyRotation, generalBodyRotate, Time.deltaTime * moveSpeed);
        }
    }

    /// <summary>
    /// Change camera follow target, the target would be followed by camera 
    /// until another target is assigned.
    /// </summary>
    /// <param name="transform">
    /// The Transform of the object that the camera should follow
    /// </param>
    public void ChangeFollowTarget(Transform transform, Action onTargeted = null)
    {
        if (debugMode)
            Debug.Log($"Change target {transform.name}");
        followTarget = transform;
        isTargeting = true;
    }

    public void TurnOnTargeting(bool enabled)
    {
        if (followTarget != null)
            isTargeting = true;
    }

    public Vector3 GetCameraTransformDirection(Vector3 direction)
    {
        if (cameraBody != null && !enableTacticalView)
        {
            Vector3 rotatedDirection = cameraBody.transform.TransformDirection(direction);
            rotatedDirection.y = 0;
            Vector3 normalizedDir = rotatedDirection.normalized;
            return normalizedDir;
        }
        return direction;
    }
    private void OnDrawGizmos()
    {
        if (pivotPoint != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawSphere(pivotPoint.position, 0.1f);
        }
    }
}