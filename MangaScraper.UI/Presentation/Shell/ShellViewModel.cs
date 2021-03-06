﻿using System.Collections.Generic;
using System.Linq;
using Caliburn.Micro;
using MangaScraper.UI.Composition;
using MangaScraper.UI.Presentation.TabsConductor;

namespace MangaScraper.UI.Presentation.Shell {
  public class ShellViewModel : PropertyChangedBase {
    public ShellViewModel(IEnumerable<IPrimaryScreen> screens) {
      TabsControl = new TabsConductorViewModel(screens.OrderBy(e => e.Order));
    }

    //public MenuViewModel Menu { get; set; } = new MenuViewModel();

    public TabsConductorViewModel TabsControl { get; set; }

  }
}