// Copyright (C) 2025 Kampute
//
// This file is part of the Kampute.Retry package and is released under the terms of the MIT license.
// See the LICENSE file in the project root for the full license text.

namespace Kampute.Retry.Strategies
{
    using System;

    /// <summary>
    /// Computes retry delays that saturate at the limits of <see cref="TimeSpan"/> instead of overflowing.
    /// </summary>
    /// <remarks>
    /// Delays that grow with the number of attempts exceed <see cref="TimeSpan.MaxValue"/> after enough attempts. A saturated delay is longer than a
    /// <see cref="RetrySession"/> waits, so the session stops retrying instead of failing with an <see cref="OverflowException"/>.
    /// </remarks>
    internal static class DelayMath
    {
        /// <summary>
        /// Converts a number of milliseconds to a <see cref="TimeSpan"/>, saturating at <see cref="TimeSpan.MinValue"/> and <see cref="TimeSpan.MaxValue"/>.
        /// </summary>
        /// <param name="milliseconds">The number of milliseconds. <see cref="double.NaN"/> is treated as an unbounded delay.</param>
        /// <returns>The delay, or <see cref="TimeSpan.MaxValue"/> or <see cref="TimeSpan.MinValue"/> if <paramref name="milliseconds"/> is out of range.</returns>
        public static TimeSpan FromMilliseconds(double milliseconds)
        {
            if (double.IsNaN(milliseconds) || milliseconds >= TimeSpan.MaxValue.TotalMilliseconds)
                return TimeSpan.MaxValue;
            if (milliseconds <= TimeSpan.MinValue.TotalMilliseconds)
                return TimeSpan.MinValue;

            return TimeSpan.FromMilliseconds(milliseconds);
        }

        /// <summary>
        /// Computes <paramref name="initial"/> plus <paramref name="attempts"/> times <paramref name="step"/>, exactly when the result fits in a
        /// <see cref="TimeSpan"/> and saturated otherwise.
        /// </summary>
        /// <param name="initial">The initial delay.</param>
        /// <param name="step">The amount added for each attempt.</param>
        /// <param name="attempts">The number of attempts.</param>
        /// <returns>The delay, saturated at <see cref="TimeSpan.MinValue"/> and <see cref="TimeSpan.MaxValue"/>.</returns>
        public static TimeSpan AddMultiple(TimeSpan initial, TimeSpan step, uint attempts)
        {
            var ticks = initial.Ticks + (double)step.Ticks * attempts;
            if (ticks >= long.MaxValue)
                return TimeSpan.MaxValue;
            if (ticks <= long.MinValue)
                return TimeSpan.MinValue;

            return initial + TimeSpan.FromTicks(step.Ticks * attempts);
        }
    }
}
