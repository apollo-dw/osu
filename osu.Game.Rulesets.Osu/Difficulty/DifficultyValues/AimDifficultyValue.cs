// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Rulesets.Osu.Difficulty.DifficultyValues
{
    public class AimDifficultyValue : DifficultyValue
    {
        public double DifficultyWithoutSliders { get; set; }

        public AimDifficultyValue(double difficulty)
            : base(difficulty)
        {
        }

        public override double[] GetDifficultyValues()
        {
            return new double[] { Difficulty, DifficultyWithoutSliders };
        }
    }
}
