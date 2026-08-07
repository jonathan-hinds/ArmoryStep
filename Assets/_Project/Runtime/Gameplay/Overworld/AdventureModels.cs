using System;
using System.Collections.Generic;

namespace OneStep.Gameplay.Overworld
{
    public enum EnemyKind
    {
        Slime,
        Bat,
        Skeleton
    }

    [Serializable]
    public sealed class CharacterRosterData
    {
        public const int SlotCount = 5;

        public int version = 1;
        public List<CharacterSlotData> slots = new();

        public void Normalize()
        {
            slots ??= new List<CharacterSlotData>();
            while (slots.Count < SlotCount)
            {
                slots.Add(new CharacterSlotData());
            }

            if (slots.Count > SlotCount)
            {
                slots.RemoveRange(SlotCount, slots.Count - SlotCount);
            }

            for (var index = 0; index < slots.Count; index++)
            {
                slots[index] ??= new CharacterSlotData();
                slots[index].Normalize();
            }
        }
    }

    [Serializable]
    public sealed class CharacterSlotData
    {
        public bool occupied;
        public CharacterData character;

        public void Normalize()
        {
            if (!occupied || character == null)
            {
                occupied = false;
                character = null;
                return;
            }

            character.Normalize();
        }
    }

    [Serializable]
    public sealed class CharacterData
    {
        public string id;
        public string displayName;
        public string classId = "Wayfarer";
        public int level = 1;
        public int experience;
        public int maxHealth = 24;
        public int maxMana = 8;
        public int meleeDamage = 5;
        public int adventuresStarted;
        public int bestProgress;
        public bool hasActiveAdventure;
        public AdventureSaveData activeAdventure;

        public bool HasSavedAdventure => hasActiveAdventure && activeAdventure != null;
        public int ExperienceToNextLevel => ExperienceRequiredForLevel(level);

        public static CharacterData Create(string name, AdventureConfiguration configuration)
        {
            return new CharacterData
            {
                id = Guid.NewGuid().ToString("N"),
                displayName = string.IsNullOrWhiteSpace(name) ? "Wayfarer" : name.Trim(),
                classId = configuration.PlaceholderClassName,
                level = 1,
                experience = 0,
                maxHealth = configuration.BaseHealth,
                maxMana = configuration.BaseMana,
                meleeDamage = configuration.BaseMeleeDamage
            };
        }

        public void Normalize()
        {
            id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id;
            displayName = string.IsNullOrWhiteSpace(displayName) ? "Wayfarer" : displayName;
            classId = string.IsNullOrWhiteSpace(classId) ? "Wayfarer" : classId;
            level = Math.Max(1, level);
            maxHealth = Math.Max(1, maxHealth);
            maxMana = Math.Max(0, maxMana);
            meleeDamage = Math.Max(1, meleeDamage);
            if (!hasActiveAdventure || activeAdventure == null)
            {
                hasActiveAdventure = false;
                activeAdventure = null;
            }
        }

        public static int ExperienceRequiredForLevel(int currentLevel) => 12 + Math.Max(1, currentLevel) * 8;
    }

    [Serializable]
    public sealed class AdventureSaveData
    {
        public int seed;
        public int playerX;
        public int playerY;
        public int health;
        public int mana;
        public int progress;
        public int highestPlayerY;
        public int generatedThroughY;
        public int turnNumber;
        public List<EnemySaveData> enemies = new();
    }

    [Serializable]
    public sealed class EnemySaveData
    {
        public int id;
        public EnemyKind kind;
        public int x;
        public int y;
        public int health;

        public EnemySaveData Clone()
        {
            return new EnemySaveData { id = id, kind = kind, x = x, y = y, health = health };
        }
    }
}
