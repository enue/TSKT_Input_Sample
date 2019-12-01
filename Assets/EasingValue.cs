using UnityEngine;
using System.Collections;

namespace TSKT
{
    [System.Serializable]
    public class EasingValue : EasingValueBase
    {
        float from = 0f;

        public System.Func<float, float, float, float> EasingFunction { get; set; }

        public EasingValue(float value = 0f, System.Func<float, float, float, float> function = null)
        {
            if (function == null)
            {
                EasingFunction = TSKT.EasingFunction.Quad.EaseIn;
            }
            else
            {
                EasingFunction = function;
            }
            from = value;
            ToValue = value;
        }

        public override float Value
        {
            get
            {
                if (duration == 0f)
                {
                    return ToValue;
                }

                return EasingFunction.Invoke(from, ToValue, NormalizedElapsedTime);
            }
        }

        public float ToValue { get; private set; } = 0f;

        public void EaseTo(float value, float duration)
        {
            from = Value;
            ToValue = value;
            this.duration = duration;
            modifiedTime = Now;
        }

        public void JumpTo(float value)
        {
            from = value;
            ToValue = value;
            duration = 0f;
            modifiedTime = Now;
        }
    }
}
