using UnityEngine;
using UnityEngine.InputSystem;
using R3;

namespace IwakenLabUnityStudy
{
    /// <summary>
    /// 描画されたノードデータを受け取り、武器としてGameObject化するクラス
    /// </summary>
    public class BattleWeapon : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private float lineWidth = 0.1f;
        [SerializeField] private Material lineMaterial;
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float targetSize = 3.0f; // 武器の目標サイズ（最大寸法）
        [SerializeField] private float swingAngle = 45f; // 振りの角度
        [SerializeField] private float swingSpeed = 10f; // 振りから戻る速度
        [SerializeField] private float hitThresholdAngle = 35f; // Hit判定が有効になる最小角度

        private readonly Subject<Collider> _onHit = new();
        public Observable<Collider> OnHit => _onHit;

        private Vector3[] _nodes;
        private Vector3[] _baseNodes; // 回転前の基準ノード座標
        private Vector3 _pivotPoint; // 回転の中心点
        private GameObject _colliderContainer;
        private float _currentSwingAngle = 0f; // 現在の振り角度

        /// <summary>
        /// ノードデータを受け取り、武器を初期化する
        /// </summary>
        /// <param name="nodes">描画されたノードの座標配列</param>
        public void Initialize(Vector3[] nodes)
        {
            if (nodes == null || nodes.Length < 2)
            {
                Debug.LogWarning("BattleWeapon: ノードが不足しています");
                return;
            }

            // ノードを目標サイズにスケーリング
            var scaledNodes = ScaleNodesToTargetSize(nodes);
            _nodes = scaledNodes;
            _baseNodes = (Vector3[])scaledNodes.Clone(); // 基準座標を保存

            // 回転の中心点を計算
            var bounds = new Bounds(scaledNodes[0], Vector3.zero);
            foreach (var node in scaledNodes)
            {
                bounds.Encapsulate(node);
            }

            _pivotPoint = bounds.center;

            // LineRendererの設定
            SetupLineRenderer(scaledNodes);

            // Colliderの設定
            SetupCollider(scaledNodes);
        }

        /// <summary>
        /// ノード配列を目標サイズにスケーリングする
        /// </summary>
        private Vector3[] ScaleNodesToTargetSize(Vector3[] nodes)
        {
            // バウンディングボックスを計算
            var bounds = new Bounds(nodes[0], Vector3.zero);
            foreach (var node in nodes)
            {
                bounds.Encapsulate(node);
            }

            // 最大寸法を取得（X軸とY軸の大きい方）
            var maxDimension = Mathf.Max(bounds.size.x, bounds.size.y);

            // スケールが小さすぎる場合は処理をスキップ
            if (maxDimension < 0.001f)
            {
                return nodes;
            }

            // スケール比率を計算
            var scaleFactor = targetSize / maxDimension;

            // 中心点を基準にスケーリング
            var center = bounds.center;
            var scaledNodes = new Vector3[nodes.Length];

            for (var i = 0; i < nodes.Length; i++)
            {
                var offset = nodes[i] - center;
                scaledNodes[i] = center + offset * scaleFactor;
            }

            return scaledNodes;
        }

        private void SetupLineRenderer(Vector3[] nodes)
        {
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            lineRenderer.positionCount = nodes.Length;
            lineRenderer.SetPositions(nodes);
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.useWorldSpace = true;

            if (lineMaterial != null)
            {
                lineRenderer.material = lineMaterial;
            }
        }

