﻿using static MoreLinq.Extensions.ForEachExtension;

namespace Advent.Year2020 {
    [Day(2020, 7)]
    public class Day07 : DayBase {
        Dictionary<string, HashSet<string>> Bags = new();
        readonly HashSet<string> Containers = new();

        public override async Task<string> PartOne(string input) {
            //input = @"light red bags contain 1 bright white bag, 2 muted yellow bags.
            //        dark orange bags contain 3 bright white bags, 4 muted yellow bags.
            //        bright white bags contain 1 shiny gold bag.
            //        muted yellow bags contain 2 shiny gold bags, 9 faded blue bags.
            //        shiny gold bags contain 1 dark olive bag, 2 vibrant plum bags.
            //        dark olive bags contain 3 faded blue bags, 4 dotted black bags.
            //        vibrant plum bags contain 5 faded blue bags, 6 dotted black bags.
            //        faded blue bags contain no other bags.
            //        dotted black bags contain no other bags.";

            var rules = input.AsLines();
            foreach (var rule in rules) {
                var split = rule.Split("bags contain", StringSplitOptions.TrimEntries);
                var containerColour = split[0];
                var contained = split[1].Split(",", StringSplitOptions.TrimEntries);

                if (contained.Length == 1 && contained[0].StartsWith("no other bags")) {
                    // not interested
                    continue;
                }

                foreach (var bag in contained) {
                    var bagSplit = bag.Split(" ");
                    var bagColour = bagSplit[1] + " " + bagSplit[2];

                    if (!Bags.ContainsKey(bagColour)) {
                        Bags[bagColour] = new HashSet<string>();
                    }

                    Bags[bagColour].Add(containerColour);
                }
            }

            CountContainers("shiny gold");
            WriteLine();
            Containers.ForEach(c => WriteLine(c));
            return Containers.Count.ToString();
        }

        Dictionary<string, Dictionary<string, int>> Contains = new Dictionary<string, Dictionary<string, int>>();

        public override async Task<string> PartTwo(string input) {
            //input = @"light red bags contain 1 bright white bag, 2 muted yellow bags.
            //        dark orange bags contain 3 bright white bags, 4 muted yellow bags.
            //        bright white bags contain 1 shiny gold bag.
            //        muted yellow bags contain 2 shiny gold bags, 9 faded blue bags.
            //        shiny gold bags contain 1 dark olive bag, 2 vibrant plum bags.
            //        dark olive bags contain 3 faded blue bags, 4 dotted black bags.
            //        vibrant plum bags contain 5 faded blue bags, 6 dotted black bags.
            //        faded blue bags contain no other bags.
            //        dotted black bags contain no other bags.";

            var rules = input.AsLines();

            foreach (var rule in rules) {
                var split = rule.Split("bags contain", StringSplitOptions.TrimEntries);
                var containerColour = split[0];
                var contained = split[1].Split(",", StringSplitOptions.TrimEntries);

                if (contained.Length == 1 && contained[0].StartsWith("no other bags")) {
                    // not interested
                    continue;
                }

                foreach (var bag in contained) {
                    var bagSplit = bag.Split(" ");
                    var bagCount = Int32.Parse(bagSplit[0]);
                    var bagColour = bagSplit[1] + " " + bagSplit[2];

                    if (!Contains.ContainsKey(containerColour)) {
                        Contains[containerColour] = new Dictionary<string, int>();
                    }

                    Contains[containerColour][bagColour] = bagCount;
                }
            }

            return CountContained("shiny gold").ToString();
        }

        void CountContainers(string colour) {
            WriteLine(colour);
            if (Bags.ContainsKey(colour)) {
                foreach (var container in Bags[colour]) {
                    Containers.Add(container);
                    CountContainers(container);
                }
            }
        }

        int CountContained(string colour) {
            if (Contains.ContainsKey(colour)) {
                var count = 0;

                var bags = Contains[colour];
                foreach (var bag in bags.Keys) {
                    count += bags[bag];
                    count += bags[bag] * CountContained(bag);
                }

                return count;
            }
            return 0;
        }
    }
}
