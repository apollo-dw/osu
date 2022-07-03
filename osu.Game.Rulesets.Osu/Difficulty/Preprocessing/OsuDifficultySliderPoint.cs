// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Game.Rulesets.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Difficulty.Preprocessing
{
    public class OsuDifficultySliderPoint
    {
        public readonly HitObject BaseObject;
        public Vector2 Position;
        public double Time;

        public OsuDifficultySliderPoint(Vector2 position, double time, HitObject baseObject = null)
        {
            BaseObject = baseObject;
            Position = position;
            Time = time;
        }
    }
}
