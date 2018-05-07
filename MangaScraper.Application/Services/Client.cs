﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.Caching;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;

namespace MangaScraper.Application.Services {
  public static class Client {
    //todo proper cache with timeout?
   // private static readonly MemoryCache MemoryCache = new MemoryCache("documentCache");
    private static HttpClient HttpClient { get; } = new HttpClient(new RetryHandler());
    private static HtmlParser HtmlParser { get; } = new HtmlParser();

    public static async Task DownloadToStream(this Stream fs, string url) {
      var fst = await HttpClient.GetStreamAsync(url);
      await fst.CopyToAsync(fs);
    }

    public static async Task<IHtmlDocument> GetCachedDocumentAsync(string url) {
      //if (MemoryCache.Contains(url))
      //  return MemoryCache.Get(url) as IHtmlDocument;
      var doc = await GetDocumentAsync(url);
      //Cache.Add(url, doc);
   //   MemoryCache.Set(new CacheItem(url, doc), new CacheItemPolicy { SlidingExpiration = TimeSpan.FromSeconds(90) });
      return doc;
    }

    public static async Task<IHtmlDocument> GetDocumentAsync(string url) {
      using (var webResponse = await Get(url))
      using (var responseStream = await HandleResponse(webResponse)) {
        return await HtmlParser.ParseAsync(responseStream).ConfigureAwait(false);
      }
    }

    private static async Task<HttpResponseMessage> Get(string url) {
      using (var request = new HttpRequestMessage(HttpMethod.Get, url) { Version = new Version(1, 1) }) {
        var resp = await HttpClient.SendAsync(request);
        resp.EnsureSuccessStatusCode();
        return resp;
      }
    }


    private static async Task<Stream> HandleResponse(HttpResponseMessage webResponse) {
      var stream = await webResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
      return webResponse.Content.Headers.ContentEncoding.Contains("gzip")
        ? new GZipStream(stream, CompressionMode.Decompress)
        : stream;
    }
  }
}