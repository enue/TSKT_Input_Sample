using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UniRx.Async;

namespace TSKT
{
    public class Cursor : MonoBehaviour
    {
        static public Cursor Instance { get; private set; }

        [SerializeField]
        Image image = default;

        RectTransform focusObject;
        Canvas focusObjectRootCanvas;
        RectTransform viewport;
        Vector3? focusPosition;

        EasingValue x;
        EasingValue y;
        Vector3? initialScale;

        public bool IsMouseMode { get; set; }
        public bool IsSleeping { get; private set; }

        [SerializeField]
        bool hideWhenMouseMoved = true;
        public bool HideWhenMouseMoved
        {
            get
            {
                return hideWhenMouseMoved;
            }
        }

        readonly Vector3[] cornersBuffer = new Vector3[4];

        void OnEnable()
        {
            Instance = this;
        }

        void OnDisable()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        void Sleep()
        {
            image.enabled = false;
            focusObject = null;
            focusObjectRootCanvas = null;
            focusPosition = null;
            x = null;
            y = null;
        }

        public void SetFocus(RectTransform target, RectTransform viewport)
        {
            IsSleeping = false;
            focusObject = target;
            focusObjectRootCanvas = null;
            focusPosition = null;
            this.viewport = viewport;
        }

        public void SetFocus(Vector3 worldPosition)
        {
            IsSleeping = false;
            focusObject = null;
            focusObjectRootCanvas = null;
            focusPosition = worldPosition;
            viewport = null;
        }

        void Update()
        {
            // マウスを動かしたらカーソル隠す。隠すだけで動きはする。
            if (HideWhenMouseMoved)
            {
                if (!IsMouseMode)
                {
                    if (Input.GetAxisRaw("Mouse X") > 0f
                        || Input.GetAxisRaw("Mouse Y") > 0f)
                    {
                        IsMouseMode = true;
                        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                    }
                }
            }

            if (!focusObject && !focusPosition.HasValue)
            {
                Sleep();
                return;
            }
            if (focusObject && !focusObject.gameObject.activeInHierarchy)
            {
                Sleep();
                return;
            }

            // フォーカスオブジェクトがある場合は活性化
            image.enabled = !IsMouseMode;
            IsSleeping = false;

            Vector3 screenPosition;
            if (focusObject)
            {
                if (!focusObjectRootCanvas)
                {
                    focusObjectRootCanvas = focusObject.GetComponentInParent<Canvas>().rootCanvas;
                }
                focusObject.GetLocalCorners(cornersBuffer);
                // オブジェクトから少し離す
                var margin = 4f;
                Vector3 worldPos;
                {
                    var pos = new Vector3(
                        cornersBuffer[0].x - margin,
                        (cornersBuffer[0].y + cornersBuffer[2].y) / 2f,
                        cornersBuffer[0].z);
                    worldPos = focusObject.transform.localToWorldMatrix.MultiplyPoint(pos);
                }
                if (viewport)
                {
                    var localPos = viewport.transform.worldToLocalMatrix.MultiplyPoint(worldPos);

                    viewport.GetLocalCorners(cornersBuffer);
                    var xMin = cornersBuffer[0].x - margin;
                    var yMin = cornersBuffer[0].y;
                    var xMax = cornersBuffer[2].x;
                    var yMax = cornersBuffer[2].y;

                    localPos.x = Mathf.Clamp(localPos.x, xMin, xMax);
                    localPos.y = Mathf.Clamp(localPos.y, yMin, yMax);

                    worldPos = viewport.transform.localToWorldMatrix.MultiplyPoint(localPos);
                }

                if (focusObjectRootCanvas.worldCamera)
                {
                    screenPosition = focusObjectRootCanvas.worldCamera.WorldToScreenPoint(worldPos);
                }
                else
                {
                    screenPosition = worldPos;
                }
            }
            else
            {
                screenPosition = focusPosition.Value;
            }

            if (x == null)
            {
                x = new EasingValue
                {
                    timeType = EasingValueBase.TimeType.RealTime,
                    EasingFunction = TSKT.EasingFunction.Quad.EaseOut
                };
                x.JumpTo(screenPosition.x);
            }
            else
            {
                if (x.ToValue != screenPosition.x)
                {
                    x.EaseTo(screenPosition.x, 0.1f);
                }
            }

            if (y == null)
            {
                y = new EasingValue
                {
                    timeType = EasingValueBase.TimeType.RealTime,
                    EasingFunction = TSKT.EasingFunction.Quad.EaseOut
                };
                y.JumpTo(screenPosition.y);
            }
            else
            {
                if (y.ToValue != screenPosition.y)
                {
                    y.EaseTo(screenPosition.y, 0.1f);
                }
            }

            transform.position = new Vector3(x.Value, y.Value, transform.position.z);
        }
    }
}
