using UnityEngine.UI;
using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;

public enum AbilityType
{
    Skill, Spell, Inventory
}

public class SkillUIManager : MonoBehaviour
{
    [Serializable]
    public class UIImage
    {
        public RectTransform skillListTransform;
        public Image backgroundImage;
        public Image leftConerImage;
        public Image skillImage;
        public Image lineImage;
        public TextMeshProUGUI skillText;

        public Image conditionImage;
        public Image conditionIcon;
        public TextMeshProUGUI spText;

        public UIImage(Transform parent, TMP_FontAsset fontAsset = null, Sprite iconSprite = null, string skillName = null,
            int spValue = 0, Sprite iconSPSprite = null)
        {
            GenerateEmptySkillUI(parent);
            if (iconSprite != null && skillName != null)
            {
                GenerateContent(fontAsset, iconSprite, skillName);
                GenerateConditionContent(spValue, iconSPSprite);
            }
        }

        public void GenerateEmptySkillUI(Transform parent)
        {
            skillListTransform = new GameObject("Skill List").AddComponent<RectTransform>();
            skillListTransform.sizeDelta = new Vector2(450, 90);
            skillListTransform.SetParent(parent, false);

            RectTransform skillUIObject = new GameObject("Skill UI").AddComponent<RectTransform>();
            skillUIObject.SetParent(skillListTransform, false);

            backgroundImage = new GameObject("Background").AddComponent<Image>();
            backgroundImage.rectTransform.sizeDelta = new Vector2(450, 90);
            float alpha = 200 / 255f;
            backgroundImage.color = new Color(0, 0, 0, alpha);
            backgroundImage.rectTransform.SetParent(skillUIObject.transform, false);

            leftConerImage = new GameObject("Left Coner Image").AddComponent<Image>();
            leftConerImage.rectTransform.SetParent(skillListTransform, false);
            leftConerImage.rectTransform.anchoredPosition = new Vector2(-218, 0);
            leftConerImage.rectTransform.sizeDelta = new Vector2(10, 90);
        }
        public void GenerateContent(TMP_FontAsset fontAsset, Sprite icon, string skillName)
        {
            skillImage = new GameObject("Skill Type Icon").AddComponent<Image>();
            skillImage.rectTransform.SetParent(skillListTransform, false);
            skillImage.rectTransform.anchoredPosition = new Vector2(-165, 23);
            skillImage.rectTransform.sizeDelta = new Vector2(35, 35);
            skillImage.sprite = icon;

            lineImage = new GameObject("Line").AddComponent<Image>();
            lineImage.rectTransform.SetParent(skillListTransform, false);
            lineImage.rectTransform.anchoredPosition = new Vector2(0, 0);
            lineImage.rectTransform.sizeDelta = new Vector2(375, 2);

            skillText = new GameObject($"{skillName} Text").AddComponent<TextMeshProUGUI>();
            skillText.rectTransform.SetParent(skillListTransform, false);
            skillText.rectTransform.anchoredPosition = new Vector2(20, 22);
            skillText.rectTransform.sizeDelta = new Vector2(300, 24);
            skillText.font = fontAsset;
            skillText.fontSize = 24;
            skillText.text = skillName;
        }
        public void GenerateConditionContent(int requireSP, Sprite spIcon)
        {
            conditionImage = new GameObject("Condition Image").AddComponent<Image>();
            conditionImage.rectTransform.SetParent(skillListTransform, false);
            conditionImage.rectTransform.anchoredPosition = new Vector2(-92, -20);
            conditionImage.rectTransform.sizeDelta = new Vector2(110, 20);

            conditionIcon = new GameObject("Condition Icon").AddComponent<Image>();
            conditionIcon.rectTransform.SetParent(conditionImage.rectTransform, false);
            conditionIcon.rectTransform.anchoredPosition = new Vector2(-68, 0);
            conditionIcon.rectTransform.sizeDelta = new Vector2(20, 20);
            if (spIcon != null)
            {
                conditionIcon.sprite = spIcon;
            }

            spText = new GameObject("SP Text").AddComponent<TextMeshProUGUI>();
            spText.rectTransform.SetParent(conditionImage.rectTransform, false);
            spText.rectTransform.sizeDelta = new Vector2(110, 20);
            spText.fontSize = 15;
            spText.color = Color.black;
            spText.alignment = TextAlignmentOptions.Center;

            if (requireSP == 0)
            {
                spText.text = "SP ---";
            }
            else
            {
                spText.text = "SP " + requireSP.ToString();
            }
        }
    }

    [Serializable]
    public class TypeRespository
    {
        public Sprite sprite;
        public AbilityType type;
    }

    [Serializable]
    public class TypeUIImage
    {
        public AbilityType type;
        public Image backgroundImage;
        public Image contentImage;

