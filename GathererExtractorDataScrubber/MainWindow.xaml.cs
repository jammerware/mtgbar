﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Xml.Linq;
using Bazam.Modules;
using Bazam.Slugging;
using Melek;
using Melek.Models;

namespace GathererExtractorDataScrubber
{
    public partial class MainWindow : Window
    {
        private const int SOFT_HYPHEN_CODE = 8722;

        private Dictionary<string, string> _SetCodeReplacements = new Dictionary<string, string>() {
            { "AL", "ALL" },
            { "AN", "ARN" },
            { "AP", "APC"},
            { "AQ", "ATQ"},
            { "AVB", "DDH" },
            { "BD", "BTD" },
            { "BR", "BRB" },
            { "CH", "CHR" },
            { "CM1", "CMA"},
            { "CRS", "CMA"},
            { "CS", "CSP" },
            { "DK", "DRK" },
            { "DS", "DST" },
            { "DVD", "DDC" },
            { "EVT", "DDF" },
            { "FD", "5DN" },
            { "GP", "GPT" },
            { "GVL", "DDD" },
            { "HVM", "DDL" },
            { "IA", "ICE"},
            { "IVG", "DDJ" },
            { "JVC", "DD2" },
            { "KVD", "DDG" },
            { "MI", "MIR" },
            { "MM", "MMQ" },
            { "MR", "MRD" },
            { "NE", "NMS" },
            { "ON", "ONS" },
            { "P02", "PO2" },
            { "PVC", "DDE" },
            { "SVT", "DDK" },
            { "UD", "UDS" },
            { "UG", "UGL" },
            { "UL", "ULG" },
            { "US", "USG" },
            { "VVK", "DDI" },
        };

        // because i refuse to call the first commander precon set "Magic: the Gathering-Commander"
        private Dictionary<string, string> _SetNames = new Dictionary<string, string>() {
            { "CMD", "Commander" }
        };

        // this is for channel fireball - in the case of long set names, they abbreviate them (i.e. Betrayers of Kamigaway => Betrayers). It won't crash the app or anything
        // if they're not there, but getting price data fails. I can just updates these as I notice them.
        private Dictionary<string, string> _CFSetNames = new Dictionary<string, string>() {
            { "BOK", "Betrayers" },
            { "C13", "Commander 2013" },
            { "CMD", "Commander" },
            { "COK", "Champions" },
            { "SOK", "Saviors" }
        };

        // similar for TCGPlayer.com
        private Dictionary<string, string> _TCGPlayerSetNames = new Dictionary<string, string>() {
            { "C13", "Commander 2013" },
            { "CMD", "Commander" },
            { "M10", "magic 2010 (m10)" },
            { "M11", "magic 2011 (m11)" },
            { "M12", "magic 2012 (m12)" },
            { "M13", "magic 2013 (m13)" },
            { "M14", "magic 2014 (m14)" },
            { "m15", "magic 2015 (m15)" }
        };

        public MainWindow()
        {
            InitializeComponent();
        }

        private string GetCardTribe(string input)
        {
            if (input.IndexOf('—') >= 0) {
                input = input.Substring(input.IndexOf('—') + 1);
            }
            return input.Trim();
        }

