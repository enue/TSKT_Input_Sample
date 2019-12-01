using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UniRx.Async;

namespace TSKT
{
    public class CursorController : KeyBind
    {
        struct Axis
        {
            public const float DefaultThreshold = 0.2f;
            const float FirstInterval = 0.5f;
            const float SecondInterval = 0.1f;
            float startedPressingTime;
            float previousElapsedTime;
            int previousFrame;

            public float Threshold { private get; set; }

            public bool Update(out int pulse, float axisPosition)
            {
                if ((axisPosition * axisPosition) < (Threshold * Threshold))
                {
                    pulse = 0;
                    return false;
                }

                if (previousFrame != Time.frameCount - 1)
                {
                    startedPressingTime = Time.realtimeSinceStartup;
                    previousElapsedTime = -1f;
                }
                previousFrame = Time.frameCount;

                var elapsedTime = Time.realtimeSinceStartup - startedPressingTime;

                int a;
                int b;
                if (elapsedTime < FirstInterval)
                {
                    a = Mathf.FloorToInt(elapsedTime / FirstInterval);
                    b = Mathf.FloorToInt(previousElapsedTime / FirstInterval);
                }
                else
                {
                    a = Mathf.FloorToInt(elapsedTime / SecondInterval);
                    b = Mathf.FloorToInt(previousElapsedTime / SecondInterval);
                }

                if (a != b)
                {
                    if (axisPosition < 0f)
                    {
                        pulse = -1;
                    }
                    else
                    {
                        pulse = 1;
                    }
                }
                else
                {
                    pulse = 0;
                }

                previousElapsedTime = elapsedTime;
                return true;
            }
        }

        public enum Direction
        {
            Vertical = 0,
            Horizontal = 1,
            Auto = 2,
            MultiRows = 3,
            MultiRowsOnlyActiveObjects = 4,
        }

        [SerializeField]
        Direction direction = default;

        [SerializeField]
        GameObject initialItem = default;

        [SerializeField]
        List<GameObject> items = default;

        [SerializeField]
        ScrollRect scrollRect = default;

        [SerializeField]
        [Range(0, 20)]
        int countPerPage = 0;

        [SerializeField]
        bool blockingSignals = true;
        public override bool BlockingSignals => blockingSignals;

        readonly static Vector3[] cornersBuffer = new Vector3[4];

        GameObject _currentItem;
        public GameObject CurrentItem
        {
            get
            {
                if (items == null)
                {
                    return null;
                }
                if (items.Count == 0)
                {
                    return null;
                }

                if (_currentItem)
                {
                    if (items.IndexOf(_currentItem) < 0)
                    {
                        _currentItem = null;
                    }
                }

                if (!_currentItem)
                {
                    if (initialItem)
                    {
                        _currentItem = initialItem;
                    }
                    else if (direction == Direction.Auto)
                    {
                        var angle = new Vector3(-1f, 1f, 0f);
                        _currentItem = ArrayUtil.MaxBy<GameObject, IEnumerable<GameObject>, float>(items.Where(_ => _), _ => {
                            ((RectTransform)_.transform).GetWorldCorners(cornersBuffer);
                            return cornersBuffer.Max(corner => Vector3.Dot(angle, corner));
                            });
                    }
                    else
                    {
                        if (items != null && items.Count > 0)
                        {
                            _currentItem = items[0];
                        }
                        Debug.Assert(_currentItem, "not found current item");
                    }
                }
                return _currentItem;
            }
            set
            {
                _currentItem = value;
            }
        }

        Axis horizontalAxis = new Axis() { Threshold = Axis.DefaultThreshold };
        Axis verticalAxis = new Axis() { Threshold = Axis.DefaultThreshold };

        readonly Dictionary<GameObject, ScrollRect> scrollRectCache = new Dictionary<GameObject, ScrollRect>();
        readonly Dictionary<GameObject, Selectable> selectableCache = new Dictionary<GameObject, Selectable>();

        readonly Dictionary<GameObject, System.Action> onSelectCallbacks = new Dictionary<GameObject, System.Action>();

