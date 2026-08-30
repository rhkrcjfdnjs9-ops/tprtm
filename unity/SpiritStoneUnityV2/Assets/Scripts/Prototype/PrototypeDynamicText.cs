using System;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritStone.Prototype
{
    [DisallowMultipleComponent]
    public sealed class PrototypeDynamicText : MonoBehaviour
    {
        private Text label;
        private Func<string> labelProvider;

        public void Initialize(Func<string> provider)
        {
            label = GetComponent<Text>();
            labelProvider = provider;
        }

        private void Update()
        {
            if (label != null && labelProvider != null) label.text = labelProvider();
        }
    }
}
