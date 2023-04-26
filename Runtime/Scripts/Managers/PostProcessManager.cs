using System.Collections.Generic;
using TriLibCore.Samples;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

namespace StudioManette.Edna
{
    public class PostProcessManager : AbstractInputSystem
    {
        public Volume globalVolume;
        public Volume globalRaytracing;
        public void ToggleSSR(bool toggle)
        {
            Volume currentVolume = globalVolume;
            ScreenSpaceReflection outpp;
            currentVolume.profile.TryGet(out outpp);
            if (toggle)
            {
                outpp.active = true;
            }
            else
            {
                outpp.active = false;
            }
        }

        public void ToggleAO(bool toggle)
        {
            Volume currentVolume = globalVolume;
            ScreenSpaceAmbientOcclusion outpp;
            currentVolume.profile.TryGet(out outpp);
            if (toggle)
            {
                outpp.active = true;
            }
            else
            {
                outpp.active = false;
            }
        }

        public void ToggleBloom(bool toggle)
        {
            Volume currentVolume = globalVolume;
            Bloom outpp;
            currentVolume.profile.TryGet(out outpp);
            if (toggle)
            {
                outpp.active = true;
            }
            else
            {
                outpp.active = false;
            }
        }

        public void ToggleRaytracing(bool toggle)
        {
            Volume currentVolume = globalRaytracing;
            RecursiveRendering outpp;
            currentVolume.profile.TryGet(out outpp);
            if (toggle)
            {
                outpp.active = true;
            }
            else
            {
                outpp.active = false;
            }
        }
    }
}
