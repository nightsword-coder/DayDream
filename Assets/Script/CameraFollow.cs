using UnityEngine;

/// <summary>
/// 挂在相机上。跟随目标的平面位置，不另加偏移，并保留相机自己的前后距离。
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target;

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 position = transform.position;
        position.x = target.position.x;
        position.y = target.position.y;
        transform.position = position;
    }
}
