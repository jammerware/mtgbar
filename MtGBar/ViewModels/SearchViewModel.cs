﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Bazam.Modules;
using BazamWPF.ViewModels;
using Melek.Models;
using MtGBar.Infrastructure;
using MtGBar.Infrastructure.Utilities;

namespace MtGBar.ViewModels
{
    public class SearchViewModel : ViewModelBase
    {
        #region Constants
        private const string PRICE_DEFAULT = "--";
        #endregion

        #region Fields
        private string _AmazonLink;
        private string _AmazonPrice = PRICE_DEFAULT;
        private CardViewModel[] _CardMatches;
        private string _CFLink;
        private string _CFPrice = PRICE_DEFAULT;
        private string _DefaultBackground;
        private Dictionary<string, Dictionary<string, string>> _PriceCache = new Dictionary<string, Dictionary<string, string>>();
        private string _SearchTerm;
        private CardAppearance _SelectedAppearance;
        private BitmapImage _SelectedAppearanceImage;
        private Card _SelectedCard;
        private bool _ShowPricingData = AppState.Instance.Settings.ShowPricingData;
        private string _TCGPlayerLink;
        private string _TCGPlayerPrice = PRICE_DEFAULT;
        private string _WatermarkText;
        #endregion

        #region Events
        public event EventHandler CardSelected;
        #endregion

        #region Constructor
        public SearchViewModel()
        {
            _DefaultBackground = Path.Combine(FileSystemManager.PackageArtDirectory, "default.jpg");
            ShuffleWatermarkText();

            AppState.Instance.Settings.Updated += (theSettings, haveChanged) => {
                ShowPricingData = AppState.Instance.Settings.ShowPricingData;
            };
        }
        #endregion

        #region Properties
        public string AmazonLink
        {
            get { return _AmazonLink; }
            set
            {
                if (_AmazonLink != value) {
                    _AmazonLink = value;
                    OnPropertyChanged("AmazonLink");
                }
            }
        }

        public string AmazonPrice
        {
            get { return _AmazonPrice; }
            set
            {
                if (_AmazonPrice != value) {
                    _AmazonPrice = value;
                    OnPropertyChanged("AmazonPrice");
                }
            }
        }

        public CardViewModel[] CardMatches
        {
            get { return _CardMatches; }
            set
            {
                _CardMatches = value;
                OnPropertyChanged("CardMatches");
            }
        }

        public string CFLink
        {
            get { return _CFLink; }
            set
            {
                if (_CFLink != value) {
                    _CFLink = value;
                    OnPropertyChanged("CFLink");
                }
            }
        }

        public string CFPrice
        {
            get { return _CFPrice; }
            set
            {
                if (_CFPrice != value) {
                    _CFPrice = value;
                    OnPropertyChanged("CFPrice");
                }
            }
        }

        public string DefaultBackground
        {
            get { return _DefaultBackground; }
        }

        public BitmapImage DefaultCardIcon
        {
            get { return new BitmapImage(new Uri("pack://application:,,,/Assets/card-back.png")); }
        }

        public string SearchTerm
        {
            get { return _SearchTerm; }
            set
            {
                if (string.IsNullOrEmpty(_SearchTerm) && !string.IsNullOrEmpty(value)) {
                    ShuffleWatermarkText();
                }

                if (_SearchTerm != value) {
                    _SearchTerm = value;
                    string searchTerm = value.Trim().ToLower();
                    string setCode = string.Empty;
                    Match setCodeMatch = Regex.Match(searchTerm, "^([a-z0-9]{2,3}):");

                    if (setCodeMatch != null && setCodeMatch.Groups.Count == 2) {
                        setCode = setCodeMatch.Groups[1].Value;
                        searchTerm = searchTerm.Replace(setCodeMatch.Groups[0].Value, string.Empty).Trim();
                    }

                    UpdateResults(searchTerm, setCode);
                    OnPropertyChanged("SearchTerm");
                }
            }
        }

        public CardAppearance SelectedAppearance
        {
            get { return _SelectedAppearance; }
            set
            {
                if (_SelectedAppearance != value) {
                    _SelectedAppearance = value;
                    if (SelectedAppearance != null) {
                        QueryPriceData();
                        AppearanceSelected(value);
                    }
                    OnPropertyChanged("SelectedAppearance");
                }
            }
        }

        public BitmapImage SelectedAppearanceImage
        {
            get { return _SelectedAppearanceImage; }
            private set
            {
                if (_SelectedAppearanceImage != value) {
                    _SelectedAppearanceImage = value;
                    OnPropertyChanged("SelectedAppearanceImage");
                }
            }
        }

        public Card SelectedCard
        {
            get { return _SelectedCard; }
            set
            {
                if (SelectedCard != value) {
                    _SelectedCard = value;

                    // clear the price cache
                    ResetPriceData();
                    _PriceCache.Clear();

                    // changing the selected appearance automatically requeries price data
                    if (value == null) {
                        SelectedAppearance = null;
                    }
                    else {
                        SelectedAppearance = value.Appearances.OrderByDescending(a => a.Set.Date).First();
                    }

                    OnPropertyChanged("SelectedCard");
                    if (CardSelected != null) {
                        CardSelected(this, EventArgs.Empty);
                    }
                }
            }
        }

        public bool ShowPricingData
        {
            get { return _ShowPricingData; }
            set
            {
                if (_ShowPricingData != value) {
                    _ShowPricingData = value;
                    OnPropertyChanged("ShowPricingData");
                }
            }
        }

        public string TCGPlayerLink
        {
            get { return _TCGPlayerLink; }
            set
            {
                if (_TCGPlayerLink != value) {
                    _TCGPlayerLink = value;
                    OnPropertyChanged("TCGPlayerLink");
                }
            }
        }

