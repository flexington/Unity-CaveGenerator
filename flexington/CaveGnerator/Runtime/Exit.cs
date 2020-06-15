using UnityEngine;

namespace flexington.CaveGenerator
{
    public class Exit
    {
        private Direction _direction;
        public Direction Direction
        {
            get { return _direction; }
        }

        private Vector2Int[] _tiles;
        public Vector2Int[] Tiles
        {
            get { return _tiles; }
        }

        public Exit(Direction direction, Vector2Int[] tiles)
        {
            _tiles = tiles;
            _direction = direction;
        }
    }
}