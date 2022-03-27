// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Difficulty.Preprocessing
{
    public class OsuDifficultyHitObject : DifficultyHitObject
    {
        private const int normalised_radius = 50; // Change radius to 50 to make 100 the diameter. Easier for mental maths.
        private const int min_delta_time = 25;

        protected new OsuHitObject BaseObject => (OsuHitObject)base.BaseObject;

        /// <summary>
        /// Milliseconds elapsed since the start time of the previous <see cref="OsuDifficultyHitObject"/>, with a minimum of 25ms.
        /// </summary>
        public readonly double StrainTime;

        /// <summary>
        /// Milliseconds elapsed since the start time of the previous actively clicked <see cref="OsuDifficultyHitObject"/>, with a minimum of 25ms.
        /// </summary>
        public readonly double TapTime;

        public double Distance { get; private set; }

        public double TapDistance { get; private set; }

        public bool Clickable { get; private set; }

        /// <summary>
        /// Angle the player has to take to hit this <see cref="OsuDifficultyHitObject"/>.
        /// Calculated as the angle between the circles (current-2, current-1, current).
        /// </summary>
        public double? Angle { get; private set; }

        private readonly OsuHitObject lastLastObject;
        private readonly OsuHitObject lastObject;
        private readonly OsuHitObject lastActiveObject;

        public OsuDifficultyHitObject(HitObject hitObject, HitObject lastLastObject, HitObject lastObject, HitObject lastActiveObject, double clockRate)
            : base(hitObject, lastObject, clockRate)
        {
            this.lastLastObject = (OsuHitObject)lastLastObject;
            this.lastObject = (OsuHitObject)lastObject;
            this.lastActiveObject = (OsuHitObject)lastActiveObject;

            // Capped to 25ms to prevent difficulty calculation breaking from simultaneous objects.
            StrainTime = Math.Max(DeltaTime, min_delta_time);
            TapTime = Math.Max((hitObject.StartTime - lastActiveObject.StartTime) / clockRate, min_delta_time);

            if (hitObject is SliderTailCircle || hitObject is SliderRepeat || hitObject is SliderTick)
                Clickable = false;
            else
                Clickable = true;

            setDistances();
        }

        private void setDistances()
        {
            // We will scale distances by this factor, so we can assume a uniform CircleSize among beatmaps.
            float scalingFactor = normalised_radius / (float)BaseObject.Radius;

            if (!Clickable)
                scalingFactor /= 2.4f;

            if (BaseObject.Radius < 30)
            {
                float smallCircleBonus = Math.Min(30 - (float)BaseObject.Radius, 5) / 50;
                scalingFactor *= 1 + smallCircleBonus;
            }

            Vector2 lastCursorPosition = getEndCursorPosition(lastObject);

            Distance = (BaseObject.StackedPosition * scalingFactor - lastObject.StackedPosition * scalingFactor).Length;
            TapDistance = (BaseObject.StackedPosition * scalingFactor - lastActiveObject.StackedPosition * scalingFactor).Length;

            if (lastLastObject != null && !(lastLastObject is Spinner))
            {
                Vector2 lastLastCursorPosition = getEndCursorPosition(lastLastObject);

                Vector2 v1 = lastLastCursorPosition - lastObject.StackedPosition;
                Vector2 v2 = BaseObject.StackedPosition - lastCursorPosition;

                float dot = Vector2.Dot(v1, v2);
                float det = v1.X * v2.Y - v1.Y * v2.X;

                Angle = Math.Abs(Math.Atan2(det, dot));
            }
        }

        private Vector2 getEndCursorPosition(OsuHitObject hitObject)
        {
            Vector2 pos = hitObject.StackedPosition;

            return pos;
        }
    }
}
