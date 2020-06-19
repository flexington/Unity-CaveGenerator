using UnityEngine;

namespace flexington.CaveGenerator
{
    /// <summary>
    /// Represents the exists of a cave
    /// </summary>
    public class Exit
    {
        /// <summary>
        /// Positions forming the top exit
        /// </summary>
        public int[] Top { get; set; }

        /// <summary>
        /// Positions forming the right exit
        /// </summary>
        public int[] Right { get; set; }

        /// <summary>
        /// Positions forming the bottom exit
        /// </summary>
        public int[] Bottom { get; set; }

        /// <summary>
        /// Positions forming the left exit
        /// </summary>
        public int[] Left { get; set; }

        /// <summary>
        /// Creates a new Exit object
        /// </summary>
        public Exit()
        {
        }

        /// <summary>
        /// Creates a new Exit object
        /// </summary>
        public Exit(Exit exit)
        {
            Top = exit.Top;
            Right = exit.Right;
            Bottom = exit.Bottom;
            Left = exit.Left;
        }

        /// <summary>
        /// Creates a new Exit object
        /// </summary>
        public Exit(int[] top, int[] right, int[] bottom, int[] left)
        {
            Top = top;
            Right = right;
            Bottom = bottom;
            Left = left;
        }
    }
}