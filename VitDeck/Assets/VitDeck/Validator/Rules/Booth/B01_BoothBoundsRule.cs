using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace VitDeck.Validator
{
    /// <summary>
    /// ブースのサイズ制限を調べるルール
    /// </summary>
    public class BoothBoundsRule : BaseRule
    {
        private readonly Bounds limit;
        private readonly string accuracy;

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="name">ルールの名前</param>
        /// <param name="size">バウンディングボックスの大きさ</param>
        /// <param name="margin">制限に持たせる余裕</param>
        public BoothBoundsRule(string name, Vector3 size, float margin)
            : this(name, size, margin, pivot: Vector3.zero) { }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        /// <param name="name">ルールの名前</param>
        /// <param name="size">バウンディングボックスの大きさ</param>
        /// <param name="margin">制限に持たせる余裕</param>
        /// <param name="pivot">バウンディングボックスの原点（中心下）</param>
        public BoothBoundsRule(string name, Vector3 size, float margin, Vector3 pivot) : base(name)
        {
            var center = pivot + (Vector3.up * size.y * 0.5f);
            var limit = new Bounds(center, size);
            limit.Expand(margin);
            this.limit = limit;
            //size, marginのうち最小の桁数が指定されたものの小数点以下の桁数+1桁の表示精度を指定
            var settingValues = new float[] { size.x, size.y, size.z, margin };
            var DigitsCount = settingValues.Select(val => GetDigitCountUnderPoint(val)).Max<int>();
            accuracy = string.Format("f{0}", DigitsCount + 1);
        }

        private int GetDigitCountUnderPoint(float val)
        {
            var pointIndex = val.ToString().IndexOf(".");
            if (pointIndex == -1)
                return 0;
            else
                return val.ToString().Substring(pointIndex).Length - 1;
        }

        protected override void Logic(ValidationTarget target)
        {
            var oversizes = target
                .GetAllObjects()
                .SelectMany(GetObjectBounds)
                .Where(data => !LimitContains(data.bounds));

            foreach (var oversize in oversizes)
            {
                var limitSize = limit.size.ToString();
                var message = string.Format("オブジェクトがブースサイズ制限{0}の外に出ています。{4}制限={1}{4}対象={2}{4}オブジェクトの種類={3}", limitSize, limit.ToString(accuracy), oversize.bounds.ToString(accuracy), oversize.objectReference.GetType().Name, System.Environment.NewLine);
                AddIssue(new Issue(oversize.objectReference, IssueLevel.Error, message));
            }
        }

        private bool LimitContains(Bounds bounds)
        {
            return limit.Contains(bounds.min) && limit.Contains(bounds.max);
        }

        private static IEnumerable<BoundsData> GetObjectBounds(GameObject gameObject)
        {
            var transform = gameObject.transform;
            if (transform is RectTransform)
            {
                yield return BoundsData.FromRectTransform((RectTransform)transform);
            }
            else
            {
                yield return BoundsData.FromTransform(transform);
            }

            foreach (var renderer in gameObject.GetComponents<Renderer>())
            {
                yield return BoundsData.FromRenderer(renderer);
            }
        }

        private struct BoundsData
        {
            public readonly Object objectReference;
            public readonly Bounds bounds;

            public BoundsData(Object objectReference, Vector3 center)
                : this(objectReference, center, Vector3.zero) { }

            public BoundsData(Object objectReference, Vector3 center, Vector3 size)
                : this(objectReference, new Bounds(center, size)) { }

            public BoundsData(Object objectReference, Bounds bounds)
            {
                if (objectReference == null)
                    throw new System.ArgumentNullException("objectReference");
                this.objectReference = objectReference;
                this.bounds = bounds;
            }

            public static BoundsData FromTransform(Transform transform)
            {
                return new BoundsData(transform, transform.position);
            }

            public static BoundsData FromRectTransform(RectTransform transform)
            {
                var bounds = new Bounds(transform.position, Vector3.zero);

                var corners = new Vector3[4];
                transform.GetWorldCorners(corners);
                foreach (var corner in corners)
                {
                    bounds.Encapsulate(corner);
                }

                return new BoundsData(transform, bounds);
            }

            public static BoundsData FromRenderer(Renderer renderer)
            {
                //Recalculate bounds for ParticleSystem
                var particleSystem = renderer.gameObject.GetComponent<ParticleSystem>();
                if (particleSystem != null)
                    particleSystem.Simulate(0f);
                //Reculculate bounds for TrailRenderer
                if (renderer is TrailRenderer)
                {
                    var originalFlag = renderer.enabled;
                    renderer.enabled = !originalFlag;
                    renderer.enabled = originalFlag;
                }
                return new BoundsData(renderer, renderer.bounds);
            }
        }
    }
}
