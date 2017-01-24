using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MediaPlayback.MultiThumbSlider
{
    public static class MediaHelper
    {
        /// <summary>
        /// The smallest value that will recognized as a difference when comparing two media-related values.
        /// </summary>
        public const double MediaEpsilon = 0.000001;

        /// <summary>
        /// Tests whether the left-hand side is greater than the right-hand side, taking into account the <see cref="MediaEpsilon"/>
        /// variance.
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool IsGreaterThan(double lhs, double rhs)
        {
            return (lhs - rhs) > MediaEpsilon;
        }

        /// <summary>
        /// Tests whether the left-hand side is less than the right-hand side, taking into account the <see cref="MediaEpsilon"/>
        /// variance.
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool IsLessThan(double lhs, double rhs)
        {
            return (lhs - rhs) < MediaEpsilon;
        }

        /// <summary>
        /// Tests whether the left-hand side is equal to the right-hand side value, taking into account the <see cref="MediaEpsilon"/>
        /// variance.
        /// </summary>
        /// <param name="lhs"></param>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public static bool AreEqual(double lhs, double rhs)
        {
            return Math.Abs(lhs - rhs) < MediaEpsilon;
        }

        /// <summary>
        /// Tests whether the value is equal zero, taking into account the <see cref="MediaEpsilon"/> variance.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsZero(double value)
        {
            return Math.Abs(value) < MediaEpsilon;
        }
    }
}
