using UnityEngine;
using System.Collections;

namespace TSKT
{
    [System.Serializable]
    public abstract class EasingValueBase
    {
        public enum TimeType
        {
            DefaultTime = 0,
            FixedTime = 1,
            RealTime = 2
        }

        protected float modifiedTime = 0f;
        protected float duration = 0f;

        public TimeType timeType = TimeType.DefaultTime;

        public abstract float Value { get; }

        public bool Completed
        {
            get
            {
                if (duration == 0f)
                {
                    return true;
                }
                return NormalizedElapsedTime == 1f;
            }
        }

        protected float NormalizedElapsedTime
        {
            get
            {
                return Mathf.Clamp01((Now - modifiedTime) / duration);
            }
        }

        protected float Now
        {
            get
            {
                switch (timeType)
                {
                    case TimeType.DefaultTime:
                        return Time.time;
                    case TimeType.FixedTime:
                        return Time.fixedTime;
                    case TimeType.RealTime:
                        return Time.realtimeSinceStartup;
                    default:
                        Debug.LogError("wrong time type");
                        return 0f;
                }
            }
        }
    }
}
