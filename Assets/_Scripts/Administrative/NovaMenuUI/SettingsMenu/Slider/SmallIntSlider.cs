﻿using System.Collections.Generic;
using Nova;
using SuperspectiveUtils;
using UnityEngine;

namespace NovaMenuUI {
    public class SmallIntSlider : Slider {
        private static UIStylesheet Style => UIStyleManager.instance.CurrentStylesheet;
    
        public UIBlock pipParent;
        public UIBlock2D pipPrefab;
        private List<UIBlock2D> pips = new List<UIBlock2D>();

        private int numPips => 1+Mathf.RoundToInt(setting.maxValue - setting.minValue);

        private const float widthOfPip = 1.219215f; // In percent... not exact

        public void CreatePips() {
            if (setting == null) return;

            pipParent.AutoLayout.Spacing.Percent = 1f / Mathf.Max(1f, numPips-1);
            pipParent.AutoLayout.Spacing.Percent -= (widthOfPip / 100f);

            //Debug.Log($"Destroying {pips.Count} pips");
            foreach (var pip in pips) {
                Destroy(pip.gameObject);
            }
            pips.Clear();
        
            //Debug.Log($"Creating {numPips} pips");
            for (int i = 0; i < numPips; i++) {
                UIBlock2D pip = Instantiate(pipPrefab, pipParent.transform);
                pip.Size.Value = pip.Size.Value.WithY((i == 0 || i == numPips-1) ? 6 : 5);

                pip.Color = i >= setting.value ? Style.Settings.SmallIntSlider.unfilledColor : Style.Settings.SmallIntSlider.fillColor;

                pips.Add(pip);
            }
        }

        public void UpdatePipColors() {
            for (int i = 0; i < numPips; i++) {
                var pip = pips[i];
                pip.Color = i >= setting.value ? Style.Settings.SmallIntSlider.unfilledColor : Style.Settings.SmallIntSlider.fillColor;
            }
        }
    }
}
