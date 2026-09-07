using System.Collections.Generic;
using System.Linq;
using Live2D.Cubism.Core;
using UI.SexPosition;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace MaisLoveStory.Live2D.Editor
{
    /// <summary>
    /// Standalone bench for testing any Live2D model's animations in isolation.
    /// It disables scene Live2D models while its own model is spawned and lets
    /// physical-position animation events use the shared FMOD SFX path.
    ///
    /// It drives the shared <see cref="Live2DMotionController"/> API, so it faithfully
    /// covers BOTH wiring paths that exist in the game:
    ///  - Dialogue models (Mai/Staff): PlayMotion(AnimationType.ToString(), loop, floatValue)
    ///    as done by Live2DPanel.
    ///  - Simulation models (Missionary/Roleplay*): raw SetTrigger/SetBool/SetFloat on the
    ///    animator parameters as done by SimulationNavigationPanel.
    ///
    /// The window builds its own bench at Play Mode entry (a camera + the instantiated
    /// prefab), so no hand-authored scene asset is required. Model prefabs already carry
    /// their full Cubism update pipeline, so they animate on their own once instantiated.
    /// </summary>
    public class Live2DTestBenchWindow : EditorWindow
    {
        private const string BenchRootName = "__Live2DTestBench__";
        private const string BenchCameraName = "__Live2DTestBenchCamera__";
        private const string RoleplayPanelName = "__RoleplaySimulationPanel__";
        private const string SearchFolder = "Assets/Image";

        [System.Serializable]
        private class ModelEntry
        {
            public string guid;
            public string path;
            public string displayName;
        }

        // Serialized so state survives the domain reload triggered by entering Play Mode.
        [SerializeField] private string selectedGuid;
        [SerializeField] private bool pendingSpawn;
        [SerializeField] private bool loopDialogue = true;
        [SerializeField] private float dialogueFloatValue = 0.5f;
        [SerializeField] private int dialogueEnumIndex;

        // Simulation-transition bench state (mirrors the position configs' own fields).
        [SerializeField] private bool benchPussySelected = true;
        [SerializeField] private bool benchMissionaryUseCondom;
        [SerializeField] private int benchMissionarySkillTier;
        [SerializeField] private bool benchCowgirlUseCondom;
        [SerializeField] private int benchCowgirlLewdTier;
        [SerializeField] private RoleplayCategory benchRoleplayCategory = RoleplayCategory.Hand;
        [SerializeField] private bool benchRoleplayCategorySelected;
        [SerializeField] private int benchRoleplayAnimation;
        [SerializeField] private bool showRawAnimatorParameters;

        private readonly List<ModelEntry> models = new List<ModelEntry>();
        private string[] modelDisplayNames = new string[0];
        private string[] animationEnumNames = new string[0];
        private Live2DMotionController activeController;
        private SimulationNavigationPanel roleplayPanel;
        private SexPositionConfig roleplayConfig;
        private int lastRoleplayAnimatorStateHash;
        private readonly Dictionary<GameObject, bool> isolatedModelStates =
            new Dictionary<GameObject, bool>();
        private Vector2 scroll;

        [MenuItem("Tools/Live2D/Test Bench")]
        public static void Open()
        {
            var window = GetWindow<Live2DTestBenchWindow>("Live2D Test Bench");
            window.minSize = new Vector2(380, 460);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshModelList();
            animationEnumNames = System.Enum.GetNames(typeof(AnimationType));
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += OnEditorUpdate;
            TryRebindActiveController();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.update -= OnEditorUpdate;

            if (Application.isPlaying)
            {
                ClearBench();
            }
        }

        private void OnEditorUpdate()
        {
            if (!Application.isPlaying || activeController == null)
            {
                return;
            }

            MaintainModelIsolation();
            SyncRoleplayAnimatorStateEntry();
            Repaint();
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && pendingSpawn)
            {
                pendingSpawn = false;
                DoSpawn();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                ClearBench();
            }

            Repaint();
        }

        // ---------------------------------------------------------------------
        // Model discovery
        // ---------------------------------------------------------------------

        private void RefreshModelList()
        {
            models.Clear();

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { SearchFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                // A model is testable if it either bakes in the controller (Staff, Roleplay positions)
                // or carries the raw Cubism pipeline (CubismModel + Animator), in which case the
                // game attaches the controller at the scene level — we reproduce that at spawn.
                bool hasController = prefab.GetComponentInChildren<Live2DMotionController>(true) != null;
                bool hasCubismPipeline = prefab.GetComponentInChildren<CubismModel>(true) != null
                                          && prefab.GetComponentInChildren<Animator>(true) != null;
                if (!hasController && !hasCubismPipeline)
                {
                    continue;
                }

                models.Add(new ModelEntry
                {
                    guid = guid,
                    path = path,
                    displayName = System.IO.Path.GetFileNameWithoutExtension(path)
                });
            }

            models.Sort((a, b) => string.Compare(a.displayName, b.displayName, System.StringComparison.OrdinalIgnoreCase));
            modelDisplayNames = models.Select(m => m.displayName).ToArray();

            if (string.IsNullOrEmpty(selectedGuid) && models.Count > 0)
            {
                selectedGuid = models[0].guid;
            }
        }

        private int SelectedIndex
        {
            get
            {
                for (int i = 0; i < models.Count; i++)
                {
                    if (models[i].guid == selectedGuid)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        // ---------------------------------------------------------------------
        // Bench lifecycle
        // ---------------------------------------------------------------------

        private void EnterPlayModeAndSpawn()
        {
            if (Application.isPlaying)
            {
                DoSpawn();
                return;
            }

            pendingSpawn = true;
            EditorApplication.isPlaying = true;
        }

        private void DoSpawn()
        {
            ClearBench();

            int index = SelectedIndex;
            if (index < 0)
            {
                Debug.LogWarning("[Live2DTestBench] No model selected.");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(models[index].path);
            if (prefab == null)
            {
                Debug.LogError($"[Live2DTestBench] Failed to load prefab at {models[index].path}");
                return;
            }

            var root = new GameObject(BenchRootName);

            var instance = Object.Instantiate(prefab, root.transform);
            instance.name = prefab.name;
            instance.transform.localPosition = Vector3.zero;
            instance.SetActive(true);

            activeController = instance.GetComponentInChildren<Live2DMotionController>(true);
            if (activeController == null)
            {
                // Not baked into the prefab (e.g. Mai Body, Sex Missionary) — the game adds it
                // at the scene level, so attach it to the Cubism model's GameObject here.
                // Live2DMotionController.Awake() auto-resolves CubismModel + Animator via
                // GetComponent, so it must sit on the object that holds both.
                var cubismModel = instance.GetComponentInChildren<CubismModel>(true);
                if (cubismModel != null)
                {
                    activeController = cubismModel.gameObject.AddComponent<Live2DMotionController>();
                }
                else
                {
                    Debug.LogError($"[Live2DTestBench] Spawned '{prefab.name}' has no CubismModel; cannot attach a controller.");
                }
            }

            InitializeRoleplayBench(root);
            InitializeSexSimulationSfxPreview(root);
            IsolateSceneModels(root);
            EnsureBenchCamera(instance);
            Selection.activeGameObject = instance;
            Repaint();
        }

        private void EnsureBenchCamera(GameObject target)
        {
            var camGo = GameObject.Find(BenchCameraName);
            Camera cam;
            if (camGo == null)
            {
                camGo = new GameObject(BenchCameraName);
                cam = camGo.AddComponent<Camera>();
            }
            else
            {
                cam = camGo.GetComponent<Camera>();
            }

            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.16f, 0.19f, 1f);

            var renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                cam.orthographicSize = 3f;
                cam.transform.position = new Vector3(0f, 0f, -10f);
                cam.transform.rotation = Quaternion.identity;
                return;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers)
            {
                bounds.Encapsulate(r.bounds);
            }

            cam.orthographicSize = Mathf.Max(0.5f, bounds.extents.y * 1.15f);
            cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
            cam.transform.rotation = Quaternion.identity;
        }

        private void ClearBench()
        {
            var existingRoot = GameObject.Find(BenchRootName);
            if (existingRoot != null)
            {
                DestroyImmediate(existingRoot);
            }

            var existingCam = GameObject.Find(BenchCameraName);
            if (existingCam != null)
            {
                DestroyImmediate(existingCam);
            }

            RestoreSceneModels();
            activeController = null;
            roleplayPanel = null;
            roleplayConfig = null;
            lastRoleplayAnimatorStateHash = 0;
        }

        private void InitializeSexSimulationSfxPreview(GameObject benchRoot)
        {
            SexSimulationSfxPreviewContext preview =
                benchRoot.GetComponent<SexSimulationSfxPreviewContext>();

            if (IsMissionaryLike())
            {
                preview ??= benchRoot.AddComponent<SexSimulationSfxPreviewContext>();
                preview.Initialize(SexPositionConfigType.Missionary);
            }
            else if (IsCowgirlLike())
            {
                preview ??= benchRoot.AddComponent<SexSimulationSfxPreviewContext>();
                preview.Initialize(SexPositionConfigType.Cowgirl);
            }
            else if (preview != null)
            {
                DestroyImmediate(preview);
            }
        }

        private void IsolateSceneModels(GameObject benchRoot)
        {
            RestoreSceneModels();
            TrackAndDisableSceneModels(benchRoot);
        }

        private void MaintainModelIsolation()
        {
            GameObject benchRoot = GameObject.Find(BenchRootName);
            if (benchRoot == null)
            {
                RestoreSceneModels();
                return;
            }

            TrackAndDisableSceneModels(benchRoot);
        }

        private void TrackAndDisableSceneModels(GameObject benchRoot)
        {
            CubismModel[] sceneModels = Object.FindObjectsByType<CubismModel>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (CubismModel model in sceneModels)
            {
                if (model == null ||
                    !model.gameObject.scene.IsValid() ||
                    model.transform.IsChildOf(benchRoot.transform))
                {
                    continue;
                }

                GameObject modelObject = model.gameObject;
                if (!isolatedModelStates.ContainsKey(modelObject))
                {
                    isolatedModelStates.Add(modelObject, modelObject.activeSelf);
                }

                if (modelObject.activeSelf)
                {
                    modelObject.SetActive(false);
                }
            }
        }

        private void RestoreSceneModels()
        {
            foreach (KeyValuePair<GameObject, bool> entry in isolatedModelStates)
            {
                if (entry.Key != null && entry.Key.scene.IsValid())
                {
                    entry.Key.SetActive(entry.Value);
                }
            }

            isolatedModelStates.Clear();
        }

        private void TryRebindActiveController()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var existingRoot = GameObject.Find(BenchRootName);
            if (existingRoot != null)
            {
                activeController = existingRoot.GetComponentInChildren<Live2DMotionController>(true);
                InitializeRoleplayBench(existingRoot);
            }
        }

        private void InitializeRoleplayBench(GameObject benchRoot)
        {
            roleplayPanel = null;
            roleplayConfig = null;
            lastRoleplayAnimatorStateHash = 0;

            bool isRoleplayBlowjob = IsRoleplayBlowjobLike();
            bool isRoleplayButthole = IsRoleplayButtholeLike();
            bool isRoleplayPaizuri = IsRoleplayPaizuriLike();
            bool isRoleplayPussy = IsRoleplayPussyLike();
            if (activeController == null ||
                (!isRoleplayBlowjob &&
                 !isRoleplayButthole &&
                 !isRoleplayPaizuri &&
                 !isRoleplayPussy))
            {
                return;
            }

            Transform panelTransform = benchRoot.transform.Find(RoleplayPanelName);
            GameObject panelObject;
            if (panelTransform == null)
            {
                panelObject = new GameObject(RoleplayPanelName);
                panelObject.transform.SetParent(benchRoot.transform, false);
                panelObject.SetActive(false);
                roleplayPanel = panelObject.AddComponent<SimulationNavigationPanel>();
            }
            else
            {
                panelObject = panelTransform.gameObject;
                roleplayPanel = panelObject.GetComponent<SimulationNavigationPanel>();
                if (roleplayPanel == null)
                {
                    roleplayPanel = panelObject.AddComponent<SimulationNavigationPanel>();
                }
            }

            SexPositionConfigType roleplayType = isRoleplayBlowjob
                ? SexPositionConfigType.RoleplayBlowjob
                : isRoleplayPaizuri
                    ? SexPositionConfigType.RoleplayPaizuri
                : isRoleplayButthole
                    ? SexPositionConfigType.RoleplayButthole
                    : SexPositionConfigType.RoleplayPussy;
            roleplayConfig = roleplayPanel.ConfigureRoleplayTestBench(
                roleplayType,
                activeController);
            benchRoleplayCategory = roleplayConfig.GetRoleplayCategoryForButton(1);
            benchRoleplayCategorySelected = false;
            benchRoleplayAnimation = 0;
        }

        private void SyncRoleplayAnimatorStateEntry()
        {
            if (roleplayConfig == null || roleplayPanel == null)
            {
                return;
            }

            Animator animator = GetActiveAnimator();
            if (animator == null || !animator.isActiveAndEnabled)
            {
                return;
            }

            if (animator.IsInTransition(0))
            {
                return;
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.fullPathHash == 0 ||
                stateInfo.fullPathHash == lastRoleplayAnimatorStateHash)
            {
                return;
            }

            lastRoleplayAnimatorStateHash = stateInfo.fullPathHash;
            bool isRoleplayBlowjob = IsRoleplayBlowjobLike();
            bool isRoleplayButthole = IsRoleplayButtholeLike();
            bool isRoleplayPaizuri = IsRoleplayPaizuriLike();
            string startStateName = isRoleplayBlowjob
                ? "Blowjob Roleplay - Start"
                : isRoleplayPaizuri
                    ? "Paizuri Start"
                : isRoleplayButthole
                    ? "Roleplay Butthole Start"
                    : "Roleplay Pussy Start";
            if (stateInfo.IsName(startStateName))
            {
                roleplayConfig.OnRoleplayStartEntered(roleplayPanel);
                if (roleplayConfig.AreRoleplayCategoriesAvailable())
                {
                    ClearRoleplayBenchSelection();
                }
            }
            else if (!isRoleplayBlowjob && !isRoleplayPaizuri &&
                     ((isRoleplayButthole &&
                      IsAnimatorState(
                          stateInfo,
                          "Toy - Egg (Insert Loop)",
                          "Roleplay Butthole Toy - Egg (Insert Loop)")) ||
                     (!isRoleplayButthole &&
                      stateInfo.IsName("Roleplay Pussy Toy Egg Vib - Work"))))
            {
                if (isRoleplayButthole)
                {
                    roleplayConfig.OnRoleplayEggLoopEntered(roleplayPanel);
                }
                else
                {
                    roleplayConfig.OnRoleplayEggWorkEntered(roleplayPanel);
                }
            }
            else if ((isRoleplayBlowjob &&
                      IsAnimatorState(
                          stateInfo,
                          "Blowjob Roleplay - Cum Handjob Loop",
                          "Blowjob Roleplay - Cum Blowjob Loop",
                          "Blowjob Roleplay - Cum Face Loop")) ||
                     (isRoleplayButthole &&
                      IsAnimatorState(
                          stateInfo,
                          "Cumming 2",
                          "Roleplay Butthole Cumming 2")) ||
                     (isRoleplayPaizuri &&
                      IsAnimatorState(
                          stateInfo,
                          "Paizuri Cumming Dick 1 - Loop",
                          "Paizuri Cumming Dick 2 - Loop",
                          "Paizuri Cumming Dick 3 - Loop",
                          "Paizuri Cumming Mai - After")) ||
                     stateInfo.IsName("Roleplay Pussy After Cumming") ||
                     stateInfo.IsName("Roleplay Pussy After Cumming - Up"))
            {
                roleplayConfig.OnAfterCummingEntered(roleplayPanel);
            }
        }

        private void ClearRoleplayBenchSelection()
        {
            benchRoleplayCategory = roleplayConfig != null
                ? roleplayConfig.GetRoleplayCategoryForButton(1)
                : RoleplayCategory.Hand;
            benchRoleplayCategorySelected = false;
            benchRoleplayAnimation = 0;
        }

        private Animator GetActiveAnimator()
        {
            return activeController == null
                ? null
                : activeController.GetComponent<Animator>() ??
                  activeController.GetComponentInChildren<Animator>(true);
        }

        private string GetRoleplayAnimatorStateLabel()
        {
            Animator animator = GetActiveAnimator();
            if (animator == null)
            {
                return "Animator unavailable";
            }

            string current = GetAnimatorStateName(
                animator,
                animator.GetCurrentAnimatorStateInfo(0));
            if (!animator.IsInTransition(0))
            {
                return current;
            }

            string next = GetAnimatorStateName(
                animator,
                animator.GetNextAnimatorStateInfo(0));
            return $"{current}  →  {next}";
        }

        private static string GetAnimatorStateName(
            Animator animator,
            AnimatorStateInfo stateInfo)
        {
            var controller = animator.runtimeAnimatorController as AnimatorController;
            if (controller != null && controller.layers.Length > 0)
            {
                AnimatorState state = controller.layers[0].stateMachine.states
                    .Select(child => child.state)
                    .FirstOrDefault(candidate =>
                        stateInfo.shortNameHash == Animator.StringToHash(candidate.name));
                if (state != null)
                {
                    return state.name;
                }
            }

            return $"State {stateInfo.shortNameHash}";
        }

        // ---------------------------------------------------------------------
        // GUI
        // ---------------------------------------------------------------------

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawModelSelector();
            EditorGUILayout.Space();
            DrawBenchControls();

            if (activeController != null)
            {
                EditorGUILayout.Space();
                DrawPlaybackControls();
                EditorGUILayout.Space();
                DrawDialogueSection();
                EditorGUILayout.Space();
                DrawSimulationTransitionSection();
                EditorGUILayout.Space();
                showRawAnimatorParameters = EditorGUILayout.Foldout(
                    showRawAnimatorParameters,
                    "Raw Animator Parameters",
                    true);
                if (showRawAnimatorParameters)
                {
                    DrawParameterSection();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawModelSelector()
        {
            EditorGUILayout.LabelField("Model", EditorStyles.boldLabel);

            if (models.Count == 0)
            {
                EditorGUILayout.HelpBox($"No prefabs with a Live2DMotionController found under {SearchFolder}.", MessageType.Warning);
                if (GUILayout.Button("Refresh Model List"))
                {
                    RefreshModelList();
                }

                return;
            }

            EditorGUILayout.BeginHorizontal();
            int current = Mathf.Max(0, SelectedIndex);
            int next = EditorGUILayout.Popup(current, modelDisplayNames);
            if (next != current)
            {
                selectedGuid = models[next].guid;
            }

            if (GUILayout.Button("Refresh", GUILayout.Width(70)))
            {
                RefreshModelList();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBenchControls()
        {
            EditorGUILayout.LabelField("Bench", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Animations run only in Play Mode. This spawns the selected model with its own camera, disables every other scene Live2D model, and restores them on Clear or exit.",
                    MessageType.Info);

                using (new EditorGUI.DisabledScope(models.Count == 0))
                {
                    if (GUILayout.Button("Enter Play Mode & Spawn", GUILayout.Height(28)))
                    {
                        EnterPlayModeAndSpawn();
                    }
                }

                return;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(activeController == null ? "Spawn" : "Respawn", GUILayout.Height(24)))
            {
                DoSpawn();
            }

            if (GUILayout.Button("Clear", GUILayout.Height(24)))
            {
                ClearBench();
                Repaint();
            }

            if (GUILayout.Button("Exit Play Mode", GUILayout.Height(24)))
            {
                EditorApplication.isPlaying = false;
            }

            EditorGUILayout.EndHorizontal();

            if (activeController == null)
            {
                EditorGUILayout.HelpBox("No model spawned. Press Spawn.", MessageType.None);
            }
        }

        private void DrawDialogueSection()
        {
            List<string> resolvable = GetResolvableAnimationTypeNames();
            if (resolvable.Count == 0)
            {
                // No dialogue AnimationType resolves on this model (e.g. simulation
                // models driven purely by raw animator parameters). Hide the section.
                return;
            }

            EditorGUILayout.LabelField("Dialogue Expression (PlayMotion)", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            string[] names = resolvable.ToArray();
            dialogueEnumIndex = Mathf.Clamp(dialogueEnumIndex, 0, names.Length - 1);
            dialogueEnumIndex = EditorGUILayout.Popup("AnimationType", dialogueEnumIndex, names);
            loopDialogue = EditorGUILayout.Toggle("Loop", loopDialogue);
            dialogueFloatValue = EditorGUILayout.Slider("Float Value", dialogueFloatValue, 0f, 1f);

            string selectedName = names[dialogueEnumIndex];
            if (GUILayout.Button($"Play '{selectedName}'"))
            {
                activeController.PlayMotion(selectedName, loopDialogue, dialogueFloatValue);
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Returns the AnimationType names that actually resolve on the active model,
        /// mirroring PlayMotion's resolution rule: a name resolves when the animator
        /// has a Trigger/Bool parameter of that name, or a Bool parameter named
        /// "{name}Loop".
        /// </summary>
        private List<string> GetResolvableAnimationTypeNames()
        {
            var resolvable = new List<string>();
            if (activeController == null || animationEnumNames == null)
            {
                return resolvable;
            }

            foreach (string name in animationEnumNames)
            {
                if (name == nameof(AnimationType.None))
                {
                    continue;
                }

                AnimatorControllerParameterType? directType = activeController.GetParameterType(name);
                bool directResolves = directType.HasValue
                    && (directType.Value == AnimatorControllerParameterType.Trigger
                        || directType.Value == AnimatorControllerParameterType.Bool);

                AnimatorControllerParameterType? loopType = activeController.GetParameterType(name + "Loop");
                bool loopResolves = loopType.HasValue
                    && loopType.Value == AnimatorControllerParameterType.Bool;

                if (directResolves || loopResolves)
                {
                    resolvable.Add(name);
                }
            }

            return resolvable;
        }

        private void DrawPlaybackControls()
        {
            EditorGUILayout.LabelField("Playback", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play Default"))
            {
                activeController.PlayDefaultMotion();
            }

            if (GUILayout.Button("Stop"))
            {
                activeController.StopCurrentMotion();
            }

            if (GUILayout.Button("Reset All Bools"))
            {
                activeController.ResetAllBoolParameters();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawParameterSection()
        {
            EditorGUILayout.LabelField("Raw Animator Parameters (Simulation)", EditorStyles.boldLabel);

            string[] parameters = activeController.GetAvailableParameters();
            if (parameters == null || parameters.Length == 0)
            {
                EditorGUILayout.HelpBox("No animator parameters found on this model.", MessageType.None);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foreach (string param in parameters.OrderBy(p => p))
            {
                DrawParameterControl(param);
            }

            EditorGUILayout.EndVertical();
        }


        // ---------------------------------------------------------------------
        // Simulation transitions
        //
        // Replays the same animator-parameter sequences the real position configs
        // drive during a sex-sim session, so the bench previews actual transitions
        // (insert -> slow/fast -> stop/cum/pullout, or roleplay action/toy swaps),
        // not just isolated parameter pokes. Only the animator writes are mirrored;
        // the configs' SexSimulationManager stat calls are gameplay bookkeeping with
        // no visual effect and require the full game, so they are intentionally omitted.
        //
        // Sources kept in sync with:
        //   Assets/Scripts/UI/SexPosition/MissionaryPositionConfig.cs
        //   Assets/Scripts/UI/SexPosition/CowgirlPositionConfig.cs
        //   Assets/Scripts/UI/SexPosition/RoleplayPussyPositionConfig.cs
        // ---------------------------------------------------------------------

        private bool IsMissionaryLike()
        {
            return activeController != null
                   && activeController.HasParameter("IsButthole")
                   && activeController.HasParameter("UseCondom")
                   && activeController.HasParameter("SkillValue")
                   && activeController.HasParameter("Insert");
        }

        private bool IsCowgirlLike()
        {
            return activeController != null &&
                   !activeController.HasParameter("IsButthole") &&
                   activeController.HasParameter("UseCondom") &&
                   activeController.HasParameter("LewdLevelValue") &&
                   activeController.HasParameter("Insert") &&
                   activeController.HasParameter("Waiting") &&
                   activeController.HasParameter("Squirting");
        }

        private bool IsRoleplayPussyLike()
        {
            if (IsRoleplayBlowjobLike() ||
                IsRoleplayButtholeLike() ||
                IsRoleplayPaizuriLike())
            {
                return false;
            }

            return IsNamedRoleplayModel("Pussy") ||
                   (activeController != null &&
                    activeController.HasParameter("SlowLoop") &&
                   activeController.HasParameter("SlowValue"));
        }

        private bool IsRoleplayBlowjobLike()
        {
            return IsNamedRoleplayModel("Blowjob");
        }

        private bool IsRoleplayButtholeLike()
        {
            return IsNamedRoleplayModel("Butthole");
        }

        private bool IsRoleplayPaizuriLike()
        {
            return IsNamedRoleplayModel("Paizuri");
        }

        private bool IsNamedRoleplayModel(string roleplayName)
        {
            if (activeController == null)
            {
                return false;
            }

            Animator animator = GetActiveAnimator();
            string controllerName = animator != null && animator.runtimeAnimatorController != null
                ? animator.runtimeAnimatorController.name
                : string.Empty;
            bool hierarchyNameMatches = activeController
                .GetComponentsInParent<Transform>(true)
                .Any(candidate => candidate.name.IndexOf(
                    roleplayName,
                    System.StringComparison.OrdinalIgnoreCase) >= 0);
            return controllerName.IndexOf(roleplayName, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   hierarchyNameMatches;
        }

        private void DrawSimulationTransitionSection()
        {
            bool missionary = IsMissionaryLike();
            bool cowgirl = IsCowgirlLike();
            bool roleplay = IsRoleplayBlowjobLike() ||
                            IsRoleplayPaizuriLike() ||
                            IsRoleplayPussyLike() ||
                            IsRoleplayButtholeLike();
            if (!missionary && !cowgirl && !roleplay)
            {
                // Not a recognized simulation model; the raw-parameter section still
                // covers whatever animator parameters it does expose.
                return;
            }

            EditorGUILayout.LabelField("Simulation Transitions", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            if (missionary)
            {
                DrawMissionaryTransitions();
            }
            else if (cowgirl)
            {
                DrawCowgirlTransitions();
            }
            else
            {
                DrawRoleplayTransitions();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawMissionaryTransitions()
        {
            benchPussySelected = EditorGUILayout.Toggle(
                benchPussySelected ? "Hole: Pussy" : "Hole: Butthole", benchPussySelected);
            benchMissionaryUseCondom = EditorGUILayout.Toggle(
                "Use Condom", benchMissionaryUseCondom);
            benchMissionarySkillTier = EditorGUILayout.Popup(
                "Skill Tier",
                Mathf.Clamp(benchMissionarySkillTier, 0, 2),
                new[] { "Level 1-2", "Level 3-4", "Level 5" });

            ApplyMissionarySelection();

            EditorGUILayout.LabelField("Sequence", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("Play Start / Reset"))
            {
                SafeBool("Thrusting", false);
                SafeBool("Fast", false);
                SafeBool("SquirtingLoop", false);
                activeController.PlayDefaultMotion();
            }

            if (GUILayout.Button("Insert"))
            {
                SafeBool("Thrusting", false);
                SafeBool("Fast", false);
                SafeBool("SquirtingLoop", false);
                SafeTrigger("Insert");
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Slow"))
            {
                StartMissionaryThrust(false);
            }

            if (GUILayout.Button("Fast"))
            {
                StartMissionaryThrust(true);
            }

            if (GUILayout.Button("Stop"))
            {
                SafeBool("Thrusting", false);
                SafeBool("Fast", false);
                SafeBool("SquirtingLoop", false);
                SafeTrigger("Stop");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Cum", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Cum Inside"))
            {
                SafeBool("Thrusting", false);
                SafeBool("SquirtingLoop", false);
                SafeTrigger("Cum");
            }

            if (GUILayout.Button("Cum Outside"))
            {
                SafeBool("Thrusting", false);
                SafeBool("SquirtingLoop", false);
                SafeTrigger("CumOutside");
            }

            if (GUILayout.Button("Pullout"))
            {
                SafeTrigger("PullOut");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Dormant Test-Bench Branches", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Waiting"))
            {
                SafeTrigger("Waiting");
            }

            if (GUILayout.Button("Squirting"))
            {
                SafeBool("Thrusting", false);
                SafeBool("SquirtingLoop", true);
                SafeTrigger("Squirting");
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ApplyMissionarySelection()
        {
            SafeBool("IsButthole", !benchPussySelected);
            SafeBool("UseCondom", benchMissionaryUseCondom);
            SafeFloat("SkillValue", GetMissionarySkillValue());
        }

        private float GetMissionarySkillValue()
        {
            return benchMissionarySkillTier switch
            {
                0 => 1.5f,
                1 => 3.5f,
                _ => 5f
            };
        }

        private void StartMissionaryThrust(bool isFast)
        {
            ApplyMissionarySelection();
            SafeBool("SquirtingLoop", false);
            SafeBool("Fast", isFast);
            SafeBool("Thrusting", true);
        }

        private void DrawCowgirlTransitions()
        {
            benchCowgirlUseCondom = EditorGUILayout.Toggle(
                "Use Condom",
                benchCowgirlUseCondom);
            benchCowgirlLewdTier = EditorGUILayout.Popup(
                "Lewd Level",
                Mathf.Clamp(benchCowgirlLewdTier, 0, 2),
                new[] { "Level 1-2", "Level 3-4", "Level 5" });

            ApplyCowgirlSelection();

            EditorGUILayout.LabelField("Sequence", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("Play Start / Reset"))
            {
                SafeBool("Thrusting", false);
                SafeBool("Fast", false);
                activeController.PlayDefaultMotion();
                ApplyCowgirlSelection();
            }

            if (GUILayout.Button("Insert Pussy"))
            {
                SafeBool("Thrusting", false);
                SafeBool("Fast", false);
                SafeTrigger("Insert");
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Slow"))
            {
                StartCowgirlThrust(false);
            }
            if (GUILayout.Button("Fast"))
            {
                StartCowgirlThrust(true);
            }
            if (GUILayout.Button("Stop"))
            {
                SafeBool("Thrusting", false);
                SafeBool("Fast", false);
                SafeTrigger("Stop");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Automatic / Mai", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Waiting"))
            {
                SafeBool("Thrusting", false);
                SafeBool("Fast", false);
                SafeTrigger("Waiting");
            }
            if (GUILayout.Button("Squirting"))
            {
                SafeBool("Thrusting", false);
                SafeBool("Fast", false);
                SafeTrigger("Squirting");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Cum", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Cum Inside"))
            {
                SafeBool("Thrusting", false);
                SafeBool("Fast", false);
                SafeTrigger("Cum");
            }
            using (new EditorGUI.DisabledScope(benchCowgirlUseCondom))
            {
                if (GUILayout.Button("Cum Outside"))
                {
                    SafeBool("Thrusting", false);
                    SafeBool("Fast", false);
                    SafeTrigger("CumOutside");
                }
            }
            if (GUILayout.Button("Pull Out"))
            {
                SafeTrigger("PullOut");
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ApplyCowgirlSelection()
        {
            SafeBool("UseCondom", benchCowgirlUseCondom);
            SafeFloat(
                "LewdLevelValue",
                benchCowgirlLewdTier switch
                {
                    0 => 1.5f,
                    1 => 3.5f,
                    _ => 5f
                });
        }

        private void StartCowgirlThrust(bool isFast)
        {
            ApplyCowgirlSelection();
            SafeBool("Fast", isFast);
            SafeBool("Thrusting", true);
        }

        private void DrawRoleplayTransitions()
        {
            if (roleplayConfig == null || roleplayPanel == null)
            {
                EditorGUILayout.HelpBox(
                    "The Roleplay simulation driver is not bound to this bench model. Respawn the model to initialize it.",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Simulation Panel Controls", EditorStyles.miniBoldLabel);
            string positionName = roleplayConfig.GetType().Name;
            EditorGUILayout.HelpBox(
                $"These buttons call the same {positionName} methods as the Game scene's Simulation panel.",
                MessageType.None);
            EditorGUILayout.LabelField("Animator State", GetRoleplayAnimatorStateLabel());
            EditorGUILayout.LabelField("Simulation State", roleplayPanel.CurrentState.ToString());
            if (IsRoleplayButtholeLike() &&
                benchRoleplayCategory == RoleplayCategory.SexToy &&
                benchRoleplayAnimation == 2)
            {
                EditorGUILayout.HelpBox(
                    "Egg: Remove Egg tests the early-removal route; Mai Cum 100% tests Mai's orgasm route. Both enter Cumming 2, which keeps looping until Stop.",
                    MessageType.None);
            }

            bool selectorsLocked = roleplayPanel.CurrentState == SimulationState.Transitioning ||
                                   roleplayPanel.CurrentState == SimulationState.AfterCumming;

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("Category", EditorStyles.miniBoldLabel);
            bool categoriesLocked = selectorsLocked ||
                                    !roleplayConfig.AreRoleplayCategoriesAvailable();
            using (new EditorGUI.DisabledScope(categoriesLocked))
            {
                EditorGUILayout.BeginHorizontal();
                for (int buttonNumber = 1; buttonNumber <= 3; buttonNumber++)
                {
                    DrawRoleplayCategoryButton(
                        roleplayConfig.GetRoleplayCategoryButtonLabel(buttonNumber),
                        roleplayConfig.GetRoleplayCategoryForButton(buttonNumber));
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.LabelField("Animation", EditorStyles.miniBoldLabel);
            bool animationsLocked = selectorsLocked ||
                                    roleplayConfig.AreRoleplayCategoriesAvailable();
            using (new EditorGUI.DisabledScope(animationsLocked))
            {
                EditorGUILayout.BeginHorizontal();
                string[] animationLabels = GetRoleplayAnimationLabels();
                for (int index = 0; index < animationLabels.Length; index++)
                {
                    DrawRoleplayAnimationButton(animationLabels[index], index + 1);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.LabelField("Playback", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            GetRoleplayPlaybackAvailability(out bool slowAvailable, out bool fastAvailable);
            slowAvailable &= !selectorsLocked;
            fastAvailable &= !selectorsLocked;
            using (new EditorGUI.DisabledScope(!slowAvailable))
            {
                if (GUILayout.Button("Slow"))
                {
                    roleplayConfig.OnThrustStarted(roleplayPanel, false);
                }
            }

            using (new EditorGUI.DisabledScope(!fastAvailable))
            {
                if (GUILayout.Button("Fast"))
                {
                    roleplayConfig.OnThrustStarted(roleplayPanel, true);
                }
            }

            string stopLabel = IsRoleplayButtholeLike() &&
                               benchRoleplayCategory == RoleplayCategory.SexToy &&
                               benchRoleplayAnimation == 2 &&
                               roleplayPanel.CurrentState == SimulationState.Thrusting
                ? "Remove Egg"
                : "Stop";
            using (new EditorGUI.DisabledScope(
                       roleplayPanel.CurrentState == SimulationState.Transitioning))
            {
                if (GUILayout.Button(stopLabel))
                {
                    roleplayConfig.OnThrustStopped(roleplayPanel);
                    if (roleplayConfig.AreRoleplayCategoriesAvailable())
                    {
                        ClearRoleplayBenchSelection();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool canTriggerRoleplayOrgasm = CanTriggerRoleplayOrgasm();
            if (IsRoleplayPaizuriLike())
            {
                bool playerCumAvailable =
                    benchRoleplayCategory == RoleplayCategory.Boob;
                using (new EditorGUI.DisabledScope(
                           !canTriggerRoleplayOrgasm || !playerCumAvailable))
                {
                    if (GUILayout.Button("Player Cum 100%"))
                    {
                        roleplayConfig.OnPlayerCumReached(roleplayPanel);
                    }
                }

                bool maiCumAvailable =
                    benchRoleplayCategory != RoleplayCategory.Boob;
                using (new EditorGUI.DisabledScope(
                           !canTriggerRoleplayOrgasm || !maiCumAvailable))
                {
                    if (GUILayout.Button("Mai Cum 100%"))
                    {
                        roleplayConfig.OnMaiOrgasm(roleplayPanel);
                    }
                }
            }
            else
            {
                using (new EditorGUI.DisabledScope(!canTriggerRoleplayOrgasm))
                {
                    bool isRoleplayBlowjob = IsRoleplayBlowjobLike();
                    string cumButtonLabel = isRoleplayBlowjob
                        ? "Player Cum 100%"
                        : "Mai Cum 100%";
                    if (GUILayout.Button(cumButtonLabel))
                    {
                        if (isRoleplayBlowjob)
                        {
                            roleplayConfig.OnPlayerCumReached(roleplayPanel);
                        }
                        else
                        {
                            roleplayConfig.OnMaiOrgasm(roleplayPanel);
                        }
                    }
                }
            }

            if (GUILayout.Button("Reset to Start"))
            {
                ClearRoleplayBenchSelection();
                lastRoleplayAnimatorStateHash = 0;
                roleplayConfig.OnPositionEntered(roleplayPanel);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRoleplayCategoryButton(string label, RoleplayCategory category)
        {
            if (!DrawSelectedButton(
                    label,
                    benchRoleplayCategorySelected && benchRoleplayCategory == category))
            {
                return;
            }

            benchRoleplayCategory = category;
            benchRoleplayCategorySelected = true;
            benchRoleplayAnimation = 0;
            roleplayConfig.OnRoleplayCategorySelected(roleplayPanel, category);
        }

        private void DrawRoleplayAnimationButton(string label, int animationNumber)
        {
            if (!DrawSelectedButton(label, benchRoleplayAnimation == animationNumber))
            {
                return;
            }

            benchRoleplayAnimation = animationNumber;
            roleplayConfig.OnRoleplayAnimationSelected(roleplayPanel, animationNumber);
        }

        private static bool DrawSelectedButton(string label, bool selected)
        {
            Color previousColor = GUI.backgroundColor;
            if (selected)
            {
                GUI.backgroundColor = new Color(0.35f, 0.65f, 1f);
            }

            bool clicked = GUILayout.Button(label, GUILayout.MinHeight(22f));
            GUI.backgroundColor = previousColor;
            return clicked;
        }

        private string[] GetRoleplayAnimationLabels()
        {
            if (IsRoleplayPaizuriLike())
            {
                switch (benchRoleplayCategory)
                {
                    case RoleplayCategory.Boob:
                        return new[]
                        {
                            "1  Boob 1",
                            "2  Boob 2",
                            "3  Boob 3",
                            "4  Boob 4"
                        };
                    case RoleplayCategory.Hand:
                        return new[] { "1  Hand 1", "2  Hand 2", "3  Hand 3" };
                    case RoleplayCategory.Tongue:
                        return new[] { "1  Tongue 1", "2  Tongue 2" };
                    default:
                        return new string[0];
                }
            }

            if (IsRoleplayBlowjobLike())
            {
                switch (benchRoleplayCategory)
                {
                    case RoleplayCategory.Hand:
                        return new[] { "1  Handjob 1", "2  Handjob 2" };
                    case RoleplayCategory.Blowjob:
                        return new[]
                        {
                            "1  Blowjob 1",
                            "2  Blowjob 2",
                            "3  Blowjob 3"
                        };
                    case RoleplayCategory.Tongue:
                        return new[]
                        {
                            "1  Kiss Dick",
                            "2  Tongue Move 1",
                            "3  Tongue Move 2"
                        };
                    default:
                        return new string[0];
                }
            }

            if (IsRoleplayButtholeLike())
            {
                switch (benchRoleplayCategory)
                {
                    case RoleplayCategory.Hand:
                        return new[]
                        {
                            "1  Finger 1",
                            "2  Finger 2",
                            "3  Finger Massage",
                            "4  Hand Grab Both"
                        };
                    case RoleplayCategory.Tongue:
                        return new[] { "1  Lick", "2  Insert" };
                    case RoleplayCategory.SexToy:
                        return new[] { "1  Pen", "2  Egg", "3  Cucumber" };
                    default:
                        return new string[0];
                }
            }

            switch (benchRoleplayCategory)
            {
                case RoleplayCategory.Hand:
                    return new[] { "1  Clit Finger", "2  2 Finger", "3  Moc Cua" };
                case RoleplayCategory.Tongue:
                    return new[] { "1  Tongue Clit", "2  Tongue Lick", "3  Tongue Insert" };
                case RoleplayCategory.SexToy:
                    return new[] { "1  Hitachi", "2  Egg Vib", "3  Cucumber", "4  Dildo" };
                default:
                    return new string[0];
            }
        }

        private void GetRoleplayPlaybackAvailability(
            out bool slowAvailable,
            out bool fastAvailable)
        {
            slowAvailable = false;
            fastAvailable = false;
            if (benchRoleplayAnimation <= 0)
            {
                return;
            }

            if (IsRoleplayPaizuriLike())
            {
                slowAvailable = true;
                fastAvailable = true;
                return;
            }

            if (IsRoleplayBlowjobLike())
            {
                bool hasSpeedControls =
                    benchRoleplayCategory != RoleplayCategory.Tongue;
                slowAvailable = hasSpeedControls;
                fastAvailable = hasSpeedControls;
                return;
            }

            if (!IsRoleplayButtholeLike())
            {
                slowAvailable = benchRoleplayCategory != RoleplayCategory.SexToy ||
                                benchRoleplayAnimation >= 2;
                fastAvailable = benchRoleplayCategory != RoleplayCategory.SexToy ||
                                benchRoleplayAnimation >= 3;
                return;
            }

            bool isBlendAction =
                (benchRoleplayCategory == RoleplayCategory.Hand &&
                 benchRoleplayAnimation <= 2) ||
                benchRoleplayCategory == RoleplayCategory.Tongue ||
                (benchRoleplayCategory == RoleplayCategory.SexToy &&
                 benchRoleplayAnimation != 2);
            slowAvailable = isBlendAction;
            fastAvailable = isBlendAction;
        }

        private bool CanTriggerRoleplayOrgasm()
        {
            Animator animator = GetActiveAnimator();
            if (animator == null || roleplayPanel.CurrentState != SimulationState.Thrusting)
            {
                return false;
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (IsRoleplayPaizuriLike())
            {
                return IsAnimatorState(
                    stateInfo,
                    "Paizuri Slow",
                    "Paizuri Fast");
            }

            if (IsRoleplayBlowjobLike())
            {
                return IsAnimatorState(
                    stateInfo,
                    "Blowjob Roleplay - Slow",
                    "Blowjob Roleplay - Fast",
                    "Blowjob Roleplay - Kiss Dick",
                    "Blowjob Roleplay - Tongue Move 1",
                    "Blowjob Roleplay - Tongue Move 2");
            }

            if (IsRoleplayButtholeLike())
            {
                return IsAnimatorState(
                    stateInfo,
                    "Slow",
                    "Fast",
                    "Hand - Finger Massage",
                    "Hand - Hand Grab Both",
                    "Toy - Egg (Insert Loop)",
                    "Roleplay Butthole Slow",
                    "Roleplay Butthole Fast",
                    "Roleplay Butthole Hand - Finger Massage",
                    "Roleplay Butthole Hand - Hand Grab Both",
                    "Roleplay Butthole Toy - Egg (Insert Loop)");
            }

            return stateInfo.IsName("Roleplay Slow") ||
                   stateInfo.IsName("Roleplay Fast") ||
                   stateInfo.IsName("Roleplay Pussy Toy Hitachi") ||
                   stateInfo.IsName("Roleplay Pussy Toy Egg Vib - Work");
        }

        private static bool IsAnimatorState(
            AnimatorStateInfo stateInfo,
            params string[] stateNames)
        {
            return stateNames.Any(stateInfo.IsName);
        }

        private void DisableAllRoleplayLoops()
        {
            SafeBool("SlowLoop", false);
            SafeBool("FastLoop", false);
            SafeBool("EggVibWorkLoop", false);
        }

        // Guarded animator writes: only touch parameters the model actually declares,
        // so replaying a sequence on a model with a slightly different param set stays
        // quiet instead of logging "parameter not found" warnings.
        private void SafeTrigger(string param)
        {
            if (activeController != null && activeController.HasParameter(param))
            {
                activeController.SetTrigger(param);
            }
        }

        private void SafeBool(string param, bool value)
        {
            if (activeController != null && activeController.HasParameter(param))
            {
                activeController.SetBool(param, value);
            }
        }

        private void SafeFloat(string param, float value)
        {
            if (activeController != null && activeController.HasParameter(param))
            {
                activeController.SetFloat(param, value);
            }
        }

        private void DrawParameterControl(string param)
        {
            AnimatorControllerParameterType? nullableType = activeController.GetParameterType(param);
            if (!nullableType.HasValue)
            {
                EditorGUILayout.LabelField($"{param} (unknown)");
                return;
            }

            AnimatorControllerParameterType type = nullableType.Value;

            switch (type)
            {
                case AnimatorControllerParameterType.Trigger:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{param} (Trigger)");
                    if (GUILayout.Button("Fire", GUILayout.Width(90)))
                    {
                        activeController.SetTrigger(param);
                    }

                    EditorGUILayout.EndHorizontal();
                    break;

                case AnimatorControllerParameterType.Bool:
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{param} (Bool)");
                    if (GUILayout.Button("On", GUILayout.Width(44)))
                    {
                        activeController.SetBool(param, true);
                    }

                    if (GUILayout.Button("Off", GUILayout.Width(44)))
                    {
                        activeController.SetBool(param, false);
                    }

                    EditorGUILayout.EndHorizontal();
                    break;

                case AnimatorControllerParameterType.Float:
                    // Live-drive the float as the slider moves (blend trees / skill values).
                    float value = EditorGUILayout.Slider($"{param} (Float)", GetCachedFloat(param), 0f, 1f);
                    if (!Mathf.Approximately(value, GetCachedFloat(param)))
                    {
                        SetCachedFloat(param, value);
                        activeController.SetFloat(param, value);
                    }

                    break;

                default:
                    EditorGUILayout.LabelField($"{param} ({type}) — unsupported");
                    break;
            }
        }

        // Float slider values are transient UI state; cache per param for smooth dragging.
        private readonly Dictionary<string, float> floatValueCache = new Dictionary<string, float>();

        private float GetCachedFloat(string param)
        {
            return floatValueCache.TryGetValue(param, out float v) ? v : 0f;
        }

        private void SetCachedFloat(string param, float value)
        {
            floatValueCache[param] = value;
        }
    }
}
