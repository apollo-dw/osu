// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Rulesets.Osu.Difficulty.DifficultyValues
{
    public class AimDifficultyValue
    {
        public double Difficulty { get; set; }
        public double DifficultyWithoutSliders { get; set; }

        public double[] GetDifficultyValues()
        {
            return new[] { Difficulty, DifficultyWithoutSliders };
        }
    }
}
