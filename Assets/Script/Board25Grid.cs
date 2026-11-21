using UnityEngine;

/// <summary>
/// 挂在 board0 上。board0 自己没有精灵，格子是子物体 Cell_列_行，共 8x8。
/// 列从近角走向右上，行从近角走向左上，都从 0 开始。
/// </summary>
public class Board25Grid : BoardGrid
{
    const int BoardSize = 8;
    const string PieceLayer = "Chess Piece";

    Transform[,] _cells;

    static readonly int Facing = Animator.StringToHash("facing");
    static readonly int Moving = Animator.StringToHash("moving");

    static Material _diamondMaterial;

    protected override void BeginPieceStep(int fromCol, int fromRow, int toCol, int toRow)
    {
        Animator animator = PieceAnimator(fromCol, fromRow);
        if (animator == null || !HasParameter(animator, Facing) || !HasParameter(animator, Moving))
            return;

        int facing = FacingOf(toCol - fromCol, toRow - fromRow);
        if (facing == 0)
            return;

        animator.SetInteger(Facing, facing);
        animator.SetBool(Moving, true);
    }

    protected override void EndPieceMove(GameObject piece)
    {
        if (piece == null)
            return;

        Animator animator = piece.GetComponentInChildren<Animator>(true);
        if (animator == null || !HasParameter(animator, Moving))
            return;

        animator.SetBool(Moving, false);
    }

    static int FacingOf(int dx, int dy)
    {
        if (dx > 0)
            return 1;
        if (dx < 0)
            return 2;
        if (dy > 0)
            return 3;
        if (dy < 0)
            return 4;
        return 0;
    }

    public override bool FaceToward(int fromCol, int fromRow, int toCol, int toRow)
    {
        return ApplyIdleFacing(fromCol, fromRow, ConeFacing(toCol - fromCol, toRow - fromRow));
    }

    protected override bool TrySetIdleFacing(int fromCol, int fromRow, int toCol, int toRow)
    {
        return ApplyIdleFacing(fromCol, fromRow, IdleFacing(toCol - fromCol, toRow - fromRow));
    }

    bool ApplyIdleFacing(int fromCol, int fromRow, int facing)
    {
        if (facing == 0)
            return false;

        Animator animator = PieceAnimator(fromCol, fromRow);
        if (animator == null || !HasParameter(animator, Facing) || !HasParameter(animator, Moving))
            return false;

        animator.SetInteger(Facing, facing);
        animator.SetBool(Moving, false);
        return true;
    }

    // 以敌人为原点，用和 y=x、y=-x+8 相同的两条对角线切开。
    // y 更大的三角是 tenidle，y 更小是 fouridle，x 更大是 twoidle，x 更小是 eightidle。
    // 正好落在对角线上时归到左右两个朝向。
    static int ConeFacing(int dx, int dy)
    {
        if (dx == 0 && dy == 0)
            return 0;

        int ax = Mathf.Abs(dx);
        if (dy > ax)
            return 3;
        if (dy < -ax)
            return 4;
        if (dx > 0)
            return 1;
        return 2;
    }

    static int IdleFacing(int dx, int dy)
    {
        if (dx == 0 && dy == 0)
            return 0;
        if (dx < 0 && dy < 0)
            return 4;
        if (dx > 0 && dy > 0)
            return 3;
        if (dx > 0 && dy < 0)
            return 1;
        if (dx < 0 && dy > 0)
            return 2;
        return FacingOf(dx, dy);
    }

    Animator PieceAnimator(int col, int row)
    {
        if (cellContent == null || col < 0 || row < 0 || col >= columns || row >= rows)
            return null;

        CellContent content = cellContent[col, row];
        if (content == null || content.instance == null)
            return null;

        return content.instance.GetComponentInChildren<Animator>(true);
    }

