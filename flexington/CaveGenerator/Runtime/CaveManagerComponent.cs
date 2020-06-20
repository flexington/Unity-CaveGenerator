using UnityEngine;

namespace flexington.CaveGenerator
{
    public class CaveManagerComponent : MonoBehaviour
    {
        [SerializeField] private Vector2Int _mapSize;

        [SerializeField] private string _seed;

        [SerializeField] private CaveObject[] _cavePresets;


        private Cave[,] _caves;
        private System.Random _rng;

        private void Start()
        {
            GenerateCaves();
        }

        public void GenerateCaves()
        {
            string seed;
            if (string.IsNullOrEmpty(_seed)) seed = Time.time.ToString();
            else seed = _seed;
            _rng = new System.Random(seed.GetHashCode());

            _caves = new Cave[_mapSize.x, _mapSize.y];
            Cave lastCave = null;
            for (int y = 0; y < _mapSize.y; y++)
            {
                for (int x = 0; x < _mapSize.x; x++)
                {
                    int[] exitTop = new int[0];
                    int[] exitRight = new int[0];
                    int[] exitBottom = new int[0];
                    int[] exitLeft = new int[0];

                    // Top
                    if (y == _mapSize.y - 1) exitTop = null;
                    // Bottom 
                    if (y == 0) exitBottom = null;
                    // Right
                    if (x == _mapSize.x - 1) exitRight = null;
                    // Left
                    if (x == 0) exitLeft = null;

                    if (x - 1 >= 0 && _caves[x - 1, y] != null) exitLeft = _caves[x - 1, y].Exits.Right;
                    if (x + 1 < _mapSize.x && _caves[x + 1, y] != null) exitRight = _caves[x + 1, y].Exits.Left;
                    if (y - 1 >= 0 && _caves[x, y - 1] != null) exitBottom = _caves[x, y - 1].Exits.Top;
                    if (y + 1 < _mapSize.y && _caves[x, y + 1] != null) exitTop = _caves[x, y + 1].Exits.Bottom;

                    Exit exits = new Exit(exitTop, exitRight, exitBottom, exitLeft);

                    int caveIndex = _rng.Next(0, _cavePresets.Length - 1);
                    CaveObject caveObject = _cavePresets[caveIndex];

                    seed = _rng.Next().ToString();
                    Cave cave = caveObject.Generate(seed, exits);
                    _caves[x, y] = cave;
                    lastCave = cave;
                }
            }
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                GenerateCaves();
            }
        }

        private void OnDrawGizmos()
        {
            if (_caves == null || _caves.Length < 1) return;

            int offsetX = 0;
            int offsetY = 0;

            for (int mx = 0; mx < _mapSize.x; mx++)
            {
                for (int my = 0; my < _mapSize.y; my++)
                {
                    Cave cave = _caves[mx, my];

                    for (int x = 0; x < cave.Size.x; x++)
                    {
                        for (int y = 0; y < cave.Size.y; y++)
                        {
                            if (cave.Map[x, y] == 0) Gizmos.color = Color.white;
                            else Gizmos.color = Color.black;
                            Gizmos.DrawCube(new Vector3(x + offsetX, y + offsetY, 0), Vector3.one);
                        }
                    }
                    offsetY += 50;
                }
                offsetX += 50;
                offsetY = 0;
            }
        }
    }
}
