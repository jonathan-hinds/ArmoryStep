using System.Collections.Generic;
using NUnit.Framework;
using OneStep.Gameplay.Overworld;
using UnityEngine;

namespace OneStep.Tests.EditMode
{
    public sealed class AdventureSessionTests
    {
        private AdventureConfiguration _configuration;

        [SetUp]
        public void SetUp()
        {
            _configuration = ScriptableObject.CreateInstance<AdventureConfiguration>();
            _configuration.ConfigureDefaults();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_configuration);

        [Test]
        public void Progress_IncreasesOnlyWhenReachingANewHighestRow()
        {
            var character = CharacterData.Create("Test", _configuration);
            var session = new AdventureSession(_configuration, character, seed: 1234);

            Assert.That(session.TryTakeTurn(Vector2Int.up), Is.EqualTo(PlayerTurnResult.Moved));
            Assert.That(session.Progress, Is.EqualTo(1));

            session.TryTakeTurn(Vector2Int.right);
            session.TryTakeTurn(Vector2Int.zero);
            session.TryTakeTurn(Vector2Int.down);
            session.TryTakeTurn(Vector2Int.up);

            Assert.That(session.Progress, Is.EqualTo(1), "Sideways, wait, backtracking, and re-climbing must not inflate adventure distance.");
        }

        [Test]
        public void MovingTowardEnemy_AttacksWithoutEnteringItsTile_AndAdvancesEnemyTurn()
        {
            var character = CharacterData.Create("Test", _configuration);
            var save = new AdventureSaveData
            {
                seed = 77,
                playerX = 4,
                playerY = 1,
                health = character.maxHealth,
                mana = character.maxMana,
                highestPlayerY = 1,
                generatedThroughY = 32,
                enemies = new List<EnemySaveData>
                {
                    new() { id = 1, kind = EnemyKind.Slime, x = 4, y = 2, health = 8 }
                }
            };
            var session = new AdventureSession(_configuration, character, save);

            var result = session.TryTakeTurn(Vector2Int.up);

            Assert.That(result, Is.EqualTo(PlayerTurnResult.Attacked));
            Assert.That(session.PlayerPosition, Is.EqualTo(new Vector2Int(4, 1)));
            Assert.That(session.Health, Is.EqualTo(character.maxHealth - 2), "The adjacent enemy should receive its turn after the bump attack.");
        }

        [Test]
        public void Campfires_ArePlacedAtEachHundredUpwardRows()
        {
            var world = new AdventureWorld(_configuration, 42);
            world.GenerateThrough(205);

            Assert.That(world.IsBonfire(new Vector2Int(4, 101)), Is.True);
            Assert.That(world.IsBonfire(new Vector2Int(4, 201)), Is.True);
            Assert.That(world.IsBonfire(new Vector2Int(4, 100)), Is.False);
        }

        [Test]
        public void Leveling_UpdatesPermanentCharacterStats()
        {
            var character = CharacterData.Create("Test", _configuration);
            character.experience = character.ExperienceToNextLevel - 1;
            character.meleeDamage = 99;
            var save = new AdventureSaveData
            {
                seed = 9,
                playerX = 4,
                playerY = 1,
                health = character.maxHealth,
                mana = character.maxMana,
                highestPlayerY = 1,
                generatedThroughY = 32,
                enemies = new List<EnemySaveData>
                {
                    new() { id = 1, kind = EnemyKind.Slime, x = 4, y = 2, health = 1 }
                }
            };
            var session = new AdventureSession(_configuration, character, save);

            session.TryTakeTurn(Vector2Int.up);

            Assert.That(character.level, Is.EqualTo(2));
            Assert.That(character.maxHealth, Is.EqualTo(_configuration.BaseHealth + 4));
            Assert.That(character.maxMana, Is.EqualTo(_configuration.BaseMana + 1));
            Assert.That(character.meleeDamage, Is.EqualTo(100));
        }

        [Test]
        public void EmptySlots_RemainEmptyAfterUnityJsonRoundTrip()
        {
            var roster = new CharacterRosterData();
            roster.Normalize();

            var json = JsonUtility.ToJson(roster);
            var restored = JsonUtility.FromJson<CharacterRosterData>(json);
            restored.Normalize();

            Assert.That(restored.slots, Has.Count.EqualTo(CharacterRosterData.SlotCount));
            foreach (var slot in restored.slots)
            {
                Assert.That(slot.occupied, Is.False);
                Assert.That(slot.character, Is.Null);
            }
        }

        [Test]
        public void OnlyExplicitlySavedAdventure_RoundTripsAsResumable()
        {
            var roster = new CharacterRosterData();
            roster.Normalize();
            var character = CharacterData.Create("Scout", _configuration);
            roster.slots[0].occupied = true;
            roster.slots[0].character = character;

            var unsaved = JsonUtility.FromJson<CharacterRosterData>(JsonUtility.ToJson(roster));
            unsaved.Normalize();
            Assert.That(unsaved.slots[0].character.HasSavedAdventure, Is.False);

            character.hasActiveAdventure = true;
            character.activeAdventure = new AdventureSaveData { seed = 123, health = 10, mana = 4, progress = 100 };
            var saved = JsonUtility.FromJson<CharacterRosterData>(JsonUtility.ToJson(roster));
            saved.Normalize();
            Assert.That(saved.slots[0].character.HasSavedAdventure, Is.True);
            Assert.That(saved.slots[0].character.activeAdventure.progress, Is.EqualTo(100));
        }

        [TestCase(2, -90f, 0f, 5, 3)]
        [TestCase(2, 90f, 0f, 5, 1)]
        [TestCase(2, -20f, -500f, 5, 3)]
        [TestCase(2, 20f, 500f, 5, 1)]
        [TestCase(0, 100f, 0f, 5, 0)]
        [TestCase(4, -100f, 0f, 5, 4)]
        public void CharacterCarousel_ChoosesOneBoundedSlotPerGesture(
            int startIndex,
            float dragDelta,
            float velocity,
            int slotCount,
            int expectedIndex)
        {
            var target = CharacterCarouselMath.CalculateTargetIndex(
                startIndex,
                dragDelta,
                velocity,
                slotCount,
                dragThreshold: 72f,
                velocityThreshold: 420f);

            Assert.That(target, Is.EqualTo(expectedIndex));
        }
    }
}
