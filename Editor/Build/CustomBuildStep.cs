using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StudioManette.Edna
{
    public class CustomBuildStep
    {
        public delegate void callBackStep();

        public bool willDoThisStep = true;
        public int index;
        public string description;
        public callBackStep callback;

        public CustomBuildStep(int index, string description, callBackStep callback)
        {
            this.index = index;
            this.description = description;
            this.callback = callback;
        }
    }
}
