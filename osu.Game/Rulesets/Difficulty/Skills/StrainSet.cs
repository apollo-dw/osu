// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Difficulty.Skills
{
    public class StrainSet
    {
        /// <summary>
        /// The length of each strain section.
        /// </summary>
        public int SectionLength { get; set; } = 400;

        /// <summary>
        /// Determines how quickly strain decays for the given skill.
        /// For example a value of 0.15 indicates that strain decays to 15% of its original value in one second.
        /// </summary>
        public double StrainDecayBase { get; set; } = 0.15;

        public double DecayWeight { get; set; } = 0.9;

        public double DifficultyMultiplier { get; set; } = 1.06;

        private double currentSectionPeak;
        private double currentSectionEnd;

        private readonly List<double> strainPeaks = new List<double>();

        private double currentStrain;

        public virtual void AddNewStrain(double difficulty, DifficultyHitObject current)
        {
            // The first object doesn't generate a strain, so we begin with an incremented section end
            if (current.Index == 0)
                currentSectionEnd = Math.Ceiling(current.StartTime / SectionLength) * SectionLength;

            while (current.StartTime > currentSectionEnd)
            {
                strainPeaks.Add(currentSectionPeak);

                // The maximum strain of the new section is not zero by default
                // This means we need to capture the strain level at the beginning of the new section, and use that as the initial peak level.
                currentSectionPeak = currentStrain * strainDecay(currentSectionEnd - current.Previous(0).StartTime);

                currentSectionEnd += SectionLength;
            }

            // Apply strain decay and add new strain.
            currentStrain *= strainDecay(current.DeltaTime);
            currentStrain += difficulty;

            currentSectionPeak = Math.Max(currentStrain, currentSectionPeak);
        }

        public virtual double AggregateDifficulty()
        {
            double difficulty = 0;
            double weight = 1;

            // Sections with 0 strain are excluded to avoid worst-case time complexity of the following sort (e.g. /b/2351871).
            // These sections will not contribute to the difficulty.
            var peaks = GetCurrentStrainPeaks().Where(p => p > 0);

            // Difficulty is the weighted sum of the highest strains from every section.
            // We're sorting from highest to lowest strain.
            foreach (double strain in peaks.OrderByDescending(d => d))
            {
                difficulty += strain * weight;
                weight *= DecayWeight;
            }

            return difficulty;
        }

        public IEnumerable<double> GetCurrentStrainPeaks() => strainPeaks.Append(currentSectionPeak);

        private double strainDecay(double ms) => Math.Pow(StrainDecayBase, ms / 1000);
    }
}
