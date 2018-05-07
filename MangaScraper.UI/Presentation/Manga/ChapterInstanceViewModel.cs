﻿using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Caliburn.Micro;
using MangaScraper.Application.Services;
using MangaScraper.UI.Helpers;
using MangaScraper.UI.Presentation.Common;

namespace MangaScraper.UI.Presentation.Manga {
  public class ChapterInstanceViewModel : PropertyChangedBase {
    public ChapterInstanceViewModel() =>
      SelectedRows.CollectionChanged += (s, e) => NotifyOfPropertyChange(() => CanDownloadSelected);

    public BindableCollection<ChapterInstance> Chapters { get; set; }
    public BindableCollection<ChapterInstance> SelectedRows { get; } = new BindableCollection<ChapterInstance>();

    public void SelectedRowsChanged(SelectionChangedEventArgs e) {
      SelectedRows.AddRange(e.AddedItems.Cast<ChapterInstance>());

      SelectedRows.RemoveRange(e.RemovedItems.Cast<ChapterInstance>());
    }

    public bool CanDownloadSelected => SelectedRows?.Any() == true;

    public void DownloadSelected() => Download(SelectedRows);

    public void DownloadManga_All() => Download(Chapters);

    public void DownloadManga_OnClick(ChapterInstance chapter) => Download(new[] { chapter });

    private void Download(IEnumerable<ChapterInstance> chaptersToDownload) {
      var (targetFolder, canceled) = FolderDialog.GetTargetFolder();
      if (canceled) return;


      var a = chaptersToDownload
        .Select(chapter => (chapter.Number, (MultiProgressViewModel.UpdateInstruction)(progress => chapter.DownloadTo(targetFolder, progress))))
        .Reverse()
        .ToList();
      MultiProgress.ScheduleProgress(a);
    }

    public MultiProgressViewModel MultiProgress { get; set; } = 
      new MultiProgressViewModel {
        ErrorLog = (s, exception) => {
        
        }
    };
  }


}