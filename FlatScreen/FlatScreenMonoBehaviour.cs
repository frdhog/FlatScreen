using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VTOLVR.Multiplayer;
using Triquetra.FlatScreen.TrackIR;
using System.Reflection;

namespace Triquetra.FlatScreen
{
    public class FlatScreenMonoBehaviour : MonoBehaviour
    {
        // TODO: Fix hand positions
        // TODO?: Hide mouse on right click
        // TODO?: WASDEQ controls
        // TODO?: Better ImGui for restarting/ending mission
        // TODO?: Bobblehead gets a VRInteractable
        // TODO?: Back button (ESC)

        private Rect windowRect = new Rect(25, 25, 350, 500);
        private bool showWindow = true;
        private bool flatScreenEnabled = true;
        public TrackIRTransformer TrackIRTransformer { get; private set; }

        public VRInteractable targetedVRInteractable;
        public IEnumerable<VRInteractable> VRInteractables = new List<VRInteractable>();

        private Rect endScreenWindowRect = new Rect(Screen.width / 2 - 80, Screen.height / 2 - 150, 300, 160);
        private bool showEndScreenWindow = false;
        private bool thirdPerson = false;
        EndMission endMission;

        public static FlatScreenMonoBehaviour Instance { get; private set; }

        public bool Enabled
        {
            get
            {
                return flatScreenEnabled;
            }
            set
            {
                if (flatScreenEnabled != value)
                {
                    if (value) // just re-enabled
                        CheckEndMission();
                    else // just disabled
                        ResetCameraRotation();

                    flatScreenEnabled = value;
                }
            }
        }

        public void OnGUI()
        {
            if (showWindow)
                windowRect = GUI.Window(405, windowRect, DoWindow, "FlatScreen Control Panel");
            if (showEndScreenWindow)
                endScreenWindowRect = GUI.Window(406, endScreenWindowRect, DoEndScreenWindow, "FlatScreen End Screen");
        }

        public void ShowEndScreenWindow(EndMission endMission)
        {
            // recenter just in case
            endScreenWindowRect = new Rect(Screen.width / 2 - 80, Screen.height / 2 - 150, 300, 160);
            showEndScreenWindow = true;
            this.endMission = endMission;
            GUI.FocusWindow(406);
        }

        private void DoEndScreenWindow(int id)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            if (endMission == null)
            {
                showEndScreenWindow = false;
                return;
            }

            // GUILayout.Label($"Completion Time: {endMission.metCompleteText?.text}");

            if (GUILayout.Button("Restart Mission"))
            {
                endMission?.ReloadSceneButton();
                showEndScreenWindow = false;
            }
            if (GUILayout.Button("Finish Mission"))
            {
                endMission?.ReturnToMainButton();
                showEndScreenWindow = false;
            }
        }

