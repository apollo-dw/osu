// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;
using System.Collections.Generic;
using osu.Game.Rulesets.Difficulty.Skills;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to read every object in the map.
    /// </summary>
    public class Visual : OsuStrainSkill
    {
        public Visual(Mod[] mods)
            : base(mods)
        {
        }

        protected override int HistoryLength => 32;
        private double skillMultiplier => 25;
        private double strainDecayBase => 0.1;
        protected override double DecayWeight => 0.9;
        private double currentStrain;

        private double strainValueOf(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner)
                return 0;

            OsuDifficultyHitObject osuCurrent = (OsuDifficultyHitObject)current;

            double strain = 0.0;

            if (Mods.Any(h => h is OsuModHidden))
                strain += 0.1 * osuCurrent.NoteDensity;

            double preemptStrain = 0.0;
            if (osuCurrent.preempt < 400)
                preemptStrain += 0.008 * (400 - osuCurrent.preempt);

            return strain + preemptStrain;
        }

        private double strainDecay(double ms) => Math.Pow(strainDecayBase, ms / 1000);

        protected override double CalculateInitialStrain(double time) => currentStrain * strainDecay(time - Previous[0].StartTime);

        protected override double StrainValueAt(DifficultyHitObject current)
        {
            currentStrain *= strainDecay(current.DeltaTime);
            currentStrain += strainValueOf(current) * skillMultiplier;

            return currentStrain;
        }

    }
}
