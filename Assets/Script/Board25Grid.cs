using UnityEngine;

/// <summary>
/// 挂在 board0 上。board0 自己没有精灵，格子是子物体 Cell_列_行，共 8x8。
/// 列从近角走向右上，行从近角走向左上，都从 0 开始。
/// 角色站在格子点上方。最下方一格 (0,0) 的角色世界坐标是 bottomPieceWorld。
/// 名字以 block 开头的障碍用另一层高度，最下方一格是 bottomBlockWorld。
/// </summary>
public class Board25Grid : BoardGrid
{
    [Header("最下方格子上的角色世界坐标")]
    public Vector3 bottomPieceWorld = new Vector3(0f, -3f, -1f);

    readonly Vector3 bottomBlockWorld = new Vector3(0f, -3.8f, -1f);
    static readonly Vector3 BottomPieceDefault = new Vector3(0f, -3f, -1f);

    const int BoardSize = 8;
    const string PieceLayer = "Chess Piece";

    Transform[,] _cells;

    static readonly int Facing = Animator.StringToHash("facing");
    static readonly int Moving = Animator.StringToHash("moving");

    static Material _diamondMaterial;
    static Sprite _cellDiamond;

    const int DiamondPixelsX = 128;
    const int DiamondPixelsY = 64;
    const float CellDiamondWidth = 2f;
    const float CellDiamondHeight = 1f;

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

    // 格子图是 1200 的方画布，不透明部分才是 2:1 的扁菱形。
    // Sprite.bounds 往往是整张画布，拿它画高亮会变成正菱形。
    // 这里用同一张图的两格间距，得到和地砖一样的半对角线。
    bool TryPitch(out float halfW, out float halfH)
    {
        halfW = 0f;
        halfH = 0f;
        if (!EnsureCells())
            return false;

        Transform origin = _cells[0, 0];
        Transform alongCol = _cells[2, 0];
        if (origin == null || alongCol == null)
            return false;

        Vector3 step = (alongCol.localPosition - origin.localPosition) * 0.5f;
        halfW = Mathf.Abs(step.x);
        halfH = Mathf.Abs(step.y);
        return halfW > 0.0001f && halfH > 0.0001f;
    }

    bool TryMetrics(int col, int row, out Vector3 center, out float halfW, out float halfH)
    {
        center = Vector3.zero;
        halfW = 0f;
        halfH = 0f;
        if (!EnsureCells() || col < 0 || row < 0 || col >= BoardSize || row >= BoardSize)
            return false;
        if (_cells[col, row] == null)
            return false;

        if (TryPitch(out halfW, out halfH))
        {
            center = new Vector3((col - row) * halfW, (col + row - (BoardSize - 1)) * halfH, 0f);
            return true;
        }

        return TryVertexDiamond(_cells[col, row], out center, out halfW, out halfH);
    }

    static bool TryVertexDiamond(Transform cell, out Vector3 center, out float halfW, out float halfH)
    {
        center = Vector3.zero;
        halfW = 0f;
        halfH = 0f;
        if (cell == null)
            return false;

        SpriteRenderer renderer = cell.GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null)
            return false;

        Vector2[] verts = renderer.sprite.vertices;
        if (verts == null || verts.Length == 0)
            return false;

        float minX = verts[0].x;
        float maxX = minX;
        float minY = verts[0].y;
        float maxY = minY;
        for (int i = 1; i < verts.Length; i++)
        {
            Vector2 v = verts[i];
            if (v.x < minX) minX = v.x;
            if (v.x > maxX) maxX = v.x;
            if (v.y < minY) minY = v.y;
            if (v.y > maxY) maxY = v.y;
        }

        float width = maxX - minX;
        float height = maxY - minY;
        if (width < 0.0001f || height < 0.0001f)
            return false;

        // 顶点框接近正方形时，这是整张画布，不是扁菱形。
        if (width < height * 1.5f && height < width * 1.5f)
            return false;

