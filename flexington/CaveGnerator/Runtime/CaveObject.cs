using System;
using System.Collections.Generic;
using UnityEngine;

namespace flexington.CaveGenerator
{
    [CreateAssetMenu(fileName = "Cave", menuName = "flexington/CaveGenerator/New Cave", order = 0)]
    public class CaveObject : ScriptableObject
    {
        [SerializeField] private Vector2Int _size;
        public Vector2Int Size
        {
            get { return _size; }
        }

        [SerializeField] private string _seed;
        public string Seed
        {
            get { return _seed; }
            set { _seed = value; }
        }

        [SerializeField, Range(0, 100)] private int _fillThreshold;
        public int FillThreshold
        {
            get { return _fillThreshold; }
        }

        [Range(1, 15)]
        [SerializeField] private int _smoothingIterations;
        public int SmoothingIterations
        {
            get { return _smoothingIterations; }
            set { _smoothingIterations = value; }
        }

        [SerializeField, Range(1, 8)] private int _smoothingThreshold;
        public int SmoothingThreshold
        {
            get { return _smoothingThreshold; }
            set { _smoothingThreshold = value; }
        }

        [SerializeField, Range(0, 50)] private int _regionThreshold;
        public int RegionThreshold
        {
            get { return _regionThreshold; }
            set { _regionThreshold = value; }
        }

        [SerializeField] private int _pathRadius;
        public int PathRadius
        {
            get { return _pathRadius; }
            set { _pathRadius = value; }
        }

        private int[,] _map;
        private bool[,] _regionFlags;
        System.Random _rng;

        public Cave Generate(string seed = null)
        {
            _map = new int[Size.x, Size.y];
            _regionFlags = new bool[Size.x, Size.y];

            if (string.IsNullOrEmpty(seed))
            {
                if (string.IsNullOrEmpty(_seed)) seed = Time.time.ToString();
                else seed = _seed;
            }
            _rng = new System.Random(seed.GetHashCode());

            FillMap();
            Smooth();

            FilterRegions(1, 0);
            Vector2Int[][] regions = FilterRegions(0, 1);

            ConnectRegions(regions);

            FilterRegions(1, 0);

            for (int i = 1; i < _smoothingIterations; i++) Smooth();

            return new Cave(_map, _size);
        }

        private void FillMap()
        {
            for (int y = 0; y < Size.y; y++)
            {
                for (int x = 0; x < Size.x; x++)
                {
                    if (isBorder(x, y)) _map[x, y] = 1;
                    else _map[x, y] = (_rng.Next(0, 100) < FillThreshold) ? 1 : 0; ;
                }
            }
        }

        private void Smooth()
        {
            Vector2Int min = new Vector2Int(Size.x / 3, Size.y / 3);
            Vector2Int max = min * 2;

            int startX = _rng.Next(min.x, max.x);
            int startY = _rng.Next(min.y, max.y);

            bool[,] isQueued = new bool[Size.x, Size.y];

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(startX, startY));

            while (queue.Count > 0)
            {
                Vector2Int tile = queue.Dequeue();

                for (int y = tile.y - 1; y <= tile.y + 1; y++)
                {
                    for (int x = tile.x - 1; x <= tile.x + 1; x++)
                    {
                        if (x == tile.x && y == tile.y) continue;
                        if (!IsInMap(x, y) || isQueued[x, y]) continue;
                        if (isBorder(x, y)) continue;
                        if (x != tile.x && y != tile.y) continue;
                        queue.Enqueue(new Vector2Int(x, y));
                        isQueued[x, y] = true;

                    }
                }

                int wallCount = GetWalls(tile.x, tile.y);
                if (wallCount > _smoothingThreshold) _map[tile.x, tile.y] = 1;
                else if (wallCount < _smoothingThreshold) _map[tile.x, tile.y] = 0;
            }
        }

        private int GetWalls(int originX, int originY)
        {
            int count = 0;

            for (int y = originY - 1; y <= originY + 1; y++)
            {
                for (int x = originX - 1; x <= originX + 1; x++)
                {
                    if (x == originX && y == originY) continue;
                    if (!IsInMap(x, y)) count++;
                    else count += _map[x, y];
                }
            }

            return count;
        }

        private Vector2Int[][] FilterRegions(int originalTile, int newTile)
        {
            Vector2Int[][] regions = GetRegions(originalTile);
            List<Vector2Int[]> result = new List<Vector2Int[]>();

            for (int i = 0; i < regions.Length; i++)
            {
                Vector2Int[] region = regions[i];
                if (region.Length > _regionThreshold)
                {
                    result.Add(region);
                    continue;
                }

                for (int j = 0; j < region.Length; j++)
                {
                    Vector2Int tile = region[j];
                    _map[tile.x, tile.y] = newTile;
                }
            }

            return result.ToArray();
        }

        private Vector2Int[][] GetRegions(int tileType)
        {
            List<Vector2Int[]> regions = new List<Vector2Int[]>();

            for (int y = 0; y < Size.y; y++)
            {
                for (int x = 0; x < Size.x; x++)
                {
                    if (_regionFlags[x, y] || _map[x, y] != tileType) continue;
                    regions.Add(GetRegion(x, y));
                }
            }
            return regions.ToArray();
        }

