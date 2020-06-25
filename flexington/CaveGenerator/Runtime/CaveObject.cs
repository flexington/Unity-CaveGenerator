using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace flexington.CaveGenerator
{
    /// <summary>
    /// Template to generate a new cave
    /// </summary>
    [CreateAssetMenu(fileName = "Cave", menuName = "flexington/CaveGenerator/New Cave", order = 0)]
    public class CaveObject : ScriptableObject
    {
        [SerializeField] private Vector2Int _size;
        /// <summary>
        /// X and Y dimension of the map.
        /// </summary>
        public Vector2Int Size
        {
            get { return _size; }
        }

        [SerializeField] private string _seed;
        /// <summary>
        /// Seed for procedural generation
        /// </summary>
        public string Seed
        {
            get { return _seed; }
            set { _seed = value; }
        }

        [SerializeField, Range(0, 100)] private int _fillThreshold;
        /// <summary>
        /// Every number less than the threshold will be considered a wall
        /// </summary>
        public int FillThreshold
        {
            get { return _fillThreshold; }
        }

        [Range(1, 15)]
        [SerializeField] private int _smoothingIterations;
        /// <summary>
        /// Indicates how often the smoothing algoruythm will be executed
        /// </summary>
        public int SmoothingIterations
        {
            get { return _smoothingIterations; }
            set { _smoothingIterations = value; }
        }

        [SerializeField, Range(1, 8)] private int _smoothingThreshold;
        /// <summary>
        /// Number of Neighbours influencing the current value.
        /// If the number of walls is less than the threshold, field will become a free space
        /// If the number of walls is greater then the threshold, field will become a wall
        /// It the number of walls is equal to the threshold, field will not change
        /// </summary>
        public int SmoothingThreshold
        {
            get { return _smoothingThreshold; }
            set { _smoothingThreshold = value; }
        }

        [SerializeField, Range(0, 50)] private int _regionThreshold;
        /// <summary>
        /// If a region is smaller than the threshold, region will be filled up.
        /// </summary>
        public int RegionThreshold
        {
            get { return _regionThreshold; }
            set { _regionThreshold = value; }
        }

        [SerializeField] private int _pathRadius;
        /// <summary>
        /// Radius of the path that connects different regions.
        /// </summary>
        public int PathRadius
        {
            get { return _pathRadius; }
            set { _pathRadius = value; }
        }

        private int[,] _map;
        private bool[,] _regionFlags;
        System.Random _rng;

        /// <summary>
        /// Generates a new random cave and returns Cave object.
        /// </summary>
        public Cave Generate(string seed = null, Exit exits = null)
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
            exits = GenerateExits(exits);
            Smooth();

            FilterRegions(1, 0);
            Vector2Int[][] regions = FilterRegions(0, 1);

            ConnectRegions(regions);

            FilterRegions(1, 0);

            for (int i = 1; i < _smoothingIterations; i++) Smooth();

            return new Cave(_map, _size, exits);
        }

        /// <summary>
        /// Filles the map randomly using the filling threshold
        /// </summary>
        private void FillMap()
        {
            for (int x = 0; x < Size.x; x++)
            {
                for (int y = 0; y < Size.y; y++)
                {
                    if (isBorder(x, y)) _map[x, y] = 1;
                    else _map[x, y] = (_rng.Next(0, 100) < FillThreshold) ? 1 : 0; ;
                }
            }
        }

        /// <summary>
        /// Smoothes the map using the smoothing iterations and threshold parameters
        /// </summary>
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

                for (int x = tile.x - 1; x <= tile.x + 1; x++)
                {
                    for (int y = tile.y - 1; y <= tile.y + 1; y++)
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

        /// <summary>
        /// Returns the number of sourrounding walls for the given position
        /// </summary>
        private int GetWalls(int originX, int originY)
        {
            int count = 0;

            for (int x = originX - 1; x <= originX + 1; x++)
            {
                for (int y = originY - 1; y <= originY + 1; y++)
                {
                    if (x == originX && y == originY) continue;
                    if (!IsInMap(x, y)) count++;
                    else count += _map[x, y];
                }
            }

            return count;
        }

        /// <summary>
        /// Filters our all regions smaller than the threshold.
        /// </summary>
        private Vector2Int[][] FilterRegions(int originalTile, int newTile)
        {
            Vector2Int[][] regions = GetRegions(originalTile);
            List<Vector2Int[]> result = new List<Vector2Int[]>();

            for (int i = 0; i < regions.Length; i++)
            {
                Vector2Int[] region = regions[i];
                if (region.Length >= _regionThreshold || IsBorderRegion(region))
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

        /// <summary>
        /// Returns all regions for the given tile type
        /// </summary>
        private Vector2Int[][] GetRegions(int tileType)
        {
            List<Vector2Int[]> regions = new List<Vector2Int[]>();

            for (int x = 0; x < Size.x; x++)
            {
                for (int y = 0; y < Size.y; y++)
                {
                    if (_regionFlags[x, y] || _map[x, y] != tileType) continue;
                    regions.Add(GetRegion(x, y));
                }
            }
            return regions.ToArray();
        }

        /// <summary>
        /// Indicates of the given region contains a border tile
        /// </summary>
        private bool IsBorderRegion(Vector2Int[] region)
        {
            for (int i = 0; i < region.Length; i++)
            {
                Vector2Int position = region[i];
                if (isBorder(position.x, position.y)) return true;
            }
            return false;
        }

        /// <summary>
        /// Get the region the given position belongs to
        /// </summary>
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

                for (int x = tile.x - 1; x <= tile.x + 1; x++)
                {
                    for (int y = tile.y - 1; y <= tile.y + 1; y++)
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

        /// <summary>
        /// Connects the given regions with eacho ther.
        /// </summary>
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

        /// <summary>
        /// Calculates the pathes between regions
        /// </summary>
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

        /// <summary>
        /// Returns the border tiles of the given region.
        /// </summary>
        private Vector2Int[] GetRegionBorder(Vector2Int[] region, bool diagonal = false)
        {
            List<Vector2Int> result = new List<Vector2Int>();

            for (int i = 0; i < region.Length; i++)
            {
                Vector2Int tile = region[i];
                for (int x = tile.x - 1; x <= tile.x + 1; x++)
                {
                    for (int y = tile.y - 1; y <= tile.y + 1; y++)
                    {
                        if (!IsInMap(x, y)) continue;
                        if ((x != tile.x && y != tile.y) && !diagonal) continue;
                        if (_map[x, y] == 1) continue;
                        result.Add(tile);
                    }
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Generats a path between two points.
        /// </summary>
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

        /// <summary>
        /// Generates exits for all four sides based on the given Exit object.
        /// </summary>
        private Exit GenerateExits(Exit exit)
        {
            if (exit == null) exit = new Exit();
            if (exit.Top != null && exit.Top.Length == 0) exit.Top = GenerateExit(Direction.Top);
            if (exit.Right != null && exit.Right.Length == 0) exit.Right = GenerateExit(Direction.Right);
            if (exit.Bottom != null && exit.Bottom.Length == 0) exit.Bottom = GenerateExit(Direction.Bottom);
            if (exit.Left != null && exit.Left.Length == 0) exit.Left = GenerateExit(Direction.Left);

            ApplyExit(exit.Top, Direction.Top);
            ApplyExit(exit.Right, Direction.Right);
            ApplyExit(exit.Bottom, Direction.Bottom);
            ApplyExit(exit.Left, Direction.Left);

            return new Exit(exit);
        }

        /// <summary>
        /// Generates 1 to 3 exits with random size.
        /// </summary>
        private int[] GenerateExit(Direction direction)
        {
            List<int> result = new List<int>();
            int exits = _rng.Next(1, 3);

            for (int i = 0; i < exits; i++)
            {
                int max = -1;
                if (direction == Direction.Top || direction == Direction.Bottom) max = Size.y - 2;
                if (direction == Direction.Right || direction == Direction.Left) max = Size.x - 2;
                int start = _rng.Next(1, max - 3);
                int length = _rng.Next(3, max);
                int end = start + length;
                if (end > max) end = max;

                for (int j = start; j <= end; j++)
                {
                    result.Add(j);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Changes the fields at the border to represent exits
        /// </summary>
        private void ApplyExit(int[] exit, Direction direction)
        {
            if (exit == null) return;

            for (int i = 0; i < exit.Length; i++)
            {
                int x = -1;
                int y = -1;

                if (direction == Direction.Top)
                {
                    x = exit[i];
                    y = Size.y - 1;
                }
                else if (direction == Direction.Right)
                {
                    x = Size.x - 1;
                    y = exit[i];
                }
                else if (direction == Direction.Bottom)
                {
                    x = exit[i];
                    y = 0;
                }
                else if (direction == Direction.Left)
                {
                    x = 0;
                    y = exit[i];
                }

                if (!IsInMap(x, y)) continue;
                _map[x, y] = 0;
            }
        }

        /// <summary>
        /// Changes the fields along the given path.
        /// </summary>
        private void ApplyPath(Vector2Int[] path)
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
                        if (isBorder(pathX, pathY)) continue;
                        _map[pathX, pathY] = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Indicates if position x and y is a valid position on the map
        /// </summary>
        private bool IsInMap(int x, int y)
        {
            return x >= 0 && x < Size.x && y >= 0 && y < Size.y;
        }

        /// <summary>
        /// Indicates if position x and y is at the border of the cave
        /// </summary>
        private bool isBorder(int x, int y)
        {
            return (x == 0 || x == Size.x - 1 || y == 0 || y == Size.y - 1);
        }
    }
}