        void DoWindow(int windowID)
        {
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            GUILayout.Label("Press F9 to show/hide this window");
            GUILayout.Space(20);

            Enabled = GUILayout.Toggle(Enabled, " Enabled");

            GUI.enabled = Enabled;

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            {
                GUILayout.BeginHorizontal();
                {
                    float FOV = GetCameraFOV();
                    float newFOV = FOV;
                    GUILayout.Label($"FOV: {FOV}");
                    newFOV = Mathf.Round(GUILayout.HorizontalSlider(FOV, 30f, 120f));
                    if (newFOV != FOV)
                        SetCameraFOV(newFOV);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label($"Mouse Sensitivity: {Sensitivity}");
                Sensitivity = Mathf.Round(GUILayout.HorizontalSlider(Sensitivity, 1f, 9f));
                GUILayout.EndHorizontal();

                LimitXRotation = GUILayout.Toggle(LimitXRotation, " Limit X Rotation");
                LimitYRotation = GUILayout.Toggle(LimitYRotation, " Limit Y Rotation");

                if (GUILayout.Button("Reset Camera Rotation"))
                    ResetCameraRotation();

                GUILayout.Space(30);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label($"Hovered VRInteractable:");
                    if (targetedVRInteractable != null)
                        GUILayout.Label(targetedVRInteractable?.interactableName ?? "???");
                    else
                        GUILayout.Label("[None]");
                }
                GUILayout.EndHorizontal();
                GUILayout.Label("Press Middle mouse to click and scroll wheel for non-integer knobs");

                GUILayout.Space(20);

                if (GUILayout.Button("Fix Camera"))
                {
                    Reclean();
                }

                if (GUILayout.Button(thirdPerson ? "First Person" : "Third Person"))
                {
                    thirdPerson = !thirdPerson;
                    foreach (Camera specCam in GetSpectatorCameras())
                    {
                        specCam.depth = thirdPerson ? -6 : 50;
                    }
                }

                GUI.enabled = true;

                /*if (IsReadyRoomScene())
                {
                    if (GUILayout.Button("Quick Select Vehicle"))
                    {
                        PilotSelectUI pilotSelectUI = FindObjectOfType<PilotSelectUI>();
                        pilotSelectUI.StartSelectedPilotButton();
                        pilotSelectUI.SelectVehicleButton();
                    }
                }*/

                GUILayout.Space(30);

                if (GUILayout.Button("Start Tracking"))
                {
                    if (TrackIRTransformer == null)
                        TrackIRTransformer = GetComponent<TrackIRTransformer>() ?? gameObject.AddComponent<TrackIRTransformer>();
                    TrackIRTransformer.StartTracking();
                }
                if (GUILayout.Button("Stop Tracking"))
                {
                    if (TrackIRTransformer == null)
                        TrackIRTransformer = GetComponent<TrackIRTransformer>() ?? gameObject.AddComponent<TrackIRTransformer>();
                    TrackIRTransformer.StopTracking();
                }

                GUILayout.Space(30);
                /*
                Camera camera = GetEyeCamera();
                if (camera != null)
                {
                    GUILayout.Label($"Camera: {camera.name}");
                    GUILayout.Label($"Camera GameObject: {GetEyeCameraGameObject()?.name}");
                    GUILayout.Label($"Depth: {camera.depth}");
                    GUILayout.Label($"Enabled: {camera.enabled}");
                    GUILayout.Label($"Is Active and Enabled: {camera.isActiveAndEnabled}");
                    GUILayout.Label($"Quad Parent: {camera.transform.parent?.parent?.parent?.parent?.name}");
                }

                GUILayout.Space(30);

                if (targetedVRInteractable != null)
                {
                    VRThrottle throttle = targetedVRInteractable.GetComponent<VRThrottle>();
                    GUILayout.Label($"Throttle: {throttle}");
                    GUILayout.Label($"Throttle Transform: {throttle?.throttleTransform?.name}");
                }*/
            }
            GUILayout.EndScrollView();
        }

        public static bool IsFlyingScene()
        {
            int buildIndex = SceneManager.GetActiveScene().buildIndex;
            return buildIndex == 7 || buildIndex == 11;
        }
        public static bool IsReadyRoomScene()
        {
            int buildIndex = SceneManager.GetActiveScene().buildIndex;
            return buildIndex == 2;
        }

        public void SetTrackingObject(GameObject trackingObject)
        {

            if (TrackIRTransformer == null)
                TrackIRTransformer = GetComponent<TrackIRTransformer>() ?? gameObject.AddComponent<TrackIRTransformer>();

            if (TrackIRTransformer == null)
                return;

            TrackIRTransformer.trackedObject = trackingObject?.transform;
        }

        bool hasCleanedCameras = false;
        public void CleanCameras()
        {
            if (hasCleanedCameras)
                return;
            Camera eyeCamera = GetEyeCamera();
            GameObject helmetCam = eyeCamera?.transform?.Find("Camera (eye) Helmet")?.gameObject;

            if (eyeCamera == null || helmetCam == null)
                return;

            foreach(Camera specCam in GetSpectatorCameras())
            {
                specCam.depth = -6;
            }

            helmetCam?.SetActive(false);
            eyeCamera.fieldOfView = DefaultCameraFOV;
            hasCleanedCameras = true;
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
                showWindow = !showWindow;

            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
                ResetCameraRotation();

            if (Input.mouseScrollDelta.y != 0 && Input.GetKey(KeyCode.LeftControl)) // ctrl + scroll to zoom (set fov)
            {
                float deltaFOV = Input.mouseScrollDelta.y < 0 ? 5 : -5;
                SetCameraFOV(Math.Min(Math.Max(GetCameraFOV() + deltaFOV, 30), 120));
            }

            HighlightObject(targetedVRInteractable);

            if (targetedVRInteractable == null)
                return;

            VRTwistKnob twistKnob = targetedVRInteractable?.GetComponent<VRTwistKnob>();
            VRTwistKnobInt twistKnobInt = targetedVRInteractable?.GetComponent<VRTwistKnobInt>();
            VRLever lever = targetedVRInteractable?.GetComponent<VRLever>();
            VRThrottle throttle = targetedVRInteractable?.GetComponent<VRThrottle>();

            if (Input.GetMouseButtonDown(2)) // select with middle mouse
            {
                if (targetedVRInteractable != null && twistKnob == null)
                {
                    Interactions.Interact(targetedVRInteractable);
                }
            }
            if (Input.GetMouseButtonUp(2)) // select with middle mouse
            {
                if (targetedVRInteractable != null && twistKnob == null)
                {
                    Interactions.AntiInteract(targetedVRInteractable);
                }
            }
            if (Input.mouseScrollDelta.y != 0) // scroll wheel to move knobs/levers & ctrl+scroll to zoom
            {
                if (!Input.GetKey(KeyCode.LeftControl)) // ctrl not being held
                {
                    if (twistKnob != null)
                    {
                        Interactions.TwistKnob(twistKnob, Input.mouseScrollDelta.y < 0 ? true : false, 0.05f);
                    }
                    else if (twistKnobInt != null)
                    {
                        Interactions.MoveTwistKnobInt(twistKnobInt, Input.mouseScrollDelta.y < 0 ? 1 : -1, true);
                    }
                    else if (lever != null)
                    {
                        Interactions.MoveLever(lever, Input.mouseScrollDelta.y < 0 ? 1 : -1, true);
                    }
                    else if (throttle != null)
                    {
                        Interactions.MoveThrottle(throttle, Input.mouseScrollDelta.y > 0 ? -0.05f : 0.05f);
                    }
                }
            }
        }

