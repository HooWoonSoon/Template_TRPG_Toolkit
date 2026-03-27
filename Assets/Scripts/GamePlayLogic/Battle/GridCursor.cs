using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GridCursor : Entity
{
    public Camera referenceCamera;

    [Header("Battle Cursor")]
    [SerializeField] private GameObject cursor;
    [SerializeField] private float heightOffset = 2.5f;

    private bool activateCursor = false;
    private float keyPressTimer;
    private float intervalPressTimer;
    public bool hasMoved = false;

    [Header("Battle Cusor Related UI")]
    public GameObject actionOptionPanel;
    public GameObject battlefieldInfoPanel;
    public TextMeshProUGUI characterTextUI;

    public TextMeshProUGUI maxHealthTextUI;
    public TextMeshProUGUI healthTextUI;
    public Image healthUIImage;

    public TextMeshProUGUI maxMentalTextUI;
    public TextMeshProUGUI mentalTextUI;
    public Image mentalUIImage;

    public GameNode currentNode { get; private set; }

    protected override void Start()
    {
        base.Start();
        cursor.SetActive(false);
        actionOptionPanel.SetActive(false);
        battlefieldInfoPanel.SetActive(false);
    }

    private void Update()
    {
        if (activateCursor)
        {
            hasMoved = false;

            if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.A) ||
                Input.GetKeyDown(KeyCode.W) && Input.GetKeyDown(KeyCode.A))
                HandleInput(Vector3Int.forward + Vector3Int.left, 0.2f, 0.04f);
            else if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.D) ||
                Input.GetKeyDown(KeyCode.W) && Input.GetKeyDown(KeyCode.D))
                HandleInput(Vector3Int.forward + Vector3Int.right, 0.2f, 0.04f);
            else if (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.S) ||
                Input.GetKeyDown(KeyCode.A) && Input.GetKeyDown(KeyCode.S))
                HandleInput(Vector3Int.left + Vector3Int.back, 0.2f, 0.04f);
            else if (Input.GetKey(KeyCode.D) && Input.GetKey(KeyCode.S) ||
                Input.GetKeyDown(KeyCode.D) && Input.GetKeyDown(KeyCode.S))
                HandleInput(Vector3Int.right + Vector3Int.back, 0.2f, 0.04f);
            else if (Input.GetKey(KeyCode.W) || Input.GetKeyDown(KeyCode.W))
                HandleInput(Vector3Int.forward, 0.2f, 0.04f);
            else if (Input.GetKey(KeyCode.S) || Input.GetKeyDown(KeyCode.S))
                HandleInput(Vector3Int.back, 0.2f, 0.04f);
            else if (Input.GetKey(KeyCode.A) || Input.GetKeyDown(KeyCode.A))
                HandleInput(Vector3Int.left, 0.2f, 0.04f);
            else if (Input.GetKey(KeyCode.D) || Input.GetKeyDown(KeyCode.D))
                HandleInput(Vector3Int.right, 0.2f, 0.04f);
        }
    }

    private void TryMove(Vector3Int direction)
    {
        Vector3Int gridDirection = direction;
        gridDirection = Vector3Int.RoundToInt(CameraController.instance.
            GetCameraTransformDirection(direction));
        
        Vector3Int nodePos = Utils.RoundXZFloorYInt(cursor.transform.position);
        GameNode gameNode = world.GetHeightNodeWithCube(nodePos.x + gridDirection.x, nodePos.z + gridDirection.z);
        if (gameNode != null)
        {
            cursor.transform.position = gameNode.GetNodeVectorInt() + new Vector3(0, heightOffset);
            currentNode = gameNode;
            hasMoved = true;
        }
        CameraController.instance.TurnOnTargeting(true);
    }

    private void HandleInput(Vector3Int direction, float initialTimer, float interval)
    {
        if (Input.anyKeyDown)
        {
            keyPressTimer = 0;
            TryMove(direction);
        }
        if (Input.anyKey)
        {
            keyPressTimer += Time.deltaTime;
            if (keyPressTimer > initialTimer)
            {
                intervalPressTimer += Time.deltaTime;
                if (intervalPressTimer >= interval)
                {
                    TryMove(direction);
                    intervalPressTimer = 0;
                }
            }
        }
    }

    /// <summary>
    /// Set the grid cursor to target node position
    /// </summary>
    public void SetGridCursorAt(GameNode targetNode)
    {
        currentNode = targetNode;
        activateCursor = true;

        cursor.SetActive(true);
        Vector3Int position = targetNode.GetNodeVectorInt();
        cursor.transform.position = position + new Vector3(0, heightOffset);
        CameraController.instance.ChangeFollowTarget(cursor.transform);
        CharacterBase character = targetNode.GetUnitGridCharacter();
        if (character != null)
        {
            CTTurnUIManager.instance.TargetCursorCharacterUI(character);
        }
    }

    public void ActivateMoveCursor(bool allowControl, bool hide)
    {
        activateCursor = allowControl;
        cursor.SetActive(!hide);
    }

    public void SwitchActionPanel()
    {
        actionOptionPanel.SetActive(true);
        battlefieldInfoPanel.SetActive(false);
    }

    public void SwitchInfoPanel()
    {
        actionOptionPanel.SetActive(false);
        battlefieldInfoPanel.SetActive(true);
        CharacterBase character = currentNode.GetUnitGridCharacter();
        if (character != null)
        {
            characterTextUI.text = character.data.characterName;
            maxHealthTextUI.text = character.data.health.ToString();
            healthTextUI.text = character.currentHealth.ToString();

            healthUIImage.type = Image.Type.Filled;
            healthUIImage.fillMethod = Image.FillMethod.Horizontal;
            healthUIImage.fillAmount = (float)character.currentHealth / character.data.health;

            maxMentalTextUI.text = character.data.mental.ToString();
            mentalTextUI.text = character.currentMental.ToString();

            mentalUIImage.type = Image.Type.Filled;
            mentalUIImage.fillMethod = Image.FillMethod.Horizontal;
            mentalUIImage.fillAmount = (float)character.currentMental / character.data.mental;
        }
    }

    public void OffPanel()
    {
        actionOptionPanel.SetActive(false);
        battlefieldInfoPanel.SetActive(false);
    }
}