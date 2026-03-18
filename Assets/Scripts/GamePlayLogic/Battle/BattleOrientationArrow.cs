using UnityEngine;

public class BattleOrientationArrow : MonoBehaviour
{
    [SerializeField] private GameObject frontArrow;
    [SerializeField] private GameObject leftArrow;
    [SerializeField] private GameObject rightArrow;
    [SerializeField] private GameObject backArrow;

    private GameObject[] arrows;
    private bool activateArrow = false;
    public Orientation currentOrientation;

    public Material normalMat;
    public Material highlightMat;

    private void Update()
    {
        if (!activateArrow) { return; }

        Vector3 direction = Vector3.zero;

        if (Input.GetKeyDown(KeyCode.W))
        {
            SetArrowOrientation(Vector3.forward);
        }
        else if (Input.GetKeyDown(KeyCode.A))
        {
            SetArrowOrientation(Vector3.left);
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            SetArrowOrientation(Vector3.right);
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            SetArrowOrientation(Vector3.back);
        }
    }

    private void SetArrowOrientation(Vector3 direction)
    {
        Vector3 arrowDirection = Vector3.zero;
        arrowDirection = Vector3Int.RoundToInt(CameraController.instance.
            GetCameraTransformDirection(direction));

        if (arrowDirection == Vector3.forward)
        {
            currentOrientation = Orientation.forward;
        }
        else if (arrowDirection == Vector3.left)
        {
            currentOrientation = Orientation.left;
        }
        else if (arrowDirection == Vector3.right)
        {
            currentOrientation = Orientation.right;
        }
        else if (arrowDirection == Vector3.back)
        {
            currentOrientation = Orientation.back;
        }
        HighlightOrientationArrow(currentOrientation);
    }

    public void HighlightOrientationArrow(Orientation orientation)
    {
        foreach (var arrow in arrows)
        {
            var renderer = arrow.GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.material = normalMat;
        }

        GameObject selectedArrow = null;
        switch (orientation)
        {
            case Orientation.forward:
                selectedArrow = frontArrow;
                break;
            case Orientation.left:
                selectedArrow = leftArrow;
                break;
            case Orientation.right:
                selectedArrow = rightArrow;
                break;
            case Orientation.back:
                selectedArrow = backArrow;
                break;
        }

        if (selectedArrow != null)
        {
            var renderer = selectedArrow.GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.material = highlightMat;
        }
    }

    public void ShowArrows(Orientation orientation, GameNode targetNode, float centerOffset = 1.2f)
    {
        arrows = new GameObject[] { frontArrow, leftArrow, rightArrow, backArrow };
        Vector3 target = targetNode.GetNodeVector();
        currentOrientation = orientation;

        frontArrow.SetActive(true);
        frontArrow.transform.position = target + new Vector3(0, 1f, centerOffset);
        leftArrow.SetActive(true);
        leftArrow.transform.position = target + new Vector3(-centerOffset, 1f, 0);
        rightArrow.SetActive(true);
        rightArrow.transform.position = target + new Vector3(centerOffset, 1f, 0);
        backArrow.SetActive(true);
        backArrow.transform.position = target + new Vector3(0, 1f, -centerOffset);
        activateArrow = true;

        HighlightOrientationArrow(currentOrientation);
    }
    public void HideAll()
    {
        if (arrows == null)
        {
            Debug.LogWarning("Missing arrows");
            return;
        }
        foreach (var arrow in arrows)
            arrow.SetActive(false);
        activateArrow = false;
    }
}