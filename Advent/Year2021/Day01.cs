namespace Advent.Year2021 {
    [Day(2021, 1)]
    public class Day01 : DayBase {
        public override async Task<string> PartOne(string input) {

            var depth = 99999; // so the first line isn't an increase...
            var increaseCount = 0;

            foreach (var reading in input.AsInts()) {
                if (reading > depth) {
                    increaseCount++;
                }
                depth = reading;
            }

            return increaseCount.ToString();
        }

        public override async Task<string> PartTwo(string input) {
            var readings = input.AsInts().ToList();

            var depth = 99999; // so the first line isn't an increase...
            var increaseCount = 0;

            for (var i = 0; i <= (readings.Count - 3); i++) {
                var reading = readings[i] + readings[i + 1] + readings[i + 2];

                if (reading > depth) {
                    increaseCount++;
                }
                depth = reading;
            }

            return increaseCount.ToString();
        }
    }
}
