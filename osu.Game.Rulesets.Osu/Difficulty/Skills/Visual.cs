// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Difficulty.Preprocessing;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;
using osu.Framework.Utils;
using System.Collections.Generic;
using osu.Game.Rulesets.Difficulty.Skills;

namespace osu.Game.Rulesets.Osu.Difficulty.Skills
{
    /// <summary>
    /// Represents the skill required to read every object in the map.
    /// </summary>
    public class Visual : Skill
    {
        public Visual(Mod[] mods, double preempt)
            : base(mods)
        {
            this.preempt = preempt;
        }

        private const double rhythm_multiplier = 1.2;
        private const double aim_multiplier = 2.5;

        private const double reading_window_backwards = 250.0;
        private const double reading_window_forwards = 3000.0;

        protected double preempt;

        private double skillMultiplier => 3;

        protected override int HistoryLength => 32;

        private List<double> difficultyValues = new List<double>();
        private Dictionary<double, int> previousRhythmRatios = new Dictionary<double, int>();
        private int currentObjVisibleIndex;

        private double difficultyValueOf(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner || Previous.Count < 2)
                return 0;

            var osuCurrent = (OsuDifficultyHitObject)current;

            var rhythmReadingComplexity = 0.0;
            var aimReadingComplexity = 0.0;

            // the reading window represents the player's focus when processing the screen
            // we include the previous 500ms of objects, and the next 3000ms worth of visual objects
            // previous 500ms: previous objects influence the readibility of current
            // next 3000ms: next objects influence the readibility of current
            List<OsuDifficultyHitObject> readingWindow = new List<OsuDifficultyHitObject>();

            foreach (OsuDifficultyHitObject hitObject in Previous)
            {
                if (osuCurrent.StartTime - hitObject.StartTime <= reading_window_backwards)
                    readingWindow.Insert(0, hitObject);
            }

            readingWindow.Add(osuCurrent);
            currentObjVisibleIndex = readingWindow.Count - 1;

            foreach (OsuDifficultyHitObject hitObject in osuCurrent.visibleObjects)
            {
                if (hitObject.StartTime - osuCurrent.StartTime <= reading_window_forwards)
                    readingWindow.Add(hitObject);
            }

            if (osuCurrent.NoteDensity > 1)
            {
                rhythmReadingComplexity = calculateRhythmReading(readingWindow, (OsuDifficultyHitObject)Previous[0], osuCurrent, preempt) * rhythm_multiplier;
                aimReadingComplexity = calculateAimReading(readingWindow, osuCurrent, osuCurrent.visibleObjects[0]) * aim_multiplier;
            }

            // Reading density strain represents the amount of *stuff* on screen.
            // Higher weighting given to objects within the reading window.
            var readingDensityStrain = (readingWindow.Count * 2 + osuCurrent.NoteDensity) / 32;
            readingDensityStrain *= logistic((osuCurrent.JumpDistance - 78) / 26);

            var strain = readingDensityStrain + Math.Pow(rhythmReadingComplexity + aimReadingComplexity, 1.2) * 8.0;

            //if (strain > 1)
            //Console.WriteLine( Math.Round((current.StartTime / 1000.0), 3).ToString() + "  " + Math.Round(strain, 3).ToString() + "  " + Math.Round(readingDensityStrain, 3).ToString() + "   " + Math.Round(rhythmReadingComplexity, 3).ToString() + "  " + Math.Round(aimReadingComplexity, 3).ToString());

            return strain;
        }

        private double calculateRhythmReading(List<OsuDifficultyHitObject> visibleObjects, OsuDifficultyHitObject prevObject, OsuDifficultyHitObject currentObject, double preempt)
        {
            var overlapnessTotal = 0.0;

            // calculate how much visible objects overlap the previous
            for (int i = 0; i < visibleObjects.Count; i++)
            {
                // lets not consider simultaneous objects
                if (visibleObjects[i].StartTime == currentObject.StartTime)
                    continue;

                var overlapness = 0.0;

                var tCurrNext = currentObject.StrainTime;
                var tPrevCurr = Math.Abs(visibleObjects[i].StartTime - currentObject.StartTime) / Math.Abs(currentObjVisibleIndex - i);
                var tRatio = Math.Max(tCurrNext / tPrevCurr, tPrevCurr / tCurrNext);

                var constantRhythmNerf = 1 - Math.Max(0, -100 * Math.Pow(tRatio - 1, 2) + 1);

                var distanceRatio = currentObject.JumpDistance / (visibleObjects[i].JumpDistance + 1e-10);
                var changeRatio = distanceRatio * tRatio;
                var spacingChange = Math.Min(1, Math.Pow(changeRatio - 1, 2) * 1000) * Math.Min(1.0, Math.Pow(distanceRatio - 1, 2) * 1000);

                overlapness += logistic((18 - currentObject.NormalisedDistanceTo(visibleObjects[i])) / 5);

                var distanceToLast = visibleObjects[i].JumpDistance;
                var distanceToLastOverlapness = logistic((128 - distanceToLast) / 5);

                // Stacked successive objects are only hard to read if there's a rhythm difference.
                overlapness *= 1 - (distanceToLastOverlapness * (1 - Math.Min(rhythmRepeatNerf(changeRatio), constantRhythmNerf)));

                // Out-of-order overlaps are buffed by difference in index.
                overlapness *= 1 + ((1 - distanceToLastOverlapness) * ((Math.Abs(currentObjVisibleIndex - i) / 2) - 1));

                overlapness *= spacingChange;
                overlapness *= windowFalloff(currentObject.StartTime, visibleObjects[i].StartTime);
                overlapness *= visibleObjects[i].GetVisibilityAtTime(currentObject.StartTime);

                //if (overlapness > 0.5)
                //    Console.WriteLine(Math.Round((visibleObjects[i].StartTime / 1000.0), 3).ToString() + "  " + Math.Round((visibleObjects[w].StartTime / 1000.0), 3).ToString() + "  " + (logistic((18 - visibleObjects[i].NormalisedDistanceTo(visibleObjects[w])) / 5)).ToString());

                overlapnessTotal += Math.Max(0, overlapness);          
            }

            return overlapnessTotal;
        }

