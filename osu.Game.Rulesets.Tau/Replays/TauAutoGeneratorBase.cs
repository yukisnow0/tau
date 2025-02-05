﻿using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.Tau.UI;

namespace osu.Game.Rulesets.Tau.Replays
{
    public abstract class TauAutoGeneratorBase : AutoGenerator
    {
        #region Constants

        protected readonly float Offset = TauPlayfield.BaseSize.Y / 2f;
        protected const float CURSOR_DISTANCE = 250;

        #endregion

        #region Construction / Initialization

        protected Replay Replay;
        protected List<ReplayFrame> Frames => Replay.Frames;
        private readonly IReadOnlyList<IApplicableToRate> timeAffectingMods;

        protected TauAutoGeneratorBase(IBeatmap beatmap, IReadOnlyList<Mod> mods)
            : base(beatmap)
        {
            Replay = new Replay();
            timeAffectingMods = mods.OfType<IApplicableToRate>().ToList();
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Returns the real duration of time between <paramref name="startTime"/> and <paramref name="endTime"/>
        /// after applying rate-affecting mods.
        /// </summary>
        /// <remarks>
        /// This method should only be used when <paramref name="startTime"/> and <paramref name="endTime"/> are very close.
        /// That is because the track rate might be changing with time,
        /// and the method used here is a rough instantaneous approximation.
        /// </remarks>
        /// <param name="startTime">The start time of the time delta, in original track time.</param>
        /// <param name="endTime">The end time of the time delta, in original track time.</param>
        protected double ApplyModsToTimeDelta(double startTime, double endTime)
        {
            double delta = endTime - startTime;

            return timeAffectingMods.Aggregate(delta, (current, mod) => current / mod.ApplyToRate(startTime));
        }

        protected double ApplyModsToRate(double time, double rate)
            => timeAffectingMods.Aggregate(rate, (current, mod) => mod.ApplyToRate(time, current));

        /// <summary>
        /// Calculates the interval after which the next <see cref="ReplayFrame"/> should be generated,
        /// in milliseconds.
        /// </summary>
        /// <param name="time">The time of the previous frame.</param>
        protected double GetFrameDelay(double time)
            => ApplyModsToRate(time, 1000.0 / 60);

        private class ReplayFrameComparer : IComparer<ReplayFrame>
        {
            public int Compare(ReplayFrame f1, ReplayFrame f2)
            {
                if (f1 == null) throw new ArgumentNullException(nameof(f1));
                if (f2 == null) throw new ArgumentNullException(nameof(f2));

                return f1.Time.CompareTo(f2.Time);
            }
        }

        private static readonly IComparer<ReplayFrame> replay_frame_comparer = new ReplayFrameComparer();

        protected int FindInsertionIndex(ReplayFrame frame)
        {
            int index = Frames.BinarySearch(frame, replay_frame_comparer);

            if (index < 0)
            {
                index = ~index;
            }
            else
            {
                // Go to the first index which is actually bigger
                while (index < Frames.Count && frame.Time == Frames[index].Time)
                {
                    ++index;
                }
            }

            return index;
        }

        protected void AddFrameToReplay(ReplayFrame frame) => Frames.Insert(FindInsertionIndex(frame), frame);

        #endregion
    }
}
