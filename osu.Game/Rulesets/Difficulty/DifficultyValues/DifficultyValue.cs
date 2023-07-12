// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Rulesets.Difficulty.DifficultyValues
{
    public class DifficultyValue
    {
        public double Difficulty { get; }

        public DifficultyValue(double difficulty)
        {
            Difficulty = difficulty;
        }

        public virtual double[] GetDifficultyValues()
        {
            return new[] { Difficulty };
        }
    }
}
