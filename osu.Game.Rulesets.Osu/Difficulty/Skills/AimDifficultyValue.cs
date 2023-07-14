// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Osu.Difficulty.Evaluators;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Stores multiple difficulty values to be returned by the <see cref="AimEvaluator"/>, and consumed by <see cref="Aim"/>.
    /// </summary>
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
