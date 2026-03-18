using TMPro;
using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;

public class CTTurnUIManager : MonoBehaviour
{
    [Serializable]
    public class TurnUIImage
    {
        public Image backgroundPanel;
        public Image characterImage;
        public TextMeshProUGUI turnUIText;

        public CharacterBase character;
        public int roundCount;
        public int turnCount;

        public TurnUIImage(Transform parent, CharacterBase character, TMP_FontAsset fontAsset, int roundCount, int turnCount)
        {
            this.character = character;
            this.roundCount = roundCount;
            this.turnCount = turnCount;

            CreateBackgroundPanel(parent);
            CreateCharacterImage(backgroundPanel.transform);
            CreateTurnText(backgroundPanel.transform, fontAsset);
        }

        private void CreateBackgroundPanel(Transform parent)
        {
            backgroundPanel = new GameObject("BackgroundPanel").AddComponent<Image>();
            backgroundPanel.transform.SetParent(parent, false);
            backgroundPanel.rectTransform.sizeDelta = new Vector2(140, 100);
            backgroundPanel.color = new Color(0, 0, 0, 220 / 255f);
        }

        private void CreateCharacterImage(Transform parent)
        {
            characterImage = new GameObject("CharacterImage").AddComponent<Image>();
            characterImage.gameObject.transform.SetParent(parent, false);
            characterImage.rectTransform.anchoredPosition = Vector2.zero;
            characterImage.rectTransform.sizeDelta = new Vector2(130, 90);
            characterImage.sprite = character.data.turnUISprite;
        }

        private void CreateTurnText(Transform parent, TMP_FontAsset fontAsset)
        {
            turnUIText = new GameObject("TurnText").AddComponent<TextMeshProUGUI>();
            turnUIText.gameObject.transform.SetParent(parent, false);
            turnUIText.rectTransform.anchoredPosition = new Vector2(-20, -56);
            turnUIText.rectTransform.sizeDelta = new Vector2(100, 20);
            turnUIText.fontSize = 16;
            turnUIText.fontStyle = FontStyles.Italic | FontStyles.SmallCaps;
            turnUIText.font = fontAsset;
            turnUIText.color = new Color(165, 165, 165);
            turnUIText.text = $"Turn {roundCount}";
        }

    }

    [Header("Target Image")]
    [SerializeField] private TextMeshProUGUI targetCharacterNameText;
    [SerializeField] private Image targetUnitImage;
    [SerializeField] private TextMeshProUGUI maxHeathText;
    [SerializeField] private TextMeshProUGUI currentHeathText;
    [SerializeField] private Image heathBarImage;
    [SerializeField] private TextMeshProUGUI maxMentalText;
    [SerializeField] private TextMeshProUGUI currentMentalText;
    [SerializeField] private Image mentalBarImage;

    [Header("Content Image")]
    [SerializeField] private GameObject turnUIContent;
    [SerializeField] private GameObject roundPhaseUI;
    [SerializeField] private TMP_FontAsset fontAsset;
    private List<TurnUIImage> turnUIImages = new List<TurnUIImage>();
    private List<GameObject> roundPhaseUIObjects = new List<GameObject>();
    private List<TurnUIImage> pastTurnUIImages = new List<TurnUIImage>();

    private List<CTRound> allCTRound;
    private CTRound currentCTRound;
    private CTTurn currentCTTurn;
    private int generatedRoundIndex = -1;
    private int generatedTurnIndex = -1;

    public CharacterBase currentTargetCharacter { get; private set; }
    
