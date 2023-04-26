using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StudioManette.Edna
{
    public class BlendshapeConstraint : MonoBehaviour
    {
        public static readonly string BLENDSHAPE_PREFIX = "shapekey_";

        public enum Axis { X, Y, Z };
        public Axis axisToReadPosition = Axis.X;

        private SkinnedMeshRenderer targetSMR;
        private int blendShapeIndex = -1;

        public void Init()
        {
            string thisShapeName = null;

            targetSMR = this.GetComponentInParent<SkinnedMeshRenderer>();
            if (targetSMR != null)
            {
                if (this.gameObject.name.IndexOf(BLENDSHAPE_PREFIX) != 0)
                {
                    Debug.LogError("the current object does not begins with Blendshape Prefix " + BLENDSHAPE_PREFIX + " ( " + this.gameObject.name + " ) ");
                    return;
                }
                else
                {
                    thisShapeName = this.gameObject.name.Substring(BLENDSHAPE_PREFIX.Length);
                }
                if (targetSMR.sharedMesh != null)
                {
                    for (int i = 0; i < targetSMR.sharedMesh.blendShapeCount; i++)
                    {
                        if (string.Equals(targetSMR.sharedMesh.GetBlendShapeName(i), thisShapeName))
                        {
                            blendShapeIndex = i;
                            continue;
                        }
                    }

                    if (blendShapeIndex != -1)
                    {
                        ActivateConstraint();
                    }
                    else
                    {
                        Debug.LogWarning("Current Blendshape no found in SkinnedMeshRenderer : " + thisShapeName);
                    }
                }
            }
        }

        [ContextMenu("Activate Constraint")]
        public void ActivateConstraint()
        {
            targetSMR.SetBlendShapeWeight(blendShapeIndex, ReadValue());
        }

        private float ReadValue()
        {
            float value;
            switch (axisToReadPosition)
            {
                default:
                case Axis.X:
                    value = this.transform.localPosition.x;
                    break;
                case Axis.Y:
                    value = this.transform.localPosition.y;
                    break;
                case Axis.Z:
                    value = this.transform.localPosition.z;
                    break;
            }
            return Mathf.Abs(value) * 100f;
        }
    }
}