        MeshRenderer PreviouslyHighlightedInteractableRenderer;
        Dictionary<MeshRenderer, Color> VRInteractableOriginalColors = new Dictionary<MeshRenderer, Color>();
        private void HighlightObject(VRInteractable targetedVRInteractable)
        {
            HighlightImage(targetedVRInteractable);

            if (PreviouslyHighlightedInteractableRenderer != null)
            {
                PreviouslyHighlightedInteractableRenderer.material.color = VRInteractableOriginalColors[PreviouslyHighlightedInteractableRenderer];
                PreviouslyHighlightedInteractableRenderer = null;
            }

            if (targetedVRInteractable == null)
                return;

            MeshRenderer renderer = GetMeshRendererFromVRInteractable(targetedVRInteractable);
            if (renderer == null)
            {
                return;
            }

            VRInteractableOriginalColors[renderer] = renderer.material.color;

            if (renderer != null)
                renderer.material.color = Color.yellow;

            if (targetedVRInteractable != null)
                PreviouslyHighlightedInteractableRenderer = renderer;
        }

        Image PreviouslyHighlightedInteractableImage;
        Dictionary<Image, Color> VRInteractableImageOriginalColors = new Dictionary<Image, Color>();
        private void HighlightImage(VRInteractable targetedVRInteractable)
        {
            if (PreviouslyHighlightedInteractableImage != null)
            {
                PreviouslyHighlightedInteractableImage.color = VRInteractableImageOriginalColors[PreviouslyHighlightedInteractableImage];
                PreviouslyHighlightedInteractableImage = null;
            }

            if (targetedVRInteractable == null)
                return;

            Image image = GetImageFromVRInteractable(targetedVRInteractable);
            if (image == null)
            {
                return;
            }

            VRInteractableImageOriginalColors[image] = image.color;

            if (image != null)
                image.color = Color.yellow;

            if (targetedVRInteractable != null)
                PreviouslyHighlightedInteractableImage = image;
        }

        private MeshRenderer GetMeshRendererFromVRInteractable(VRInteractable interactable)
        {
            VRButton button = interactable.GetComponent<VRButton>();
            VRLever lever = interactable.GetComponent<VRLever>();
            VRTwistKnob twistKnob = interactable.GetComponent<VRTwistKnob>();
            VRTwistKnobInt twistKnobInt = interactable.GetComponent<VRTwistKnobInt>();
            MeshRenderer meshRenderer = interactable.GetComponent<MeshRenderer>();
            MeshRenderer childMeshRenderer = interactable.GetComponentInChildren<MeshRenderer>();

            if (meshRenderer != null)
                return meshRenderer;
            if (childMeshRenderer != null)
                return childMeshRenderer;

            Transform transform = button?.buttonTransform ??
                lever?.leverTransform ??
                twistKnob?.knobTransform ??
                twistKnobInt?.knobTransform;

            return transform?.GetComponent<MeshRenderer>() ??
                transform?.parent?.GetComponent<MeshRenderer>() ??
                transform?.parent?.parent?.GetComponent<MeshRenderer>() ??
                transform?.GetComponentInChildren<MeshRenderer>();
        }

