using System;
using System.Collections.Generic;
using UnityEngine;

namespace OneStep.Gameplay.Overworld
{
    public sealed class AdventureWorld
    {
        private readonly AdventureConfiguration _configuration;
        private readonly int _seed;
        private readonly HashSet<Vector2Int> _obstacles = new();
        private readonly HashSet<Vector2Int> _details = new();
        private readonly Dictionary<Vector2Int, EnemySaveData> _enemiesByPosition = new();
        private readonly Dictionary<int, EnemySaveData> _enemiesById = new();

        public AdventureWorld(AdventureConfiguration configuration, int seed)
        {
            _configuration = configuration;
            _seed = seed;
        }

        public int GeneratedThroughY { get; private set; } = -1;
        public IReadOnlyCollection<Vector2Int> Obstacles => _obstacles;
        public IReadOnlyCollection<Vector2Int> Details => _details;
        public IReadOnlyCollection<EnemySaveData> Enemies => _enemiesById.Values;

        public void GenerateThrough(int targetY, bool spawnEnemies = true)
        {
            targetY = Mathf.Max(targetY, GeneratedThroughY);
            for (var y = GeneratedThroughY + 1; y <= targetY; y++)
            {
                GenerateRow(y, spawnEnemies);
            }

            GeneratedThroughY = targetY;
        }

        public bool IsInside(Vector2Int position) =>
            position.x >= 0 && position.x < _configuration.WorldWidth && position.y >= 0 && position.y <= GeneratedThroughY;

        public bool IsObstacle(Vector2Int position) => _obstacles.Contains(position);
        public bool HasDetail(Vector2Int position) => _details.Contains(position);

        public bool IsBonfire(Vector2Int position)
        {
            return position.x == _configuration.WorldWidth / 2 &&
                   position.y > 1 &&
                   (position.y - 1) % _configuration.CampfireInterval == 0;
        }

        public bool TryGetEnemy(Vector2Int position, out EnemySaveData enemy) => _enemiesByPosition.TryGetValue(position, out enemy);

        public void ReplaceEnemies(IEnumerable<EnemySaveData> enemies)
        {
            _enemiesById.Clear();
            _enemiesByPosition.Clear();
            if (enemies == null)
            {
                return;
            }

            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.health <= 0)
                {
                    continue;
                }

                AddEnemy(enemy.Clone());
            }
        }

        public bool TryMoveEnemy(EnemySaveData enemy, Vector2Int destination, bool ignoreObstacles)
        {
            if (!IsInside(destination) || (!ignoreObstacles && IsObstacle(destination)) || IsBonfire(destination) ||
                _enemiesByPosition.ContainsKey(destination))
            {
                return false;
            }

            _enemiesByPosition.Remove(new Vector2Int(enemy.x, enemy.y));
            enemy.x = destination.x;
            enemy.y = destination.y;
            _enemiesByPosition[destination] = enemy;
            return true;
        }

        public void RemoveEnemy(EnemySaveData enemy)
        {
            _enemiesById.Remove(enemy.id);
            _enemiesByPosition.Remove(new Vector2Int(enemy.x, enemy.y));
        }

        private void GenerateRow(int y, bool spawnEnemy)
        {
            var center = _configuration.WorldWidth / 2;
            var isCampfireRow = y > 1 && (y - 1) % _configuration.CampfireInterval == 0;
            for (var x = 0; x < _configuration.WorldWidth; x++)
            {
                var position = new Vector2Int(x, y);
                if (Hash(x, y, 17) % 13 == 0)
                {
                    _details.Add(position);
                }

                if (y < 4 || isCampfireRow || x == center)
                {
                    continue;
                }

                if (Hash(x, y, 31) % 17 == 0)
                {
                    _obstacles.Add(position);
                }
            }

            if (!spawnEnemy || y < 5 || isCampfireRow || y % 5 != 0)
            {
                return;
            }

            var xPosition = Hash(0, y, 47) % _configuration.WorldWidth;
            var candidate = new Vector2Int(xPosition, y);
            for (var attempt = 0; attempt < _configuration.WorldWidth && (IsObstacle(candidate) || candidate.x == center); attempt++)
            {
                candidate.x = (candidate.x + 1) % _configuration.WorldWidth;
            }

            if (IsObstacle(candidate) || candidate.x == center)
            {
                return;
            }

            var kind = (EnemyKind)((y / 5) % 3);
            var definition = _configuration.GetEnemy(kind);
            AddEnemy(new EnemySaveData
            {
                id = y * 32 + candidate.x,
                kind = kind,
                x = candidate.x,
                y = candidate.y,
                health = definition.MaxHealth
            });
        }

        private void AddEnemy(EnemySaveData enemy)
        {
            var position = new Vector2Int(enemy.x, enemy.y);
            if (_enemiesById.ContainsKey(enemy.id) || _enemiesByPosition.ContainsKey(position))
            {
                return;
            }

            _enemiesById.Add(enemy.id, enemy);
            _enemiesByPosition.Add(position, enemy);
        }

        private int Hash(int x, int y, int salt)
        {
            unchecked
            {
                var value = _seed;
                value = (value * 397) ^ x;
                value = (value * 397) ^ y;
                value = (value * 397) ^ salt;
                value ^= value >> 16;
                return value == int.MinValue ? int.MaxValue : Math.Abs(value);
            }
        }
    }
}
