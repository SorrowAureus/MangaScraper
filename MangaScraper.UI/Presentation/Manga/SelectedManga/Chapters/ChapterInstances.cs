﻿using Caliburn.Micro;
using MangaScraper.Application.Services;
using MangaScraper.UI.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace MangaScraper.UI.Presentation.Manga.SelectedManga.Chapters {
    public class ChapterInstances : PropertyChangedBase {
        public ChapterInstances(IEnumerable<ChapterInstance> chapters) =>
            Chapters = chapters.OrderBy(e => e.Number).ToBindableCollection();


        public BindableCollection<ChapterInstance> Chapters { get; set; }
        public BindableCollection<ChapterInstance> SelectedRows { get; } = new BindableCollection<ChapterInstance>();

        public void SelectedRowsChanged(SelectionChangedEventArgs e) {
            SelectedRows.AddRange(e.AddedItems.Cast<ChapterInstance>());

            SelectedRows.RemoveRange(e.RemovedItems.Cast<ChapterInstance>());
        }
    }
}