        private Image GetImageFromVRInteractable(VRInteractable interactable)
        {
            Image image = interactable.GetComponent<Image>();
            Image childImage = interactable.GetComponentInChildren<Image>();
            Image parentImage = interactable.GetComponentInParent<Image>();
            Image parentParentImage = interactable.transform.parent?.GetComponentInParent<Image>();

            return image ??
                childImage ??
                parentImage ??
                parentParentImage;
        }

        int frame = 0;
        const int framesPerTick = 1 * 60;
        const int miniTick = 5;
        bool wasOnTeam = false;
        public void FixedUpdate()
        {
            if (!Enabled)
                return;

            frame++;
            if (frame % miniTick == 0) // every mini tick get the hovered object
            {
                GetHoveredObject();
            }

            if (frame >= framesPerTick)
            {
                frame = 0;
                if (IsFlyingScene())
                    CleanCameras();

                CleanGloves();
                CheckEndMission();

                SetTrackingObject(cameraEyeGameObject);

                VRInteractables = GameObject.FindObjectsOfType<VRInteractable>(false);

                bool isOnTeam = VTOLMPSceneManager.instance?.localPlayer?.chosenTeam ?? false;
                if (isOnTeam != wasOnTeam)
                    Reclean();

                wasOnTeam = isOnTeam;
            }
        }

        public void LateUpdate()
        {
            if (!Enabled)
                return;

            MoveCamera();
        }

        public float Sensitivity = 2f;
        public float RotationLimitX = 160f; // set to -1 to disable
        public float RotationLimitY = 89f; // set to -1 to disable

        public bool LimitXRotation
        {
            get { return RotationLimitX >= 0; }
            set { RotationLimitX = value ? 160f : -1f; }
        }
        public bool LimitYRotation
        {
            get { return RotationLimitY >= 0; }
            set { RotationLimitY = value ? 89f : -1f; }
        }

        public GameObject cameraEyeGameObject;
        public static float DefaultCameraFOV = 80f;
        private Vector2 cameraRotation = Vector2.zero;
        private const string xAxis = "Mouse X";
        private const string yAxis = "Mouse Y";

        public void MoveCamera()
        {
            if (cameraEyeGameObject == null)
                cameraEyeGameObject = GetEyeCameraGameObject();
            if (cameraEyeGameObject == null)
                return;

            if (Input.GetMouseButton(1))
            {
                foreach (Camera camera in GetAllEyeCameras()) // move each camera because the current Eye Camera might be old and inactive
                {
                    cameraRotation.x += Input.GetAxis(xAxis) * Sensitivity;
                    cameraRotation.y += Input.GetAxis(yAxis) * Sensitivity;
                    if (RotationLimitX > 0)
                        cameraRotation.x = Mathf.Clamp(cameraRotation.x, -RotationLimitX, RotationLimitX);
                    if (RotationLimitY > 0)
                        cameraRotation.y = Mathf.Clamp(cameraRotation.y, -RotationLimitY, RotationLimitY);
                    var xQuat = Quaternion.AngleAxis(cameraRotation.x, Vector3.up);
                    var yQuat = Quaternion.AngleAxis(cameraRotation.y, Vector3.left);

                    camera.transform.localRotation = xQuat * yQuat; //Quaternions seem to rotate more consistently than EulerAngles. Sensitivity seemed to change slightly at certain degrees using Euler.
                                                                                 //transform.localEulerAngles = new Vector3(-rotation.y, rotation.x, 0);
                }
            }
        }

        public void ResetCameraRotation()
        {
            if (cameraEyeGameObject == null)
                cameraEyeGameObject = GetEyeCameraGameObject();
            if (cameraEyeGameObject == null)
                return;

            cameraEyeGameObject.transform.localRotation = Quaternion.identity;
        }

        public void SetCameraFOV(float fov)
        {
            if (cameraEyeGameObject == null)
                cameraEyeGameObject = GetEyeCameraGameObject();
            if (cameraEyeGameObject == null)
                return;

            cameraEyeGameObject.GetComponent<Camera>().fieldOfView = fov;
        }
        public float GetCameraFOV()
        {
            if (cameraEyeGameObject == null)
                cameraEyeGameObject = GetEyeCameraGameObject();
            if (cameraEyeGameObject == null)
                return DefaultCameraFOV;

            return cameraEyeGameObject.GetComponent<Camera>().fieldOfView;
        }
        public void Awake()
        {
            SceneManager.activeSceneChanged += RecleanOnSceneChange;
            Instance = this;
        }

