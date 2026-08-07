using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OneStep.Gameplay.Overworld
{
    public enum PlayerTurnResult
    {
        Rejected,
        Waited,
        Moved,
        Attacked
    }

    public sealed class AdventureSession
    {
        private readonly AdventureConfiguration _configuration;
        private readonly CharacterData _character;

        public AdventureSession(AdventureConfiguration configuration, CharacterData character, AdventureSaveData savedAdventure = null, int? seed = null)
        {
            _configuration = configuration;
            _configuration.EnsureDefaults();
            _character = character;

            if (savedAdventure == null)
            {
                var runSeed = seed ?? UnityEngine.Random.Range(1, int.MaxValue);
                World = new AdventureWorld(configuration, runSeed);
                PlayerPosition = new Vector2Int(configuration.WorldWidth / 2, 1);
                Health = character.maxHealth;
                Mana = character.maxMana;
                HighestPlayerY = PlayerPosition.y;
                World.GenerateThrough(configuration.InitialGeneratedRows, true);
                Seed = runSeed;
            }
            else
            {
                Seed = savedAdventure.seed;
                World = new AdventureWorld(configuration, Seed);
                World.GenerateThrough(Mathf.Max(savedAdventure.generatedThroughY, savedAdventure.playerY + configuration.GenerationLookAhead), false);
                World.ReplaceEnemies(savedAdventure.enemies);
                PlayerPosition = new Vector2Int(savedAdventure.playerX, savedAdventure.playerY);
                Health = Mathf.Clamp(savedAdventure.health, 1, character.maxHealth);
                Mana = Mathf.Clamp(savedAdventure.mana, 0, character.maxMana);
                Progress = Mathf.Max(0, savedAdventure.progress);
                HighestPlayerY = Mathf.Max(PlayerPosition.y, savedAdventure.highestPlayerY);
                TurnNumber = Mathf.Max(0, savedAdventure.turnNumber);
            }
        }

        public event Action Changed;
        public event Action BonfireEntered;
        public event Action Died;
        public event Action<int> LeveledUp;
        public event Action<string> MessageRaised;

        public AdventureWorld World { get; }
        public int Seed { get; }
        public Vector2Int PlayerPosition { get; private set; }
        public int Health { get; private set; }
        public int Mana { get; private set; }
        public int Progress { get; private set; }
        public int HighestPlayerY { get; private set; }
        public int TurnNumber { get; private set; }
        public bool IsDead { get; private set; }
        public CharacterData Character => _character;

        public PlayerTurnResult TryTakeTurn(Vector2Int direction)
        {
            if (IsDead || Mathf.Abs(direction.x) + Mathf.Abs(direction.y) > 1)
            {
                return PlayerTurnResult.Rejected;
            }

            PlayerTurnResult result;
            if (direction == Vector2Int.zero)
            {
                result = PlayerTurnResult.Waited;
            }
            else
            {
                var destination = PlayerPosition + direction;
                if (!CanPlayerEnter(destination))
                {
                    return PlayerTurnResult.Rejected;
                }

                if (World.TryGetEnemy(destination, out var enemy))
                {
                    AttackEnemy(enemy);
                    result = PlayerTurnResult.Attacked;
                }
                else
                {
                    PlayerPosition = destination;
                    if (direction.y > 0 && PlayerPosition.y > HighestPlayerY)
                    {
                        HighestPlayerY = PlayerPosition.y;
                        Progress++;
                        _character.bestProgress = Mathf.Max(_character.bestProgress, Progress);
                    }

                    result = PlayerTurnResult.Moved;
                }
            }

            TurnNumber++;
            EnsureWorldAhead();
            TakeEnemyTurns();
            Changed?.Invoke();

            if (!IsDead && result == PlayerTurnResult.Moved && World.IsBonfire(PlayerPosition))
            {
                BonfireEntered?.Invoke();
            }

            return result;
        }

        public void Rest()
        {
            if (IsDead || !World.IsBonfire(PlayerPosition))
            {
                return;
            }

            Health = _character.maxHealth;
            Mana = _character.maxMana;
            MessageRaised?.Invoke("Restored health and mana. This is not a checkpoint.");
            Changed?.Invoke();
        }

        public AdventureSaveData CreateSave()
        {
            return new AdventureSaveData
            {
                seed = Seed,
                playerX = PlayerPosition.x,
                playerY = PlayerPosition.y,
                health = Health,
                mana = Mana,
                progress = Progress,
                highestPlayerY = HighestPlayerY,
                generatedThroughY = World.GeneratedThroughY,
                turnNumber = TurnNumber,
                enemies = World.Enemies.Select(enemy => enemy.Clone()).ToList()
            };
        }

        private bool CanPlayerEnter(Vector2Int destination)
        {
            var minimumY = Mathf.Max(0, HighestPlayerY - _configuration.DownwardScreenLimit);
            if (destination.y < minimumY)
            {
                MessageRaised?.Invoke("The path below has passed beyond the screen.");
                return false;
            }

            return World.IsInside(destination) && !World.IsObstacle(destination);
        }

        private void EnsureWorldAhead()
        {
            var requiredY = PlayerPosition.y + _configuration.GenerationLookAhead;
            if (requiredY > World.GeneratedThroughY)
            {
                World.GenerateThrough(requiredY, true);
            }
        }

        private void AttackEnemy(EnemySaveData enemy)
        {
            enemy.health -= _character.meleeDamage;
            MessageRaised?.Invoke($"You hit the {_configuration.GetEnemy(enemy.kind).DisplayName} for {_character.meleeDamage}.");
            if (enemy.health > 0)
            {
                return;
            }

            var definition = _configuration.GetEnemy(enemy.kind);
            World.RemoveEnemy(enemy);
            GainExperience(definition.ExperienceReward);
            MessageRaised?.Invoke($"{definition.DisplayName} defeated. +{definition.ExperienceReward} XP");
        }

        private void TakeEnemyTurns()
        {
            var actors = World.Enemies.OrderBy(enemy => enemy.id).ToArray();
            foreach (var enemy in actors)
            {
                if (IsDead || enemy.health <= 0)
                {
                    break;
                }

                TakeEnemyTurn(enemy);
            }
        }

        private void TakeEnemyTurn(EnemySaveData enemy)
        {
            var enemyPosition = new Vector2Int(enemy.x, enemy.y);
            var delta = PlayerPosition - enemyPosition;
            var distance = Mathf.Abs(delta.x) + Mathf.Abs(delta.y);
            var definition = _configuration.GetEnemy(enemy.kind);

            if (enemy.kind == EnemyKind.Skeleton && TurnNumber % 2 == 1)
            {
                return;
            }

            var awareness = enemy.kind switch
            {
                EnemyKind.Slime => 7,
                EnemyKind.Bat => 9,
                EnemyKind.Skeleton => 6,
                _ => 6
            };

            if (distance == 1)
            {
                DamagePlayer(definition.Damage, definition.DisplayName);
                return;
            }

            if (distance > awareness)
            {
                return;
            }

            var horizontalFirst = enemy.kind == EnemyKind.Bat || Mathf.Abs(delta.x) > Mathf.Abs(delta.y);
            var primary = horizontalFirst ? HorizontalStep(delta) : VerticalStep(delta);
            var secondary = horizontalFirst ? VerticalStep(delta) : HorizontalStep(delta);
            var ignoreObstacles = enemy.kind == EnemyKind.Bat;

            if (primary != Vector2Int.zero && PlayerPosition != enemyPosition + primary &&
                World.TryMoveEnemy(enemy, enemyPosition + primary, ignoreObstacles))
            {
                return;
            }

            if (secondary != Vector2Int.zero && PlayerPosition != enemyPosition + secondary)
            {
                World.TryMoveEnemy(enemy, enemyPosition + secondary, ignoreObstacles);
            }
        }

        private void DamagePlayer(int damage, string attacker)
        {
            Health = Mathf.Max(0, Health - damage);
            MessageRaised?.Invoke($"{attacker} hits for {damage}.");
            if (Health > 0)
            {
                return;
            }

            IsDead = true;
            Died?.Invoke();
        }

        private void GainExperience(int amount)
        {
            _character.experience += Mathf.Max(0, amount);
            while (_character.experience >= _character.ExperienceToNextLevel)
            {
                _character.experience -= _character.ExperienceToNextLevel;
                _character.level++;
                _character.maxHealth += 4;
                _character.maxMana += 1;
                _character.meleeDamage += 1;
                Health += 4;
                Mana += 1;
                LeveledUp?.Invoke(_character.level);
            }
        }

        private static Vector2Int HorizontalStep(Vector2Int delta) => new(Math.Sign(delta.x), 0);
        private static Vector2Int VerticalStep(Vector2Int delta) => new(0, Math.Sign(delta.y));
    }
}