        private double calculateAimReading(List<OsuDifficultyHitObject> visibleObjects, OsuDifficultyHitObject currentObject, OsuDifficultyHitObject nextObject)
        {
            var intersections = 0.0;

            var movementDistance = nextObject.JumpDistance;

            // calculate amount of circles intersecting the movement excluding current and next circles
            for (int i = 0; i < visibleObjects.Count; i++)
            {
                if (visibleObjects[i].StartTime < currentObject.StartTime)
                    continue;

                var visibleToCurrentDistance = currentObject.NormalisedDistanceTo(visibleObjects[i]);
                var visibleToNextDistance = nextObject.NormalisedDistanceTo(visibleObjects[i]);

                // scale the bonus by distance of movement and distance between intersected object and movement end object
                var intersectionBonus = checkMovementIntersect(currentObject, nextObject, visibleObjects[i]) *
                                        logistic((movementDistance - 78) / 26) *
                                        logistic((visibleToCurrentDistance - 78) / 26) *
                                        logistic((visibleToNextDistance - 78) / 26) *
                                        visibleObjects[i].GetVisibilityAtTime(currentObject.StartTime) *
                                        nextObject.GetVisibilityAtTime(currentObject.StartTime) *
                                        windowFalloff(currentObject.StartTime, visibleObjects[i].StartTime) *
                                        Math.Abs(visibleObjects[i].StartTime - currentObject.StartTime) / 1000;

                // TODO: approach circle intersections

                intersections += intersectionBonus;
            }

            return intersections;
        }

        private double checkMovementIntersect(OsuDifficultyHitObject currentObject, OsuDifficultyHitObject nextObject, OsuDifficultyHitObject visibleObject)
        {

            Vector2 startCircle = ((OsuHitObject)currentObject.BaseObject).StackedPosition;
            Vector2 endCircle = ((OsuHitObject)nextObject.BaseObject).StackedPosition;
            Vector2 visibleCircle = ((OsuHitObject)visibleObject.BaseObject).StackedPosition;
            double radius = ((OsuHitObject)currentObject.BaseObject).Radius;

            var numerator = Math.Abs( ((endCircle.X - startCircle.X) * (startCircle.Y - visibleCircle.Y)) - ((startCircle.X - visibleCircle.X) * (endCircle.Y - startCircle.Y)));
            var denominator = Math.Sqrt(Math.Pow(endCircle.X - startCircle.X, 2) + Math.Pow(endCircle.Y - startCircle.Y, 2));

            if (double.IsNaN(numerator / denominator))
                return 0;

            return 1 - Math.Min(1, (numerator / denominator) / radius);
        }

        private double rhythmRepeatNerf(double ratio)

        {
            int repeats;

            if (previousRhythmRatios.ContainsKey(ratio))
            {
                repeats = previousRhythmRatios[ratio];
                previousRhythmRatios[ratio]++;
            }
            else
            {
                repeats = 0;
                previousRhythmRatios.Add(ratio, 0);
            }

            return 1.0 - repeats / 4.0;
        }

        private double windowFalloff(double currentTime, double visualTime)
        {
            if (currentTime > visualTime)
                return windowBackwardsFalloff(currentTime, visualTime);
            else if (currentTime < visualTime)
                return windowForwardsFalloff(currentTime, visualTime);

            return 1.0;
        }

        private double windowBackwardsFalloff(double currentTime, double visualTime) => (reading_window_backwards - (currentTime - visualTime)) / reading_window_backwards;

        private double windowForwardsFalloff(double currentTime, double visualTime) => (reading_window_forwards - (visualTime - currentTime)) / reading_window_forwards;

        private double logistic(double x) => 1 / (1 + Math.Pow(Math.E, -x));

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
