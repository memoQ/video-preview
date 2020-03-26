using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoQ.VideoPreview
{
    public class MissingFontDialogViewModel
    {
        /// <summary>
        /// If this is true the installing will start.
        /// </summary>
        public bool InstallFont = false;

        /// <summary>
        /// If this is true the recommanded font install form will never appear again
        /// </summary>
        public bool IgnorePermamently = false;
    }
}
