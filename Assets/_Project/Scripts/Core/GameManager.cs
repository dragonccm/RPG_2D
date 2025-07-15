using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Skill Management")]
        [SerializeField] private ModularSkillManager skillManager;
        [SerializeField] private List<SkillModule> defaultSkills = new List<SkillModule>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                EventBus.Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeSkills();
        }

        private void InitializeSkills()
        {
            if (skillManager == null)
            {
                Debug.LogError("SkillManager is not assigned in GameManager.");
                return;
            }

            // Add default skills to the skill manager
            foreach (var skill in defaultSkills)
            {
                skillManager.AddAvailableSkill(skill);
            }

            // Auto-equip default skills into unlocked slots
            AutoEquipDefaultSkills();
        }

        private void AutoEquipDefaultSkills()
        {
            var unlockedSlots = skillManager.GetUnlockedSlots();
            int equipped = 0;

            foreach (var slot in unlockedSlots)
            {
                if (equipped < defaultSkills.Count && !slot.HasSkill())
                {
                    if (skillManager.EquipSkill(slot.slotIndex, defaultSkills[equipped]))
                    {
                        equipped++;
                        Debug.Log($"Auto-equipped {defaultSkills[equipped - 1].skillName} to slot {slot.slotIndex + 1}");
                    }
                }
            }

            Debug.Log($"Auto-equipped {equipped} skills to player");
        }

        public void UnlockSkillSlot(int playerLevel)
        {
            skillManager.UpdateUnlockedSlots();
            Debug.Log($"Skill slots updated for player level {playerLevel}");
        }

        public void EquipSkill(int slotIndex, SkillModule skill)
        {
            if (skillManager.EquipSkill(slotIndex, skill))
            {
                Debug.Log($"Equipped {skill.skillName} to slot {slotIndex}");
            }
            else
            {
                Debug.LogError($"Failed to equip {skill.skillName} to slot {slotIndex}");
            }
        }

        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
