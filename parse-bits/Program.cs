using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace parse_bits
{
    class Program
    {
        static int MaxChapter = 18;
        static int MaxStaminaCost = 12;
        static int[] Points = new[] { 0, 0, 0, 1500, 1280, 0 };
        static void Main(string[] args)
        {
            // https://wakkatu.github.io/dhero/en/item_list.htm
            var req = WebRequest.CreateHttp("https://wakkatu.github.io/dhero/en/item_list.htm");
            var res = req.GetResponse();
            var reader = new StreamReader(res.GetResponseStream());
            var input = reader.ReadToEnd().Split('\n');

            var levels = new List<Level>();

            var addingCampaigns = false;
            Reward currentBit = null;
            for (var i = 0; i < input.Length; i++)
            {
                var line = input[i];
                if (line.Contains("item-row-SHARD_"))
                {
                    var rarity = ParseRarity(input[i + 1]);
                    var name = ParseName(input[i + 4]);
                    i += 9;
                    addingCampaigns = true;
                    currentBit = new Reward { Name = name, Rarity = rarity };
                }
                else if (addingCampaigns && line.Contains("<li>Campaign "))
                {
                    int chapter = 0;
                    int station = 0;
                    ParseCampaign(line, out chapter, out station);
                    if (chapter > MaxChapter) continue;

                    var level = levels.FirstOrDefault(l => l.ChapterNumber == chapter && l.StationNumber == station);
                    if (level == null)
                    {
                        level = new Level { ChapterNumber = chapter, StationNumber = station, StaminaCost = GetStamCostForChapter(chapter) };
                        levels.Add(level);
                    }
                    level.Rewards.Add(currentBit);
                    var totalPointsForLevel = level.Rewards.Sum(r => Points[(int)r.Rarity]);
                    var pointsPerRaid = totalPointsForLevel / 4;
                    var pointsPerStamina = pointsPerRaid / level.StaminaCost;
                    level.ContestPointsPerStamina = pointsPerStamina;
                }
                else
                {
                    addingCampaigns = false;
                }
            }

            foreach (var level in levels.OrderBy(l => l.ContestPointsPerStamina).ThenBy(l => l.ChapterNumber).ThenBy(l => l.StationNumber))
            {
                Console.WriteLine($"{level.ChapterNumber}-{level.StationNumber} PPS: {level.ContestPointsPerStamina} [Orange: {level.Rewards.Count(r => r.Rarity == Rarity.Orange)}, Purple: {level.Rewards.Count(r => r.Rarity == Rarity.Purple)}]");
            }

            Console.ReadLine();
        }

        static int GetStamCostForChapter(int c)
        {
            if (c > MaxChapter) return int.MaxValue;

            return Math.Max(6, MaxStaminaCost - (2 * (MaxChapter - c)));
        }

        static Rarity ParseRarity(string line)
        {
            if (line.Contains("rarity-orange")) return Rarity.Orange;
            if (line.Contains("rarity-purple")) return Rarity.Purple;
            if (line.Contains("rarity-blue")) return Rarity.Blue;
            if (line.Contains("rarity-green")) return Rarity.Green;
            if (line.Contains("rarity-red")) return Rarity.Red;
            return Rarity.White;
        }

        static string ParseName(string line)
        {
            var parts = line.Split(new[] { '<', '>' }, StringSplitOptions.RemoveEmptyEntries);
            return parts[3];
        }

        static void ParseCampaign(string line, out int chapter, out int station)
        {
            var parts = line.Split(new[] { '<', '>' }, StringSplitOptions.RemoveEmptyEntries);
            var parts2 = parts[2].Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            chapter = int.Parse(parts2[1]);
            station = int.Parse(parts2[2]);
        }
    }

    public class Level
    {
        public int ChapterNumber { get; set; }
        public int StaminaCost { get; set; }
        public int StationNumber { get; set; }
        public int ContestPointsPerStamina { get; set; }
        public List<Reward> Rewards { get; set; }

        public Level()
        {
            Rewards = new List<Reward>();
        }
    }

    public class Reward
    {
        public string Name { get; set; }
        public Rarity Rarity { get; set; }

        public override string ToString()
        {
            return $"{Name} ({Rarity})";
        }
    }

    public enum Rarity
    {
        White = 0,
        Green,
        Blue,
        Purple,
        Orange,
        Red
    }
}
