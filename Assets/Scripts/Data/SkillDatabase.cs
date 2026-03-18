using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SkillDatabase", menuName = "Tactics/Skill Database")]
public class SkillDatabase : ScriptableObject
{
    public List<SkillData> allSkills = new List<SkillData>();

    public void AddSkill(SkillData skill)
    {
        allSkills.RemoveAll(skill => skill == null);
        if (!allSkills.Contains(skill)) 
            allSkills.Add(skill);
    }

    public void RemoveSkill(SkillData skill)
    {
        allSkills.Remove(skill);
    }
}
