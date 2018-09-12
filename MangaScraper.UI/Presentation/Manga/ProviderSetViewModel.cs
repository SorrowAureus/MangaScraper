﻿using Caliburn.Micro;
using MangaScraper.Application.Services;
using MangaScraper.Core.Helpers;
using MangaScraper.Core.Scrapers;
using MangaScraper.UI.Helpers;
using MangaScraper.UI.Presentation.Manga.SelectedManga.Chapters;
using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static System.Math;

namespace MangaScraper.UI.Presentation.Manga {
    public class ProviderSetViewModel : PropertyChangedBase {
        private readonly IMangaIndex _mangaIndex;
        public delegate ProviderSetViewModel Factory(MangaInfo mangaInfo);

        public ProviderSetViewModel(MangaInfo mangaInfo, IMangaIndex mangaIndex, SubscriptionViewModel.Factory factory) {
            _mangaIndex = mangaIndex;
            SubscriptionFactory = factory;
            Name = mangaInfo.Name;
            Providers = mangaInfo.Instances.Select(t => new ProviderData { Provider = t.provider, Url = t.url }).ToBindableCollection();
            MetaData = mangaInfo.MetaData;
            var providerData = this
                .OnPropertyChanges(s => s.SelectedProvider)
                .SelectTask(a => GetProviderData(a.Provider, a.Url))
                .ObserveOnDispatcher();

            SelectedInstance =
                providerData
                .Select(vt => CreateInstanceViewModel(vt.coverUrl, new ChapterInstanceViewModel(vt.chapters)))
                .ToReactiveProperty();

            Test =
                providerData
                .Select(vt => CreateInstanceViewModel(vt.coverUrl, SubscriptionFactory((SelectedProvider.Provider, Name), vt.chapters)))
                .ToReactiveProperty();

            SelectedInstance
                .Subscribe(x => x?.ChapterInstanceViewModel.SelectedRows.Clear());
        }

        private InstanceViewModel CreateInstanceViewModel(string coverUrl, ChapterInstances c) => new InstanceViewModel {
            Cover = string.IsNullOrEmpty(coverUrl) ? null : new BitmapImage(new Uri(coverUrl)),
            ChapterInstanceViewModel = c,
            Name = Name,
            MetaData = MetaData
        };

        private async Task<(IEnumerable<ChapterInstance> chapters, string coverUrl)> GetProviderData(string provider, string url) {
            var coverUrl = await _mangaIndex.GetCoverUrl(provider, url).ConfigureAwait(false);
            var chapters = url == null
                ? Enumerable.Empty<ChapterInstance>()
                : await GetChapters(_mangaIndex, provider, url).ConfigureAwait(false);
            return (chapters, coverUrl);
        }

        public static async Task<IEnumerable<ChapterInstance>> GetChapters(IMangaIndex index, string provider, string url) {
            try {
                var chapters = await index.Chapters(provider, url).ToListAsync();

                var maxDigits = chapters.Count == 0 ? 1 : (int)Floor(Log10(Abs(chapters.Count))) + 1;
                return chapters
                    .Select(c => new ChapterInstance {
                        MaxDigits = maxDigits,
                        Number = c.Number,
                        Parser = c,
                        Index = index
                    });
            }
            catch (Exception) {
                return new BindableCollection<ChapterInstance>();
            }
        }


        public BindableCollection<ProviderData> Providers { get; set; }

        public MetaData MetaData { get; set; }
        public string Name { get; set; }

        public ProviderData SelectedProvider { get; set; }

        public ReactiveProperty<InstanceViewModel> SelectedInstance { get; set; }

        public ReactiveProperty<InstanceViewModel> Test { get; set; }
        public SubscriptionViewModel.Factory SubscriptionFactory { get; }

        public struct ProviderData {
            public string Provider { get; set; }
            public string Url { get; set; }
        }
    }
}