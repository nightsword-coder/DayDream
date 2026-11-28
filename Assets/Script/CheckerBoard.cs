using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 挂到空物体上。两张 2:1 菱形格子交替拼成 8x8 的俯视棋盘。
/// (0,0) 在最下方的角，用 cell1。列走向右上，行走向左上。
/// 视觉左侧（列 0）交替贴 bottom1、bottom2，视觉右侧（行 0）交替贴 bottom3、bottom4。
/// 底座按图里不透明的上沿，对齐到对应地砖的下边。
/// </summary>
public class CheckerBoard : MonoBehaviour
{
    public Sprite cell1;
    public Sprite cell2;
    public Sprite bottom1;
    public Sprite bottom2;
    public Sprite bottom3;
    public Sprite bottom4;


    const int Size = 8;
    const string CellPrefix = "Cell_";

    struct Diamond
    {
        public Vector2 size;
        public Vector3 center;
        public float ppu;
        public Vector3 edgeA;
        public Vector3 edgeB;
    }

    readonly Dictionary<int, Diamond> _diamonds = new Dictionary<int, Diamond>();
    bool _building;

    void Awake()
    {
        Build();
        ApplyFloorOrder();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall -= RebuildInEditor;
        UnityEditor.EditorApplication.delayCall += RebuildInEditor;
    }

    void RebuildInEditor()
    {
        if (this == null)
            return;
        Build();
        ApplyFloorOrder();
    }
#endif

    public void Build()
    {
        if (_building || AlreadyMatches())
            return;

        _building = true;
        ClearCells();
        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                Sprite sprite = (col + row) % 2 == 0 ? cell1 : cell2;
                if (sprite == null)
                    continue;

                GameObject cell = new GameObject(CellPrefix + col + "_" + row);
                cell.transform.SetParent(transform, false);
                Place(col, row, sprite, out Vector3 localPosition, out int sortingOrder);
                cell.transform.localPosition = localPosition;
                SpriteRenderer renderer = cell.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingLayerID = 0;
                renderer.sortingOrder = sortingOrder;
            }
        }

        for (int i = 0; i < Size; i++)
        {
            SpawnBase(0, i, i % 2 == 0 ? bottom1 : bottom2, true);
            SpawnBase(i, 0, i % 2 == 0 ? bottom3 : bottom4, false);
        }

        _building = false;
        ApplyFloorOrder();
    }

    // 格子和底座都在默认层，顺序 0。高亮用顺序 1 盖在上面。
    void ApplyFloorOrder()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            string name = child.name;
            if (!name.StartsWith(CellPrefix) && !name.StartsWith("BaseL_") && !name.StartsWith("BaseR_"))
                continue;

            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            if (renderer == null)
                continue;
            renderer.sortingLayerID = 0;
            renderer.sortingOrder = 0;
        }
    }

    void SpawnBase(int col, int row, Sprite sprite, bool leftSide)
    {
        if (sprite == null || !PlaceBase(col, row, sprite, leftSide, out Vector3 localPosition, out int sortingOrder))
            return;

        GameObject piece = new GameObject((leftSide ? "BaseL_" : "BaseR_") + (leftSide ? row : col));
        piece.transform.SetParent(transform, false);
        piece.transform.localPosition = localPosition;
        SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerID = 0;
        renderer.sortingOrder = sortingOrder;
    }

    Sprite PitchSprite()
    {
        return cell1 != null ? cell1 : cell2;
    }

    bool TryDiamond(Sprite sprite, out Diamond diamond)
    {
        diamond = default;
        if (sprite == null)
            return false;
        int id = sprite.GetInstanceID();
        if (_diamonds.TryGetValue(id, out diamond))
            return true;
        if (!Measure(sprite, out diamond))
            return false;
        _diamonds[id] = diamond;
        return true;
    }

    static bool Measure(Sprite sprite, out Diamond diamond)
    {
        diamond = default;
        if (TryOpaque(sprite, out Vector2 size, out Vector3 center, out Vector3 edgeA, out Vector3 edgeB))
        {
            diamond.size = size;
            diamond.center = center;
            diamond.ppu = sprite.pixelsPerUnit;
            diamond.edgeA = edgeA;
            diamond.edgeB = edgeB;
            return true;
        }

        Vector2[] verts = sprite.vertices;
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

        diamond.size = new Vector2(maxX - minX, maxY - minY);
        diamond.center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        diamond.ppu = sprite.pixelsPerUnit;
        float slop = 1f / Mathf.Max(diamond.ppu, 1f);
        Vector2 leftTop = new Vector2(minX, float.MinValue);
        Vector2 rightTop = new Vector2(maxX, float.MinValue);
        for (int i = 0; i < verts.Length; i++)
        {
            Vector2 v = verts[i];
            if (v.x <= minX + slop && v.y > leftTop.y)
                leftTop = v;
            if (v.x >= maxX - slop && v.y > rightTop.y)
                rightTop = v;
        }
        diamond.edgeA = leftTop;
        diamond.edgeB = rightTop;
        return diamond.size.x > 0.001f && diamond.size.y > 0.001f;
    }

    static bool TryOpaque(Sprite sprite, out Vector2 size, out Vector3 center, out Vector3 edgeA, out Vector3 edgeB)
    {
        size = default;
        center = default;
        edgeA = default;
        edgeB = default;
#if UNITY_EDITOR
        string path = UnityEditor.AssetDatabase.GetAssetPath(sprite);
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return false;

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (IOException)
        {
            return false;
        }

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes))
        {
            Object.DestroyImmediate(tex);
            return false;
        }

        Color32[] pixels = tex.GetPixels32();
        int width = tex.width;
        int minX = width;
        int minY = tex.height;
        int maxX = -1;
        int maxY = -1;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a == 0)
                continue;
            int x = i % width;
            int y = i / width;
            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
        }

        if (maxX < minX)
        {
            Object.DestroyImmediate(tex);
            return false;
        }

        int leftTop = -1;
        int rightTop = -1;
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a == 0)
                continue;
            int x = i % width;
            int y = i / width;
            if (x == minX && y > leftTop)
                leftTop = y;
            if (x == maxX && y > rightTop)
                rightTop = y;
        }

        Object.DestroyImmediate(tex);
        float ppu = sprite.pixelsPerUnit;
        size = new Vector2((maxX - minX + 1) / ppu, (maxY - minY + 1) / ppu);
        Vector2 pivot = sprite.pivot;
        Rect rect = sprite.rect;
        float cx = (minX + maxX) * 0.5f - rect.x;
        float cy = (minY + maxY) * 0.5f - rect.y;
        center = new Vector3((cx - pivot.x) / ppu, (cy - pivot.y) / ppu, 0f);
        edgeA = ToPivot(sprite, minX, leftTop);
        edgeB = ToPivot(sprite, maxX, rightTop);
        return leftTop >= 0 && rightTop >= 0;