        Dictionary<GameObject, DirectionMap<GameObject>> _graph;
        Dictionary<GameObject, DirectionMap<GameObject>> Graph
        {
            get
            {
                if (_graph == null)
                {
                    horizontalAxis.Threshold = Axis.DefaultThreshold;
                    verticalAxis.Threshold = Axis.DefaultThreshold;

                    if (direction == Direction.Auto)
                    {
                        _graph = CursorControllerUtil.BuildAutoGraph(items);
                    }
                    else if (direction == Direction.MultiRows)
                    {
                        _graph = CursorControllerUtil.BuildMultiRowsGraph(loop: true, containsInactiveObjects: true, items: items.ToArray());
                    }
                    else if (direction == Direction.MultiRowsOnlyActiveObjects)
                    {
                        _graph = CursorControllerUtil.BuildMultiRowsGraph(loop: true, containsInactiveObjects: false, items: items.ToArray());
                    }
                    else
                    {
                        _graph = new Dictionary<GameObject, DirectionMap<GameObject>>();
                        for (int i = 0; i < items.Count; ++i)
                        {
                            var nexts = new DirectionMap<GameObject>();
                            _graph.Add(items[i], nexts);
                            if (direction == Direction.Vertical)
                            {
                                nexts.Down = items[(i - 1 + items.Count) % items.Count];
                                nexts.Up = items[(i + 1) % items.Count];

                                if (countPerPage != 0)
                                {
                                    // ページ送りはグイっと押し込まないときかないようにする
                                    horizontalAxis.Threshold = 0.95f;
                                    var left = items[Mathf.Clamp(i - countPerPage, 0, items.Count - 1)];
                                    if (left != items[i])
                                    {
                                        nexts.Left = left;
                                    }
                                    var right = items[Mathf.Clamp(i + countPerPage, 0, items.Count - 1)];
                                    if (right != items[i])
                                    {
                                        nexts.Right = right;
                                    }
                                }
                            }
                            else if (direction == Direction.Horizontal)
                            {
                                nexts.Left = items[(i - 1 + items.Count) % items.Count];
                                nexts.Right = items[(i + 1) % items.Count];

                                if (countPerPage != 0)
                                {
                                    // ページ送りはグイっと押し込まないときかないようにする
                                    verticalAxis.Threshold = 0.95f;
                                    var up = items[Mathf.Clamp(i - countPerPage, 0, items.Count - 1)];
                                    if (up != items[i])
                                    {
                                        nexts.Left = up;
                                    }
                                    var down = items[Mathf.Clamp(i + countPerPage, 0, items.Count - 1)];
                                    if (down != items[i])
                                    {
                                        nexts.Right = down;
                                    }
                                }
                            }
                            else
                            {
                                Debug.Assert(false, "unknown direction : " + direction);
                            }
                        }
                    }
                }
                return _graph;
            }
        }

        public void AppendItem(GameObject item, System.Action onSelectCallback = null)
        {
            items.Add(item);
            if (onSelectCallback != null)
            {
                onSelectCallbacks[item] = onSelectCallback;
            }
            _graph = null;
        }

        public void ClearItems()
        {
            CurrentItem = null;
            items.Clear();
            onSelectCallbacks.Clear();
            _graph = null;
            scrollRectCache.Clear();
            selectableCache.Clear();
        }

        public void SetGraph(Dictionary<GameObject, DirectionMap<GameObject>> graph)
        {
            ClearItems();

            _graph = graph;
            items = graph.Keys.ToList();
        }

        public void SetOnSelectedEvent(GameObject item, System.Action onSelected)
        {
            onSelectCallbacks[item] = onSelected;
        }

        public override void OnSelected()
        {
            if (UnityEngine.EventSystems.EventSystem.current)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            }

            if (CurrentItem)
            {
                var selectable = GetSelectableOf(CurrentItem);
                if (selectable)
                {
                    if (!Cursor.Instance.IsMouseMode)
                    {
                        selectable.Select();
                    }
                }

                if (onSelectCallbacks.TryGetValue(CurrentItem, out var callback))
                {
                    callback?.Invoke();
                }
            }

