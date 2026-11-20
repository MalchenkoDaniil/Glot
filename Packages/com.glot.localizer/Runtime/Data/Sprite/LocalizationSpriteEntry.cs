using System;
using System.Collections.Generic;

namespace Runtime.Data.SpriteData
{
    [Serializable]
    public class LocalizationSpriteEntry
    {
        public string Key;
        public List<LocalizationSpriteValue> Values = new List<LocalizationSpriteValue>();
    }
}