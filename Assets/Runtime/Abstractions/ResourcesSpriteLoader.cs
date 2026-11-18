using UnityEngine;

namespace Localization.Abstractions
{
    public sealed class ResourcesSpriteLoader
    {
        private readonly string _root;

        public ResourcesSpriteLoader(string root = "LocImages") { _root = root; }

        public Sprite Load(string language, string key)
            => Resources.Load<Sprite>($"{_root}/{language}/{key}");
    }
}
