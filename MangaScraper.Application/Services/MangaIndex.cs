﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MangaScraper.Application.Persistence;
using MangaScraper.Core.Helpers;
using MangaScraper.Core.Scrapers;
using MangaScraper.Core.Scrapers.Manga;

namespace MangaScraper.Application.Services {
  public class MangaIndex : IMangaIndex {
    private readonly IMangaDownloader _manager;
    private readonly IMetaDataService _metaDataService;
    private readonly IMemCache _memCache;

    private AsyncLazy<MangaInfo[]> MyDictionary { get; set; }

    public MangaIndex(IMangaDownloader manager, IMetaDataService metaDataService, IMemCache memCache) {
      _manager = manager;
      _metaDataService = metaDataService;
      _memCache = memCache;
      MyDictionary = new AsyncLazy<MangaInfo[]>(CreateDictionary);
      //todo
      // _metaDataService.Start(CancellationToken.None);
    }

    private async Task<MangaInfo[]> CreateDictionary() {
      var res2 = await _memCache.GetAsync();
      var metaData = await _metaDataService.GetMetaData();
      var metaDataDict = metaData.Where(m => !string.IsNullOrEmpty(m.name)).GroupBy(m => m.name).ToDictionary(m => m.Key, x => x.First().metaData);


      var dict = res2//
           .GroupBy(t => t.name, t => (t.provider, t.url))
           .Select(g => new MangaInfo {
             Name = g.Key,
             Instances = g.ToList(),
             MetaData = metaDataDict.ContainsKey(g.Key) ? metaDataDict[g.Key] : default
           })
           .ToArray();
      return dict;
    }


    public async Task<IEnumerable<MangaInfo>> FindMangas(Genre genres) {
      //todo index, store to disk, etc
      var dict = await MyDictionary;
      return dict
        .AsParallel()
        .Where(kvp => kvp.MetaData.Genres.HasFlag(genres))
        .ToList();
    }

    public async Task<IEnumerable<MangaInfo>> FindMangas(string name) {
      //todo index, store to disk, etc
      var lower = name.ToLowerInvariant();
      var dict = await MyDictionary;
      return dict
        .AsParallel()
        .Where(kvp => kvp.Name.ToLowerInvariant().Contains(lower))
        .ToList();
    }

    public async Task Update() {
      var result = await Task.WhenAll(_manager.Providers.Select(p => _manager.ListInstances(p).ContinueWith(t => (t.Result, provider: p))));
      var group = result.AsParallel().SelectMany(q => q.Result, (x, t) => (x.provider, t.name, t.url));
      await _memCache.WriteToDisk(group);

      MyDictionary = new AsyncLazy<MangaInfo[]>(CreateDictionary);


      //todo create index, store to disk, etc
    }


    public Task<string> GetCoverUrl(string provider, string url) {
      return _manager.CoverUrl(provider, url);
    }

    public Task<IEnumerable<IChapterParser>> Chapters(string provider, string url) {
      return _manager.ChapterParsers(provider, url);
    }

    public Task DownloadChapter(IChapterParser parser, string basePath, IProgress<double> progress = null) =>
      _manager.DownloadChapterTo(parser, basePath, progress);
  }
}