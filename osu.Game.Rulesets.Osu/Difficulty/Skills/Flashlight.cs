// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Evaluators;
using osu.Game.Rulesets.Osu.Mods;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to memorise and hit every object in a map with the Flashlight mod enabled.
    /// </summary>
    public class Flashlight : Skill
    {
        private readonly bool hasHiddenMod;

        public Flashlight(Mod[] mods)
            : base(mods)
        {
            hasHiddenMod = mods.Any(m => m is OsuModHidden);
        }

        private readonly StrainSet strains = new StrainSet();

        private double skillMultiplier => 0.052;

        public sealed override void Process(DifficultyHitObject current)
        {
            double difficulty = FlashlightEvaluator.EvaluateDifficultyOf(current, hasHiddenMod) * skillMultiplier;
            strains.AddNewStrain(difficulty, current);
        }

        public override double DifficultyValue() => strains.GetCurrentStrainPeaks().Sum() * OsuStrainSkill.DEFAULT_DIFFICULTY_MULTIPLIER;
    }
}

