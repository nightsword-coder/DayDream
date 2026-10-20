using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ShopManager : MonoBehaviour
{
    public Book[] books;
    public Button purchaseButton;
    public Inventory inventory;
    public Backpack backpack;

    Image _detailImage;
    TMP_Text _detailName;
    TMP_Text _detailDescription;
    readonly List<Slot> _slots = new List<Slot>();
    Slot _selected;

    class Slot
    {
        public RectTransform rect;
        public Image icon;
        public Image highlight;
        public Book book;
    }

    void Awake()
    {
        Transform detail = transform.Find("DescriptionBlock/item");
        if (detail != null)
        {
            _detailImage = detail.GetComponent<Image>();
            Transform name = detail.Find("itemname");
            Transform description = detail.Find("descriptiontext");
            if (name != null)
                _detailName = name.GetComponent<TMP_Text>();
            if (description != null)
                _detailDescription = description.GetComponent<TMP_Text>();
        }

        for (int i = 1; i <= 6; i++)
        {
            Transform item = transform.Find("shopitem" + i);
            if (item == null)
                continue;
            Transform icon = item.Find("book");
            _slots.Add(new Slot
            {
                rect = item as RectTransform,
                icon = icon != null ? icon.GetComponent<Image>() : null,
                highlight = CreateHighlight(item),
            });
        }

        if (purchaseButton != null)
            purchaseButton.onClick.AddListener(Purchase);
    }

    void OnEnable()
    {
        if (inventory != null)
            inventory.RefreshGold();
        Stock();
        _selected = null;
        ClearDetail();
    }

    public void Purchase()
    {
        if (_selected == null || _selected.book == null)
            return;

        Book book = _selected.book;
        if (inventory != null)
            inventory.Spend(inventory.bookPrice);
        if (backpack != null)
            backpack.Place(book);
        else
            Debug.LogWarning("ShopManager 的 Backpack 是空的，把 backpackpanel 上的 Backpack 拖上去。");

        _selected.book = null;
        if (_selected.highlight != null)
            _selected.highlight.enabled = false;
        if (_selected.rect != null)
            _selected.rect.gameObject.SetActive(false);
        _selected = null;
        ClearDetail();
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        Slot slot = SlotUnderMouse();
        if (slot == null)
            return;
        Select(slot);
    }

    void Select(Slot slot)
    {
        _selected = slot;
        for (int i = 0; i < _slots.Count; i++)
        {
            if (_slots[i].highlight != null)
                _slots[i].highlight.enabled = _slots[i] == slot;
        }
        ShowDetail(slot.book);
    }

    void Stock()
    {
        List<Book> pool = LoadBooks();
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int swap = Random.Range(0, i + 1);
            Book temp = pool[i];
            pool[i] = pool[swap];
            pool[swap] = temp;
        }

        int count = Mathf.Min(_slots.Count, Random.Range(3, 7));
        count = Mathf.Min(count, pool.Count);

        for (int i = 0; i < _slots.Count; i++)
        {
            Slot slot = _slots[i];
            bool onShelf = i < count;
            slot.book = onShelf ? pool[i] : null;
            if (slot.rect != null)
                slot.rect.gameObject.SetActive(onShelf);
            if (!onShelf || slot.icon == null)
                continue;
            slot.icon.sprite = slot.book.sprite;
            slot.icon.enabled = slot.book.sprite != null;
            if (slot.highlight != null)
                slot.highlight.enabled = false;
            Transform price = slot.rect != null ? slot.rect.Find("price/goldtext") : null;
            if (price != null && inventory != null)
            {
                TMP_Text priceText = price.GetComponent<TMP_Text>();
                if (priceText != null)
                    priceText.text = inventory.bookPrice.ToString();
            }
        }
    }

    Image CreateHighlight(Transform item)
    {
        var go = new GameObject("Highlight", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(item, false);
        go.transform.SetAsFirstSibling();

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-10f, -10f);
        rect.offsetMax = new Vector2(10f, 10f);

        Image image = go.GetComponent<Image>();
        image.sprite = WhiteSprite();
        image.color = new Color(1f, 0.82f, 0.15f, 0.9f);
        image.raycastTarget = false;
        image.enabled = false;
        return image;
    }

    static Sprite _whiteSprite;

    static Sprite WhiteSprite()
    {
        if (_whiteSprite != null)
            return _whiteSprite;

        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return _whiteSprite;
    }

    Slot SlotUnderMouse()
    {
        Camera camera = UiCamera();
        for (int i = 0; i < _slots.Count; i++)
        {
            Slot slot = _slots[i];
            if (slot.book == null || slot.rect == null || !slot.rect.gameObject.activeInHierarchy)
                continue;
            if (RectTransformUtility.RectangleContainsScreenPoint(slot.rect, Input.mousePosition, camera))
                return slot;
        }
        return null;
    }

    Camera UiCamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            return canvas.worldCamera;
        return null;
    }

    void ShowDetail(Book book)
    {
        if (_detailImage != null)
        {
            _detailImage.sprite = book.sprite;
            _detailImage.enabled = book.sprite != null;
        }
        if (_detailName != null)
            _detailName.text = book.bookName;
        if (_detailDescription != null)
            _detailDescription.text = book.description;
    }

    void ClearDetail()
    {
        if (_detailImage != null)
        {
            _detailImage.sprite = null;
            _detailImage.enabled = false;
        }
        if (_detailName != null)
            _detailName.text = "";
        if (_detailDescription != null)
            _detailDescription.text = "";
    }

    List<Book> LoadBooks()
    {
        var result = new List<Book>();
        if (books != null)
        {
            for (int i = 0; i < books.Length; i++)
            {
                if (books[i] != null)
                    result.Add(books[i]);
            }
        }
        if (result.Count > 0)
            return result;

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:Book", new[] { "Assets/Image/books" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Book book = AssetDatabase.LoadAssetAtPath<Book>(path);
            if (book != null)
                result.Add(book);
        }
#endif
        return result;
    }
}
