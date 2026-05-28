using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ModelViewer : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] CinemachineCamera FLCamera;
    CinemachineOrbitalFollow FLCameraOrbitalFollow;
    [SerializeField] bool cameraLightOn;
    [SerializeField] GameObject cameraLight;
    [SerializeField] float followDistance = 20f;

    [Header("Zoom")]
    // How close to the model we are (in and out)
    [SerializeField] float minZoom = 5f;
    [SerializeField] float maxZoom = 100f;
    [SerializeField] float curZoom;
    [Tooltip("Starting zoom level and level to adjust to")]
    [SerializeField] float curZoomTarget = 60f;
    [Range(0f, 1f)]
    [Tooltip("How quickly the camera approaches the zoom target; lower is smoother")]
    [SerializeField] float zoomDamping = 0.05f;
    float zoomSensitivity;

    [Header("Models")]
    [Tooltip("Whether to let the user cycle through models")]
    [SerializeField] bool cycleThroughModels = false;
    int curModelIdx;
    [Tooltip("The list of models to cycle through (if cycleThroughModels is true)")]
    [SerializeField] List<GameObject> models;

    [Tooltip("The current model the camera is viewing; Cinemachine Tracking Target")]
    [SerializeField] GameObject curModel;
    [Tooltip("An invisible object for the camera to lock onto; Cinemachine Look At Target")]
    [SerializeField] GameObject center;

    [Header("Position")]
    [Range(0f, 1f)]
    [SerializeField] float movementSensitivity = 0.1f;
    // Where on the model the camera is centered (up and down)
    [SerializeField] float tiltMin = 0;
    [SerializeField] float tiltMax = 15f;
    [SerializeField] float curTilt;
    [Tooltip("Starting tilt angle and angle to adjust to")]
    [SerializeField] float curTiltTarget = 7f;
    // Spinning around the model (left to right)
    [SerializeField] float curRotation;
    [Tooltip("Starting rotation angle and angle to adjust to")]
    [SerializeField] float curRotationTarget = 0f;
    // Rotating around the current center (up and down spherically)
    [SerializeField] float panMin = 1f;
    [SerializeField] float panMax = 85f;
    [SerializeField] float curPan;
    [Tooltip("Starting pan angle and angle to adjust to")]
    [SerializeField] float curPanTarget = 17.5f;

    [Header("Skybox")]
    [SerializeField] bool nightMode = false;
    [Tooltip("'Sun' graphic for the night mode button")]
    [SerializeField] GameObject nightModeSun;
    [Tooltip("'Moon' graphic for the night mode button")]
    [SerializeField] GameObject nightModeMoon;
    Button nightModeButton;
    [SerializeField] Skybox skybox;
    [SerializeField] Material daySkybox;
    [SerializeField] Material nightSkybox;
    [SerializeField] Light directionalLight;

    [Header("Settings")]
    [SerializeField] GameObject settingsScreen;
    [SerializeField] bool settingsOpen = false;

    [Tooltip("'Pause' graphic for the pause button")]
    [SerializeField] GameObject pauseIcon;
    [Tooltip("'Play' graphic for the pause button")]
    [SerializeField] GameObject playIcon;

    InputAction scrollAction;
    InputAction moveAction;
    InputAction settingsAction;
    InputAction pauseAction;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        scrollAction = InputSystem.actions.FindAction("ScrollWheel");
        moveAction = InputSystem.actions.FindAction("Move");
        settingsAction = InputSystem.actions.FindAction("Settings");
        pauseAction = InputSystem.actions.FindAction("Pause");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        setupCinemachineCamera();
        nightModeButton = nightModeSun.transform.parent.GetComponent<Button>();

        if (cycleThroughModels) {
            curModelIdx = 0;
            setModelVals();
        }

        // Make sure to center model at the beginning
        zoomCamera();
        tiltCamera();
        rotateCamera();
        panCamera();

        settingsOpen = false;
        setNightModeVals();
        setSettingsVals();
        setCameraLightVals();
        setAnimationVals(1);
    }

    // Update is called once per frame
    void Update()
    {
        zoomCamera();
        tiltCamera();

        if (cycleThroughModels) checkCycle();
        checkAnimations();
        
        checkSettings();
        if (settingsOpen)
        {
            rotateCamera();
            panCamera();
        }
    }

    void setupCinemachineCamera()
    {
        FLCameraOrbitalFollow = FLCamera.gameObject?.GetComponent<CinemachineOrbitalFollow>();
        if (FLCameraOrbitalFollow == null) {
            Debug.LogWarning("Please attach a Cinemachine Orbital Follow component to this Camera!");
            return;
        }

        FLCameraOrbitalFollow.VerticalAxis.Range.Set(panMin, panMax);
        FLCameraOrbitalFollow.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
        FLCameraOrbitalFollow.Radius = followDistance;

        FLCamera.Target.TrackingTarget = curModel.transform;
        FLCamera.LookAt = center.transform;

        curZoom = FLCamera.Lens.FieldOfView;
        curTilt = center.transform.position.y;
        curRotation = FLCameraOrbitalFollow.HorizontalAxis.Value;
        curPan = FLCameraOrbitalFollow.VerticalAxis.Value;
    }

    // -------------------- MODEL CYCLING -------------------- \\

    void checkCycle()
    {
        if (moveAction.WasPerformedThisFrame())
        {
            int dir = moveAction.ReadValue<Vector2>().x > 0? 1 : -1;
            toggleCycle(dir);
        }
    }

    public void toggleCycle(int dir)
    {
        curModelIdx = (curModelIdx + dir + models.Count) % models.Count;
        setModelVals();
    }

    void setModelVals()
    {
        curModel = models[curModelIdx];
        for (int i = 0; i < models.Count; i++)
        {
            if (i == curModelIdx)
            {
                models[i].SetActive(true);
            }
            else
            {
                models[i].SetActive(false);
            }
        }
    }

    // -------------------- ANIMATIONS -------------------- \\

    void checkAnimations()
    {
        if (pauseAction.WasPerformedThisFrame())
        {
            toggleAnimations();
        }
    }

    public void toggleAnimations()
    {
        float newSpeed = 1 - curModel.GetComponent<Animator>().speed;
        for (int i = 0; i < models.Count; i++)
        {
            models[i].GetComponent<Animator>().speed = newSpeed;

            setAnimationVals(newSpeed);
        }
    }

    void setAnimationVals(float curSpeed)
    {
        if (curSpeed == 0)
        {
            pauseIcon.SetActive(false);
            playIcon.SetActive(true);
        }
        else
        {
            pauseIcon.SetActive(true);
            playIcon.SetActive(false);
        }
    }

    // -------------------- ZOOM, PAN, ROTATE, AND TILT -------------------- \\

    public void zoomCameraButton(float zoomDir)
    {
        curZoomTarget += zoomDir * zoomSensitivity;
    }

    void zoomCamera()
    {
        zoomSensitivity = curZoom / 2f;
        float scrollValue = scrollAction.ReadValue<Vector2>().y * 0.1f;
        curZoomTarget -= scrollValue * zoomSensitivity;

        if (curZoom == curZoomTarget)
        {
            return;
        }

        if (curZoomTarget > maxZoom)
        {
            curZoomTarget = maxZoom;
        }

        if (curZoomTarget < minZoom)
        {
            curZoomTarget = minZoom;
        }

        curZoom = Mathf.Lerp(curZoom, curZoomTarget, zoomDamping * Time.deltaTime * 100f);
        FLCamera.Lens.FieldOfView = curZoom;
    }

    public void panCameraButton(float panDir)
    {
        curPanTarget += panDir * movementSensitivity;

        if (curPanTarget > panMax)
        {
            curPanTarget = panMax;
        }

        if (curPanTarget < panMin)
        {
            curPanTarget = panMin;
        }
    }

    public void rotateCameraButton(float rotateDir)
    {
        curRotationTarget += rotateDir * movementSensitivity;
    }

    public void tiltCameraButton(float tiltDir)
    {
        curTiltTarget += tiltDir * movementSensitivity;
    }

    void panCamera()
    {
        float curVerticalPos = FLCameraOrbitalFollow.VerticalAxis.Value;
        curPan = Mathf.Lerp(curVerticalPos, curPanTarget, zoomDamping * Time.deltaTime * 100f);
        FLCameraOrbitalFollow.VerticalAxis.Value = curPan;
    }

    void rotateCamera()
    {
        float curHorizontalPos = FLCameraOrbitalFollow.HorizontalAxis.Value;
        curRotation = Mathf.Lerp(curHorizontalPos, curRotationTarget, zoomDamping * Time.deltaTime * 100f);
        FLCameraOrbitalFollow.HorizontalAxis.Value = curRotation;
    }

    void tiltCamera()
    {
        float verticalMovement = moveAction.ReadValue<Vector2>().y;
        curTiltTarget += verticalMovement * movementSensitivity;

        if (curTilt == curTiltTarget)
        {
            return;
        }

        if (curTiltTarget > tiltMax)
        {
            curTiltTarget = tiltMax;
        }

        if (curTiltTarget < tiltMin)
        {
            curTiltTarget = tiltMin;
        }

        curTilt = Mathf.Lerp(curTilt, curTiltTarget, zoomDamping * Time.deltaTime * 100f);
        center.transform.position = Vector3.up * curTilt;
    }

    // -------------------- SETTINGS -------------------- \\

    void checkSettings()
    {
        if (settingsAction.WasPerformedThisFrame())
        {
            toggleSettings();
            curRotationTarget = FLCamera.gameObject.GetComponent<CinemachineOrbitalFollow>().HorizontalAxis.Value;
            curPanTarget = FLCamera.gameObject.GetComponent<CinemachineOrbitalFollow>().VerticalAxis.Value;
        }
    }

    public void toggleSettings()
    {
        settingsOpen = !settingsOpen;
        setSettingsVals();
    }

    void setSettingsVals()
    {
        settingsScreen.SetActive(settingsOpen);
        FLCamera.gameObject.GetComponent<CinemachineInputAxisController>().enabled = !settingsOpen;
        Cursor.visible = settingsOpen;

        if (settingsOpen)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void toggleNightMode()
    {
        nightMode = !nightMode;
        setNightModeVals();
    }

    void setNightModeVals()
    {
        if (nightMode)
        {
            nightModeMoon.SetActive(false);
            nightModeSun.SetActive(true);
            skybox.material = nightSkybox;
            nightModeButton.targetGraphic = nightModeSun.GetComponent<Image>();
            directionalLight.intensity = 0.175f;
            directionalLight.colorTemperature = 20000;
        }
        else
        {
            nightModeMoon.SetActive(true);
            nightModeSun.SetActive(false);
            skybox.material = daySkybox;
            nightModeButton.targetGraphic = nightModeMoon.GetComponent<Image>();
            directionalLight.intensity = 5f;
            directionalLight.colorTemperature = 6500;
        }
    }

    public void toggleCameraLight()
    {
        cameraLightOn = !cameraLightOn;
        setCameraLightVals();
    }

    void setCameraLightVals()
    {
        cameraLight.SetActive(cameraLightOn);
    }
}