        private Vector2Int[] GetRegion(int startX, int startY)
        {
            List<Vector2Int> region = new List<Vector2Int>();
            int tileType = _map[startX, startY];

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(startX, startY));
            _regionFlags[startX, startY] = true;

            while (queue.Count > 0)
            {
                Vector2Int tile = queue.Dequeue();
                region.Add(tile);

                for (int y = tile.y - 1; y <= tile.y + 1; y++)
                {
                    for (int x = tile.x - 1; x <= tile.x + 1; x++)
                    {
                        if (!IsInMap(x, y) || (x != tile.x && y != tile.y)) continue;
                        if (_regionFlags[x, y] || _map[x, y] != tileType) continue;
                        _regionFlags[x, y] = true;
                        queue.Enqueue(new Vector2Int(x, y));
                    }
                }
            }
            return region.ToArray();
        }

        private void ConnectRegions(Vector2Int[][] regions)
        {
            Vector2Int[] connections = GetConnections(regions);

            for (int i = 0; i < connections.Length; i += 2)
            {
                Vector3 start = new Vector3(connections[i].x, connections[i].y);
                Vector3 end = new Vector3(connections[i + 1].x, connections[i + 1].y);

                Vector2Int[] path = GetPath(connections[i], connections[i + 1]);
                ApplyPath(path);
            }
        }

        private Vector2Int[] GetConnections(Vector2Int[][] regions)
        {
            List<Vector2Int> result = new List<Vector2Int>();
            for (int i = 0; i < regions.Length; i++)
            {
                Vector2Int[] regionA = regions[i];
                Vector2Int[] borderA = GetRegionBorder(regionA);

                for (int j = i + 1; j < regions.Length; j++)
                {
                    Vector2Int[] regionB = regions[j];
                    Vector2Int[] borderB = GetRegionBorder(regionB);
                    Vector2Int[] connection = new Vector2Int[2];

                    int distance = int.MaxValue;
                    for (int a = 0; a < borderA.Length; a++)
                    {
                        Vector2Int tileA = borderA[a];
                        for (int b = 0; b < borderB.Length; b++)
                        {
                            Vector2Int tileB = borderB[b];
                            int newDistance = (int)(Mathf.Pow(tileA.x - tileB.x, 2) + Mathf.Pow(tileA.y - tileB.y, 2));
                            if (newDistance >= distance) continue;
                            distance = newDistance;
                            connection[0] = tileA;
                            connection[1] = tileB;
                        }
                    }
                    result.Add(connection[0]);
                    result.Add(connection[1]);
                }
            }
            return result.ToArray();
        }

        private Vector2Int[] GetRegionBorder(Vector2Int[] region)
        {
            List<Vector2Int> result = new List<Vector2Int>();

            for (int i = 0; i < region.Length; i++)
            {
                Vector2Int tile = region[i];
                for (int y = tile.y - 1; y <= tile.y + 1; y++)
                {
                    for (int x = tile.x - 1; x <= tile.x + 1; x++)
                    {
                        if (IsInMap(x, y) && (x == tile.x || y == tile.y) && _map[x, y] == 1)
                        {
                            result.Add(tile);
                        }
                    }
                }
            }

            return result.ToArray();
        }

        private Vector2Int[] GetPath(Vector2Int start, Vector2Int end)
        {
            List<Vector2Int> result = new List<Vector2Int>();

            bool inverted = false;
            int x = start.x;
            int y = start.y;
            int dx = end.x - start.x;
            int dy = end.y - start.y;
            int step = Math.Sign(dx);
            int gradientStep = Math.Sign(dy);
            int longest = Mathf.Abs(dx);
            int shortest = Mathf.Abs(dy);

            if (longest < shortest)
            {
                inverted = true;
                step = Math.Sign(dy);
                gradientStep = Math.Sign(dx);
                longest = Mathf.Abs(dy);
                shortest = Mathf.Abs(dx);
            }

            int gradientAccumulation = longest / 2;
            for (int i = 0; i < longest; i++)
            {
                result.Add(new Vector2Int(x, y));
                if (inverted) y += step;
                else x += step;

                gradientAccumulation += shortest;
                if (gradientAccumulation >= longest)
                {
                    if (inverted) x += gradientStep;
                    else y += gradientStep;
                    gradientAccumulation -= longest;
                }
            }


            return result.ToArray();
        }

        private void CheckExit()
        {

        }

        public void ApplyPath(Vector2Int[] path)
        {
            for (int i = 0; i < path.Length; i++)
            {
                Vector2Int tile = path[i];
                for (int x = -_pathRadius; x <= _pathRadius; x++)
                {
                    for (int y = -_pathRadius; y <= _pathRadius; y++)
                    {
                        if (x * x + y * y > _pathRadius * _pathRadius) continue;
                        int pathX = tile.x + x;
                        int pathY = tile.y + y;
                        if (!IsInMap(pathX, pathY)) continue;
                        if (isBorder(x, y)) continue;
                        _map[pathX, pathY] = 0;

                    }
                }
            }
        }

        private bool IsInMap(int x, int y)
        {
            return x >= 0 && x < Size.x && y >= 0 && y < Size.y;
        }

        private bool isBorder(int x, int y)
        {
            return (x == 0 || x == Size.x - 1 || y == 0 || y == Size.y - 1);
        }
    }


}