        private CardType[] GetCardTypes(string input)
        {
            List<CardType> retVal = new List<CardType>();
            input = input.ToUpper();

            if (input.IndexOf('—') >= 0) {
                input = input.Substring(0, input.IndexOf('—'));
            }
            string[] splitInput = input.Split(new Char[] { ' ', '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (input.Contains("ENCHANT ")) {
                retVal.Add(CardType.ENCHANTMENT);
            }
            else if (input.Contains("SCHEME")) {
                retVal.Add(CardType.SCHEME);
            }
            else {
                foreach (string inputPiece in splitInput) {
                    if (inputPiece == "EATURECRAY") {
                        retVal.Add(CardType.CREATURE);
                    }
                    else if (inputPiece == "INTERRUPT") {
                        retVal.Add(CardType.INSTANT);
                    }
                    else {
                        retVal.Add(EnuMaster.Parse<CardType>(inputPiece.ToUpper().Trim()));
                    }
                }
            }

            return retVal.ToArray();
        }

        private DateTime? GetSetDate(string input)
        {
            DateTime? retVal = null;
            DateTime parseTarget;
            if (DateTime.TryParse(input, out parseTarget)) {
                retVal = new DateTime?(parseTarget);
            }
            else {
                Match match = Regex.Match(input, "([0-9]{2})/([0-9]{4})");
                int month, year;
                Int32.TryParse(match.Groups[1].Value, out month);
                Int32.TryParse(match.Groups[2].Value, out year);

                if (month > 0 && year > 0) {
                    retVal = new DateTime?(new DateTime(year, month, 1));
                }
            }

            return retVal;
        }

        private CardRarity GetRarity(string input)
        {
            try {
                return EnuMaster.Parse<CardRarity>(input[0].ToString());
            }
            catch (Exception) {
                return CardRarity.C;
            }
        }

        private Card GetCard(string id, string rarity, string setCode, string artist, string cardTypes, string cost, string flavor, string name, string power, string text, string toughness, string tribeData, string watermark, Dictionary<string, Set> setDictionary)
        {
            int dummyForOutParams = 0;

            // With the gatherer update in 2014, Wizards stopped using hard hyphens (the subtraction sign on every keyboard) and instead
            // replaced them with ASCII character 8722, which http://www.ascii.cl/htmlcodes.htm describes as a "soft hyphen." BECAUSE
            // THAT FUCKING MAKES SENSE. How do they even enter the data in the DB with a character that isn't on a keyboard?
            // ... GOD. Just replace them with the normal hyphen sign, which was good enough for our parents.
            text = text.Replace((char)SOFT_HYPHEN_CODE, '-');

            return new Card() {
                Appearances = new List<CardAppearance>() { 
                    new CardAppearance() {
                        Artist = artist,
                        FlavorText = flavor,
                        MultiverseID = id,
                        Rarity = GetRarity(rarity),
                        Set = setDictionary[setCode],
                    }
                },
                CardTypes = GetCardTypes(cardTypes),
                Cost = (!string.IsNullOrEmpty(cost) ? new CardCostCollection(cost) : null),
                FlavorText = flavor,
                Name = name,
                Power = (Int32.TryParse(power, out dummyForOutParams) ? (int?)Int32.Parse(power) : null),
                Text = text,
                Toughness = (Int32.TryParse(toughness, out dummyForOutParams) ? (int?)Int32.Parse(toughness) : null),
                Tribe = GetCardTribe(tribeData),
                Watermark = watermark
            };
        }

        private string GetSplitCardName(string name, bool firstHalf)
        {
            return name + " (" + name.Split("//".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[firstHalf ? 0 : 1].Trim() + ")";
        }

        private string GetSplitCardValue(string input, bool firstHalf)
        {
            Match match = Regex.Match(input, "([\\s\\S]+)//([\\s\\S]+)");
            if (match.Value != string.Empty) {
                return (firstHalf ? match.Groups[1].Value : match.Groups[2].Value).Trim();
            }
            return input;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            // init some things
            Dictionary<string, Set> setDictionary = new Dictionary<string, Set>();
            List<XElement> setElements = new List<XElement>();
            List<XElement> cardElements = new List<XElement>();
            
            XDocument doc = XDocument.Load("Data/database.xml");

            IEnumerable<Set> sets = (
                from set in doc.Root.Element("sets").Elements("set")
                select new Set() {
                    Code = XMLPal.GetString(set.Element("code")),
                    Date = GetSetDate(XMLPal.GetString(set.Element("date"))),
                    Name = (_SetNames.Keys.Contains(XMLPal.GetString(set.Element("code"))) ? _SetNames[XMLPal.GetString(set.Element("code"))] : XMLPal.GetString(set.Element("name"))),
                }
            );

            foreach (Set set in sets.OrderBy(s => s.Name)) {
                if (_SetCodeReplacements.Keys.Contains(set.Code)) {
                    set.Code = _SetCodeReplacements[set.Code];
                }

                if (_CFSetNames.Keys.Contains(set.Code)) {
                    set.CFName = _CFSetNames[set.Code];
                }

                if (_TCGPlayerSetNames.Keys.Contains(set.Code)) {
                    set.TCGPlayerName = _TCGPlayerSetNames[set.Code];
                }

                setDictionary.Add(set.Code, set);
                setElements.Add(
                    new XElement(
                        "set",
                        new XAttribute("name", set.Name),
                        new XAttribute("code", set.Code),
                        (string.IsNullOrEmpty(set.CFName) ? null : new XAttribute("cfName", set.CFName)),
                        (string.IsNullOrEmpty(set.TCGPlayerName) ? null : new XAttribute("tcgPlayerName", set.TCGPlayerName)),
                        (set.Date == null ? null : new XAttribute("date", set.Date))
                    )
                );
            }

            if (cbGathererExtractor.IsChecked.Value) {
                Dictionary<string, Card> cards = new Dictionary<string, Card>();

                // pass to load card data
                foreach (XElement card in doc.Root.Element("cards").Elements("card")) {
                    // read some generally useful things
                    string name = XMLPal.GetString(card.Element("name"));
                    string sluggedName = Slugger.Slugify(name);
                    string setCode = card.Element("set").Value;
                    if (_SetCodeReplacements.Keys.Contains(setCode)) {
                        setCode = _SetCodeReplacements[setCode];
                    }

                    // we used the sluggedName variable to check if this card has been loaded before. if this is a split card, we can just check one half to see if it's been loaded before.
                    if(name.Contains("//")) {
                        sluggedName = Slugger.Slugify(GetSplitCardName(name, true));
                    }

                    // first we need to know if this card has already been loaded (because it's in another set), in which case we need to 
                    // add an appearance to the existing card for the current set. we can do this by checking its name, except the split
                    // cards need to be checked twice (once for each half).
                    if (cards.Keys.Contains(sluggedName)) {
                        // create the appearance, we'll need it no matter what
                        CardAppearance appearance = new CardAppearance() {
                            Artist = XMLPal.GetString(card.Element("artist")),
                            FlavorText = XMLPal.GetString(card.Element("flavor")),
                            MultiverseID = XMLPal.GetString(card.Element("id")),
                            Rarity = GetRarity(XMLPal.GetString(card.Element("rarity"))),
                            Set = setDictionary[setCode]
                        };

                        if (!name.Contains("//")) {
                            // normal cards
                            cards[sluggedName].Appearances.Add(appearance);
                        }
                        else {
                            // split cards
                            string turnsSluggedName = sluggedName;
                            string burnsSluggedName = Slugger.Slugify(GetSplitCardName(name, false));

                            cards[turnsSluggedName].Appearances.Add(appearance);
                            cards[burnsSluggedName].Appearances.Add(appearance);
                        }
                    }
                    else {
                        // either it's a regular card or a split card
                        // parse data into variables for manipulation in the case of a split card
                        string id = XMLPal.GetString(card.Element("id"));
                        string rarity = XMLPal.GetString(card.Element("rarity"));
                        string artist = XMLPal.GetString(card.Element("artist"));
                        string types = XMLPal.GetString(card.Element("type"));
                        string cost = XMLPal.GetString(card.Element("manacost"));
                        string flavor = XMLPal.GetString(card.Element("flavor"));
                        string power = XMLPal.GetString(card.Element("power"));
                        string text = XMLPal.GetString(card.Element("ability"));
                        string toughness = XMLPal.GetString(card.Element("toughness"));
                        string tribe = types;
                        string watermark = XMLPal.GetString(card.Element("watermark"));

                        if (!name.Contains("//")) {
                            cards.Add(Slugger.Slugify(name), GetCard(id, rarity, setCode, artist, types, cost, flavor, name, power, text, toughness, tribe, watermark, setDictionary));
                        }
                        else {
                            string turnsArtist = GetSplitCardValue(artist, true);
                            string burnsArtist = GetSplitCardValue(artist, false);
                            string turnsTypes = GetSplitCardValue(types, true);
                            string burnsTypes = GetSplitCardValue(types, false);
                            string turnsCost = GetSplitCardValue(cost, true);
                            string burnsCost = GetSplitCardValue(cost, false);
                            string turnsFlavor = GetSplitCardValue(flavor, true);
                            string burnsFlavor = GetSplitCardValue(flavor, false);
                            string turnsName = GetSplitCardName(name, true);
                            string burnsName = GetSplitCardName(name, false);
                            string turnsPower = GetSplitCardValue(power, true);
                            string burnsPower = GetSplitCardValue(power, false);
                            string turnsText = GetSplitCardValue(text, true);
                            string burnsText = GetSplitCardValue(text, false);
                            string turnsToughness = GetSplitCardValue(toughness, true);
                            string burnsToughness = GetSplitCardValue(toughness, false);
                            string turnsTribe = GetSplitCardValue(tribe, true);
                            string burnsTribe = GetSplitCardValue(tribe, false);
                            string turnsWatermark = GetSplitCardValue(watermark, true);
                            string burnsWatermark = GetSplitCardValue(watermark, false);

                            cards.Add(Slugger.Slugify(turnsName), GetCard(id, rarity, setCode, turnsArtist, turnsTypes, turnsCost, turnsFlavor, turnsName, turnsPower, turnsText, turnsToughness, turnsTribe, turnsWatermark, setDictionary));
                            cards.Add(Slugger.Slugify(burnsName), GetCard(id, rarity, setCode, burnsArtist, burnsTypes, burnsCost, burnsFlavor, burnsName, burnsPower, burnsText, burnsToughness, burnsTribe, burnsWatermark, setDictionary));
                        }
                    }
                }

                // pass to generate xml
                foreach (Card card in cards.Values) {
                    List<XElement> cardTypes = new List<XElement>();
                    foreach (CardType cardType in card.CardTypes) {
                        cardTypes.Add(new XElement("type", new XAttribute("name", cardType.ToString())));
                    }

                    List<XElement> cardAppearances = new List<XElement>();
                    foreach (CardAppearance appearance in card.Appearances) {
                        cardAppearances.Add(
                            new XElement(
                                "appearance", 
                                new XAttribute("multiverseID", appearance.MultiverseID),
                                new XAttribute("rarity", appearance.Rarity.ToString()),
                                new XAttribute("setCode", appearance.Set.Code),
                                new XAttribute("flavor", appearance.FlavorText),
                                new XAttribute("artist", appearance.Artist)
                            )
                        );
                    }

                    string power = (card.Power == null ? card.Power.ToString() : string.Empty);
                    string toughness = (card.Toughness == null ? card.Toughness.ToString() : string.Empty);

                    XElement cardElement = new XElement(
                        "card",
                        new XAttribute("name", card.Name),
                        (card.Cost != null && card.Cost.ToString() != string.Empty ? new XAttribute("cost", card.Cost.ToString()) : null),
                        new XAttribute("flavor", card.FlavorText),
                        new XAttribute("text", card.Text),
                        new XAttribute("tribe", card.Tribe),
                        new XAttribute("watermark", card.Watermark),
                        new XElement("types", cardTypes),
                        new XElement("appearances", cardAppearances)
                    );

                    if (!string.IsNullOrEmpty(power)) {
                        cardElement.Attribute("power").Value = power;
                    }

                    if (!string.IsNullOrEmpty(toughness)) {
                        cardElement.Attribute("toughness").Value = toughness;
                    }

                    cardElements.Add(cardElement);
                }

                XDocument outputSets = new XDocument(new XElement("sets"));
                outputSets.Root.Add(setElements);

                if (!Directory.Exists("Output")) {
                    Directory.CreateDirectory("Output");
                }

                XDocument outputCards = new XDocument(new XElement("cards"));
                outputCards.Root.Add(cardElements);

                outputSets.Save("Output/sets.xml");
                outputCards.Save("Output/cards.xml");
            }
        }
    }
}