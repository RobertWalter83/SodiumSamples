using System;
using System.Windows;

namespace SodiumPong
{
    static class Util
    {
        public static Vector VectorZero = new Vector(0, 0);
        public static Vector VectorN = new Vector(0, -1);
        public static Vector VectorS = new Vector(0, 1);

        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }
    }
}
