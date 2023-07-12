// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to correctly aim at every object in the map with a uniform CircleSize and normalized distances.
    /// </summary>
    public class Aim : MultiStrainSkill
    {
        public Aim(Mod[] mods, bool withSliders)
            : base(mods)
        {
            this.withSliders = withSliders;
            currentStrain = new double[ConcurrentStrainCount];
        }

        private readonly bool withSliders;

        private double[] currentStrain;

        private double skillMultiplier => 23.55;
        private double strainDecayBase => 0.15;

        protected override int ConcurrentStrainCount => 2;

        private double strainDecay(double ms) => Math.Pow(strainDecayBase, ms / 1000);

        protected override double CalculateInitialStrain(double time, DifficultyHitObject current, int offset) => currentStrain[offset] * strainDecay(time - current.Previous(0).StartTime);

        protected override double[] StrainValuesAt(DifficultyHitObject current)
        {
            double[] difficulties = AimEvaluator.EvaluateDifficultyOf(current, withSliders).GetDifficultyValues();

            for (int i = 0; i < ConcurrentStrainCount; i++)
            {
                currentStrain[i] *= strainDecay(current.DeltaTime);
                currentStrain[i] += difficulties[i] * skillMultiplier;
            }

            return currentStrain;
        }
    }
}
