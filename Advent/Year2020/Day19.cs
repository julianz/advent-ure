using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using MoreLinq;

using Advent.Util;

namespace Advent.Year2020 {
    [Day(2020, 19)]
    public class Day19 : DayBase {
        public override string PartOne(string input) {
            var sections = input.SplitOnBlankLines().ToList();
            
            Dictionary<string, string> rules = new();
            foreach (var line in sections[0].AsLines()) {
                var bits = line.Split(":");
                rules[bits[0]] = bits[1].Trim().Trim('\"'); // strip quotes from the individual letters
            }

            var messages = sections[1].AsLines().ToList();

            // Convert the input into a regex
            var expr = Expand(rules["0"], rules);
            //Out.Print(expr);
            var re = new Regex(@$"^{expr}$", RegexOptions.IgnorePatternWhitespace);

            return messages.Count(m => re.IsMatch(m)).ToString();
        }

        public override string PartTwo(string input) {
            throw new PuzzleNotSolvedException();
        }

        string Expand(string rule, Dictionary<string, string> rules) {
            if ("ab".Contains(rule)) {
                return rule;
            } else if (rule.Contains("|")) {
                return "( " + String.Join(" | ", rule.Split(" | ").Select(r => Expand(r, rules))) + " )";
            } else {
                return String.Join("", rule.Split().Select(id => Expand(rules[id], rules)));
            }
        }
    }
}
