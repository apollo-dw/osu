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
    public class Visual : Skill
    {
        public Visual(Mod[] mods)
            : base(mods)
        {
        }

        protected double preempt;
        protected override int HistoryLength => 32;
        private double skillMultiplier => 5;
        private List<double> difficultyValues = new List<double>();

        private double difficultyValueOf(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner || Previous.Count < 2)
                return 0;

            OsuDifficultyHitObject osuCurrent = (OsuDifficultyHitObject)current;

            double strain = 0.0;

            if (Mods.Any(h => h is OsuModHidden))
                strain += 2.5 * osuCurrent.NoteDensity;

            return strain;
        }

        protected override void Process(DifficultyHitObject current)
        {
            difficultyValues.Add(difficultyValueOf(current) * skillMultiplier);
        }

        public override double DifficultyValue()
        {
            double difficulty = 0;
            double weight = 1;

            // Difficulty is the weighted sum of the highest strains from every section.
            // We're sorting from highest to lowest strain.
            foreach (double value in difficultyValues.OrderByDescending(d => d))
            {
                difficulty += value * weight;
                weight *= 0.9;
            }

            return difficulty;
        }
    }
}
