using UnityEngine;

namespace flexington.CaveGenerator
{

    public class CaveManagerComponent : MonoBehaviour
    {
        [SerializeField] private CaveObject[] _cavePresets;

        private int[,] _map;


        private void Start()
        {
            _map = _cavePresets[0].Generate();
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _map = _cavePresets[0].Generate();
            }
        }

        private void OnDrawGizmos()
        {
            if (_map == null) return;
            CaveObject cave = _cavePresets[0];
            for (int y = 0; y < cave.Size.y; y++)
            {
                for (int x = 0; x < cave.Size.x; x++)
                {
                    if (_map[x, y] == 0) Gizmos.color = Color.white;
                    else Gizmos.color = Color.black;
                    Gizmos.DrawCube(new Vector3(x, y, 0), Vector3.one);
                }
            }
        }
    }
}