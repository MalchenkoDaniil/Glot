using System;
using System.Collections.Generic;

namespace Localization.Data
{
    [Serializable]
    public class LocalizationTable
    {
        public List<LocalizationData> Entries = new List<LocalizationData>();
    }
}
