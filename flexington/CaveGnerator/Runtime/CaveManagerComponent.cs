using System.Collections.Generic;
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
            _rng = new System.Random(_seed.GetHashCode());

            _caves = new Cave[_mapSize.x, _mapSize.y];

            for (int y = 0; y < _mapSize.y; y++)
            {
                for (int x = 0; x < _mapSize.x; x++)
                {
                    CaveObject caveObject = _cavePresets[_rng.Next(0, _cavePresets.Length - 1)];
                    Cave cave = caveObject.Generate(_rng.Next().ToString());
                    _caves[x, y] = cave;
                }
            }
        }

        private void Update()
        {
            // if (Input.GetMouseButtonDown(0))
            // {
            //     _map = _cavePresets[0].Generate();
            // }
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
                    for (int y = 0; y < cave.Size.y; y++)
                    {
                        for (int x = 0; x < cave.Size.x; x++)
                        {
                            if (cave.Map[x, y] == 0) Gizmos.color = Color.white;
                            else Gizmos.color = Color.black;
                            Gizmos.DrawCube(new Vector3(x + offsetX, y + offsetY, 0), Vector3.one);
                        }
                    }
                    offsetX += 50;
                }
                offsetY += 50;
                offsetX = 0;
            }
        }
    }
}
