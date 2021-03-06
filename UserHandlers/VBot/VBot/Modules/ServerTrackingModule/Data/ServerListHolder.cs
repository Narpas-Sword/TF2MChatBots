﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace SteamBotLite
{
    public class TrackingServerListHolder : BaseModule, ServerMapChangeListiner
    {
        public Dictionary<string, List<PlayEntry>> MapTests;

        private string[] Header = new string[] { "Map", "Time Played" };

        private IHTMLFileFromArrayPasser listiner;

        private List<Maplist> Maplists;

        public TrackingServerListHolder(ModuleHandler bot, IHTMLFileFromArrayPasser listiner, Dictionary<string, Dictionary<string, object>> Jsconfig) : base(bot, Jsconfig)
        {
            loadPersistentData();
            this.Maplists = new List<Maplist>();
            List<Maplist> Templist = new List<Maplist>();
            Templist = JsonConvert.DeserializeObject<List<Maplist>>(config["ListConfigs"].ToString());
            if (Templist != null)
            {
                this.Maplists = Templist;
            }
            this.listiner = listiner;
            UpdateList();

            bot.AddMapChangeEventListiner(this);
        }

        private enum SummariseMethod
        { Blacklist, Whitelist }

        public void AddEntry(string MapName, PlayEntry Entry)
        {
            if (MapTests.ContainsKey(MapName))
            {
                MapTests[MapName].Add(Entry);
            }
            else
            {
                MapTests.Add(MapName, new List<PlayEntry>() { Entry });
            }
            savePersistentData();
        }

        public string Decompress(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return null;
            }

            byte[] persistantdata = Convert.FromBase64String((data));

            using (MemoryStream input = new MemoryStream(persistantdata))
            using (MemoryStream output = new MemoryStream())
            {
                using (GZipStream dstream = new GZipStream(input, CompressionMode.Decompress))
                {
                    dstream.CopyTo(output);
                }
                return Encoding.Unicode.GetString(output.ToArray());
            }
        }

        public override string getPersistentData()
        {
            byte[] persistantdata = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(MapTests));

            using (MemoryStream mis = new MemoryStream(persistantdata))
            using (MemoryStream output = new MemoryStream())
            {
                using (var dstream = new GZipStream(output, CompressionMode.Compress))
                {
                    mis.CopyTo(dstream);
                }
                return Convert.ToBase64String(output.ToArray());
            }
        }

        public override void loadPersistentData()
        {
            if (File.Exists(ModuleSavedDataFilePath()))
            {
                string data = System.IO.File.ReadAllText(ModuleSavedDataFilePath());
                string DataAsString = Decompress(data);
                if (string.IsNullOrEmpty(DataAsString))
                {
                    MapTests = new Dictionary<string, List<PlayEntry>>();
                }
                else
                {
                    MapTests = JsonConvert.DeserializeObject<Dictionary<string, List<PlayEntry>>>(DataAsString);
                }
            }
            else
            {
                MapTests = new Dictionary<string, List<PlayEntry>>();
            }
        }

        public override void OnAllModulesLoaded()
        {
        }

        public void OnMapChange(TrackingServerInfo args)
        {
            //  Tuple<string,string,string> entry = new Tuple<string, string, string>()
            PlayEntry entry = new PlayEntry(args.playerCount.ToString(), args.serverIP, System.DateTime.Now.ToShortDateString() + " : " + System.DateTime.Now.ToShortTimeString());
            AddEntry(args.currentMap, entry);
            savePersistentData();
            UpdateList();
        }

        private void Export()
        {
            List<string[]> Data = new List<string[]>();
            string[] header = { "MapName", "IP", "Playercount", "TimeEntered" };
            foreach (KeyValuePair<string, List<PlayEntry>> Item in MapTests)
            {
                foreach (PlayEntry PlayCache in Item.Value)
                {
                    string[] DataEntry = { Item.Key, PlayCache.ServerIP, PlayCache.PlayerCount, PlayCache.TimeEntered };
                    Data.Add(DataEntry);
                }

                HTMLFileFromArray HtmlFromArray = new HTMLFileFromArray();
                HtmlFromArray.Data = Data;
                HtmlFromArray.Headernames = header;
                HtmlFromArray.TableKey = "ExportedData";

                listiner.HandleCommand(HtmlFromArray);
            }
        }

        private List<string[]> ParseSummarisedListToHTMLTable(Dictionary<string, int> Dictionary)
        {
            List<string[]> Array = new List<string[]>();
            foreach (KeyValuePair<string, int> entry in Dictionary)
            {
                Array.Add(new string[] { entry.Key, entry.Value.ToString() });
            }
            return Array;
        }

        /// <summary>
        /// Used to summarise the Dictionary but only return entries in the whitelist or not in the blacklist
        /// </summary>
        /// <param name="Dictionary"></param>
        /// <param name="Filter"></param>
        /// <returns></returns>
        private Dictionary<string, int> SummariseEntries(Dictionary<string, List<PlayEntry>> Dictionary, List<string> Filter, SummariseMethod MethodToSummariseWith)
        {
            //Assign Values for the Boolean
            bool SummariseMethod;

            if (MethodToSummariseWith.Equals(TrackingServerListHolder.SummariseMethod.Whitelist))
            {
                SummariseMethod = true;
            }
            else
            {
                SummariseMethod = false;
            }

            Dictionary<string, int> SumamrisedDictionary = new Dictionary<string, int>();

            foreach (KeyValuePair<string, List<PlayEntry>> Item in Dictionary)
            {
                if ((Filter.Contains(Item.Key) == (SummariseMethod))) //If we are using a whitelist, the bool will be "True" and if the list contains it, it'll be true, so it will add. Inversely for the blacklist.
                {
                    SumamrisedDictionary.Add(Item.Key, Item.Value.Count);
                }
            }
            return SumamrisedDictionary;
        }

        private void UpdateList()
        {
            foreach (Maplist entry in Maplists)
            {
                HTMLFileFromArray HtmlFromArray = new HTMLFileFromArray();
                HtmlFromArray.Data = ParseSummarisedListToHTMLTable(SummariseEntries(MapTests, entry.Maps, entry.ListKind));
                HtmlFromArray.Headernames = Header;
                HtmlFromArray.TableKey = entry.ListName;

                listiner.HandleCommand(HtmlFromArray);
            }
        }

        public class PlayEntry
        {
            public PlayEntry(string playercount, string serverip, string timeentered)
            {
                this.PlayerCount = playercount;
                this.ServerIP = serverip;
                this.TimeEntered = timeentered;
            }

            public string PlayerCount { get; set; }
            public string ServerIP { get; set; }
            public string TimeEntered { get; set; }
        }

        private class Maplist
        {
            public SummariseMethod ListKind;
            public string ListName;
            public List<string> Maps;
        }
    }
}