    static bool HasParameter(Animator animator, int hash)
    {
        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].nameHash == hash)
                return true;
        }
        return false;
    }

    protected override void ApplyPieceOrder(GameObject piece, int col, int row)
    {
        if (piece == null)
            return;

        int order = 14 - col - row;
        Renderer[] renderers = piece.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingLayerName = PieceLayer;
            renderers[i].sortingOrder = order;
        }
    }

    void Reset()
    {
        columns = BoardSize;
        rows = BoardSize;
    }

    protected override bool CanUseSetup()
    {
        return setup != null && setup.pieces != null
            && setup.columns == columns && setup.rows == rows;
    }

    bool EnsureCells()
    {
        columns = BoardSize;
        rows = BoardSize;
        if (_cells != null && _cells[0, 0] != null)
            return true;

        _cells = new Transform[BoardSize, BoardSize];
        bool any = false;
        for (int row = 0; row < BoardSize; row++)
        {
            for (int col = 0; col < BoardSize; col++)
            {
                Transform cell = transform.Find("Cell_" + col + "_" + row);
                _cells[col, row] = cell;
                if (cell != null)
                    any = true;
            }
        }
        return any;
    }

    static bool TryMetrics(Transform cell, out Vector3 center, out float halfW, out float halfH)
    {
        center = Vector3.zero;
        halfW = 0f;
        halfH = 0f;
        if (cell == null)
            return false;

        center = cell.localPosition;
        SpriteRenderer renderer = cell.GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null)
            return false;

        center += renderer.sprite.bounds.center;
        halfW = renderer.sprite.bounds.extents.x;
        halfH = renderer.sprite.bounds.extents.y;
        return halfW > 0.0001f && halfH > 0.0001f;
    }

    protected override bool TryGetPieceLocal(int col, int row, out Vector3 local)
    {
        local = Vector3.zero;
        if (!TryGetCellLocal(col, row, out local, out _))
            return false;

        local.z = -1f;
        return true;
    }

    public override bool TryWorldToCell(Vector3 world, out int col, out int row)
    {
        col = -1;
        row = -1;
        if (!EnsureCells())
            return false;

        Vector3 local = transform.InverseTransformPoint(world);
        float best = float.MaxValue;
        for (int r = 0; r < BoardSize; r++)
        {
            for (int c = 0; c < BoardSize; c++)
            {
                if (!TryMetrics(_cells[c, r], out Vector3 center, out float halfW, out float halfH))
                    continue;

                float score = Mathf.Abs(local.x - center.x) / halfW + Mathf.Abs(local.y - center.y) / halfH;
                if (score > 1f || score >= best)
                    continue;

                best = score;
                col = c;
                row = r;
            }
        }
        return col >= 0;
    }

    protected override bool TryGetCellLocal(int col, int row, out Vector3 localCenter, out Vector2 localSize)
    {
        localCenter = Vector3.zero;
        localSize = Vector2.zero;
        if (!EnsureCells() || col < 0 || row < 0 || col >= BoardSize || row >= BoardSize)
            return false;
        if (!TryMetrics(_cells[col, row], out localCenter, out float halfW, out float halfH))
            return false;

        localSize = new Vector2(halfW * 2f, halfH * 2f);
        return true;
    }

    protected override void PlaceOverlay(SpriteRenderer overlay, int col, int row, Color color)
    {
        overlay.enabled = true;
        overlay.sprite = null;
        overlay.sortingOrder = 1;
        overlay.transform.localPosition = Vector3.zero;
        overlay.transform.localRotation = Quaternion.identity;
        overlay.transform.localScale = Vector3.one;

        if (!TryGetCellLocal(col, row, out Vector3 center, out Vector2 size))
            return;

        float halfW = size.x * 0.5f;
        float halfH = size.y * 0.5f;
        Vector3 right = center + new Vector3(halfW, 0f, OverlayZ);
        Vector3 top = center + new Vector3(0f, halfH, OverlayZ);
        Vector3 left = center + new Vector3(-halfW, 0f, OverlayZ);
        Vector3 bottom = center + new Vector3(0f, -halfH, OverlayZ);
        DiamondMark mark = overlay.GetComponent<DiamondMark>();
        if (mark == null)
            mark = overlay.gameObject.AddComponent<DiamondMark>();
        mark.Show(overlay, new[] { right, top, left, bottom }, color);
    }

    void OnDrawGizmosSelected()
    {
        if (!EnsureCells())
            return;

        Gizmos.color = new Color(0.2f, 1f, 0.45f, 0.9f);
        for (int row = 0; row < BoardSize; row++)
        {
            for (int col = 0; col < BoardSize; col++)
            {
                if (!TryMetrics(_cells[col, row], out Vector3 center, out float halfW, out float halfH))
                    continue;

                Vector3 right = transform.TransformPoint(center + new Vector3(halfW, 0f, 0f));
                Vector3 top = transform.TransformPoint(center + new Vector3(0f, halfH, 0f));
                Vector3 left = transform.TransformPoint(center + new Vector3(-halfW, 0f, 0f));
                Vector3 bottom = transform.TransformPoint(center + new Vector3(0f, -halfH, 0f));
                Gizmos.DrawLine(right, top);
                Gizmos.DrawLine(top, left);
                Gizmos.DrawLine(left, bottom);
                Gizmos.DrawLine(bottom, right);
            }
        }
    }

    static Material DiamondMaterial()
    {
        if (_diamondMaterial != null)
            return _diamondMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            return null;

        _diamondMaterial = new Material(shader);
        _diamondMaterial.mainTexture = Texture2D.whiteTexture;
        return _diamondMaterial;
    }

    sealed class DiamondMark : MonoBehaviour
    {
        SpriteRenderer _sprite;
        MeshRenderer _meshRenderer;
        Mesh _mesh;
        readonly Color[] _colors = new Color[4];

        public void Show(SpriteRenderer sprite, Vector3[] corners, Color color)
        {
            if (corners == null || corners.Length != 4)
                return;

            _sprite = sprite;
            EnsureView();
            if (_mesh == null || _meshRenderer == null)
                return;

            for (int i = 0; i < 4; i++)
                _colors[i] = color;
            _mesh.vertices = corners;
            _mesh.colors = _colors;
            _mesh.RecalculateBounds();

            _meshRenderer.sortingOrder = 1;
            if (_meshRenderer != null)
                _meshRenderer.enabled = true;
        }

        void EnsureView()
        {
            if (_mesh != null && _meshRenderer != null)
                return;

            Transform view = transform.Find("Diamond");
            GameObject go;
            if (view == null)
            {
                go = new GameObject("Diamond");
                go.transform.SetParent(transform, false);
            }
            else
            {
                go = view.gameObject;
            }

            if (_mesh == null)
            {
                _mesh = new Mesh { name = "CellDiamond" };
                _mesh.vertices = new Vector3[4];
                _mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                _mesh.uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f),
                };
            }

            MeshFilter filter = go.GetComponent<MeshFilter>();
            if (filter == null)
                filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = _mesh;

            if (_meshRenderer == null)
                _meshRenderer = go.GetComponent<MeshRenderer>();
            if (_meshRenderer == null)
                _meshRenderer = go.AddComponent<MeshRenderer>();

            Material material = DiamondMaterial();
            if (material != null)
                _meshRenderer.sharedMaterial = material;
        }

        void LateUpdate()
        {
            if (_sprite == null)
                _sprite = GetComponent<SpriteRenderer>();
            if (_meshRenderer == null || _sprite == null)
                return;
            _meshRenderer.enabled = _sprite.enabled;
        }
    }
}
