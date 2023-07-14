// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Difficulty.Evaluators;

namespace osu.Game.Rulesets.Taiko.Difficulty.Skills
{
    /// <summary>
    /// Calculates the stamina coefficient of taiko difficulty.
    /// </summary>
    public class Stamina : Skill
    {
        private double skillMultiplier => 1.1;

        public readonly StrainSet Strains = new StrainSet()
        {
            StrainDecayBase = 0.4,
        };

        /// <summary>
        /// Creates a <see cref="Stamina"/> skill.
        /// </summary>
        /// <param name="mods">Mods for use in skill calculations.</param>
        public Stamina(Mod[] mods)
            : base(mods)
        {
        }

        public override void Process(DifficultyHitObject current)
        {
            double difficulty = StaminaEvaluator.EvaluateDifficultyOf(current) * skillMultiplier;
            Strains.AddNewStrain(difficulty, current);
        }

        public override double DifficultyValue() => Strains.AggregateDifficulty();
    }
}