            RefreshCursor();
        }

        public override bool OnKeyDown(string button)
        {
            if (button == InputSetting.Instance.Submit)
            {
                if (Cursor.Instance.IsMouseMode)
                {
                    Cursor.Instance.IsMouseMode = false;
                    return true;
                }

                var current = CurrentItem.GetComponent<Button>();
                if (current)
                {
                    if (current.IsInteractable())
                    {
                        current.onClick.Invoke();
                        return true;
                    }
                }
                else
                {
                    var toggle = CurrentItem.GetComponent<Toggle>();
                    if (toggle)
                    {
                        if (toggle.group)
                        {
                            toggle.isOn = true;
                        }
                        else
                        {
                            toggle.isOn = !toggle.isOn;
                        }
                    }
                }
            }

            return false;
        }

        public override bool OnKeyUp(string button)
        {
            return false;
        }

        public override bool OnAxis(Dictionary<string, float> axisPositions)
        {
            bool input = false;
            var pulse = Vector2Int.zero;
            var axis = Vector2.zero;

            {
                // 斜め移動は許可しない。どちらかを破棄する。

                axisPositions.TryGetValue("Horizontal", out var horizontalAxis);
                axisPositions.TryGetValue("Vertical", out var verticalAxis);

                if (horizontalAxis == 0f && verticalAxis == 0f)
                {
                    return false;
                }

                // 絶対値でとっているのでangleは0-90までの値になる。
                var angle = Mathf.Atan2(Mathf.Abs(verticalAxis), Mathf.Abs(horizontalAxis))
                    * Mathf.Rad2Deg;

                if (angle < 90f / 4f)
                {
                    if (this.horizontalAxis.Update(out var horizontalPulse, horizontalAxis))
                    {
                        input = true;
                        axis.x = horizontalAxis;
                    }
                    pulse.x = horizontalPulse;
                }
                else if (angle > 90f * 3f / 4f)
                {
                    if (this.verticalAxis.Update(out var verticalPulse, verticalAxis))
                    {
                        input = true;
                        axis.y = verticalAxis;
                    }
                    verticalPulse *= -1;
                    pulse.y = verticalPulse;
                }
                else
                {
                    // 斜めすぎる場合は両方破棄
                    return false;
                }
            }

            if (!input)
            {
                return false;
            }

            if (pulse.sqrMagnitude != 0)
            {
                if (Cursor.Instance.IsMouseMode)
                {
                    Cursor.Instance.IsMouseMode = false;
                }

                var originalItem = CurrentItem;

                while (true)
                {
                    if (!Graph.TryGetValue(CurrentItem, out var nexts))
                    {
                        break;
                    }
                    var next = nexts[pulse.x, pulse.y];
                    if (!next)
                    {
                        break;
                    }

                    CurrentItem = next;

                    if (next == originalItem)
                    {
                        break;
                    }
                    if (!next || !next.activeInHierarchy)
                    {
                        continue;
                    }

                    var selectable = GetSelectableOf(CurrentItem);
                    if (selectable)
                    {
                        if (!Cursor.Instance.IsMouseMode)
                        {
                            selectable.Select();
                        }
                    }
                    else
                    {
                        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                    }
                    break;
                }

                if (onSelectCallbacks.TryGetValue(CurrentItem, out var callback))
                {
                    callback?.Invoke();
                }
                RefreshScroll();
                RefreshCursor();
            }

            if (axis.x != 0f)
            {
                var slider = CurrentItem.GetComponent<Slider>();
                if (slider)
                {
                    if (slider.IsInteractable())
                    {
                        if (slider.wholeNumbers)
                        {
                            slider.value += pulse.x;
                        }
                        else
                        {
                            slider.normalizedValue += Time.unscaledDeltaTime * axis.x;
                        }
                    }
                    return false;
                }
            }
            var scrollRect = GetScrollRectOf(CurrentItem);
            if (scrollRect)
            {
                if (scrollRect.horizontal && axis.x != 0f)
                {
                    var pos = scrollRect.horizontalNormalizedPosition + Time.unscaledDeltaTime * axis.x;
                    if (scrollRect.movementType == ScrollRect.MovementType.Clamped)
                    {
                        pos = Mathf.Clamp01(pos);
                    }
                    scrollRect.horizontalNormalizedPosition = pos;
                    return false;
                }
                if (scrollRect.vertical && axis.y != 0)
                {
                    var pos = scrollRect.verticalNormalizedPosition + Time.unscaledDeltaTime * axis.y;
                    if (scrollRect.movementType == ScrollRect.MovementType.Clamped)
                    {
                        pos = Mathf.Clamp01(pos);
                    }
                    scrollRect.verticalNormalizedPosition = pos;
                    return false;
                }
            }
            return false;
        }

