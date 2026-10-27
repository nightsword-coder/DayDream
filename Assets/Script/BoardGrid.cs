using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>
/// 6x6 无边缘棋盘。整张图都是格子，图片中心对应世界坐标 (0,0)。
/// 列从左往右、行从下往上，都从 0 开始。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BoardGrid : MonoBehaviour
{
    public static BoardGrid Instance { get; private set; }

    protected const float OverlayZ = -0.4f;
    const float PieceZ = -1f;
    const float ArrowZ = -1.2f;

    public int columns = 6;
    public int rows = 6;

    private float paddingLeft;
    private float paddingRight;
    private float paddingBottom;
    private float paddingTop;

    [Header("悬停高亮")]
    public Color highlightColor = new Color(0.15f, 1f, 0.25f, 0.45f);

    [Header("点击高亮")]
    public Color emptyClickColor = new Color(1f, 0.86f, 0.1f, 0.5f);
    public Color pieceClickColor = new Color(0.62f, 0.2f, 1f, 0.55f);

    [Header("进攻")]
    public Color attackColor = new Color(1f, 0.2f, 0.15f, 0.5f);
    public int attackRange = 2;

    [Header("开局")]
    public BoardSetup setup;

    [Header("移动")]
    public Color moveRangeColor = new Color(0.25f, 0.55f, 1f, 0.45f);
    [Min(0.02f)]
    public float stepSeconds = 0.5f;

    [Header("路径箭头")]
    public Color pathColor = new Color(1f, 0.48f, 0.12f, 0.95f);
    [Range(0.08f, 0.45f)]
    public float pathWidth = 0.2f;
    [Tooltip("横向箭身，轴点在中心。不填就用纯色方条。")]
    public Sprite pathShaftSprite;
    [Tooltip("朝右的箭头，轴点放在尖端。不填就用三角形。")]
    public Sprite pathHeadSprite;
    [Tooltip("拐角接头，轴点在中心。不填就用方块。")]
    public Sprite pathCornerSprite;

    [Header("点击")]
    public UnityEvent<int, int> onCellClicked;

    public CellContent[,] cellContent;

    SpriteRenderer _renderer;
    SpriteRenderer _hover;
    SpriteRenderer _mark;
    int _hoverCol = -1;
    int _hoverRow = -1;

    int _selectedCol = -1;
    int _selectedRow = -1;
    bool _pieceSelected;
    CommandMode _command;

    enum CommandMode
    {
        None,
        Move,
        Attack,
    }

    readonly List<SpriteRenderer> _attackMarks = new List<SpriteRenderer>();
    int _attackUsed;
    readonly List<SpriteRenderer> _skillMarks = new List<SpriteRenderer>();
    int _skillUsed;
    readonly List<SpriteRenderer> _moveMarks = new List<SpriteRenderer>();
    int _moveUsed;
    readonly List<RaycastResult> _uiHits = new List<RaycastResult>();

    bool _moving;
    bool _slide;
    List<Vector2Int> _path;
    int _pathIndex;
    float _moveTime;
    Vector3 _slideFrom;
    Vector3 _slideTo;

    Transform _pathRoot;
    readonly List<SpriteRenderer> _arrowPool = new List<SpriteRenderer>();
    int _arrowUsed;
    int _arrowFromCol = int.MinValue;
    int _arrowFromRow = int.MinValue;
    int _arrowToCol = int.MinValue;
    int _arrowToRow = int.MinValue;

    static Sprite _whiteSprite;
    static Sprite _arrowHeadSprite;
    static readonly Vector2Int[] Neighbors =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
    };

    public class CellContent
    {
        public GameObject prefab;
        public GameObject instance;
    }

    void Awake()
    {
        Instance = this;
        _renderer = GetComponent<SpriteRenderer>();
        _hover = CreateOverlay("CellHover");
        _mark = CreateOverlay("CellMark");
    }

    void Start()
    {
        SpawnInitialPieces();
    }

    void OnValidate()
    {
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
    }

    void Update()
    {
        if (_slide)
            TickSlide(Time.deltaTime);

        if (Camera.main == null)
            return;

        bool inside = TryScreenToCell(Input.mousePosition, out int col, out int row);
        UpdateHover(inside, col, row);
        UpdatePathArrow(inside, col, row);

        if (_moving || !inside || !Input.GetMouseButtonDown(0) || PointerOverButton())
            return;

        HandleClick(col, row);
    }

    bool PointerOverButton()
    {
        if (EventSystem.current == null)
            return false;

        _uiHits.Clear();
        var pointer = new PointerEventData(EventSystem.current);
        pointer.position = Input.mousePosition;
        EventSystem.current.RaycastAll(pointer, _uiHits);
        for (int i = 0; i < _uiHits.Count; i++)
        {
            if (_uiHits[i].gameObject.GetComponentInParent<UnityEngine.UI.Button>() != null)
                return true;
        }
        return false;
    }

    protected virtual bool CanUseSetup()
    {
        return setup != null && setup.pieces != null;
    }

    protected virtual void SpawnInitialPieces()
    {
        cellContent = new CellContent[columns, rows];
        if (!CanUseSetup())
            return;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                GameObject prefab = setup.GetPrefab(col, row);
                if (prefab == null)
                    continue;
                if (!TryGetPieceLocal(col, row, out Vector3 localCenter))
                    return;

                var instance = Instantiate(prefab, transform);
                instance.transform.localPosition = localCenter;
                instance.transform.localRotation = Quaternion.identity;
                instance.name = $"Piece_{col}_{row}";
                cellContent[col, row] = new CellContent
                {
                    prefab = prefab,
                    instance = instance,
                };
                ApplyPieceOrder(instance, col, row);
            }
        }
    }

    protected virtual void ApplyPieceOrder(GameObject piece, int col, int row)
    {
    }

    GameObject GetInitialPrefab(int col, int row)
    {
        if (!CanUseSetup() || col < 0 || row < 0 || col >= columns || row >= rows)
            return null;
        return setup.GetPrefab(col, row);
    }

    void HandleClick(int col, int row)
    {
        bool occupied = cellContent != null && cellContent[col, row] != null;
        onCellClicked.Invoke(col, row);

        if (SkillExecutor.Instance != null && SkillExecutor.Instance.HandleClick(col, row, occupied))
            return;

        if (_command == CommandMode.Attack && _pieceSelected && CanAttack() && InAttackRange(col, row))
        {
            if (occupied)
                ApplyAttack(col, row);
            EndAttack();
            return;
        }

        if (occupied)
        {
            Role role = RoleAt(col, row);
            if (role == null || !CanControl(role))
                return;
            SelectPiece(col, row);
            return;
        }

        if (_command == CommandMode.Move && _pieceSelected)
            TryBeginMove(col, row);
    }

    public bool HasHighlightedPiece => _pieceSelected;

    public Role SelectedRole => _pieceSelected ? RoleAt(_selectedCol, _selectedRow) : null;

    public void HighlightRole(Role role)
    {
        if (role == null || cellContent == null)
            return;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (RoleAt(col, row) != role)
                    continue;
                SelectPiece(col, row);
                return;
            }
        }
    }

    public void BeginMove()
    {
        if (ActionSkills.Instance != null)
            ActionSkills.Instance.ClearSelection();
        if (!_pieceSelected || _moving || !CanMove())
            return;

        _command = CommandMode.Move;
        ClearAttackRange();
        ShowMoveRange();
    }

    public void BeginAttack()
    {
        if (ActionSkills.Instance != null)
            ActionSkills.Instance.ClearSelection();
        if (!_pieceSelected || _moving || !CanAttack())
            return;

        _command = CommandMode.Attack;
        ClearPathArrow();
        ClearMoveRange();
        ShowAttackRange();
    }

    public void EndCommand()
    {
        if (_moving)
            return;

        _command = CommandMode.None;
        ClearAttackRange();
        ClearMoveRange();
        ClearPathArrow();
    }

    public bool Moving => _moving;

    public bool TryMoveTo(int col, int row)
    {
        if (!_pieceSelected || _moving || !CanMove() || (col == _selectedCol && row == _selectedRow))
            return false;

        List<Vector2Int> path = FindShortestPath(_selectedCol, _selectedRow, col, row);
        if (path == null || !InMoveRange(col, row))
            return false;

        StartMove(path, col, row);
        return true;
    }

    public bool TryAttack(int col, int row)
    {
        if (!_pieceSelected || _moving || !CanAttack() || cellContent == null || !InBoard(col, row))
            return false;
        if (cellContent[col, row] == null || !InAttackRange(col, row))
            return false;

        ApplyAttack(col, row);
        return true;
    }

    bool CanMove()
    {
        return BattleManager.Instance == null || BattleManager.Instance.CanMove;
    }

    bool CanAttack()
    {
        return BattleManager.Instance == null || BattleManager.Instance.CanAttack;
    }

    bool CanControl(Role role)
    {
        if (role.GetComponent<Enemy>() == null)
            return true;

        BattleManager battle = BattleManager.Instance;
        if (battle == null)
            return true;

        Role current = battle.Current;
        return current != null && current.GetComponent<Enemy>() != null;
    }

    void SelectPiece(int col, int row)
    {
        _pieceSelected = true;
        _selectedCol = col;
        _selectedRow = row;
        _command = CommandMode.None;
        ClearAttackRange();
        ClearMoveRange();
        ClearPathArrow();
        ShowMark(col, row, pieceClickColor);
    }

    void ApplyAttack(int col, int row)
    {
        Role attacker = RoleAt(_selectedCol, _selectedRow);
        Role target = RoleAt(col, row);
        if (attacker == null || target == null)
            return;

        int damage = attacker.physicalAttack + attacker.magicAttack;
        target.health -= damage;
        target.PlayHurt();
        DamagePopup.Spawn(target, damage);
        if (BattleManager.Instance != null)
            BattleManager.Instance.UseAttack();
        Debug.Log($"造成 {damage} 点伤害，剩余生命 {target.health}");
    }

    void EndAttack()
    {
        _command = CommandMode.None;
        ClearAttackRange();
    }

    bool InAttackRange(int col, int row)
    {
        int distance = Mathf.Abs(col - _selectedCol) + Mathf.Abs(row - _selectedRow);
        return distance > 0 && distance <= attackRange;
    }

    public Role RoleInCell(int col, int row)
    {
        return RoleAt(col, row);
    }

    public bool TryCellOf(Role role, out int col, out int row)
    {
        col = -1;
        row = -1;
        if (role == null || cellContent == null)
            return false;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                if (RoleInCell(c, r) != role)
                    continue;
                col = c;
                row = r;
                return true;
            }
        }

        return false;
    }

    Role RoleAt(int col, int row)
    {
        if (cellContent == null || !InBoard(col, row))
            return null;
        CellContent content = cellContent[col, row];
        if (content == null || content.instance == null)
            return null;
        return content.instance.GetComponent<Role>();
    }

    void ShowAttackRange()
    {
        _attackUsed = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (!InAttackRange(col, row))
                    continue;
                PlaceOverlay(RentAttackMark(), col, row, attackColor);
            }
        }

        for (int i = _attackUsed; i < _attackMarks.Count; i++)
            _attackMarks[i].enabled = false;
    }

    void ShowMoveRange()
    {
        _moveUsed = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (!InMoveRange(col, row))
                    continue;
                PlaceOverlay(RentMoveMark(), col, row, moveRangeColor);
            }
        }

        for (int i = _moveUsed; i < _moveMarks.Count; i++)
            _moveMarks[i].enabled = false;
    }

    void ClearMoveRange()
    {
        for (int i = 0; i < _moveMarks.Count; i++)
            _moveMarks[i].enabled = false;
        _moveUsed = 0;
    }

    SpriteRenderer RentMoveMark()
    {
        SpriteRenderer mark;
        if (_moveUsed < _moveMarks.Count)
            mark = _moveMarks[_moveUsed];
        else
        {
            mark = CreateOverlay("MoveMark");
            _moveMarks.Add(mark);
        }

        _moveUsed++;
        return mark;
    }

    bool InMoveRange(int col, int row)
    {
        if (!_pieceSelected || (col == _selectedCol && row == _selectedRow))
            return false;
        if (!InBoard(col, row) || cellContent[col, row] != null)
            return false;

        List<Vector2Int> path = FindShortestPath(_selectedCol, _selectedRow, col, row);
        if (path == null)
            return false;

        Role mover = RoleAt(_selectedCol, _selectedRow);
        int range = mover != null ? Mathf.Max(0, mover.moveRange) : 0;
        return path.Count - 1 <= range;
    }

    SpriteRenderer RentAttackMark()
    {
        SpriteRenderer mark;
        if (_attackUsed < _attackMarks.Count)
            mark = _attackMarks[_attackUsed];
        else
        {
            mark = CreateOverlay("AttackMark");
            _attackMarks.Add(mark);
        }

        _attackUsed++;
        return mark;
    }

    void ClearAttackRange()
    {
        for (int i = 0; i < _attackMarks.Count; i++)
            _attackMarks[i].enabled = false;
        _attackUsed = 0;
    }

    public void ShowSkillMarks(List<Vector2Int> rangeCells, List<Vector2Int> castCells)
    {
        _skillUsed = 0;
        StampSkillMarks(rangeCells, attackColor);
        StampSkillMarks(castCells, attackColor);

        for (int i = _skillUsed; i < _skillMarks.Count; i++)
            _skillMarks[i].enabled = false;
    }

    void StampSkillMarks(List<Vector2Int> cells, Color color)
    {
        if (cells == null)
            return;

        for (int i = 0; i < cells.Count; i++)
        {
            if (!InBoard(cells[i].x, cells[i].y))
                continue;
            PlaceOverlay(RentSkillMark(), cells[i].x, cells[i].y, color);
        }
    }

    public void ClearSkillMarks()
    {
        for (int i = 0; i < _skillMarks.Count; i++)
            _skillMarks[i].enabled = false;
        _skillUsed = 0;
    }

    SpriteRenderer RentSkillMark()
    {
        SpriteRenderer mark;
        if (_skillUsed < _skillMarks.Count)
            mark = _skillMarks[_skillUsed];
        else
        {
            mark = CreateOverlay("SkillMark");
            _skillMarks.Add(mark);
        }

        _skillUsed++;
        return mark;
    }

    void TryBeginMove(int col, int row)
    {
        if (col == _selectedCol && row == _selectedRow || !CanMove())
            return;

        if (!InMoveRange(col, row))
            return;

        var path = FindShortestPath(_selectedCol, _selectedRow, col, row);
        if (path == null)
        {
            Debug.Log($"从 ({_selectedCol},{_selectedRow}) 到 ({col},{row}) 没有通路。");
            return;
        }

        StartMove(path, col, row);
    }

    void StartMove(List<Vector2Int> path, int col, int row)
    {
        _path = path;
        _pathIndex = 0;
        _moveTime = 0f;
        _moving = true;
        _slide = false;
        if (BattleManager.Instance != null)
            BattleManager.Instance.UseMove();
        ClearMoveRange();
        ShowMark(col, row, emptyClickColor);

        if (path.Count < 2)
            return;

        _slide = PieceHasAnimator(path[0].x, path[0].y);
        BeginPieceStep(path[0].x, path[0].y, path[1].x, path[1].y);
        if (_slide)
            CacheSlide(path[0], path[1]);
    }

    void FixedUpdate()
    {
        if (!_moving || _path == null || _slide)
            return;

        _moveTime += Time.fixedDeltaTime;
        if (_moveTime < stepSeconds)
            return;

        _moveTime -= stepSeconds;
        if (_pathIndex >= _path.Count - 1)
        {
            FinishMove();
            return;
        }

        ArriveCurrentStep();
    }

    void TickSlide(float dt)
    {
        if (!_moving || _path == null)
            return;

        if (_pathIndex >= _path.Count - 1)
        {
            FinishMove();
            return;
        }

        _moveTime += dt;
        float duration = Mathf.Max(0.02f, stepSeconds);
        float t = Mathf.Clamp01(_moveTime / duration);
        Vector2Int from = _path[_pathIndex];
        CellContent piece = cellContent[from.x, from.y];
        if (piece != null && piece.instance != null)
            piece.instance.transform.localPosition = Vector3.Lerp(_slideFrom, _slideTo, t);

        if (t < 1f)
            return;

        _moveTime -= duration;
        ArriveCurrentStep();
    }

    void ArriveCurrentStep()
    {
        Vector2Int from = _path[_pathIndex];
        Vector2Int to = _path[_pathIndex + 1];
        CellContent piece = cellContent[from.x, from.y];
        cellContent[from.x, from.y] = null;
        cellContent[to.x, to.y] = piece;

        if (piece != null && piece.instance != null)
        {
            if (TryGetPieceLocal(to.x, to.y, out Vector3 localCenter))
                piece.instance.transform.localPosition = localCenter;
            ApplyPieceOrder(piece.instance, to.x, to.y);
        }

        _pathIndex++;

        if (_pathIndex >= _path.Count - 1)
        {
            FinishMove();
            return;
        }

        BeginPieceStep(_path[_pathIndex].x, _path[_pathIndex].y, _path[_pathIndex + 1].x, _path[_pathIndex + 1].y);
        if (_slide)
            CacheSlide(_path[_pathIndex], _path[_pathIndex + 1]);
    }

    bool PieceHasAnimator(int col, int row)
    {
        if (cellContent == null || col < 0 || row < 0 || col >= columns || row >= rows)
            return false;

        CellContent content = cellContent[col, row];
        if (content == null || content.instance == null)
            return false;

        return content.instance.GetComponentInChildren<Animator>(true) != null;
    }

    void CacheSlide(Vector2Int from, Vector2Int to)
    {
        CellContent content = cellContent[from.x, from.y];
        if (content != null && content.instance != null)
            _slideFrom = content.instance.transform.localPosition;
        else if (TryGetPieceLocal(from.x, from.y, out Vector3 fromLocal))
            _slideFrom = fromLocal;

        if (TryGetPieceLocal(to.x, to.y, out Vector3 toLocal))
            _slideTo = toLocal;
    }

    protected virtual void BeginPieceStep(int fromCol, int fromRow, int toCol, int toRow)
    {
    }

    protected virtual void EndPieceMove(GameObject piece)
    {
    }

    void FinishMove()
    {
        Vector2Int end = _path[_path.Count - 1];
        _moving = false;
        _slide = false;
        _path = null;
        _command = CommandMode.None;
        _pieceSelected = true;
        _selectedCol = end.x;
        _selectedRow = end.y;
        ClearPathArrow();
        ShowMark(_selectedCol, _selectedRow, pieceClickColor);

        CellContent arrived = cellContent[end.x, end.y];
        if (arrived == null)
            return;

        ApplyPieceOrder(arrived.instance, end.x, end.y);
        EndPieceMove(arrived.instance);
    }

    void UpdatePathArrow(bool inside, int col, int row)
    {
        if (_moving || _command != CommandMode.Move || !_pieceSelected || cellContent == null)
        {
            if (_arrowFromCol != int.MinValue)
                ClearPathArrow();
            return;
        }

        if (!inside)
        {
            if (_arrowToCol != int.MinValue)
                ClearPathArrow();
            return;
        }

        if (col == _arrowToCol && row == _arrowToRow && _selectedCol == _arrowFromCol && _selectedRow == _arrowFromRow)
            return;

        _arrowFromCol = _selectedCol;
        _arrowFromRow = _selectedRow;
        _arrowToCol = col;
        _arrowToRow = row;

        if (!InMoveRange(col, row))
        {
            HidePathArrow();
            return;
        }

        var path = FindShortestPath(_selectedCol, _selectedRow, col, row);
        if (path == null || path.Count < 2)
        {
            HidePathArrow();
            return;
        }

        DrawPathArrow(path);
    }

    void DrawPathArrow(List<Vector2Int> path)
    {
        if (!TryGetCellLocal(path[0].x, path[0].y, out _, out Vector2 cellSize))
        {
            HidePathArrow();
            return;
        }

        var centers = new Vector3[path.Count];
        for (int i = 0; i < path.Count; i++)
        {
            if (!TryGetCellLocal(path[i].x, path[i].y, out centers[i], out _))
            {
                HidePathArrow();
                return;
            }
        }

        float cell = Mathf.Min(cellSize.x, cellSize.y);
        float thickness = cell * pathWidth;
        float headLen = Mathf.Min(cell * 0.42f, thickness * 2.4f);

        BeginArrow();
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 from = centers[i];
            Vector3 to = centers[i + 1];
            if (i == path.Count - 2)
            {
                Vector3 delta = to - from;
                float length = delta.magnitude;
                if (length > headLen)
                    to -= delta / length * headLen;
            }
            PlaceShaft(RentArrow(ShaftSprite, 4), from, to, thickness);
        }

        for (int i = 1; i < path.Count - 1; i++)
            PlaceJoint(RentArrow(CornerSprite, 5), centers[i], thickness);

        PlaceHead(RentArrow(HeadSprite, 6), centers[path.Count - 2], centers[path.Count - 1], headLen);
        EndArrow();
    }

    void BeginArrow()
    {
        _arrowUsed = 0;
        if (_pathRoot == null)
        {
            var go = new GameObject("PathArrow");
            go.transform.SetParent(transform, false);
            _pathRoot = go.transform;
        }
        _pathRoot.gameObject.SetActive(true);
    }

    void EndArrow()
    {
        for (int i = _arrowUsed; i < _arrowPool.Count; i++)
            _arrowPool[i].gameObject.SetActive(false);
    }

    SpriteRenderer RentArrow(Sprite sprite, int orderOffset)
    {
        SpriteRenderer piece;
        if (_arrowUsed < _arrowPool.Count)
            piece = _arrowPool[_arrowUsed];
        else
        {
            var go = new GameObject("PathPiece");
            go.transform.SetParent(_pathRoot, false);
            piece = go.AddComponent<SpriteRenderer>();
            _arrowPool.Add(piece);
        }

        _arrowUsed++;
        piece.gameObject.SetActive(true);
        piece.sprite = sprite;
        piece.color = pathColor;
        piece.sortingLayerID = _renderer.sortingLayerID;
        piece.sortingOrder = _renderer.sortingOrder;
        Vector3 depth = piece.transform.localPosition;
        depth.z = ArrowZ - orderOffset * 0.02f;
        piece.transform.localPosition = depth;
        return piece;
    }

    static void PlaceShaft(SpriteRenderer piece, Vector3 from, Vector3 to, float thickness)
    {
        Vector3 delta = to - from;
        float length = delta.magnitude;
        if (length < 0.0001f)
        {
            piece.gameObject.SetActive(false);
            return;
        }

        Vector2 size = piece.sprite.bounds.size;
        float z = piece.transform.localPosition.z;
        Vector3 mid = (from + to) * 0.5f;
        mid.z = z;
        piece.transform.localPosition = mid;
        piece.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        piece.transform.localScale = new Vector3(length / Mathf.Max(0.0001f, size.x), thickness / Mathf.Max(0.0001f, size.y), 1f);
    }

    static void PlaceJoint(SpriteRenderer piece, Vector3 center, float thickness)
    {
        Vector2 size = piece.sprite.bounds.size;
        float z = piece.transform.localPosition.z;
        center.z = z;
        piece.transform.localPosition = center;
        piece.transform.localRotation = Quaternion.identity;
        piece.transform.localScale = new Vector3(
            thickness / Mathf.Max(0.0001f, size.x),
            thickness / Mathf.Max(0.0001f, size.y),
            1f);
    }

    static void PlaceHead(SpriteRenderer piece, Vector3 from, Vector3 to, float headLen)
    {
        Vector3 delta = to - from;
        Vector2 size = piece.sprite.bounds.size;
        float scale = headLen / Mathf.Max(0.0001f, size.x);
        float z = piece.transform.localPosition.z;
        to.z = z;
        piece.transform.localPosition = to;
        piece.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        piece.transform.localScale = new Vector3(scale, scale, 1f);
    }

    Sprite ShaftSprite => pathShaftSprite != null ? pathShaftSprite : GetWhiteSprite();
    Sprite HeadSprite => pathHeadSprite != null ? pathHeadSprite : GetArrowHeadSprite();
    Sprite CornerSprite => pathCornerSprite != null ? pathCornerSprite : GetWhiteSprite();

    void HidePathArrow()
    {
        if (_pathRoot != null)
            _pathRoot.gameObject.SetActive(false);
    }

    void ClearPathArrow()
    {
        _arrowFromCol = _arrowFromRow = _arrowToCol = _arrowToRow = int.MinValue;
        HidePathArrow();
    }

    /// <summary>
    /// 上下左右四方向的最短路。其他预制体视为障碍，返回包含起点和终点的格子列表。
    /// </summary>
    public List<Vector2Int> FindShortestPath(int fromCol, int fromRow, int toCol, int toRow, bool allowOccupiedGoal = false)
    {
        if (!InBoard(fromCol, fromRow) || !InBoard(toCol, toRow))
            return null;
        if (cellContent[toCol, toRow] != null && !allowOccupiedGoal)
            return null;

        var cameFrom = new Vector2Int[columns, rows];
        var seen = new bool[columns, rows];
        var queue = new Queue<Vector2Int>();
        var start = new Vector2Int(fromCol, fromRow);
        queue.Enqueue(start);
        seen[fromCol, fromRow] = true;
        cameFrom[fromCol, fromRow] = start;

        bool found = fromCol == toCol && fromRow == toRow;
        while (!found && queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            for (int i = 0; i < Neighbors.Length; i++)
            {
                int col = current.x + Neighbors[i].x;
                int row = current.y + Neighbors[i].y;
                if (!InBoard(col, row) || seen[col, row])
                    continue;
                bool goal = col == toCol && row == toRow;
                if (cellContent[col, row] != null && !(allowOccupiedGoal && goal))
                    continue;

                seen[col, row] = true;
                cameFrom[col, row] = current;
                if (col == toCol && row == toRow)
                {
                    found = true;
                    break;
                }
                queue.Enqueue(new Vector2Int(col, row));
            }
        }

        if (!found)
            return null;

        var path = new List<Vector2Int>();
        var step = new Vector2Int(toCol, toRow);
        path.Add(step);
        while (step != start)
        {
            step = cameFrom[step.x, step.y];
            path.Add(step);
        }
        path.Reverse();
        return path;
    }

    bool InBoard(int col, int row)
    {
        return col >= 0 && row >= 0 && col < columns && row < rows && cellContent != null;
    }

    void UpdateHover(bool inside, int col, int row)
    {
        if (!inside)
        {
            _hoverCol = _hoverRow = -1;
            _hover.enabled = false;
            return;
        }

        if (col == _hoverCol && row == _hoverRow && _hover.enabled)
            return;

        _hoverCol = col;
        _hoverRow = row;
        PlaceOverlay(_hover, col, row, highlightColor);
    }

    void ShowMark(int col, int row, Color color)
    {
        PlaceOverlay(_mark, col, row, color);
    }

    protected virtual void PlaceOverlay(SpriteRenderer overlay, int col, int row, Color color)
    {
        overlay.enabled = true;
        overlay.color = color;
        overlay.sortingLayerID = _renderer.sortingLayerID;
        overlay.sortingOrder = _renderer.sortingOrder;

        if (!TryGetCellLocal(col, row, out Vector3 localCenter, out Vector2 localSize))
            return;

        localCenter.z = OverlayZ;
        overlay.transform.localPosition = localCenter;
        overlay.transform.localScale = new Vector3(localSize.x, localSize.y, 1f);
    }

    SpriteRenderer CreateOverlay(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var overlay = go.AddComponent<SpriteRenderer>();
        overlay.sprite = GetWhiteSprite();
        overlay.enabled = false;
        return overlay;
    }

    public bool TryScreenToCell(Vector3 screenPosition, out int col, out int row)
    {
        Camera cam = Camera.main;
        float depth = Mathf.Abs(cam.transform.position.z - transform.position.z);
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
        return TryWorldToCell(world, out col, out row);
    }

    public virtual bool TryWorldToCell(Vector3 world, out int col, out int row)
    {
        col = -1;
        row = -1;
        if (!TryGetLayout(out float left, out float bottom, out float cellW, out float cellH, out Vector2 pivot, out float ppu))
            return false;

        Vector3 local = transform.InverseTransformPoint(world);
        float pixelX = local.x * ppu + pivot.x;
        float pixelY = local.y * ppu + pivot.y;

        col = Mathf.FloorToInt((pixelX - left) / cellW);
        row = Mathf.FloorToInt((pixelY - bottom) / cellH);
        return col >= 0 && col < columns && row >= 0 && row < rows;
    }

    /// <summary>
    /// 格子中心的世界坐标。col 从左往右，row 从下往上，都从 0 开始。
    /// 6x6 时，四个中心格 (2,2)(3,2)(2,3)(3,3) 的公共角在棋盘中心。
    /// </summary>
    public Vector3 GetCellCenter(int col, int row)
    {
        if (!TryGetCellLocal(col, row, out Vector3 localCenter, out _))
            return transform.position;
        return transform.TransformPoint(localCenter);
    }

    protected virtual bool TryGetPieceLocal(int col, int row, out Vector3 local)
    {
        local = Vector3.zero;
        if (!TryGetCellLocal(col, row, out local, out _))
            return false;

        local.z = PieceZ;
        return true;
    }

    protected virtual bool TryGetCellLocal(int col, int row, out Vector3 localCenter, out Vector2 localSize)
    {
        localCenter = Vector3.zero;
        localSize = Vector2.zero;
        if (!TryGetLayout(out float left, out float bottom, out float cellW, out float cellH, out Vector2 pivot, out float ppu))
            return false;

        float pixelX = left + (col + 0.5f) * cellW;
        float pixelY = bottom + (row + 0.5f) * cellH;
        localCenter = new Vector3((pixelX - pivot.x) / ppu, (pixelY - pivot.y) / ppu, 0f);
        localSize = new Vector2(cellW / ppu, cellH / ppu);
        return true;
    }

    bool TryGetLayout(out float left, out float bottom, out float cellW, out float cellH, out Vector2 pivot, out float ppu)
    {
        left = bottom = cellW = cellH = ppu = 0f;
        pivot = Vector2.zero;

        if (_renderer == null)
            _renderer = GetComponent<SpriteRenderer>();
        Sprite sprite = _renderer != null ? _renderer.sprite : null;
        if (sprite == null || columns <= 0 || rows <= 0)
            return false;

        Rect rect = sprite.rect;
        float innerW = rect.width - paddingLeft - paddingRight;
        float innerH = rect.height - paddingBottom - paddingTop;
        if (innerW <= 0f || innerH <= 0f)
            return false;

        left = paddingLeft;
        bottom = paddingBottom;
        cellW = innerW / columns;
        cellH = innerH / rows;
        pivot = sprite.pivot;
        ppu = sprite.pixelsPerUnit;
        return true;
    }

    static Sprite GetWhiteSprite()
    {
        if (_whiteSprite != null)
            return _whiteSprite;

        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _whiteSprite;
    }

    static Sprite GetArrowHeadSprite()
    {
        if (_arrowHeadSprite != null)
            return _arrowHeadSprite;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float centerY = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float along = x / (float)(size - 1);
                float halfHeight = (1f - along) * centerY;
                bool inside = Mathf.Abs(y - centerY) <= halfHeight;
                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        _arrowHeadSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(1f, 0.5f), size);
        return _arrowHeadSprite;
    }

    [ContextMenu("打印所有格子中心")]
    void LogAllCellCenters()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
                Debug.Log($"格子 ({col},{row}) 中心 {GetCellCenter(col, row)}");
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!TryGetLayout(out _, out _, out _, out _, out _, out _))
            return;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                bool occupied = Application.isPlaying && cellContent != null
                    ? cellContent[col, row] != null
                    : GetInitialPrefab(col, row) != null;
                Gizmos.color = occupied
                    ? new Color(0.7f, 0.3f, 1f, 0.95f)
                    : new Color(1f, 0.85f, 0.1f, 0.9f);
                Gizmos.DrawSphere(GetCellCenter(col, row), occupied ? 0.1f : 0.06f);
            }
        }
    }
}
