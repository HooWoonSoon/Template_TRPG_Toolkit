using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleUIManager : MonoBehaviour
{
    public GameObject battleStatePanel;

    [Header("Battle Event Display UI")]
    public GameObject battleStartDisplayUI;
    public GameObject battleEndDisplayUI;

    [Header("Battle Set Skill UI")]
    public GameObject skillUI;
    public GameObject cTTimelineUI;

    public static BattleUIManager instance { get; private set; }
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        battleStatePanel.SetActive(false);
        battleStartDisplayUI.SetActive(false);
        skillUI.SetActive(false);
        cTTimelineUI.SetActive(false);
    }

    public void PrepareBattleUI()
    {
        GameEvent.onBattleUIStart?.Invoke();
        battleStatePanel.SetActive(true);
        battleStartDisplayUI.SetActive(true);
        cTTimelineUI.SetActive(true);
        StartCoroutine(PrepareBattleSequence());
    }
    private IEnumerator PrepareBattleSequence()
    {
        yield return new WaitForSeconds(2f);
        GameEvent.onBattleUIFinish?.Invoke();
        battleStartDisplayUI.SetActive(false);
    }

    public void CompleteBattleUI()
    {
        battleEndDisplayUI.SetActive(true);
        StartCoroutine(CompleteBattleSequence());
    }
    private IEnumerator CompleteBattleSequence()
    {
        yield return new WaitForSeconds(2f);
        battleEndDisplayUI.SetActive(false);

        yield return new WaitForSeconds(2f);
        battleStatePanel.SetActive(false);
        cTTimelineUI.SetActive(false);
        BattleManager.instance.EndBattle();
    }

    public void OpenUpdateSkillUI(CharacterBase character)
    {
        if (skillUI.activeSelf == true) { return; } 
        skillUI.SetActive(true);
        List<SkillData> characterSkillList = character.skillDatas;
        TeamDeployment teamDeployment = character.currentTeam;
        List<InventoryData> invetoryList = teamDeployment.inventoryDatas;
        if (characterSkillList != null)
        {
            SkillUIManager.instance.Initialize(characterSkillList, invetoryList, character);
        }
    }
    public void CloseSkillUI()
    {
        SkillUIManager.instance.ResetAll();
        skillUI.SetActive(false);
    }

    public void ActiveAllCharacterInfoTip(bool active)
    {
        foreach (var character in BattleManager.instance.GetBattleUnits())
        {
            SelfCanvasController selfCanvasController = character.selfCanvasController;
            if (selfCanvasController == null)
            {
                Debug.LogWarning($"{character} missing Self Canvas Controller");
                continue;
            }

            if (active)
            {
                selfCanvasController.ActiveAll(true);
                int queue = CTTimeline.instance.GetCharacterCurrentQueue(character);
                selfCanvasController.SetQueue(queue);
                float healthPercentage = character.GetCurrentHealthPercentage();
                selfCanvasController.SetHeathPercetange(healthPercentage);
            }
            else
            {
                selfCanvasController.ActiveAll(false);
            }
        }
    }

    public void SwitchActionPanel() => BattleManager.instance.gridCursor.SwitchActionPanel();
    public void SwitchInfoPanel() => BattleManager.instance.gridCursor.SwitchInfoPanel();
    public void OffCursorPanel() => BattleManager.instance.gridCursor.OffPanel();
}