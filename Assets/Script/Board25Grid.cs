using UnityEngine;

/// <summary>
/// 2.5D 菱形棋盘。选人、移动、进攻都走 BoardGrid，这里只换格子的形状。
/// 开局数据的行列必须和棋盘一样是 8x8，6x6 的那份不会拿来摆人。
/// 角色站在格子点上方。最下方一格 (0,0) 的角色世界坐标是 bottomPieceWorld。
/// 名字以 block 开头的障碍用另一层高度，最下方一格是 bottomBlockWorld。
/// 列从近角走向右角，行从近角走向左角，都从 0 开始。
/// </summary>
public class Board25Grid : BoardGrid
{
    [Header("最下方格子上的角色世界坐标")]
    public Vector3 bottomPieceWorld = new Vector3(0f, -2f, -1f);

    readonly Vector3 bottomBlockWorld = new Vector3(0f, -2.8f, -1f);

    // board25.png 顶面四角，贴图像素，左下为原点。
    readonly Vector2 nearCorner = new Vector2(749.2f, 372.4f);
    readonly Vector2 rightCorner = new Vector2(1491.6f, 829.7f);
    readonly Vector2 leftCorner = new Vector2(10f, 829f);
    readonly Vector2 farCorner = new Vector2(752.9f, 1259.1f);

