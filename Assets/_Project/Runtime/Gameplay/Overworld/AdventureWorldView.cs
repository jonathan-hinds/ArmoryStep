using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace OneStep.Gameplay.Overworld
{
    public sealed class AdventureWorldView : MonoBehaviour
    {
        private readonly Dictionary<EnemyKind, TileBase> _enemyTiles = new();
        private AdventureSession _session;
        private AdventureConfiguration _configuration;
        private Tilemap _ground;
        private Tilemap _detail;
        private Tilemap _obstacle;
        private Tilemap _interactable;
        private Tilemap _character;
        private Tilemap _effect;
        private TileBase _groundA;
        private TileBase _groundB;
        private TileBase _detailTile;
        private TileBase _obstacleTile;
        private TileBase _bonfireTile;
        private TileBase _playerTile;
        private int _renderedThroughY = -1;

        public void Configure(AdventureSession session, AdventureConfiguration configuration)
        {
            _session = session;
            _configuration = configuration;
            CreateLayers();
            CreateTiles();
            Synchronize();
        }

        public void Synchronize()
        {
            if (_session == null)
            {
                return;
            }

            RenderNewRows();
            _character.ClearAllTiles();
            _character.SetTile(ToCell(_session.PlayerPosition), _playerTile);
            foreach (var enemy in _session.World.Enemies)
            {
                _character.SetTile(new Vector3Int(enemy.x, enemy.y, 0), _enemyTiles[enemy.kind]);
            }
        }

        private void RenderNewRows()
        {
            for (var y = _renderedThroughY + 1; y <= _session.World.GeneratedThroughY; y++)
            {
                for (var x = 0; x < _configuration.WorldWidth; x++)
                {
                    var position = new Vector2Int(x, y);
                    var cell = ToCell(position);
                    _ground.SetTile(cell, (x + y) % 2 == 0 ? _groundA : _groundB);
                    if (_session.World.HasDetail(position))
                    {
                        _detail.SetTile(cell, _detailTile);
                    }
                    if (_session.World.IsObstacle(position))
                    {
                        _obstacle.SetTile(cell, _obstacleTile);
                    }
                    if (_session.World.IsBonfire(position))
                    {
                        _interactable.SetTile(cell, _bonfireTile);
                    }
                }
            }

            _renderedThroughY = _session.World.GeneratedThroughY;
        }

        private void CreateLayers()
        {
            if (GetComponent<Grid>() == null)
            {
                gameObject.AddComponent<Grid>();
            }

            _ground = CreateLayer("Ground", -30);
            _detail = CreateLayer("Detail", -20);
            _obstacle = CreateLayer("Obstacle", -10);
            _interactable = CreateLayer("Interactable", 0);
            _character = CreateLayer("Character", 10);
            _effect = CreateLayer("Effect", 20);
            _effect.gameObject.SetActive(true);
        }

        private Tilemap CreateLayer(string layerName, int sortingOrder)
        {
            var layer = new GameObject(layerName, typeof(Tilemap), typeof(TilemapRenderer));
            layer.transform.SetParent(transform, false);
            var renderer = layer.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = sortingOrder;
            return layer.GetComponent<Tilemap>();
        }

        private void CreateTiles()
        {
            _groundA = CreateTile(new Color32(20, 30, 34, 255), PixelShape.Full);
            _groundB = CreateTile(new Color32(24, 35, 39, 255), PixelShape.Full);
            _detailTile = CreateTile(new Color32(48, 73, 67, 255), PixelShape.Specks);
            _obstacleTile = CreateTile(new Color32(73, 93, 88, 255), PixelShape.Wall);
            _bonfireTile = CreateTile(new Color32(255, 151, 56, 255), PixelShape.Fire);
            _playerTile = CreateTile(new Color32(227, 244, 214, 255), PixelShape.Hero);
            _enemyTiles[EnemyKind.Slime] = CreateTile((Color32)_configuration.GetEnemy(EnemyKind.Slime).Color, PixelShape.Slime);
            _enemyTiles[EnemyKind.Bat] = CreateTile((Color32)_configuration.GetEnemy(EnemyKind.Bat).Color, PixelShape.Bat);
            _enemyTiles[EnemyKind.Skeleton] = CreateTile((Color32)_configuration.GetEnemy(EnemyKind.Skeleton).Color, PixelShape.Skeleton);
        }

        private static TileBase CreateTile(Color32 color, PixelShape shape)
        {
            const int size = 16;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = $"Runtime_{shape}",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };

            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    pixels[y * size + x] = IsFilled(shape, x, y) ? color : new Color32(0, 0, 0, 0);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = shape.ToString();
            sprite.hideFlags = HideFlags.DontSave;
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = shape.ToString();
            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            tile.hideFlags = HideFlags.DontSave;
            return tile;
        }

        private static bool IsFilled(PixelShape shape, int x, int y)
        {
            return shape switch
            {
                PixelShape.Full => true,
                PixelShape.Specks => (x == 3 && y is >= 2 and <= 5) || (x == 11 && y is >= 9 and <= 12) || (y == 4 && x is >= 2 and <= 5),
                PixelShape.Wall => x is >= 1 and <= 14 && y is >= 2 and <= 13 && (x is 1 or 14 || y is 2 or 13 || (y == 8 && x % 2 == 0)),
                PixelShape.Fire => (y is >= 2 and <= 4 && x is >= 3 and <= 12) || (y is >= 5 and <= 12 && Mathf.Abs(x - 8) <= (12 - y) / 2 + 1),
                PixelShape.Hero => (y is >= 3 and <= 8 && x is >= 4 and <= 11) || (y is >= 9 and <= 13 && x is >= 5 and <= 10) || (y == 11 && (x == 6 || x == 9)),
                PixelShape.Slime => y is >= 3 and <= 10 && x is >= 2 and <= 13 && (y <= 8 || x is >= 4 and <= 11),
                PixelShape.Bat => (y is >= 6 and <= 10 && x is >= 6 and <= 9) || (y is >= 7 and <= 12 && (x is >= 1 and <= 5 || x is >= 10 and <= 14)),
                PixelShape.Skeleton => (y is >= 8 and <= 13 && x is >= 4 and <= 11) || (y is >= 3 and <= 8 && x is >= 6 and <= 9) || (y == 11 && (x == 6 || x == 9)),
                _ => false
            };
        }

        private static Vector3Int ToCell(Vector2Int position) => new(position.x, position.y, 0);

        private enum PixelShape
        {
            Full,
            Specks,
            Wall,
            Fire,
            Hero,
            Slime,
            Bat,
            Skeleton
        }
    }
}