        public TypeUIImage(Transform parent, AbilityType type, Sprite sprite)
        {
            RectTransform imageRect = new GameObject($"{sprite.name} Icon").AddComponent<RectTransform>();
            imageRect.transform.SetParent(parent, false);
            imageRect.sizeDelta = new Vector2(60, 60);
            this.type = type;

            backgroundImage = new GameObject($"Background").AddComponent<Image>();
            backgroundImage.transform.SetParent(imageRect.transform, false);
            backgroundImage.rectTransform.sizeDelta = new Vector2(60, 60);
            float alpha = 200f / 255f;
            backgroundImage.color = new Color(0, 0, 0, alpha);

            contentImage = new GameObject($"{sprite.name} Image").AddComponent<Image>();
            contentImage.transform.SetParent(imageRect.transform, false);
            contentImage.rectTransform.sizeDelta = new Vector2(60, 60);
            contentImage.sprite = sprite;
        }
    }

    [SerializeField] private Transform typeUIContent;
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField] private TextMeshProUGUI powerTextUI;
    [SerializeField] private TextMeshProUGUI descriptionTextUI;

    public TypeRespository[] respositories;
    private Dictionary<AbilityType, Sprite> typeMapDictionary;
    private List<TypeUIImage> typeUIImages = new List<TypeUIImage>();
    private int typeIndex = -1;
    private AbilityType currentSelectedType;

    [SerializeField] private Transform skillUIContent;
    private List<UIImage> uIImages = new List<UIImage>();
    private List<SkillData> skillDatas;
    private List<SkillData> spellDatas;
    private List<InventoryData> inventoryDatas;
    private int listOptionIndex = -1;

    private CharacterBase currentCharacter;
    public static SkillUIManager instance { get; private set; }

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        if (typeUIImages == null || typeUIImages.Count == 0) { return; }

        if (Input.GetKeyDown(KeyCode.A))
        {
            if (typeIndex > 0)
            {
                typeIndex -= 1;
                AbilityType type = typeUIImages[typeIndex].type;
                currentSelectedType = type;
                FocusCurrentTypeUI(typeIndex);
                RefreshToNextTypeList(type);
                GameEvent.onListOptionChanged?.Invoke();
            }
        }
        else if (Input.GetKeyDown(KeyCode.D))
        { 
            if (typeIndex < typeUIImages.Count - 1)
            {
                typeIndex += 1;
                AbilityType type = typeUIImages[typeIndex].type;
                currentSelectedType = type;
                FocusCurrentTypeUI(typeIndex);
                RefreshToNextTypeList(type);
                GameEvent.onListOptionChanged?.Invoke();  
            }
        }

        if (uIImages == null || uIImages.Count == 0) { return; }

        if (Input.GetKeyDown(KeyCode.W))
        {
            if (listOptionIndex > 0)
            {
                listOptionIndex -= 1;
                FocusCurrentListUI(listOptionIndex);
                UpdateSkillDescription(listOptionIndex);
                GameEvent.onListOptionChanged?.Invoke();
            }
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            if (listOptionIndex < uIImages.Count - 1)
            {
                listOptionIndex += 1;
                FocusCurrentListUI(listOptionIndex);
                UpdateSkillDescription(listOptionIndex);
                GameEvent.onListOptionChanged?.Invoke();
            }
        }
    }

    public void Initialize(List<SkillData> skillDatas, 
        List<InventoryData> inventoryDatas, CharacterBase character)
    {
        ResetAll();
        currentCharacter = character;
        InitializeTypeIcon(skillDatas);
        InitializeSkillList(skillDatas);
    }

    public void ResetAll()
    {
        StopAllCoroutines();
        currentCharacter = null;
        typeIndex = -1;
        listOptionIndex = -1;
        currentSelectedType = 0;
        typeUIImages = new List<TypeUIImage>();
        skillDatas = new List<SkillData>();
        spellDatas = new List<SkillData>();
        inventoryDatas = new List<InventoryData>();
        uIImages = new List<UIImage>();

        foreach (Transform child in typeUIContent)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in skillUIContent)
        {
            Destroy(child.gameObject);
        }
    }

    public void RefreshToNextTypeList(AbilityType type)
    {
        StopAllCoroutines();
        listOptionIndex = 0;
        uIImages = new List<UIImage>();
        foreach (Transform child in skillUIContent)
        {
            Destroy(child.gameObject);
        }

        if (skillDatas == null) { Debug.LogWarning("Issue in skillData input"); }

        if (type == AbilityType.Skill)
        {
            InitializeSkillList(skillDatas);
        }
        else if (type == AbilityType.Inventory)
        {
            InitializeInventoryList(inventoryDatas);
        }
        else
        {
            InitializeAbilityList(skillDatas, type);
        }
        FocusCurrentListUI(0);
    }

    #region Initialize Type UI
    private void BuildDictionary()
    {
        typeMapDictionary = new Dictionary<AbilityType, Sprite>();
        foreach (TypeRespository respository in respositories)
        {
            if (!typeMapDictionary.ContainsKey(respository.type))
            {
                typeMapDictionary.Add(respository.type, respository.sprite);
            }
        }
    }

    public void InitializeTypeIcon(List<SkillData> skillDatas)
    {
        BuildDictionary();
        HashSet<AbilityType> containTypeSet = new HashSet<AbilityType>();

        typeMapDictionary.TryGetValue(AbilityType.Skill, out Sprite spriteSkill);
        if (spriteSkill != null) 
        {
            TypeUIImage typeUIImageSkill = new TypeUIImage(typeUIContent, AbilityType.Skill, spriteSkill);
            typeUIImages.Add(typeUIImageSkill);
        }

        foreach (SkillData skillData in skillDatas)
        {
            AbilityType type = skillData.abilityType;
            if (type == AbilityType.Skill || type == AbilityType.Inventory) { continue; }
            if (typeMapDictionary.ContainsKey(type))
            {
                if (!containTypeSet.Contains(type))
                {
                    typeMapDictionary.TryGetValue(type, out Sprite sprite);
                    TypeUIImage typeUIImage = new TypeUIImage(typeUIContent, type, sprite);
                    typeUIImages.Add(typeUIImage);
                    containTypeSet.Add(type);
                }
            }
        }

        typeMapDictionary.TryGetValue(AbilityType.Inventory, out Sprite spriteInventory);
        if (spriteInventory != null)
        {
            TypeUIImage typeUIInventory = new TypeUIImage(typeUIContent, AbilityType.Inventory, spriteInventory);
            typeUIImages.Add(typeUIInventory);
        }

        if (typeMapDictionary.Count > 0)
        {
            typeIndex = 0;
            FocusCurrentTypeUI(0);
        }
    }
    #endregion

    #region Initialize UI List
    public void InitializeSkillList(List<SkillData> skillDatas)
    {
        this.skillDatas = skillDatas;

        BuildSkillUI(skillDatas);
    }
    public void InitializeAbilityList(List<SkillData> skillDatas, AbilityType type)
    {
        List<SkillData> typeSkillList = new List<SkillData>();
        foreach (SkillData skillData in skillDatas)
        {
            if (skillData.abilityType == type)
            {
                typeSkillList.Add(skillData);
            }
        }
        spellDatas = typeSkillList;

        BuildSkillUI(spellDatas);
    }
    public void InitializeInventoryList(List<InventoryData> inventoryDatas)
    {
        this.inventoryDatas = inventoryDatas;

        BuildInventoryUI(inventoryDatas);
    }

    private void BuildSkillUI(List<SkillData> skillDatas)
    {
        for (int i = 0; i < skillDatas.Count; i++)
        {
            UIImage skillUIImage = new UIImage(skillUIContent, fontAsset,
                skillDatas[i].skillIcon, skillDatas[i].skillName, skillDatas[i].MPAmount, skillDatas[i].MPIcon);
            uIImages.Add(skillUIImage);
        }

        if (skillDatas.Count < 4)
        {
            int release = 4 - skillDatas.Count;
            for (int i = 0; i < release; i++)
            {
                UIImage skillUIImage = new UIImage(skillUIContent);
            }
        }

        if (skillDatas.Count > 0)
        {
            listOptionIndex = 0;
            FocusCurrentListUI(0);
            UpdateSkillDescription(0);
        }
    }
    private void BuildInventoryUI(List<InventoryData> inventoryDatas)
    {
        for (int i = 0; i < inventoryDatas.Count; i++)
        {
            UIImage skillUIImage = new UIImage(skillUIContent, fontAsset,
                inventoryDatas[i].icon, inventoryDatas[i].itemName);
            uIImages.Add(skillUIImage);
        }


        if (inventoryDatas.Count < 4)
        {
            int release = 4 - inventoryDatas.Count;
            for (int i = 0; i < release; i++)
            {
                UIImage skillUIImage = new UIImage(skillUIContent);
            }
        }

        if (inventoryDatas.Count >= 1)
        {
            listOptionIndex = 0;
            FocusCurrentListUI(0);
            UpdateInventoryDescription(0);
        }
    }
    #endregion

    private void UpdateSkillDescription(int index)
    {
        int power = skillDatas[index].damageAmount;
        powerTextUI.text = power.ToString();
        string description = skillDatas[index].description;
        descriptionTextUI.text = description;
    }
    private void UpdateInventoryDescription(int index)
    {
        InventoryData inventoryData = inventoryDatas[index];
        if (inventoryData.consumableType == ConsumableType.Damage)
        {
            int power = inventoryDatas[index].damageAmount;
            powerTextUI.text = power.ToString();
        }
        else if (inventoryData.consumableType == ConsumableType.Health)
        {
            int power = inventoryDatas[index].healthAmount;
            powerTextUI.text = power.ToString();
        }
        string description = inventoryData.description;
        descriptionTextUI.text = description;
    }

    private void RecoveryAllTypeUI()
    {
        for (int i = 0; i < typeUIImages.Count; i++)
        {
            Image backgroundImage = typeUIImages[i].backgroundImage;
            float alpha = 200f / 255f;
            Color color = new Color(0, 0, 0, alpha);
            backgroundImage.color = color;

            Image contentImage = typeUIImages[i].contentImage;
            contentImage.color = Color.white;
        }
    }
    private void FocusCurrentTypeUI(int index)
    {
        RecoveryAllTypeUI();

        if (index < 0 || index >= typeUIImages.Count) { return; }

        Image backgroundImage = typeUIImages[index].backgroundImage;
        StartCoroutine(Utils.UIColorInverseCorroutine(backgroundImage, 0.2f));
        Image contentImage = typeUIImages[index].contentImage;
        StartCoroutine(Utils.UIColorInverseCorroutine(contentImage, 0.2f));
    }

    private void RecoveryAllSkillUI()
    {
        for (int i = 0; i < uIImages.Count; i++)
        {
            RectTransform imageRect = uIImages[i].leftConerImage.rectTransform;
            imageRect.anchoredPosition = new Vector2(-218, 0);
            imageRect.sizeDelta = new Vector2(10, 90);

            Image backgroundImage = uIImages[i].backgroundImage;
            float alpha = 200f / 255f;
            Color color = new Color(0, 0, 0, alpha);
            backgroundImage.color = color;

            Image leftConerImage = uIImages[i].leftConerImage;
            leftConerImage.color = Color.white;

            Image skillImage = uIImages[i].skillImage;
            skillImage.color = Color.white;

            Image lineImage = uIImages[i].lineImage;
            lineImage.color = Color.white;

            Image conditionImage = uIImages[i].conditionImage;
            conditionImage.color = Color.white;

            Image conditionIcon = uIImages[i].conditionIcon;
            conditionIcon.color = Color.white;

            TextMeshProUGUI skillText = uIImages[i].skillText;
            skillText.color = Color.white;

            TextMeshProUGUI spText = uIImages[i].spText;
            spText.color = Color.black;
        }
    }
    private void FocusCurrentListUI(int index)
    {
        RecoveryAllSkillUI();

        if (index < 0 || index >= uIImages.Count) { return; }

        StopAllCoroutines();

        RectTransform imageRect = uIImages[index].leftConerImage.rectTransform;
        StartCoroutine(Utils.UIExtraMoveCoroutine(imageRect, new Vector2(5, 0), 0.2f));
        StartCoroutine(Utils.UIExtraScaleCoroutine(imageRect, new Vector2(10, 0), 0.2f));

        Image backgroundImage = uIImages[index].backgroundImage;
        StartCoroutine(Utils.UIColorInverseCorroutine(backgroundImage, 0.2f));
        Image leftConerImage = uIImages[index].leftConerImage;
        StartCoroutine(Utils.UIColorInverseCorroutine(leftConerImage, 0.2f));
        Image skillImage = uIImages[index].skillImage;
        StartCoroutine(Utils.UIColorInverseCorroutine(skillImage, 0.2f));
        Image lineImage = uIImages[index].lineImage;
        StartCoroutine(Utils.UIColorInverseCorroutine(lineImage, 0.2f));
        Image conditionImage = uIImages[index].conditionImage;
        StartCoroutine(Utils.UIColorInverseCorroutine(conditionImage, 0.2f));
        Image conditionIcon = uIImages[index].conditionIcon;
        StartCoroutine(Utils.UIColorInverseCorroutine(conditionIcon, 0.2f));
        TextMeshProUGUI skillText = uIImages[index].skillText;
        StartCoroutine(Utils.TextColorInverseCorroutine(skillText, 0.2f));
        TextMeshProUGUI spText = uIImages[index].spText;
        StartCoroutine(Utils.TextColorInverseCorroutine(spText, 0.2f));
    }

    public SkillData GetCurrentSelectedSkill()
    {
        if (listOptionIndex < 0) return null;
        if (currentSelectedType == AbilityType.Skill)
        {
            return skillDatas[listOptionIndex];
        }
        if (currentSelectedType == AbilityType.Spell)
        {
            return spellDatas[listOptionIndex];
        }
        return null;
    }
}