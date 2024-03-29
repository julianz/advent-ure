﻿using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace Advent {
    class Program {
        const string NewDayVerb = "newday";

        static readonly Regex DayPattern = new Regex(@"^(?<daynum>\d+)(?<daypart>[ab])$", RegexOptions.IgnoreCase);
        static readonly Regex YearPattern = new Regex(@"^20[12]\d$");
        static Config Config;

        static async Task Main(string[] args) {

            // Load configuration and sort out current working directory
            var builder = new ConfigurationBuilder()
                .AddJsonFile("settings.json");
            try {
                Config = builder.Build().Get<Config>();
                Config.SanityCheck();

                if (Config.ApplicationDirectory == "") {
                    Config.ApplicationDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
                }
            }
            catch (NullReferenceException) {
                WriteLine("Settings file was not found or is empty");

                return;
            }
            catch (Exception ex) {
                WriteLine($"{ex.GetType()}: {ex.Message}");

                return;
            }

            // Read the command line params
            //  - year is optional, otherwise we use the default from config
            //  - day must be expressed in the form 1a or 12b

            var command = Commands.RunDay;
            var day = new DaySpec { Year = Config.DefaultYear };

            foreach (var arg in args) {
                if (arg.Equals(NewDayVerb, StringComparison.InvariantCultureIgnoreCase)) {
                    command = Commands.NewDay;
                } else if (YearPattern.IsMatch(arg)) {
                    day.Year = Int32.Parse(YearPattern.Match(arg).Value);
                } else if (DayPattern.IsMatch(arg)) {
                    var match = DayPattern.Match(arg);
                    day.Day = Int32.Parse(match.Groups["daynum"].Value);
                    day.Part = match.Groups["daypart"].Value.ToDayPart();
                } else if (Int32.TryParse(arg, out var daynum)) {
                    if (daynum > 0 && daynum <= 25) {
                        day.Day = daynum;
                    }
                }
            }

            if (!day.IsSet) {
                UsageAndExit("Day was not specified on the command line");
            }

            if (command == Commands.RunDay) {
                await RunDay(day);
            } else if (command == Commands.NewDay) {
                await NewDay.Create(day);
            }
        }

        static void UsageAndExit(string message = null) {
            if (message != null) {
                WriteLine(message);
            }

            WriteLine(@"
Advent.exe - Advent of Code puzzle runner

USAGE:
Advent.exe [yyyy] [dd] (a|b)
    - run the puzzle for that year, day and part (A or B)
    - download the puzzle input if it's not there already
Advent.exe newday [yyyy] [dd]
    - create the puzzle class for a new day
");
            Environment.Exit(1);
        }

        static IEnumerable<Type> GetDayTypes() {
            return Assembly.GetExecutingAssembly()
               .ExportedTypes
               .Where(t => t.IsSubclassOf(typeof(DayBase)))
               .Where(t => t.CustomAttributes.Any(a => a.AttributeType == typeof(DayAttribute)));
        }

        static Type GetDayType(DayAttribute day) {
            return GetDayTypes().Single(t => t.GetCustomAttribute<DayAttribute>().Year == day.Year &&
                                             t.GetCustomAttribute<DayAttribute>().Day == day.Day);
        }

        static DayBase GetDayInstance(DaySpec day) {
            try {
                var dayType = GetDayType(new DayAttribute(day.Year, day.Day));
                var instance = (DayBase)Activator.CreateInstance(dayType);
                return instance;
            } catch (InvalidOperationException) {
                // No such day
                return null;
            }
        }

        static async Task<string> GetInputForDay(DaySpec day) {
            // Find out where the input dir is.
            string inputDir = Path.Combine(Config.InputDirectory, day.Year.ToString());

            // Complain if the directory doesn't exist.
            if (!Directory.Exists(inputDir)) {
                throw new DirectoryNotFoundException($"Input directory not found: '{inputDir}'");
            }
            
            var inputPath = Path.Combine(inputDir, $"day{day.Day:D2}.txt");
            WriteLine("Loading input from " + inputPath);

            if (!File.Exists(inputPath)) {
                if (!await DownloadInputForDay(day, inputPath)) {
                    Environment.Exit(1);
                }
            }

            return await File.ReadAllTextAsync(inputPath);
        }

        private static async Task<bool> DownloadInputForDay(DaySpec day, string downloadPath) {

            if (String.IsNullOrWhiteSpace(Config.SessionCookie)) {
                throw new InvalidDataException("You need to put a valid session cookie in the settings.json file");
            }

            var crumbs = Config.SessionCookie.Split("=");
            if (crumbs.Length != 2) {
                throw new InvalidDataException("A valid session cookie is in the form \"session=53616c7465...\"");
            }

            var baseAddress = "https://adventofcode.com";
            var url = $"{baseAddress}/{day.Year}/day/{day.Day}/input";

            // Add the session cookie from config
            var cookies = new CookieContainer();
            cookies.Add(new Uri(baseAddress), new Cookie(crumbs[0], crumbs[1]));
            var handler = new HttpClientHandler { CookieContainer = cookies };

            WriteLine("Downloading input file from " + url);

            try {
                using var client = new HttpClient(handler);
                using var rs = await client.GetStreamAsync(url);
                using var fs = new FileStream(downloadPath, FileMode.Create);

                await rs.CopyToAsync(fs);

            } catch (Exception ex) {
                WriteLine(ex.Message);

                return false;
            }

            return true;
        }

        private static async Task RunDay(DaySpec day) {
            // Create the day object
            var dayInstance = GetDayInstance(day);

            if (dayInstance == null) {
                UsageAndExit($"Code for {day.Year} day {day.Day} could not be found");
            }

            // If we need input for this puzzle, get it
            var input = "";

            if (dayInstance.NeedsInput) {
                input = await GetInputForDay(day);
            }

            try {
                // Run the right day part
                (var result, var elapsed) = await RunDayPart(day, dayInstance, input);

                // Output the results
                WriteLine();
                WriteLine($"ELAPSED: {elapsed.Ticks / 10000.0}ms");
                WriteLine($"RESULT : {result}");
            }
            catch (PuzzleNotSolvedException) {
                WriteLine("PUZZLE NOT SOLVED");
            }
        }


        static async Task<(string result, TimeSpan elapsed)> RunDayPart(DaySpec day, DayBase instance, string input) {
            WriteLine($"Running {day}" + Environment.NewLine);

            string result;
            var sw = Stopwatch.StartNew();

            if (day.Part == DayPart.PartOne) {
                result = await instance.PartOne(input);
            } else {
                result = await instance.PartTwo(input);
            }

            sw.Stop();
            return (result, sw.Elapsed);
        }
    }

    public enum Commands {
        RunDay, // run a puzzle for a day/part
        NewDay  // create a new day solution from a template
    }
}
