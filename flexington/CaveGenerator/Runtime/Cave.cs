using UnityEngine;

namespace flexington.CaveGenerator
{
    /// <summary>
    /// Representing a Cave
    /// </summary>
    public class Cave
    {
        private int[,] _map;
        /// <summary>
        /// Map of the cave.
        /// 0 represents free space
        /// 1 represents walls.
        /// </summary>
        public int[,] Map
        {
            get { return _map; }
        }

        private Vector2Int _size;
        /// <summary>
        /// The X and Y dimensions of the Map
        /// </summary>
        public Vector2Int Size
        {
            get { return _size; }
        }

        private Exit _exits;
        /// <summary>
        /// Object representing the exits of the cave
        /// </summary>
        public Exit Exits
        {
            get { return _exits; }
            set { _exits = value; }
        }

        /// <summary>
        /// Generates a new Cave
        /// </summary>
        public Cave(int[,] map, Vector2Int size, Exit exits)
        {
            _map = map;
            _size = size;
            _exits = exits;
        }
    }
}