using UnityEngine;
using UnityEngine.InputSystem; 

public class gracz_ruch : MonoBehaviour
{
    public float predkoscChodzenia = 5f;
    public float wysokoscSkoku = 1.5f;
    public float grawitacja = -9.81f;
    public float czuloscMyszki = 0.5f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool czyNaZiemi;
    private float obrotX = 0f;
    private Transform kameraTransform;
    private Animator animator;

    // Camera mode
    private bool isThirdPerson = false;
    private Renderer playerBody;
    private static readonly Vector3 FpsCamLocalPos = new Vector3(0f, 0.68f, 0.08f);
    private static readonly Vector3 TpsCamLocalPos = new Vector3(0.55f, 1.45f, -3.0f);

    public bool IsThirdPerson
    {
        get { return isThirdPerson; }
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        Camera cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            kameraTransform = cam.transform;
        }

        playerBody = GetComponent<Renderer>();
        if (playerBody != null) playerBody.enabled = false; // hidden in FPS by default

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (controller == null) return;

        czyNaZiemi = controller.isGrounded;

        if (czyNaZiemi && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // V: toggle first/third person camera
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            isThirdPerson = !isThirdPerson;
            if (kameraTransform != null)
                kameraTransform.localPosition = isThirdPerson ? TpsCamLocalPos : FpsCamLocalPos;
            if (playerBody != null)
                playerBody.enabled = isThirdPerson;
        }

        // Esc: unlock cursor
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // LMB click when cursor unlocked: re-lock
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame
            && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        //KLAWIATURA
        Vector2 inputRuch = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) inputRuch.y += 1;
            if (Keyboard.current.sKey.isPressed) inputRuch.y -= 1;
            if (Keyboard.current.aKey.isPressed) inputRuch.x -= 1;
            if (Keyboard.current.dKey.isPressed) inputRuch.x += 1;
        }

        // MYSZKA
        Vector2 inputMyszka = Vector2.zero;
        if (Mouse.current != null)
        {
            inputMyszka = Mouse.current.delta.ReadValue();
        }

        // Obrót kamerą myszką
        float myszX = inputMyszka.x * czuloscMyszki;
        float myszY = inputMyszka.y * czuloscMyszki;

        obrotX -= myszY;
        obrotX = Mathf.Clamp(obrotX, -90f, 90f);

        if (kameraTransform != null)
        {
            kameraTransform.localRotation = Quaternion.Euler(obrotX, 0f, 0f);
        }

        transform.Rotate(Vector3.up * myszX);

        // Ruch postaci
        Vector3 ruch = transform.right * inputRuch.x + transform.forward * inputRuch.y;

        // Skok
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame && czyNaZiemi)
        {
            velocity.y = Mathf.Sqrt(wysokoscSkoku * -2f * grawitacja);
        }

        velocity.y += grawitacja * Time.deltaTime;
        // Single Move call — avoids double-move jitter
        controller.Move((ruch * predkoscChodzenia + velocity) * Time.deltaTime);

        // Animacje
        if (animator != null)
        {
            float predkosc = inputRuch.magnitude;
            animator.SetFloat("Speed", predkosc);
            animator.SetBool("isGrounded", czyNaZiemi);
            animator.SetBool("isJumping", !czyNaZiemi && velocity.y > 0);
        }

    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || kameraTransform == null) return;

        Ray ray = new Ray(kameraTransform.position, kameraTransform.forward);
        Vector3 aimPoint = ray.GetPoint(100f);

        animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
        animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);
        animator.SetIKPosition(AvatarIKGoal.RightHand, aimPoint);

        animator.SetLookAtWeight(1f);
        animator.SetLookAtPosition(aimPoint);
    }

}
