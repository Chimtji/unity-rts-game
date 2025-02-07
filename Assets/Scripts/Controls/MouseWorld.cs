using UnityEngine;

public class MouseWorld : MonoBehaviour
{
    public static MouseWorld Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public Vector3 GetPosition()
    {
        Ray mouseCameraRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(mouseCameraRay, out RaycastHit raycastHit))
        {
            return raycastHit.point;
        }
        else
        {
            Debug.Log("did not hit anything");
            return Vector3.zero;
        }
    }
}