        public void RefreshScrollImmediately()
        {
            RefreshScroll(duration: 0f);
        }

        void RefreshScroll(float duration = 0.1f)
        {
            if (scrollRect)
            {
                if (CurrentItem.transform.IsChildOf(scrollRect.viewport))
                {
                    scrollRect.viewport.GetWorldCorners(cornersBuffer);
                    var viewPortMaxX = cornersBuffer.Max(_ => _.x);
                    var viewPortMinX = cornersBuffer.Min(_ => _.x);
                    var viewPortMaxY = cornersBuffer.Max(_ => _.y);
                    var viewPortMinY = cornersBuffer.Min(_ => _.y);
                    ((RectTransform)CurrentItem.transform).GetWorldCorners(cornersBuffer);
                    var buttonMaxX = cornersBuffer.Max(_ => _.x);
                    var buttonMinX = cornersBuffer.Min(_ => _.x);
                    var buttonMaxY = cornersBuffer.Max(_ => _.y);
                    var buttonMinY = cornersBuffer.Min(_ => _.y);

                    var worldLength = Vector3.zero;
                    if (scrollRect.horizontal)
                    {
                        if (viewPortMaxX < buttonMaxX)
                        {
                            var length = viewPortMaxX - buttonMaxX;
                            worldLength = new Vector3(length, 0f, 0f);
                        }
                        if (viewPortMinX > buttonMinX)
                        {
                            var length = viewPortMinX - buttonMinX;
                            worldLength = new Vector3(length, 0f, 0f);
                        }
                    }
                    else if (scrollRect.vertical)
                    {
                        if (viewPortMaxY < buttonMaxY)
                        {
                            var length = viewPortMaxY - buttonMaxY;
                            worldLength = new Vector3(0f, length, 0f);
                        }
                        if (viewPortMinY > buttonMinY)
                        {
                            var length = viewPortMinY - buttonMinY;
                            worldLength = new Vector3(0f, length, 0f);
                        }
                    }
                    if (worldLength.sqrMagnitude > 0f)
                    {
                        var worldPos = scrollRect.content.position + worldLength;
                        var localPos = scrollRect.content.parent.worldToLocalMatrix.MultiplyPoint(worldPos);

                        Tween.Move(scrollRect.content.gameObject, duration, scaledTime: false)
                            .Local(true)
                            .To(localPos)
                            .Function(EasingFunction.Linear);
                    }
                }
            }
        }

        void RefreshCursor()
        {
            if (CurrentItem)
            {
                var scrollRect = CurrentItem.GetComponentInParent<ScrollRect>();
                var viewport = scrollRect ? scrollRect.viewport : null;
                if (viewport && !CurrentItem.transform.IsChildOf(viewport))
                {
                    viewport = null;
                }
                Cursor.Instance.SetFocus(
                    CurrentItem.transform as RectTransform,
                    viewport);
            }
        }

        ScrollRect GetScrollRectOf(GameObject obj)
        {
            if (scrollRectCache.TryGetValue(obj, out var result))
            {
                return result;
            }
            result = obj.GetComponent<ScrollRect>();
            scrollRectCache.Add(obj, result);
            return result;
        }

        Selectable GetSelectableOf(GameObject obj)
        {
            if (selectableCache.TryGetValue(obj, out var result))
            {
                return result;
            }
            result = obj.GetComponent<Selectable>();
            selectableCache.Add(obj, result);
            return result;
        }


    }
}
