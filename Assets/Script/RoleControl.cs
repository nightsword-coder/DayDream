using UnityEngine;

/// <summary>
/// A 向左，D 向右，W 向上，S 向下。对应的走路参数在按住时为真，松开为假。
/// </summary>
public class RoleControl : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float smoothTime = 0.08f;

    static readonly int LeftWalk = Animator.StringToHash("leftwalk");
    static readonly int RightWalk = Animator.StringToHash("rightwalk");
    static readonly int DownWalk = Animator.StringToHash("downwalk");
    static readonly int UpWalk = Animator.StringToHash("upwalk");

    Animator _animator;
    float _speedX;
    float _speedY;
    float _velocityX;
    float _velocityY;

    void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    void Update()
    {
        bool up = Input.GetKey(KeyCode.W);
        bool down = Input.GetKey(KeyCode.S);
        bool vertical = up != down;
        bool left = !vertical && Input.GetKey(KeyCode.A);
        bool right = !vertical && Input.GetKey(KeyCode.D);

        float directionX = 0f;
        if (left)
            directionX -= 1f;
        if (right)
            directionX += 1f;
        float directionY = 0f;
        if (up && !down)
            directionY = 1f;
        else if (down && !up)
            directionY = -1f;

        float damp = Mathf.Max(0.01f, smoothTime);
        _speedX = Mathf.SmoothDamp(_speedX, directionX * moveSpeed, ref _velocityX, damp);
        _speedY = Mathf.SmoothDamp(_speedY, directionY * moveSpeed, ref _velocityY, damp);
        transform.position += new Vector3(_speedX, _speedY, 0f) * Time.deltaTime;

        if (_animator == null)
            return;

        _animator.SetBool(LeftWalk, directionX < 0f);
        _animator.SetBool(RightWalk, directionX > 0f);
        _animator.SetBool(DownWalk, directionY < 0f);
        _animator.SetBool(UpWalk, directionY > 0f);
    }
}