#else
        return false;
#endif
    }

    static Vector3 ToPivot(Sprite sprite, int x, int y)
    {
        float ppu = sprite.pixelsPerUnit;
        Rect rect = sprite.rect;
        Vector2 pivot = sprite.pivot;
        return new Vector3((x - rect.x - pivot.x) / ppu, (y - rect.y - pivot.y) / ppu, 0f);
    }

    bool TryFrame(int col, int row, out Vector3 center, out float halfW, out float halfH, out int sortingOrder)
    {
        center = Vector3.zero;
        halfW = 0f;
        halfH = 0f;
        sortingOrder = 0;
        if (!TryDiamond(PitchSprite(), out Diamond pitch))
            return false;

        halfW = pitch.size.x * 0.5f;
        halfH = pitch.size.y * 0.5f;
        float bleed = 1f / Mathf.Max(pitch.ppu, 1f);
        float dist = Mathf.Sqrt(halfW * halfW + halfH * halfH);
        float scale = dist > bleed ? (dist - bleed) / dist : 1f;
        center = new Vector3(
            (col - row) * halfW * scale,
            (col + row - (Size - 1)) * halfH * scale,
            0f);
        return true;
    }

    void Place(int col, int row, Sprite sprite, out Vector3 localPosition, out int sortingOrder)
    {
        localPosition = Vector3.zero;
        sortingOrder = 0;
        if (!TryFrame(col, row, out Vector3 center, out _, out _, out sortingOrder) || !TryDiamond(sprite, out Diamond self))
            return;

        localPosition = center - self.center;
    }

    bool PlaceBase(int col, int row, Sprite sprite, bool leftSide, out Vector3 localPosition, out int sortingOrder)
    {
        localPosition = Vector3.zero;
        sortingOrder = 0;
        if (!TryFrame(col, row, out Vector3 center, out float halfW, out float halfH, out sortingOrder))
            return false;
        if (!TryDiamond(sprite, out Diamond self))
            return false;
        if ((self.edgeB - self.edgeA).sqrMagnitude < 0.000001f)
            return false;

        Vector3 edgeMid = leftSide
            ? center + new Vector3(-halfW * 0.5f, -halfH * 0.5f, 0f)
            : center + new Vector3(halfW * 0.5f, -halfH * 0.5f, 0f);
        Vector3 tuck = center - edgeMid;
        float length = tuck.magnitude;
        float ppu = self.ppu > 0.001f ? self.ppu : 100f;
        if (length > 0.0001f)
            edgeMid += tuck / length * (1f / ppu);

        localPosition = edgeMid - (self.edgeA + self.edgeB) * 0.5f;
        sortingOrder = 0;
        return true;
    }

    bool AlreadyMatches()
    {
        for (int row = 0; row < Size; row++)
        {
            for (int col = 0; col < Size; col++)
            {
                Sprite sprite = (col + row) % 2 == 0 ? cell1 : cell2;
                Transform child = transform.Find(CellPrefix + col + "_" + row);
                if (sprite == null)
                {
                    if (child != null)
                        return false;
                    continue;
                }

                if (child == null)
                    return false;
                SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
                if (renderer == null || renderer.sprite != sprite)
                    return false;
                Place(col, row, sprite, out Vector3 localPosition, out int sortingOrder);
                if (renderer.sortingOrder != sortingOrder)
                    return false;
                if ((child.localPosition - localPosition).sqrMagnitude > 0.000001f)
                    return false;
            }
        }

        for (int i = 0; i < Size; i++)
        {
            if (!ExpectBase(0, i, i % 2 == 0 ? bottom1 : bottom2, true))
                return false;
            if (!ExpectBase(i, 0, i % 2 == 0 ? bottom3 : bottom4, false))
                return false;
        }

        return true;
    }

    bool ExpectBase(int col, int row, Sprite sprite, bool leftSide)
    {
        string name = (leftSide ? "BaseL_" : "BaseR_") + (leftSide ? row : col);
        Transform child = transform.Find(name);
        if (sprite == null)
            return child == null;
        if (child == null || !PlaceBase(col, row, sprite, leftSide, out Vector3 localPosition, out int sortingOrder))
            return false;

        SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite != sprite || renderer.sortingOrder != sortingOrder)
            return false;
        return (child.localPosition - localPosition).sqrMagnitude <= 0.000001f;
    }

    void ClearCells()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (!child.name.StartsWith(CellPrefix) && !child.name.StartsWith("BaseL_") && !child.name.StartsWith("BaseR_"))
                continue;
            DestroyImmediate(child.gameObject);
        }
    }
}
