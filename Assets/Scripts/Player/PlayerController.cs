using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] float walkSpeed = 5f;
    [SerializeField] float sprintSpeed = 7f;
    [SerializeField] float jumpHeight = 1f;
    [SerializeField] float stickToGroundForce = 2f;
    [SerializeField] float mouseSensitivity = 2f;

    [Header("Camera Bob Settings")]
    [SerializeField] float walkAmplitude = 0.5f;
    [SerializeField] float walkFrequency = 1f;
    [SerializeField] float runAmplitude = 1.2f;
    [SerializeField] float runFrequency = 2.0f;
    [SerializeField] float idleAmplitude = 0.5f;
    [SerializeField] float idleFrequency = 0.5f;
    [SerializeField] float bobLerpSpeed = 5f;

    CharacterController characterController;
    CinemachineVirtualCamera camera;
    Transform cameraHolder;
    CinemachineBasicMultiChannelPerlin noise;

    Vector3 velocity;

    float gravity = -9.81f;
    float xRotation = 0f;
    bool isGrounded;
    bool isRunning;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        camera = GetComponentInChildren<CinemachineVirtualCamera>();
        noise = camera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        cameraHolder = transform.Find("CameraHolder");
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        xRotation = 0f;
        cameraHolder.localRotation = Quaternion.Euler(0f, 0f, 0f);
    }

    private void Update()
    {
        HandleLook();
        CheckGrounded();
        HandleMovement();
        HandleJump();
        ApplyNoise();
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void CheckGrounded()
    {
        isGrounded = characterController.isGrounded;

        if(isGrounded && velocity.y < 0)
        {
            velocity.y = -stickToGroundForce;
        }
    }

    private void HandleMovement()
    {
        isRunning = Input.GetKey(KeyCode.LeftShift);

        float currentSpeed = isRunning ? sprintSpeed : walkSpeed;
        float moveLR = Input.GetAxis("Horizontal");
        float moveFB = Input.GetAxis("Vertical");

        Vector3 move = transform.right * moveLR + transform.forward * moveFB;

        if(move.magnitude > 1)
        {
            move.Normalize();
        }

        characterController.Move(move * currentSpeed * Time.deltaTime);
    }

    private void HandleJump()
    {
        if(Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    private void ApplyNoise()
    {
        float targetAmplitude = 0f;
        float targetFrequency = 0f;
        bool isInput = Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f;

        if(isGrounded)
        {
            targetAmplitude = idleAmplitude;
            targetFrequency = idleFrequency;

            if(isInput)
            {
                targetAmplitude = isRunning ? runAmplitude : walkAmplitude;
                targetFrequency = isRunning ? runFrequency : walkFrequency;
            }
        }

        noise.m_AmplitudeGain = Mathf.Lerp(noise.m_AmplitudeGain, targetAmplitude, Time.deltaTime * bobLerpSpeed);
        noise.m_FrequencyGain = Mathf.Lerp(noise.m_FrequencyGain, targetFrequency, Time.deltaTime * bobLerpSpeed);
    }
}