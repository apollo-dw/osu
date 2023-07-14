// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Utils;
using osu.Game.Rulesets.Difficulty.Preprocessing;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Independently handles a set of strains, allowing for concurrent strains in difficulty calculation.
    /// </summary>
    public class StrainSet
    {
        /// <summary>
        /// The number of sections with the highest strains, which the peak strain reductions will apply to.
        /// This is done in order to decrease their impact on the overall difficulty of the map for this skill.
        /// </summary>
        public virtual int ReducedSectionCount { get; set; } = 10;

        /// <summary>
        /// The baseline multiplier applied to the section with the biggest strain.
        /// </summary>
        public virtual double ReducedStrainBaseline { get; set; } = 0.75;

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

        private double currentStrain;
        private double currentSectionPeak;
        private double currentSectionEnd;

        private readonly List<double> strainPeaks = new List<double>();

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

        public double AggregateDifficulty()
        {
            double difficulty = 0;
            double weight = 1;

            // Sections with 0 strain are excluded to avoid worst-case time complexity of the following sort (e.g. /b/2351871).
            // These sections will not contribute to the difficulty.
            var peaks = GetCurrentStrainPeaks().Where(p => p > 0);

            List<double> strains = peaks.OrderByDescending(d => d).ToList();

            // We are reducing the highest strains first to account for extreme difficulty spikes
            for (int i = 0; i < Math.Min(strains.Count, ReducedSectionCount); i++)
            {
                double scale = Math.Log10(Interpolation.Lerp(1, 10, Math.Clamp((float)i / ReducedSectionCount, 0, 1)));
                strains[i] *= Interpolation.Lerp(ReducedStrainBaseline, 1.0, scale);
            }

            // Difficulty is the weighted sum of the highest strains from every section.
            // We're sorting from highest to lowest strain.
            foreach (double strain in strains.OrderByDescending(d => d))
            {
                difficulty += strain * weight;
                weight *= DecayWeight;
            }

            return difficulty * DifficultyMultiplier;
        }

        public IEnumerable<double> GetCurrentStrainPeaks() => strainPeaks.Append(currentSectionPeak);

        private double strainDecay(double ms) => Math.Pow(StrainDecayBase, ms / 1000);
    }
}
