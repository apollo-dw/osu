// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to correctly aim at every object in the map with a uniform CircleSize and normalized distances.
    /// </summary>
    public class Aim : Skill
    {
        public Aim(Mod[] mods)
            : base(mods)
        {
        }

        private const int strain_count = 2;
        private readonly OsuStrainSet[] strains = new OsuStrainSet[strain_count].Select(s => new OsuStrainSet()).ToArray();

        private double skillMultiplier => 23.55;

        public sealed override void Process(DifficultyHitObject current)
        {
            double[] difficulties = AimEvaluator.EvaluateDifficultyOf(current).GetDifficultyValues();

            for (int i = 0; i < strain_count; i++)
                strains[i].AddNewStrain(difficulties[i] * skillMultiplier, current);
        }

        public override double DifficultyValue() => DifficultyValue(0);
        public double DifficultyValue(int strainIndex) => strains[strainIndex].AggregateDifficulty();
    }
}