        Vector3 scale = cell.localScale;
        Vector3 mid = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        center = cell.localPosition + Vector3.Scale(mid, scale);
        halfW = width * 0.5f * Mathf.Abs(scale.x);
        halfH = height * 0.5f * Mathf.Abs(scale.y);
        return halfW > 0.0001f && halfH > 0.0001f;
    }

    protected override bool TryGetPieceLocal(int col, int row, out Vector3 local)
    {
        local = Vector3.zero;
        if (!TryGetCellLocal(0, 0, out Vector3 bottomLocal, out _))
            return false;
        if (!TryGetCellLocal(col, row, out Vector3 cellLocal, out _))
            return false;

        Vector3 anchor = AnchorFor(PieceSource(col, row));
        Vector3 bottomWorld = transform.TransformPoint(bottomLocal);
        Vector3 cellWorld = transform.TransformPoint(cellLocal);
        Vector3 standWorld = anchor + (cellWorld - bottomWorld);
        standWorld.z = anchor.z;
        local = transform.InverseTransformPoint(standWorld);
        return true;
    }

    // 障碍比角色默认锚点低 0.8。场景里的 Bottom Piece World 改过之后，
    // 仍用原来的绝对坐标会把 block 留在旧高度，所以只保留这个高度差。
    Vector3 AnchorFor(GameObject piece)
    {
        if (!IsBlock(piece))
            return bottomPieceWorld;
        return bottomPieceWorld + (bottomBlockWorld - BottomPieceDefault);
    }

    GameObject PieceSource(int col, int row)
    {
        if (cellContent != null && col >= 0 && row >= 0 && col < columns && row < rows)
        {
            CellContent content = cellContent[col, row];
            if (content != null)
                return content.prefab != null ? content.prefab : content.instance;
        }

        if (setup != null)
            return setup.GetPrefab(col, row);
        return null;
    }

    static bool IsBlock(GameObject piece)
    {
        if (piece == null)
            return false;
        if (piece.name.StartsWith("block", System.StringComparison.OrdinalIgnoreCase))
            return true;

        SpriteRenderer renderer = piece.GetComponent<SpriteRenderer>();
        return renderer != null && renderer.sprite != null
            && renderer.sprite.name.StartsWith("block", System.StringComparison.OrdinalIgnoreCase);
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
                if (!TryMetrics(c, r, out Vector3 center, out float halfW, out float halfH))
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
        if (!TryMetrics(col, row, out localCenter, out float halfW, out float halfH))
            return false;

        localSize = new Vector2(halfW * 2f, halfH * 2f);
        return true;
    }

    // 地砖网格是扁菱形。整张方画布的包围盒接近正方形，那种不用。
    bool TryVisualDiamond(Transform cell, out Vector3 center, out float halfW, out float halfH)
    {
        center = Vector3.zero;
        halfW = 0f;
        halfH = 0f;
        if (cell == null)
            return false;

        SpriteRenderer renderer = cell.GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null)
            return false;

        Bounds world = renderer.bounds;
        float width = world.size.x;
        float height = world.size.y;
        if (width < 0.0001f || height < 0.0001f)
            return false;
        if (width < height * 1.5f && height < width * 1.5f)
            return false;

        Vector3 lossy = transform.lossyScale;
        center = transform.InverseTransformPoint(world.center);
        halfW = world.extents.x / Mathf.Max(0.0001f, Mathf.Abs(lossy.x));
        halfH = world.extents.y / Mathf.Max(0.0001f, Mathf.Abs(lossy.y));
        return halfW > 0.0001f && halfH > 0.0001f;
    }

    static void ExpandPitchToSprite(Transform cell, ref float halfW, ref float halfH)
    {
        float ppu = 100f;
        if (cell != null)
        {
            SpriteRenderer renderer = cell.GetComponent<SpriteRenderer>();
            if (renderer != null && renderer.sprite != null)
                ppu = Mathf.Max(1f, renderer.sprite.pixelsPerUnit);
        }

        float dist = Mathf.Sqrt(halfW * halfW + halfH * halfH);
        float bleed = 1f / ppu;
        if (dist <= bleed)
            return;

        float grow = dist / (dist - bleed);
        halfW *= grow;
        halfH *= grow;
    }

    protected override void PlaceOverlay(SpriteRenderer overlay, int col, int row, Color color)
    {
        DiamondMark mark = overlay.GetComponent<DiamondMark>();
        if (mark != null)
            mark.Hide();

        // 间距比地砖小一圈，按间距画会在格子边上留缝。铺满用地砖自己的菱形。
        Transform cell = null;
        if (EnsureCells() && col >= 0 && row >= 0 && col < BoardSize && row < BoardSize)
            cell = _cells[col, row];
        bool filled = TryVisualDiamond(cell, out Vector3 center, out float halfW, out float halfH)
            || TryVertexDiamond(cell, out center, out halfW, out halfH);
        if (!filled && TryMetrics(col, row, out center, out halfW, out halfH))
        {
            ExpandPitchToSprite(cell, ref halfW, ref halfH);
            filled = true;
        }
        if (!filled)
        {
            overlay.enabled = false;
            return;
        }

        // 悬停、点击、移动、普攻、施法都走这里。
        // 地砖和底座是 Default 层顺序 0，高亮用同一层顺序 1，盖在格子上。
        // 角色在棋子层，仍画在高亮上面。
        UseSpriteMaterial(overlay);
        Sprite diamond = CellDiamond();
        overlay.enabled = true;
        overlay.sprite = diamond;
        overlay.color = color;
        overlay.flipX = false;
        overlay.flipY = false;
        overlay.sortingLayerID = 0;
        overlay.sortingOrder = 1;
        center.z = 0f;
        overlay.transform.localPosition = center;
        overlay.transform.localRotation = Quaternion.identity;
        overlay.transform.localScale = new Vector3(
            halfW * 2f / CellDiamondWidth,
            halfH * 2f / CellDiamondHeight,
            1f);
    }

    static void UseSpriteMaterial(SpriteRenderer overlay)
    {
        Shader shader = overlay.sharedMaterial != null ? overlay.sharedMaterial.shader : null;
        if (shader != null && shader.name == "Sprites/Default")
            return;

        Shader sprites = Shader.Find("Sprites/Default");
        if (sprites != null)
            overlay.sharedMaterial = new Material(sprites);
    }

    static Sprite CellDiamond()
    {
        if (_cellDiamond != null)
            return _cellDiamond;

        var tex = new Texture2D(DiamondPixelsX, DiamondPixelsY, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.HideAndDontSave;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Point;
        float cx = (DiamondPixelsX - 1) * 0.5f;
        float cy = (DiamondPixelsY - 1) * 0.5f;
        for (int y = 0; y < DiamondPixelsY; y++)
        {
            for (int x = 0; x < DiamondPixelsX; x++)
            {
                float score = Mathf.Abs(x - cx) / cx + Mathf.Abs(y - cy) / cy;
                tex.SetPixel(x, y, score <= 1f ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        }

        tex.Apply();
        _cellDiamond = Sprite.Create(
            tex,
            new Rect(0f, 0f, DiamondPixelsX, DiamondPixelsY),
            new Vector2(0.5f, 0.5f),
            DiamondPixelsY / CellDiamondHeight);
        _cellDiamond.hideFlags = HideFlags.HideAndDontSave;
        return _cellDiamond;
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
                if (!TryMetrics(col, row, out Vector3 center, out float halfW, out float halfH))
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
        bool _visible;
        readonly Color[] _colors = new Color[4];

        public void Hide()
        {
            _visible = false;
            if (_meshRenderer != null)
                _meshRenderer.enabled = false;
        }

        public void Show(SpriteRenderer sprite, Vector3[] corners, Color color)
        {
            if (corners == null || corners.Length != 4)
                return;

            _sprite = sprite;
            _visible = true;
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
            if (!_visible)
            {
                if (_meshRenderer != null)
                    _meshRenderer.enabled = false;
                return;
            }

            if (_sprite == null)
                _sprite = GetComponent<SpriteRenderer>();
            if (_meshRenderer == null || _sprite == null)
                return;
            _meshRenderer.enabled = _sprite.enabled;
        }
    }
}