        private void SetupCollider(Vector3[] nodes)
        {
            // ノード全体の中心を計算
            var bounds = new Bounds(nodes[0], Vector3.zero);
            foreach (var node in nodes)
            {
                bounds.Encapsulate(node);
            }

            transform.position = bounds.center;

            // コライダー用のコンテナを作成
            _colliderContainer = new GameObject("Colliders");
            _colliderContainer.transform.SetParent(transform);
            _colliderContainer.transform.localPosition = Vector3.zero;

            // 各セグメントにCapsuleColliderを配置
            for (var i = 0; i < nodes.Length - 1; i++)
            {
                CreateSegmentCollider(nodes[i], nodes[i + 1], i);
            }

            // Rigidbodyを追加（トリガー検出用）
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        private void CreateSegmentCollider(Vector3 start, Vector3 end, int index)
        {
            var segmentObj = new GameObject($"Segment_{index}");
            segmentObj.transform.SetParent(_colliderContainer.transform);

            // セグメントの中点に配置
            var midPoint = (start + end) / 2f;
            segmentObj.transform.position = midPoint;

            // セグメントの方向に回転
            var direction = end - start;
            var length = direction.magnitude;
            if (length > 0.001f)
            {
                segmentObj.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            }

            // CapsuleColliderを追加
            var capsule = segmentObj.AddComponent<CapsuleCollider>();
            capsule.height = length + lineWidth;
            capsule.radius = lineWidth / 2f;
            capsule.direction = 1; // Y軸方向
            capsule.isTrigger = true;

            // 当たり判定をこのオブジェクトに転送するコンポーネントを追加
            var forwarder = segmentObj.AddComponent<ColliderForwarder>();
            forwarder.Initialize(this);
        }

        public void NotifyHit(Collider other)
        {
            // 振りの角度が閾値以上の時のみHit判定を有効にする
            if (_currentSwingAngle < hitThresholdAngle) return;

            _onHit.OnNext(other);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            // スペースキーで剣を振る
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                _currentSwingAngle = swingAngle; // 反時計回りに振る
            }

            // 振りから戻る処理
            if (_currentSwingAngle > 0f)
            {
                _currentSwingAngle -= swingSpeed * Time.deltaTime * 100f;
                if (_currentSwingAngle < 0f)
                {
                    _currentSwingAngle = 0f;
                }

                ApplySwingRotation();
            }

            // 十字キー/WASDで移動
            var horizontal = 0f;
            var vertical = 0f;

            // WASD
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical += 1f;

            if (horizontal == 0f && vertical == 0f) return;

            var movement = new Vector3(horizontal, vertical, 0f) * (moveSpeed * Time.deltaTime);
            transform.Translate(movement, Space.World);

            // 基準ノードと中心点も移動
            _pivotPoint += movement;
            for (var i = 0; i < _baseNodes.Length; i++)
            {
                _baseNodes[i] += movement;
            }

            // LineRendererのノード位置も更新（useWorldSpace=trueのため）
            if (_nodes != null && lineRenderer != null)
            {
                for (var i = 0; i < _nodes.Length; i++)
                {
                    _nodes[i] += movement;
                }

                lineRenderer.SetPositions(_nodes);
            }
        }

        /// <summary>
        /// 振りの回転をノードに適用する
        /// </summary>
        private void ApplySwingRotation()
        {
            if (_baseNodes == null || lineRenderer == null) return;

            // Z軸周りの回転（反時計回り = 正の角度）
            var rotation = Quaternion.Euler(0f, 0f, _currentSwingAngle);

            for (var i = 0; i < _baseNodes.Length; i++)
            {
                var offset = _baseNodes[i] - _pivotPoint;
                var rotatedOffset = rotation * offset;
                _nodes[i] = _pivotPoint + rotatedOffset;
            }

            lineRenderer.SetPositions(_nodes);

            // コライダーの位置も更新
            UpdateColliderPositions();
        }

        /// <summary>
        /// コライダーの位置を現在のノードに合わせて更新する
        /// </summary>
        private void UpdateColliderPositions()
        {
            if (_colliderContainer == null) return;

            for (var i = 0; i < _nodes.Length - 1; i++)
            {
                var segmentTransform = _colliderContainer.transform.GetChild(i);
                if (segmentTransform == null) continue;

                var start = _nodes[i];
                var end = _nodes[i + 1];

                // セグメントの中点に配置
                segmentTransform.position = (start + end) / 2f;

                // セグメントの方向に回転
                var direction = end - start;
                if (direction.magnitude > 0.001f)
                {
                    segmentTransform.rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
                }
            }
        }

        private void OnDestroy()
        {
            _onHit.Dispose();
        }
    }
}