    [Header("UI Smooth Move")]
    [SerializeField] private float smoothTime = 0.2f;
    private Vector2 targetAnchoredPos;
    private bool isFocusing = false;
    private Vector2 velocity = Vector2.zero;
    public static CTTurnUIManager instance {  get; private set; }

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        if (isFocusing)
        {
            RectTransform rectContent = turnUIContent.GetComponent<RectTransform>();
            rectContent.anchoredPosition = Vector2.SmoothDamp(rectContent.anchoredPosition, targetAnchoredPos, ref velocity, smoothTime);

            if (Vector2.Distance(rectContent.anchoredPosition, targetAnchoredPos) < 0.1f)
            {
                rectContent.anchoredPosition = targetAnchoredPos;
                isFocusing = false;
            }
        }
    }

    public void GenerateTimelineUI()
    {
        ResetAllTurnUI();
        GenerateTurnUI();
        //  Wait 1 frame to let LayoutGroup/Canvas finish
        StartCoroutine(DelayTargetFocus());
    }
    private IEnumerator DelayTargetFocus()
    {
        yield return null;
        TargetCurrentCTTurnUI(CTTimeline.instance.GetCurrentCTRound(), CTTimeline.instance.GetCurrentCTTurn());
    }

    private void GenerateTurnUI()
    {
        for (int i = 0; i < allCTRound.Count; i++)
        {
            GameObject roundPhaseUIObject = Instantiate(roundPhaseUI);
            roundPhaseUIObject.transform.SetParent(turnUIContent.transform, false);
            roundPhaseUIObjects.Add(roundPhaseUIObject);

            List<CharacterBase> characterQueues = allCTRound[i].GetCharacterQueue();

            for (int j = 0; j < characterQueues.Count; j++)
            {
                TurnUIImage turnUIImage = new TurnUIImage(turnUIContent.transform, allCTRound[i].GetCharacterQueue()[j], fontAsset, allCTRound[i].roundCount, j);
                turnUIImages.Add(turnUIImage);
                generatedTurnIndex++;
            }
            generatedRoundIndex++;
        }
    }

    public void AppendTurnUI()
    {
        allCTRound = CTTimeline.instance.GetAllRound();

        //  Check release round
        if (generatedRoundIndex >= allCTRound.Count) { return; }

        CTRound cTRound = allCTRound[generatedRoundIndex];
        List<CharacterBase> queue = cTRound.GetCharacterQueue();

        generatedTurnIndex++;   //  Next generate
        if (generatedTurnIndex >= queue.Count)
        {
            generatedRoundIndex++;
            generatedTurnIndex = 0;

            if (generatedRoundIndex >= allCTRound.Count) { return; }

            GameObject turnPhaseGameObject = Instantiate(roundPhaseUI);
            turnPhaseGameObject.transform.SetParent(turnUIContent.transform, false);

            cTRound = allCTRound[generatedRoundIndex];
            queue = cTRound.GetCharacterQueue();
        }

        CharacterBase character = cTRound.GetCharacterAt(generatedTurnIndex);
        if (character != null)
        {
            TurnUIImage turnUIImage = new TurnUIImage(turnUIContent.transform, character, 
                fontAsset, generatedRoundIndex, generatedTurnIndex);
            turnUIImages.Add(turnUIImage);
        }
    }

    public void ResetAllTurnUI()
    {
        generatedRoundIndex = -1;
        generatedTurnIndex = -1;
        allCTRound = CTTimeline.instance.GetAllRound();
        if (allCTRound.Count == 0)
        {
            Debug.LogWarning("CTTurnUIManager: No CTRound in history!");
            return;
        }

        foreach (Transform child in turnUIContent.transform)
        {
            Destroy(child.gameObject);
        }
        turnUIImages.Clear();
        roundPhaseUIObjects.Clear();
        pastTurnUIImages.Clear();
    }

    public void AdjustTurnUIStartRound(int roundIndex)
    {
        allCTRound = CTTimeline.instance.GetAllRound();
        CTRound cTRound = allCTRound[roundIndex];
        List<CharacterBase> leaveCharacters = CTTimeline.instance.leaveBattleCharacter;

        if (leaveCharacters.Count == 0) { return; }

        foreach (CharacterBase character in leaveCharacters)
        {
            for (int i = turnUIImages.Count - 1; i >= 0; i--)
            {
                if (turnUIImages[i].roundCount != cTRound.roundCount) continue;
                if (turnUIImages[i].character != character) continue;
                if (pastTurnUIImages.Contains(turnUIImages[i])) continue;
                turnUIImages[i].backgroundPanel.gameObject.SetActive(false);
            }
        }

        int maxRound = allCTRound.Count;
        for (int i = turnUIImages.Count - 1; i >= 0; i--)
        {
            if (turnUIImages[i].roundCount >= roundIndex)
            {
                TurnUIImage turnUIImage = turnUIImages[i];
                if (turnUIImage != null)
                {
                    turnUIImage.backgroundPanel.gameObject.SetActive(false);
                }
                turnUIImages.RemoveAt(i);
            }
        }
        for (int i = roundPhaseUIObjects.Count - 1; i >= 0; i--)
        {
            roundPhaseUIObjects[i].SetActive(false);
            roundPhaseUIObjects.RemoveAt(i);
        }

        for (int i = roundIndex; i < maxRound; i++)
        {
            GameObject roundPhaseUIObject = Instantiate(roundPhaseUI);
            roundPhaseUIObject.transform.SetParent(turnUIContent.transform, false);
            roundPhaseUIObjects.Add(roundPhaseUIObject);

            List<CharacterBase> characterQueues = allCTRound[i].GetCharacterQueue();

            for (int j = 0; j < characterQueues.Count; j++)
            {
                TurnUIImage turnUIImage = new TurnUIImage(turnUIContent.transform,
                    characterQueues[j],fontAsset,allCTRound[i].roundCount,j);
                turnUIImages.Add(turnUIImage);
            }
        }
    }

    public void RecordPastTurnUI(CTRound cTRound, CTTurn cTTurn)
    {
        for (int i = turnUIImages.Count - 1; i >= 0 ; i--)
        {
            if (turnUIImages[i].roundCount == cTRound.roundCount)
            {
                if (turnUIImages[i].turnCount == cTTurn.turnCount)
                {
                    pastTurnUIImages.Add(turnUIImages[i]);
                    break;
                }
            }
        }
    }

    #region CT Target UI
    private void FocusOnCharacterUI(RectTransform target)
    {
        RectTransform rectContent = turnUIContent.GetComponent<RectTransform>();
        RectTransform rectviewport = rectContent.parent.GetComponent<RectTransform>();
        HorizontalLayoutGroup horizontalLayoutGroup = rectContent.GetComponent<HorizontalLayoutGroup>();

        float targetX = target.anchoredPosition.x + target.rect.width * 0.5f;
        float distance = rectContent.anchoredPosition.x + targetX + horizontalLayoutGroup.spacing * 0.5f;

        targetAnchoredPos = new Vector2(rectContent.anchoredPosition.x - distance, rectContent.anchoredPosition.y);
        isFocusing = true;
    }

    public void TargetCurrentCTTurnUI(CTRound cTRound, CTTurn currentTurn)
    {
        currentCTRound = cTRound;
        currentCTTurn = currentTurn;

        for (int i = turnUIImages.Count - 1; i >= 0; i--)
        {
            if (turnUIImages[i].roundCount != cTRound.roundCount) { continue; }
            if (turnUIImages[i].turnCount != currentTurn.turnCount) { continue; }

            UpdateTargetCharacterUI(turnUIImages[i].character);
            RectTransform target = turnUIImages[i].backgroundPanel.rectTransform;
            FocusOnCharacterUI(target);
            break;
        }
    }
    public void TargetCursorNodeCharacterUI(GameNode targetNode)
    {
        if (targetNode == null) return;
        CharacterBase character = targetNode.GetUnitGridCharacter();
        TargetCursorCharacterUI(character);
    }
    public void TargetCursorCharacterUI(CharacterBase character)
    {
        if (character == null) return;

        for (int i = 0; i < turnUIImages.Count; i++)
        {
            if (turnUIImages[i].roundCount < currentCTRound.roundCount) { continue; }
            if (turnUIImages[i].turnCount < currentCTTurn.turnCount && 
                turnUIImages[i].roundCount == currentCTRound.roundCount) { continue;}

            if (turnUIImages[i].character == character)
            {
                UpdateTargetCharacterUI(character);
                RectTransform target = turnUIImages[i].backgroundPanel.rectTransform;
                FocusOnCharacterUI(target);
                break;
            }
        }
    }

    private void UpdateTargetCharacterUI(CharacterBase character)
    {
        currentTargetCharacter = character;

        if (targetUnitImage == null) { return; }

        Sprite sprite = character.data.turnUISprite;
        if (sprite != null)
            targetUnitImage.sprite = sprite;
        else
        {
            Debug.LogWarning($"{character} turn image is missing");
            targetUnitImage.sprite = Utils.CreateGraySprite();
        }
        
        if (targetCharacterNameText != null)
            targetCharacterNameText.text = character.data.characterName;

        if (maxHeathText != null)
            maxHeathText.text = character.data.health.ToString();
        if (currentHeathText != null)
            currentHeathText.text = character.currentHealth.ToString();
        if (maxMentalText != null)
            maxMentalText.text = character.data.mental.ToString();
        if (currentMentalText != null)
            currentMentalText.text = character.currentMental.ToString();

        if (heathBarImage != null)
            heathBarImage.fillAmount = (float)character.currentHealth / character.data.health;
        if (mentalBarImage != null)
            mentalBarImage.fillAmount = (float)character.currentMental / character.data.mental;
    }
    public void ChangeUICurrentHealthTo(CharacterBase character, int targetHealth)
    {
        StartCoroutine(Utils.UIFilledValueChangeToCoroutine(
            heathBarImage, character.data.health, character.currentHealth, targetHealth, 0.5f));

        float value = targetHealth - character.currentHealth;
        StartCoroutine(Utils.TextValueChangeCoroutine(
            currentHeathText, character.currentHealth, value, 0.5f, true));
    }
    public void ChangeUICurrentMentalTo(CharacterBase character, int targetMental)
    {
        StartCoroutine(Utils.UIFilledValueChangeToCoroutine(
            mentalBarImage, character.data.mental, character.currentMental, targetMental, 0.5f));

        float value = targetMental - character.currentMental;
        StartCoroutine(Utils.TextValueChangeCoroutine(
            currentMentalText, character.currentMental, value, 0.5f, true));
    }
    #endregion
}