    const string PieceLayer = "Chess Piece";

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
        columns = 8;
        rows = 8;
    }

    protected override bool CanUseSetup()
    {
        return setup != null && setup.pieces != null
            && setup.columns == columns && setup.rows == rows;
    }

    protected override bool TryGetPieceLocal(int col, int row, out Vector3 local)
    {
        local = Vector3.zero;
        if (!TryGetCellLocal(0, 0, out Vector3 bottomLocal, out _))
            return false;
        if (!TryGetCellLocal(col, row, out Vector3 cellLocal, out _))
            return false;

        Vector3 anchor = IsBlock(PieceSource(col, row)) ? bottomBlockWorld : bottomPieceWorld;
        Vector3 bottomWorld = transform.TransformPoint(bottomLocal);
        Vector3 cellWorld = transform.TransformPoint(cellLocal);
        Vector3 standWorld = anchor + (cellWorld - bottomWorld);
        standWorld.z = anchor.z;
        local = transform.InverseTransformPoint(standWorld);
        return true;
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
        if (columns <= 0 || rows <= 0)
            return false;
        if (!TryLocalToPixel(transform.InverseTransformPoint(world), out Vector2 pixel))
            return false;
        if (!TrySolveUv(pixel, out float u, out float v))
            return false;

        col = Mathf.Min(columns - 1, Mathf.FloorToInt(u * columns));
        row = Mathf.Min(rows - 1, Mathf.FloorToInt(v * rows));
        return true;
    }

    protected override bool TryGetCellLocal(int col, int row, out Vector3 localCenter, out Vector2 localSize)
    {
        localCenter = Vector3.zero;
        localSize = Vector2.zero;
        if (col < 0 || row < 0 || col >= columns || row >= rows)
            return false;
        if (!TryUvLocal((col + 0.5f) / columns, (row + 0.5f) / rows, out localCenter))
            return false;

        if (TryUvLocal(col / (float)columns, (row + 0.5f) / rows, out Vector3 left)
            && TryUvLocal((col + 1f) / columns, (row + 0.5f) / rows, out Vector3 right))
        {
            float width = Vector3.Distance(left, right);
            localSize = new Vector2(width, width);
        }
        return true;
    }

    protected override void PlaceOverlay(SpriteRenderer overlay, int col, int row, Color color)
    {
        overlay.enabled = true;
        overlay.sprite = null;
        overlay.sortingLayerID = GetComponent<SpriteRenderer>().sortingLayerID;
        overlay.sortingOrder = GetComponent<SpriteRenderer>().sortingOrder;
        overlay.transform.localPosition = Vector3.zero;
        overlay.transform.localRotation = Quaternion.identity;
        overlay.transform.localScale = Vector3.one;

        if (!TryCellQuad(col, row, out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3))
            return;

        p0.z = p1.z = p2.z = p3.z = OverlayZ;
        DiamondMark mark = overlay.GetComponent<DiamondMark>();
        if (mark == null)
            mark = overlay.gameObject.AddComponent<DiamondMark>();
        mark.Show(overlay, new[] { p0, p1, p2, p3 }, color);
    }

    bool TryCellQuad(int col, int row, out Vector3 p0, out Vector3 p1, out Vector3 p2, out Vector3 p3)
    {
        p0 = p1 = p2 = p3 = Vector3.zero;
        float u0 = col / (float)columns;
        float v0 = row / (float)rows;
        float u1 = (col + 1f) / columns;
        float v1 = (row + 1f) / rows;
        return TryUvLocal(u0, v0, out p0)
            && TryUvLocal(u1, v0, out p1)
            && TryUvLocal(u1, v1, out p2)
            && TryUvLocal(u0, v1, out p3);
    }

    bool TryUvLocal(float u, float v, out Vector3 local)
    {
        local = Vector3.zero;
        if (!TryBuildMap(out Homography map))
            return false;
        return TryPixelToLocal(Project(map, u, v), out local);
    }

    struct Homography
    {
        public float m00, m01, m02;
        public float m10, m11, m12;
        public float m20, m21, m22;
    }

    bool TryBuildMap(out Homography map)
    {
        map = default;
        float a = rightCorner.x - farCorner.x;
        float b = leftCorner.x - farCorner.x;
        float c = farCorner.x - rightCorner.x - leftCorner.x + nearCorner.x;
        float d = rightCorner.y - farCorner.y;
        float e = leftCorner.y - farCorner.y;
        float g = farCorner.y - rightCorner.y - leftCorner.y + nearCorner.y;
        float det = a * e - b * d;
        if (Mathf.Abs(det) < 1e-4f)
            return false;

        float h20 = (c * e - b * g) / det;
        float h21 = (a * g - c * d) / det;
        map.m20 = h20;
        map.m21 = h21;
        map.m22 = 1f;
        map.m02 = nearCorner.x;
        map.m12 = nearCorner.y;
        map.m00 = rightCorner.x * (h20 + 1f) - nearCorner.x;
        map.m10 = rightCorner.y * (h20 + 1f) - nearCorner.y;
        map.m01 = leftCorner.x * (h21 + 1f) - nearCorner.x;
        map.m11 = leftCorner.y * (h21 + 1f) - nearCorner.y;
        return true;
    }

    static Vector2 Project(Homography map, float u, float v)
    {
        float x = map.m00 * u + map.m01 * v + map.m02;
        float y = map.m10 * u + map.m11 * v + map.m12;
        float w = map.m20 * u + map.m21 * v + map.m22;
        return new Vector2(x / w, y / w);
    }

    bool TrySolveUv(Vector2 pixel, out float u, out float v)
    {
        u = 0f;
        v = 0f;
        if (!TryBuildMap(out Homography map))
            return false;

        float a = map.m00;
        float b = map.m01;
        float c = map.m02;
        float d = map.m10;
        float e = map.m11;
        float f = map.m12;
        float g = map.m20;
        float h = map.m21;
        float i = map.m22;
        float det = a * (e * i - f * h) - b * (d * i - f * g) + c * (d * h - e * g);
        if (Mathf.Abs(det) < 1e-5f)
            return false;

        float x = ((e * i - f * h) * pixel.x + (c * h - b * i) * pixel.y + (b * f - c * e)) / det;
        float y = ((f * g - d * i) * pixel.x + (a * i - c * g) * pixel.y + (c * d - a * f)) / det;
        float w = ((d * h - e * g) * pixel.x + (b * g - a * h) * pixel.y + (a * e - b * d)) / det;
        if (Mathf.Abs(w) < 1e-5f)
            return false;

        u = x / w;
        v = y / w;
        const float edge = 0.001f;
        if (u < -edge || v < -edge || u > 1f + edge || v > 1f + edge)
            return false;

        u = Mathf.Clamp01(u);
        v = Mathf.Clamp01(v);
        return true;
    }

    bool TryPixelToLocal(Vector2 pixel, out Vector3 local)
    {
        local = Vector3.zero;
        if (!TryGetSpriteSpace(out Rect rect, out Vector2 pivot, out float ppu))
            return false;

        local = new Vector3(
            (pixel.x - rect.x - pivot.x) / ppu,
            (pixel.y - rect.y - pivot.y) / ppu,
            0f);
        return true;
    }

    bool TryLocalToPixel(Vector3 local, out Vector2 pixel)
    {
        pixel = Vector2.zero;
        if (!TryGetSpriteSpace(out Rect rect, out Vector2 pivot, out float ppu))
            return false;

        pixel = new Vector2(
            local.x * ppu + rect.x + pivot.x,
            local.y * ppu + rect.y + pivot.y);
        return true;
    }

    bool TryGetSpriteSpace(out Rect rect, out Vector2 pivot, out float ppu)
    {
        rect = default;
        pivot = Vector2.zero;
        ppu = 0f;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        Sprite sprite = renderer != null ? renderer.sprite : null;
        if (sprite == null)
            return false;

        rect = sprite.rect;
        pivot = sprite.pivot;
        ppu = sprite.pixelsPerUnit;
        return ppu > 0f;
    }

    void OnDrawGizmosSelected()
    {
        if (columns <= 0 || rows <= 0)
            return;

        Gizmos.color = new Color(0.2f, 1f, 0.45f, 0.9f);
        for (int col = 0; col <= columns; col++)
        {
            float u = col / (float)columns;
            if (!TryUvLocal(u, 0f, out Vector3 from) || !TryUvLocal(u, 1f, out Vector3 to))
                return;
            Gizmos.DrawLine(transform.TransformPoint(from), transform.TransformPoint(to));
        }

        for (int row = 0; row <= rows; row++)
        {
            float v = row / (float)rows;
            if (!TryUvLocal(0f, v, out Vector3 from) || !TryUvLocal(1f, v, out Vector3 to))
                return;
            Gizmos.DrawLine(transform.TransformPoint(from), transform.TransformPoint(to));
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

            SpriteRenderer board = transform.parent != null
                ? transform.parent.GetComponent<SpriteRenderer>()
                : null;
            if (board != null && _meshRenderer != null)
            {
                _meshRenderer.sortingLayerID = board.sortingLayerID;
                _meshRenderer.sortingOrder = board.sortingOrder;
            }
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
