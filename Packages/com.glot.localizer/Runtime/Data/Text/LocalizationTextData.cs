using System;
using System.Collections.Generic;

namespace Localization.Data
{
    [Serializable]
    public class LocalizationTextData
    {
        public string Key;
        public List<LocalizationTextValue> Values = new List<LocalizationTextValue>();
    }
}
