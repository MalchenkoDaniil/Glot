using System;
using System.Collections.Generic;

namespace Localization.Data
{
    [Serializable]
    public class LocalizationTextTable
    {
        public List<LocalizationTextData> Entries = new List<LocalizationTextData>();
    }
}
