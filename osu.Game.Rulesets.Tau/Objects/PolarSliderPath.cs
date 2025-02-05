﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using osu.Framework.Bindables;
using osu.Framework.Caching;

namespace osu.Game.Rulesets.Tau.Objects
{
    public class PolarSliderPath
    {
        [JsonIgnore]
        public IBindable<int> Version => version;

        private readonly Bindable<int> version = new();
        public readonly Bindable<double?> ExpectedDistance = new();

        public double Duration => Nodes.Max(n => n.Time);
        public SliderNode EndNode => Nodes.LastOrDefault();

        public readonly List<SliderNode> Nodes = new();
        private readonly Cached pathCache = new();

        private double calculatedLength;

        public PolarSliderPath(SliderNode[] nodes, double? expectedDistance = null)
        {
            Nodes.AddRange(nodes);
            ExpectedDistance.Value = expectedDistance;
        }

        /// <summary>
        /// The distance of the path prior to lengthening/shortening to account for <see cref="ExpectedDistance"/>.
        /// </summary>
        public double CalculatedDistance
        {
            get
            {
                ensureValid();
                return calculatedLength;
            }
        }

        /// <summary>
        /// Gets a new list of <see cref="SliderNode"/> between the start and end time.
        /// </summary>
        /// <param name="start">The start time to collect nodes.</param>
        /// <param name="end">The end time to collect nodes.</param>
        public Span<SliderNode> NodesBetween(float start, float end)
        {
            int? index = null;
            int? length = null;

            for (var i = 0; i < Nodes.Count; i++)
            {
                var node = Nodes[i];
                if (node.Time < start)
                    continue;

                if (index == null)
                {
                    index = i;
                    length = 0;
                }

                if (node.Time > end)
                    break;

                length = (i + 1) - index;
            }

            if (index is null or 0)
                return Span<SliderNode>.Empty;

            return CollectionsMarshal.AsSpan(Nodes).Slice((int)index, (int)length);
        }

        /// <summary>
        /// Interpolates the list of <see cref="Nodes"/> and returns the current angle at the set time.
        /// </summary>
        /// <param name="time">The time to get the angle at.</param>
        public float AngleAt(float time)
        {
            var last = Nodes.LastOrDefault();

            if (time <= 0)
                return Nodes.First().Angle;

            if (time >= last.Time)
                return last.Angle;

            var closest = Nodes.OrderBy(n => Math.Abs(time - n.Time)).ToArray();
            var start = closest[0];
            var end = closest[1];

            var index = Nodes.BinarySearch(start);

            if (index == Nodes.Count)
                return start.Angle;

            var deltaAngle = Extensions.GetDeltaAngle(end.Angle, start.Angle);
            var duration = end.Time - start.Time;

            return start.Angle + deltaAngle * (time - start.Time) / duration;
        }

        private void ensureValid()
        {
            if (pathCache.IsValid)
                return;

            calculateLength();

            pathCache.Validate();
        }

        private void calculateLength()
        {
            calculatedLength = 0;

            if (Nodes.Count <= 0)
                return;

            (float angle, float sum) result = (angle: Nodes[0].Angle, sum: 0f);

            foreach (var node in Nodes)
            {
                result.sum += Math.Abs(Extensions.GetDeltaAngle(result.angle, node.Angle));
                result.angle = node.Angle;
            }

            calculatedLength = result.sum;
        }
    }

    public readonly struct SliderNode : IComparable<SliderNode>
    {
        public float Time { get; }

        public float Angle { get; }

        public SliderNode(float time, float angle)
        {
            Time = time;
            Angle = angle;
        }

        public int CompareTo(SliderNode other) => Time.CompareTo(other.Time);

        public override string ToString() => $"T: {Time} | A: {Angle}";
    }
}
