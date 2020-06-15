using UnityEngine;

namespace flexington.CaveGenerator
{
    public class Cave
    {
        private int[,] _map;
        public int[,] Map
        {
            get { return _map; }
        }

        private Vector2Int _size;
        public Vector2Int Size
        {
            get { return _size; }
        }

        private Exit[] _exits;
        public Exit[] Exits
        {
            get { return _exits; }
            set { _exits = value; }
        }

        public Cave(int[,] map, Vector2Int size)
        {
            _map = map;
            _size = size;
        }
    }
}