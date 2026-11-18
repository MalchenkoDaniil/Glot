using System;
using System.Collections.Generic;

namespace Localization.Data
{
    [Serializable]
    public class LocalizationData
    {
        public string Key;
        public List<LocalizationValue> Values = new List<LocalizationValue>();
    }
}
