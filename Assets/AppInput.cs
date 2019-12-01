using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TSKT
{
    public class AppInput : IInput
    {
        public bool GetKeyDown(KeyAssign.AppKey appKey)
        {
            return Input.GetKeyDown(appKey.keyCode);
        }
        public bool GetKeyUp(KeyAssign.AppKey appKey)
        {
            return Input.GetKeyUp(appKey.keyCode);
        }
        public bool GetKey(KeyAssign.AppKey appKey)
        {
            return Input.GetKey(appKey.keyCode);
        }

        public float GetAxis(KeyAssign.AxisButton axisButton)
        {
            var position = 0f;
            if (Input.GetKey(axisButton.keyCode))
            {
                position += 1f;
            }
            if (Input.GetKey(axisButton.negativeKeyCode))
            {
                position -= 1f;
            }
            return position;
        }

        public float GetAxis(KeyAssign.AppAxis appAxis)
        {
            return Input.GetAxisRaw(appAxis.axisName);
        }
    }
}