        public string TCGPlayerPrice
        {
            get { return _TCGPlayerPrice; }
            set
            {
                if (_TCGPlayerPrice != value) {
                    _TCGPlayerPrice = value;
                    OnPropertyChanged("TCGPlayerPrice");
                }
            }
        }

        public string WatermarkText
        {
            get { return _WatermarkText; }
            set
            {
                if (_WatermarkText != value) {
                    _WatermarkText = value;
                    OnPropertyChanged("WatermarkText");
                }
            }
        }
        #endregion

        #region Private utility methods
        private async void AppearanceSelected(CardAppearance appearance)
        {
            SelectedAppearanceImage = null;
            SelectedAppearanceImage = await AppState.Instance.MelekDataStore.GetAppearanceImage(appearance);
        }

        private string ApplyPriceDefault(string price)
        {
            return price == string.Empty ? PRICE_DEFAULT : price;
        }

        private void CachePrice(string multiverseID, string vendor, string price)
        {
            if (!_PriceCache.ContainsKey(multiverseID)) {
                _PriceCache.Add(multiverseID, new Dictionary<string, string>() { { vendor, price } });
            }
            else {
                _PriceCache[multiverseID][vendor] = price;
            }
        }

        private bool IsPriceCached(string multiverseID, string vendor)
        {
            return _PriceCache.ContainsKey(multiverseID) && _PriceCache[multiverseID].ContainsKey(vendor);
        }

        private void QueryPriceData()
        {
            ResetPriceData();
            if (_ShowPricingData && SelectedAppearance != null && SelectedCard != null) {
                // snag references to these in case they're gone by the time the async functions come back
                string multiverseID = SelectedAppearance.MultiverseID;
                Card selectedCard = SelectedCard;
                Set set = SelectedAppearance.Set;

                BackgroundBuddy.RunAsync(() => {
                    AmazonLink = VendorRelations.GetAmazonLink(selectedCard);

                    string amazonPrice = string.Empty;
                    if (IsPriceCached(multiverseID, "amazon")) {
                        amazonPrice = _PriceCache[multiverseID]["amazon"];
                    }
                    else {
                        amazonPrice = VendorRelations.GetAmazonPrice(selectedCard);
                        CachePrice(multiverseID, "amazon", amazonPrice);
                    }
                    AmazonPrice = ApplyPriceDefault(amazonPrice);
                });
                BackgroundBuddy.RunAsync(() => {
                    CFLink = VendorRelations.GetCFLink(selectedCard.Name, set);

                    string cfPrice = string.Empty;
                    if (IsPriceCached(multiverseID, "cf")) {
                        cfPrice = _PriceCache[multiverseID]["cf"];
                    }
                    else {
                        cfPrice = VendorRelations.GetCFPrice(selectedCard.Name, set);
                        CachePrice(multiverseID, "cf", cfPrice);
                    }
                    CFPrice = ApplyPriceDefault(cfPrice);
                });

                BackgroundBuddy.RunAsync(() => {
                    // set the default tcgplayer link in case the API takes a bit
                    TCGPlayerLink = VendorRelations.GetTCGPlayerLinkDefault(selectedCard.Name, set);
                    string tcgPlayerAPIData = VendorRelations.GetTCGPlayerAPIData(selectedCard.Name, set);
                    string apiLink = VendorRelations.GetTCGPlayerLink(tcgPlayerAPIData);
                    if (!string.IsNullOrEmpty(apiLink)) {
                        TCGPlayerLink = VendorRelations.GetTCGPlayerLink(tcgPlayerAPIData);
                    }

                    string tcgPlayerPrice = string.Empty;
                    if (IsPriceCached(multiverseID, "tcgPlayer")) {
                        tcgPlayerPrice = _PriceCache[multiverseID]["tcgPlayer"];
                    }
                    else {
                        tcgPlayerPrice = VendorRelations.GetTCGPlayerPrice(tcgPlayerAPIData);
                        CachePrice(multiverseID, "tcgPlayer", tcgPlayerPrice);
                    }
                    TCGPlayerPrice = ApplyPriceDefault(tcgPlayerPrice);
                });
            }
        }

        private void ResetPriceData()
        {
            AmazonLink = string.Empty;
            AmazonPrice = PRICE_DEFAULT;
            CFLink = string.Empty;
            CFPrice = PRICE_DEFAULT;
            TCGPlayerLink = string.Empty;
            TCGPlayerPrice = PRICE_DEFAULT;
        }

        private void ShuffleWatermarkText()
        {
            WatermarkText = "try \"" + AppState.Instance.MelekDataStore.GetRandomCardName() + "\"";
        }

        private async void UpdateResults(string searchTerm, string setCode)
        {
            Card[] results = await Task<Card[]>.Factory.StartNew(() => { return AppState.Instance.MelekDataStore.Search(searchTerm, setCode).Take(5).ToArray(); });
            List<CardViewModel> vms = new List<CardViewModel>();

            foreach (Card card in results) {
                CardViewModel vm = new CardViewModel(card);
                vms.Add(vm);
            }

            // set the CardMatches property so the results get bound asap
            CardMatches = vms.ToArray();
            if (CardMatches.Count() != 1 || CardMatches[0].Card != SelectedCard) {
                SelectedCard = null;
            }
            
            if (CardMatches.Count() == 1) {
                SelectedCard = CardMatches[0].Card;
            }

            // then set up pretty picchurs
            foreach (CardViewModel vm in CardMatches) {
                vm.FullSize = await AppState.Instance.MelekDataStore.GetAppearanceImage(vm.Card.Appearances[0]);
                if (vm.FullSize != null) {
                    vm.Thumbnail = new CroppedBitmap(vm.FullSize, new Int32Rect(54, 54, 84, 84));
                }
            }
        }
        #endregion
    }
}