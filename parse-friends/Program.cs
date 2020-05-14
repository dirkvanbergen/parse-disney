using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace parse_friends
{
    class Program
    {
        private static List<KeyValuePair<string, string>> Missions;
        private static List<KeyValuePair<string, string>> Friendships;

        private static List<string> IgnoreHeroes = new List<string> { "", "Tron", "Animal", "Miss Piggy", "Randall Boggs", "Linguini & Remy", "Powerline", "Hank & Dory", "Gerald, Marlin, & Nemo", "Tigger", "Winnie the Pooh", "Belle", "Eeyore", "Goliath", "The Evil Queen", "Syndrome" };
        private static List<KeyValuePair<string, string>> IgnoreFriendships = new List<KeyValuePair<string, string>> {
            new KeyValuePair<string, string>("Gonzo", "Anger"),
            new KeyValuePair<string, string>("Gizmoduck", "Launchpad McQuack"),
            new KeyValuePair<string, string>("Flynn Rider", "Aladdin"),
            new KeyValuePair<string, string>("Megavolt", "Finnick"),
            new KeyValuePair<string, string>("Kida", "Hercules"),
            new KeyValuePair<string, string>("Kida", "Jasmine"),
            new KeyValuePair<string, string>("Kristoff & Sven", "Kida"),
            new KeyValuePair<string, string>("Kristoff & Sven", "Flynn Rider"),
        };

        static void Main(string[] args)
        {
            // https://wakkatu.github.io/dhero/en/disk_list.htm
            var req = WebRequest.CreateHttp("https://wakkatu.github.io/dhero/en/disk_list.htm");
            var res = req.GetResponse();
            var reader = new StreamReader(res.GetResponseStream());
            var content = reader.ReadToEnd().Split('\n'); ;

            Friendships = ParseFile(content);
            Missions = new List<KeyValuePair<string, string>>();
            OptimizeMissions();

            var i = 1;
            foreach (var couple in Missions)
            {
                Console.WriteLine("{2}: {0} - {1}", couple.Key, couple.Value, i++);
            }
        }

        private static List<KeyValuePair<string, string>> AvailableFriendships()
        {
            var usedHeroes = Missions.SelectMany(m => new[] { m.Key, m.Value }).ToList();
            return Friendships.Where(f => (usedHeroes.IndexOf(f.Key) + usedHeroes.IndexOf(f.Value)) < 0).ToList();
        }

        private static List<string> LeastAvailableHero()
        {
            var counts = new Dictionary<string, int>();
            foreach (var hero in AvailableFriendships().SelectMany(f => new[] { f.Key, f.Value }))
            {
                if (counts.ContainsKey(hero))
                {
                    counts[hero]++;
                }
                else
                {
                    counts.Add(hero, 1);
                }
            }
            return counts.OrderBy(x => x.Value).Select(x => x.Key).ToList();
        }

        private static List<string> GetFriends(string hero, bool onlyAvailableFriends = false)
        {
            if (onlyAvailableFriends)
            {
                var friendshipsByKey = AvailableFriendships().Where(f => f.Key == hero).Select(f => f.Value).ToList();
                var friendshipsByVal = AvailableFriendships().Where(f => f.Value == hero).Select(f => f.Key).ToList();
                return friendshipsByKey.Union(friendshipsByVal).ToList();
            }
            var allFriendshipsByKey = Friendships.Where(f => f.Key == hero).Select(f => f.Value).ToList();
            var allFriendshipsByVal = Friendships.Where(f => f.Value == hero).Select(f => f.Key).ToList();
            return allFriendshipsByKey.Union(allFriendshipsByVal).ToList();
        }

        private static void OptimizeMissions()
        {
            while (AvailableFriendships().Count > 0)
            {
                var hero = LeastAvailableHero().First();
                var friends = GetFriends(hero, true);
                var friend = LeastAvailableHero().First(h => friends.Contains(h));
                if (Friendships.Any(f => f.Key == hero && f.Value == friend))
                {
                    Missions.Add(new KeyValuePair<string, string>(hero, friend));
                }
                else
                {
                    Missions.Add(new KeyValuePair<string, string>(friend, hero));
                }
            }
        }

        private static List<KeyValuePair<string, string>> ParseFile(string[] content)
        {
            var readDisk = false;
            var diskLines = new List<string>();
            var friends = new List<KeyValuePair<string, string>>();
            var friendExpr = ">([a-zA-Z\\s-&;.,]*)<";
            foreach (var line in content)
            {
                if (line.Contains(" Disk { "))
                {
                    readDisk = true;
                    continue;
                }
                if (line.Contains(" Disk } "))
                {
                    if (diskLines.Count > 1)
                    {
                        var hero = diskLines[0];
                        var friend = diskLines[1];
                        if (!IgnoreFriendships.Any(f => f.Key == hero && f.Value == friend))
                        {
                            friends.Add(new KeyValuePair<string, string>(hero, friend));
                        }
                    }
                    diskLines.Clear();
                    readDisk = false;
                    continue;
                }
                if (readDisk && line.Contains("<a href=\"./hero/"))
                {
                    var match = Regex.Match(line, friendExpr);
                    var hero = match.Groups[1].Value.Replace("&amp;", "&");
                    if (IgnoreHeroes.Any(h => hero == h)) continue;
                    diskLines.Add(hero);
                }
            }

            return friends;
        }


    }
}
