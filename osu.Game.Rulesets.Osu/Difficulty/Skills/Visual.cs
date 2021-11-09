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

        private const double rhythm_multiplier = 1.4;
        private const double aim_multiplier = 1.0;

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
            //    Console.WriteLine( Math.Round((current.StartTime / 1000.0), 3).ToString() + "  " + Math.Round(strain, 3).ToString() + "  " + Math.Round(readingDensityStrain, 3).ToString() + "   " + Math.Round(rhythmReadingComplexity, 3).ToString() + "  " + Math.Round(aimReadingComplexity, 3).ToString());

            return strain;
        }

        private double calculateRhythmReading(List<OsuDifficultyHitObject> visibleObjects, OsuDifficultyHitObject prevObject, OsuDifficultyHitObject currentObject, double preempt)
        {
            var overlapnessTotal = 0.0;

            // calculate how much visible objects overlap the previous
            for (int i = 0; i < visibleObjects.Count; i++)
            {
                for (int w = 1; w < i; w++)
                {
                    // lets not consider simultaneous objects
                    if (visibleObjects[i].StartTime == visibleObjects[w].StartTime)
                        continue;

                    var overlapness = 0.0;

                    var tCurrNext = visibleObjects[i].StrainTime;
                    var tPrevCurr = Math.Abs(visibleObjects[w].StartTime - visibleObjects[i].StartTime) / Math.Abs(w - i);
                    var tRatio = Math.Max(tCurrNext / tPrevCurr, tPrevCurr / tCurrNext);

                    var distanceRatio = visibleObjects[i].JumpDistance / (visibleObjects[w].JumpDistance + 1e-10);
                    var changeRatio = Math.Round(distanceRatio * tRatio, 1);
                    var spacingChange = Math.Min(1, Math.Pow(changeRatio - 1, 2) * 1000) * Math.Min(1.0, Math.Pow(distanceRatio - 1, 2) * 1000);

                    overlapness += logistic((18 - visibleObjects[i].NormalisedDistanceTo(visibleObjects[w])) / 5);

                    var distanceToIOverlapness = logistic((128 - visibleObjects[i].JumpDistance) / 5);
                    var distanceToWOverlapness = logistic((172 - visibleObjects[w].JumpDistance) / 5);

                    var tPrevVisibleCurr = visibleObjects[w].StrainTime / visibleObjects[w - 1].StrainTime;
                    var constantRhythmNerf = 1 - Math.Max(0, -100 * Math.Pow((tPrevVisibleCurr) - 1, 2) + 1);

                    var predictableNerfFactor = 1.0;
                    var tDifference = Math.Abs(visibleObjects[w].StartTime - visibleObjects[i].StartTime);
                    var predictedObjTime = visibleObjects[w].StartTime - tDifference;

                    foreach (OsuDifficultyHitObject obj in visibleObjects.AsEnumerable().Reverse())
                    {
                        if (obj.StartTime > visibleObjects[w].StartTime)
                            continue;

                        if (Math.Abs(obj.StartTime - predictedObjTime) < 25)
                        {
                            var objOverlapness = logistic((128 - visibleObjects[w].NormalisedDistanceTo(obj)) / 5);
                            predictableNerfFactor *= 1 - objOverlapness;
                            predictedObjTime = obj.StartTime;
                        }
                    }

                    // Stacked successive objects are only hard to read if there's a rhythm difference.
                    overlapness *= 1 - (Math.Max(distanceToIOverlapness, distanceToWOverlapness) * (1 - Math.Min(rhythmRepeatNerf(changeRatio), constantRhythmNerf)));

                    // Out-of-order overlaps are buffed by difference in index.
                    overlapness *= 1 + predictableNerfFactor * ((1 - Math.Max(distanceToIOverlapness, distanceToWOverlapness)) * ((Math.Abs(i - w) / 2) - 1));

                    overlapness *= windowFalloff(currentObject.StartTime, visibleObjects[i].StartTime);
                    overlapness *= windowFalloff(currentObject.StartTime, visibleObjects[w].StartTime);
                    overlapness *= visibleObjects[i].GetVisibilityAtTime(currentObject.StartTime);
                    overlapness *= visibleObjects[w].GetVisibilityAtTime(currentObject.StartTime);
                    overlapness *= spacingChange;

                    overlapnessTotal += Math.Max(0, overlapness);
                }
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
                                        windowFalloff(currentObject.StartTime, visibleObjects[i].StartTime);

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
