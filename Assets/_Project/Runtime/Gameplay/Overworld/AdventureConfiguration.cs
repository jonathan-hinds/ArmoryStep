using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OneStep.Gameplay.Overworld
{
    [CreateAssetMenu(menuName = "OneStep/Gameplay/Adventure Configuration", fileName = "AdventureConfiguration")]
    public sealed class AdventureConfiguration : ScriptableObject
    {
        [SerializeField, Min(5)] private int worldWidth = 9;
        [SerializeField, Min(8)] private int initialGeneratedRows = 32;
        [SerializeField, Min(8)] private int generationLookAhead = 24;
        [SerializeField, Min(10)] private int campfireInterval = 100;
        [SerializeField, Min(0)] private int downwardScreenLimit = 6;
        [SerializeField, Min(1)] private int baseHealth = 24;
        [SerializeField, Min(0)] private int baseMana = 8;
        [SerializeField, Min(1)] private int baseMeleeDamage = 5;
        [SerializeField] private string placeholderClassName = "Wayfarer";
        [SerializeField, Range(0.02f, 0.2f)] private float joystickThreshold = 0.065f;
        [SerializeField, Min(0.05f)] private float joystickInitialRepeatDelay = 0.28f;
        [SerializeField, Min(0.05f)] private float joystickRepeatRate = 0.14f;
        [SerializeField, Min(0.05f)] private float keyboardRepeatRate = 0.14f;
        [SerializeField] private List<EnemyDefinition> enemies = new();

        public int WorldWidth => worldWidth;
        public int InitialGeneratedRows => initialGeneratedRows;
        public int GenerationLookAhead => generationLookAhead;
        public int CampfireInterval => campfireInterval;
        public int DownwardScreenLimit => downwardScreenLimit;
        public int BaseHealth => baseHealth;
        public int BaseMana => baseMana;
        public int BaseMeleeDamage => baseMeleeDamage;
        public string PlaceholderClassName => placeholderClassName;
        public float JoystickThreshold => joystickThreshold;
        public float JoystickInitialRepeatDelay => joystickInitialRepeatDelay;
        public float JoystickRepeatRate => joystickRepeatRate;
        public float KeyboardRepeatRate => keyboardRepeatRate;
        public IReadOnlyList<EnemyDefinition> Enemies => enemies;

        public EnemyDefinition GetEnemy(EnemyKind kind)
        {
            EnsureDefaults();
            return enemies.First(definition => definition.Kind == kind);
        }

        public void EnsureDefaults()
        {
            if (enemies == null)
            {
                enemies = new List<EnemyDefinition>();
            }

            AddIfMissing(new EnemyDefinition(EnemyKind.Slime, "Slime", 8, 2, 5, new Color(0.38f, 0.86f, 0.51f)));
            AddIfMissing(new EnemyDefinition(EnemyKind.Bat, "Bat", 6, 2, 6, new Color(0.72f, 0.47f, 0.92f)));
            AddIfMissing(new EnemyDefinition(EnemyKind.Skeleton, "Skeleton", 12, 4, 8, new Color(0.86f, 0.85f, 0.72f)));
        }

        public void ConfigureDefaults()
        {
            worldWidth = 9;
            initialGeneratedRows = 32;
            generationLookAhead = 24;
            campfireInterval = 100;
            downwardScreenLimit = 6;
            baseHealth = 24;
            baseMana = 8;
            baseMeleeDamage = 5;
            placeholderClassName = "Wayfarer";
            joystickThreshold = 0.065f;
            joystickInitialRepeatDelay = 0.28f;
            joystickRepeatRate = 0.14f;
            keyboardRepeatRate = 0.14f;
            enemies = new List<EnemyDefinition>();
            EnsureDefaults();
        }

        private void OnEnable() => EnsureDefaults();
        private void OnValidate() => EnsureDefaults();

        private void AddIfMissing(EnemyDefinition definition)
        {
            if (enemies.All(existing => existing.Kind != definition.Kind))
            {
                enemies.Add(definition);
            }
        }
    }

    [Serializable]
    public sealed class EnemyDefinition
    {
        [SerializeField] private EnemyKind kind;
        [SerializeField] private string displayName;
        [SerializeField, Min(1)] private int maxHealth;
        [SerializeField, Min(1)] private int damage;
        [SerializeField, Min(0)] private int experienceReward;
        [SerializeField] private Color color = Color.white;

        public EnemyKind Kind => kind;
        public string DisplayName => displayName;
        public int MaxHealth => maxHealth;
        public int Damage => damage;
        public int ExperienceReward => experienceReward;
        public Color Color => color;

        public EnemyDefinition(EnemyKind kind, string displayName, int maxHealth, int damage, int experienceReward, Color color)
        {
            this.kind = kind;
            this.displayName = displayName;
            this.maxHealth = maxHealth;
            this.damage = damage;
            this.experienceReward = experienceReward;
            this.color = color;
        }
    }
}
