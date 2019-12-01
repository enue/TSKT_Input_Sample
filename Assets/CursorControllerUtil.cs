using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace TSKT
{
    public class CursorControllerUtil
    {
        public static Dictionary<GameObject, DirectionMap<GameObject>> BuildAutoGraph(List<GameObject> items)
        {
            var graph = new Dictionary<GameObject, DirectionMap<GameObject>>();

            if (items.Count == 0)
            {
                return graph;
            }

            var rectMap = new Dictionary<GameObject, Rect>();
            var corners = new Vector3[4];
            foreach (var item in items)
            {
                if (!item)
                {
                    continue;
                }
                ((RectTransform)item.transform).GetWorldCorners(corners);
                var rect = Rect.MinMaxRect(
                    corners.Min(_ => _.x),
                    corners.Min(_ => _.y),
                    corners.Max(_ => _.x),
                    corners.Max(_ => _.y));
                rectMap.Add(item, rect);
            }

            foreach (var pair in rectMap)
            {
                var item = pair.Key;
                var rect = pair.Value;
                DirectionMap<GameObject> nexts = null;
                var slider = item.GetComponent<Slider>();
                var scrollRect = item.GetComponent<ScrollRect>();

                foreach (var direction in Vector2IntUtil.Directions)
                {
                    if (slider)
                    {
                        if (slider.direction == Slider.Direction.LeftToRight
                            || slider.direction == Slider.Direction.RightToLeft)
                        {
                            if (direction.x != 0)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (direction.y != 0)
                            {
                                continue;
                            }
                        }
                    }
                    if (scrollRect)
                    {
                        if (scrollRect.horizontal && direction.x != 0)
                        {
                            continue;
                        }
                        if (scrollRect.vertical && direction.y != 0)
                        {
                            continue;
                        }
                    }

                    float? minDistanceScore = null;
                    GameObject next = null;

                    foreach (var subPair in rectMap)
                    {
                        var it = subPair.Key;
                        if (it == item)
                        {
                            continue;
                        }
                        var targetRect = subPair.Value;
                        var distance = targetRect.center - rect.center;
                        distance.y *= -1f;
                        var dot = distance.x * direction.x + distance.y * direction.y;

                        if (dot < 0f)
                        {
                            continue;
                        }

                        bool inRange;
                        float length;
                        if (direction.x != 0f)
                        {
                            inRange = (Mathf.Abs(distance.y) < ((rect.height + targetRect.height) / 2f / 2f));
                            length = Mathf.Abs(distance.x);
                        }
                        else
                        {
                            inRange = (Mathf.Abs(distance.x) < ((rect.width + targetRect.width) / 2f / 2f));
                            length = Mathf.Abs(distance.y);
                        }

                        if (inRange)
                        {
                            if (!minDistanceScore.HasValue
                                || minDistanceScore.Value > length)
                            {
                                minDistanceScore = length;
                                next = it;
                            }
                        }
                    }
                    if (next)
                    {
                        if (nexts == null)
                        {
                            nexts = new DirectionMap<GameObject>();
                            graph.Add(item, nexts);
                        }
                        nexts[direction] = next;
                    }
                }
            }
            return graph;
        }
        public static Dictionary<GameObject, DirectionMap<GameObject>> BuildMultiVerticalColumnsGraph(
            bool loop = false,
            params List<GameObject>[] columns)
        {
            columns = columns.Where(_ => _.Count > 0).ToArray();
            var graph = new Dictionary<GameObject, DirectionMap<GameObject>>();

            for (var c = 0; c < columns.Length; ++c)
            {
                var items = columns[c];

                GameObject leftColumnHead = null;
                GameObject rightColumnHead = null;
                if (columns.Length > 1)
                {
                    if (c > 0)
                    {
                        leftColumnHead = columns[c - 1][0];
                    }
                    else if (loop)
                    {
                        leftColumnHead = columns[columns.Length - 1][0];
                    }

                    if (c < columns.Length - 1)
                    {
                        rightColumnHead = columns[c + 1][0];
                    }
                    else if (loop)
                    {
                        rightColumnHead = columns[0][0];
                    }
                }

                for (int i = 0; i < items.Count; ++i)
                {
                    var nexts = new DirectionMap<GameObject>();
                    graph.Add(items[i], nexts);
                    nexts.Down = items[(i - 1 + items.Count) % items.Count];
                    nexts.Up = items[(i + 1) % items.Count];
                    if (leftColumnHead)
                    {
                        nexts.Left = leftColumnHead;
                    }
                    if (rightColumnHead)
                    {
                        nexts.Right = rightColumnHead;
                    }
                }
            }

            return graph;
        }
        public static Dictionary<GameObject, DirectionMap<GameObject>> BuildMultiRowsGraph(
            bool loop = false,
            bool containsInactiveObjects = true,
            params GameObject[] items)
        {
            var rects = new Dictionary<GameObject, Rect>();
            var rows = new Dictionary<GameObject, List<GameObject>>();
            var corners = new Vector3[4];
            foreach (var it in items)
            {
                if (!it)
                {
                    continue;
                }
                if (!containsInactiveObjects)
                {
                    if (!it.activeInHierarchy)
                    {
                        continue;
                    }
                }
                var rowHead = rects.FirstOrDefault(_ =>
                    _.Value.yMin <= it.transform.position.y
                    && _.Value.yMax >= it.transform.position.y)
                    .Key;

                if (rowHead)
                {
                    rows[rowHead].Add(it);
                }
                else
                {
                    var row = new List<GameObject>() { it };
                    rows.Add(it, row);
                    ((RectTransform)it.transform).GetWorldCorners(corners);
                    var rect = Rect.MinMaxRect(
                        corners.Min(_ => _.x),
                        corners.Min(_ => _.y),
                        corners.Max(_ => _.x),
                        corners.Max(_ => _.y));
                    rects.Add(it, rect);
                }
            }

            var sortedRows = rows
                .OrderBy(_ => _.Key.transform.position.y)
                .Select(_ => _.Value)
                .Select(_ => _.OrderBy(obj => obj.transform.position.x).ToArray())
                .ToArray();

            return BuildMultiRowsGraph(loop: loop, rows: sortedRows);
        }

        public static Dictionary<GameObject, DirectionMap<GameObject>> BuildMultiRowsGraph(
            bool loop = false,
            params GameObject[][] rows)
        {
            rows = rows.Where(_ => _.Length > 0).ToArray();
            var graph = new Dictionary<GameObject, DirectionMap<GameObject>>();

            for (var c = 0; c < rows.Length; ++c)
            {
                var items = rows[c];

                GameObject upRowHead = null;
                GameObject bottomRowHead = null;
                if (rows.Length > 1)
                {
                    if (c > 0)
                    {
                        upRowHead = rows[c - 1][0];
                    }
                    else if (loop)
                    {
                        upRowHead = rows[rows.Length - 1][0];
                    }

                    if (c < rows.Length - 1)
                    {
                        bottomRowHead = rows[c + 1][0];
                    }
                    else if (loop)
                    {
                        bottomRowHead = rows[0][0];
                    }
                }

                for (int i = 0; i < items.Length; ++i)
                {
                    var nexts = new DirectionMap<GameObject>();
                    graph.Add(items[i], nexts);
                    nexts.Left = items[(i - 1 + items.Length) % items.Length];
                    nexts.Right = items[(i + 1) % items.Length];
                    if (upRowHead)
                    {
                        nexts.Up = upRowHead;
                    }
                    if (bottomRowHead)
                    {
                        nexts.Down = bottomRowHead;
                    }
                }
            }

            return graph;
        }

    }

}