        public void Reclean()
        {
            hasCleanedCameras = false;
            hasCleanedGloves = false;
            showEndScreenWindow = false;
            cameraEyeGameObject = null;
            thirdPerson = false;
        }
        private void RecleanOnSceneChange(Scene scene1, Scene scene2)
        {
            Reclean();
        }

        public void OnDestroy()
        {
            SceneManager.activeSceneChanged -= RecleanOnSceneChange;
        }

        public void GetHoveredObject()
        {
            if (cameraEyeGameObject == null)
                cameraEyeGameObject = GetEyeCameraGameObject();
            if (cameraEyeGameObject == null)
                return;

            // Logger.WriteLine($"Checking intersected VRInteractables");

            Camera camera = cameraEyeGameObject.GetComponent<Camera>();

            Ray ray = camera.ScreenPointToRay(Input.mousePosition, Camera.MonoOrStereoscopicEye.Mono);

            List<VRInteractable> intersectedInteractables = new List<VRInteractable>();

            foreach (VRInteractable interactable in VRInteractables)
            {
                if (interactable == null || interactable.transform == null)
                    continue;

                Bounds bounds;

                float radius = Mathf.Min(Mathf.Max(0.01f, interactable.radius), 0.1f); // have a minimum (and maximum) radius to avoid 0 size radius (and to avoid having to calculate rect sizes)
                bounds = new Bounds(interactable.transform.position, Vector3.one * radius);

                if (bounds.IntersectRay(ray))
                {
                    intersectedInteractables.Add(interactable);
                }
            }

            float depth = 0.5f;
            VRInteractable hoveredInteractable = intersectedInteractables
                .Where(x => x != null && x.transform != null)
                .OrderBy((x) => Vector3.Distance(x.transform.position, ray.origin + (ray.direction * depth)))
                .FirstOrDefault();

            targetedVRInteractable = hoveredInteractable;
        }

        public IEnumerable<Camera> GetSpectatorCameras()
        {
            return GameObject.FindObjectsOfType<Camera>(true).Where(c => c.name == "FlybyCam" || c.name == "flybyHMCScam");
        }

        public static Camera GetEyeCamera()
        {
            IEnumerable<Camera> cameras = GameObject.FindObjectsOfType<Camera>(false).Where(c => c.name == "Camera (eye)" && c.isActiveAndEnabled).OrderByDescending(c => c.depth);
            if (cameras.Any(x => x.gameObject?.layer == LayerMask.NameToLayer("MPBriefing")))
            {
                if (VTOLMPSceneManager.instance.localPlayer.chosenTeam)
                {
                    GameObject localAvatarObject = typeof(VTOLMPSceneManager)
                        .GetField("localAvatarObj", BindingFlags.Instance | BindingFlags.NonPublic)?
                        .GetValue(VTOLMPSceneManager.instance) as GameObject;
                    if (localAvatarObject != null)
                    {
                        Camera localAvatarCam = localAvatarObject?.GetComponentInChildren<Camera>(false);
                        if (localAvatarCam != null)
                        {
                            return localAvatarCam;
                        }
                    }
                }
            }
            return cameras.FirstOrDefault();
        }
        public static GameObject GetEyeCameraGameObject()
        {
            return GetEyeCamera()?.gameObject;
        }

        public static IEnumerable<Camera> GetAllEyeCameras()
        {
            return GameObject.FindObjectsOfType<Camera>(false).Where(c => c.name == "Camera (eye)" && c.isActiveAndEnabled);
        }

        bool hasCleanedGloves = false;
        private Vector2 scrollPosition;

        public void CleanGloves()
        {
            if (hasCleanedGloves)
                return;
            Camera eyeCamera = GetEyeCamera();

            if (eyeCamera == null)
                return;

            Transform CamRig = eyeCamera.transform.parent;
            Transform leftController = CamRig?.Find("Controller (left)");
            Transform rightController = CamRig?.Find("Controller (right)");

            // leftController.transform.position = eyeCamera.transform.position + (eyeCamera.transform.forward * 1.5f);
            // rightController.transform.position = eyeCamera.transform.position + (eyeCamera.transform.forward * 1.5f);
            leftController.gameObject.SetActive(false);
            rightController.gameObject.SetActive(false);

            hasCleanedGloves = true;
        }

        public void CheckEndMission()
        {
            EndMission endMission = GameObject.FindObjectOfType<EndMission>(false);
            if (endMission == null || endMission.enabled == false)
                return;

            if (endMission.endScreenObject?.activeSelf == true)
                ShowEndScreenWindow(endMission);
        }
    }
}
