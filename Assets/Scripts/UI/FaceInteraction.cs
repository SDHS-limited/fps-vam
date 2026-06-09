using UnityEngine;

public class FaceInteraction : MonoBehaviour
{
    public Camera playerCamera;
    public float interactDistance = 5f;

    void Update()
    {
        Ray ray = new Ray(
            playerCamera.transform.position,
            playerCamera.transform.forward
        );

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            CubeFace face = hit.collider.GetComponent<CubeFace>();

            if (face != null)
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    Interact(face.faceType);
                }
            }
        }
    }

    void Interact(FaceType type)
    {
        switch (type)
        {
            case FaceType.Start:
                Debug.Log("페이즈 시작");
                break;

            case FaceType.Ability1:
                Debug.Log("혈액 강화 선택");
                break;

            case FaceType.Ability2:
                Debug.Log("신경 강화 선택");
                break;

            case FaceType.Ability3:
                Debug.Log("근육 강화 선택");
                break;
        }
    }
}