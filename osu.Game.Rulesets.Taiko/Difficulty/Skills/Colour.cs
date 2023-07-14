// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Taiko.Difficulty.Evaluators;

namespace osu.Game.Rulesets.Taiko.Difficulty.Skills
{
    /// <summary>
    /// Calculates the colour coefficient of taiko difficulty.
    /// </summary>
    public class Colour : Skill
    {
        private double skillMultiplier => 0.12;

        public readonly StrainSet Strains = new StrainSet()
        {
            // This is set to decay slower than other skills, due to the fact that only the first note of each encoding class
            // having any difficulty values, and we want to allow colour difficulty to be able to build up even on
            // slower maps.
            StrainDecayBase = 0.8,
        };

        public Colour(Mod[] mods)
            : base(mods)
        {
        }

        public override void Process(DifficultyHitObject current)
        {
            double difficulty = ColourEvaluator.EvaluateDifficultyOf(current) * skillMultiplier;
            Strains.AddNewStrain(difficulty, current);
        }

        public override double DifficultyValue() => Strains.AggregateDifficulty();
    }
}
