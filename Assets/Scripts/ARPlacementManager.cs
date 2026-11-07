using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ARPlacementManager : MonoBehaviour
{
    public enum ARMode
    {
        ImageTracking,
        PlanePlacement
    }

    [Header("Mode")]
    [SerializeField] private ARMode startMode = ARMode.ImageTracking;

    [Header("AR References (auto-found if left empty)")]
    [SerializeField] private ARTrackedImageManager imageManager;
    [SerializeField] private ARCellInteraction cellInteraction;
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;
    [SerializeField] private ARSession arSession;
    [SerializeField] private ARCameraManager arCameraManager;

    [Header("Custom UI")]
    [SerializeField] private GameObject cellControlsPanel;

    private GameObject homeScreen;
    private GameObject quizScreen;
    private GameObject notesScreen;
    private GameObject arControls;

    private Text arStatusLabel;
    private string currentARStatus;
    private bool arExperienceActive;
    private ARMode mode;

    private Text quizCounterLabel;
    private Text quizQuestionLabel;
    private Text quizFeedbackLabel;

    private readonly Button[] quizAnswerButtons = new Button[4];
    private Button nextQuestionButton;

    private int currentQuestionIndex;
    private bool quizAnswered;

    private readonly Color answerButtonColor =
        new Color(0.10f, 0.25f, 0.21f, 1f);

    private readonly Color correctColor =
        new Color(0.20f, 0.60f, 0.32f, 1f);

    private readonly Color incorrectColor =
        new Color(0.70f, 0.22f, 0.22f, 1f);

    // =====================================================
    // QUIZ CONTENT
    // =====================================================

    private readonly string[] quizQuestions =
    {
        "Which organelle is known as the powerhouse of the cell?",
        "Which organelle contains most of the cell's genetic material?",
        "Which structure controls what enters and leaves the cell?"
    };

    private readonly string[][] quizAnswers =
    {
        new string[]
        {
            "Mitochondrion",
            "Nucleus",
            "Cell Membrane",
            "Vacuole"
        },

        new string[]
        {
            "Nucleus",
            "Ribosome",
            "Cell Wall",
            "Cytoplasm"
        },

        new string[]
        {
            "Cell Membrane",
            "Mitochondrion",
            "Nucleus",
            "Vacuole"
        }
    };

    private readonly int[] correctAnswers =
    {
        0,
        0,
        0
    };

    private readonly string[] quizExplanations =
    {
        "Mitochondria help produce energy that the cell can use.",
        "The nucleus stores most of the cell's DNA and helps control cell activity.",
        "The cell membrane regulates which materials can enter and leave the cell."
    };

    // =====================================================
    // STARTUP
    // =====================================================

    private void Awake()
    {
        if (raycastManager == null)
            raycastManager = GetComponent<ARRaycastManager>();

        if (planeManager == null)
            planeManager = GetComponent<ARPlaneManager>();

        if (arSession == null)
            arSession = FindFirstObjectByType<ARSession>();

        if (arCameraManager == null)
            arCameraManager = FindFirstObjectByType<ARCameraManager>();

        if (imageManager == null)
            imageManager = GetComponent<ARTrackedImageManager>();

        if (imageManager == null)
            imageManager = FindFirstObjectByType<ARTrackedImageManager>();

        if (cellInteraction == null)
            cellInteraction = GetComponent<ARCellInteraction>();

        if (cellInteraction == null)
            cellInteraction = FindFirstObjectByType<ARCellInteraction>();

        if (cellControlsPanel == null)
            cellControlsPanel = FindObjectByName("ControlsPanel");

        mode = startMode;

        SetARExperienceActive(false);
    }

    private void Start()
    {
        EnsureEventSystem();
        BuildInterface();
        ReturnHome();
    }

    private void Update()
    {
        if (!arExperienceActive)
            return;

        UpdateARStatus();
    }

    private GameObject FindObjectByName(string objectName)
    {
        Transform[] objects = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Transform item in objects)
        {
            if (item.name == objectName)
                return item.gameObject;
        }

        return null;
    }

    // =====================================================
    // EVENT SYSTEM
    // =====================================================

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystemObject =
            new GameObject("Mobile UI Event System");

        eventSystemObject.AddComponent<EventSystem>();

        eventSystemObject
            .AddComponent<InputSystemUIInputModule>()
            .AssignDefaultActions();
    }

    // =====================================================
    // BUILD UI
    // =====================================================

    private void BuildInterface()
    {
        GameObject canvasObject = new GameObject(
            "Mobile UI",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Canvas canvas = canvasObject.GetComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1080, 1920);

        scaler.matchWidthOrHeight = 0.5f;

        BuildHomeScreen(canvas.transform);
        BuildQuizScreen(canvas.transform);
        BuildNotesScreen(canvas.transform);
        BuildARControls(canvas.transform);
    }

    // =====================================================
    // HOME SCREEN
    // =====================================================

    private void BuildHomeScreen(Transform canvas)
    {
        homeScreen = CreatePanel(
            "Home Screen",
            canvas,
            new Color(0.035f, 0.12f, 0.105f, 1f)
        );

        AddLabel(
            homeScreen.transform,
            "AR CELL EXPLORER",
            34,
            FontStyle.Bold,
            new Color(0.62f, 0.94f, 0.72f, 1f),
            new Vector2(0.08f, 0.75f),
            new Vector2(0.92f, 0.82f),
            TextAnchor.MiddleCenter
        );

        AddLabel(
            homeScreen.transform,
            "Explore the\nworld of cells",
            82,
            FontStyle.Bold,
            Color.white,
            new Vector2(0.08f, 0.52f),
            new Vector2(0.92f, 0.74f),
            TextAnchor.MiddleCenter
        );

        AddLabel(
            homeScreen.transform,
            "Explore 3D cells using augmented reality or test your knowledge.",
            32,
            FontStyle.Normal,
            new Color(0.79f, 0.88f, 0.83f, 1f),
            new Vector2(0.12f, 0.40f),
            new Vector2(0.88f, 0.50f),
            TextAnchor.MiddleCenter
        );

        CreateButton(
            homeScreen.transform,
            "EXPLORE CELLS",
            new Vector2(0.12f, 0.25f),
            new Vector2(0.88f, 0.33f),
            new Color(0.58f, 0.91f, 0.65f, 1f),
            new Color(0.035f, 0.12f, 0.105f, 1f),
            EnterAR
        );

        CreateButton(
            homeScreen.transform,
            "QUIZ MODE",
            new Vector2(0.12f, 0.15f),
            new Vector2(0.88f, 0.23f),
            new Color(0.10f, 0.25f, 0.21f, 1f),
            Color.white,
            OpenQuiz
        );

        AddLabel(
            homeScreen.transform,
            "A camera and motion permission may be required for AR.",
            24,
            FontStyle.Normal,
            new Color(0.65f, 0.76f, 0.70f, 1f),
            new Vector2(0.08f, 0.06f),
            new Vector2(0.92f, 0.11f),
            TextAnchor.MiddleCenter
        );
    }

    // =====================================================
    // QUIZ SCREEN
    // =====================================================

    private void BuildQuizScreen(Transform canvas)
    {
        quizScreen = CreatePanel(
            "Quiz Screen",
            canvas,
            new Color(0.035f, 0.12f, 0.105f, 1f)
        );

        CreateButton(
            quizScreen.transform,
            "HOME",
            new Vector2(0.06f, 0.91f),
            new Vector2(0.30f, 0.97f),
            new Color(0.10f, 0.25f, 0.21f, 1f),
            Color.white,
            ReturnHome
        );

        AddLabel(
            quizScreen.transform,
            "CELL QUIZ",
            48,
            FontStyle.Bold,
            Color.white,
            new Vector2(0.30f, 0.90f),
            new Vector2(0.92f, 0.97f),
            TextAnchor.MiddleRight
        );

        quizCounterLabel = AddLabel(
            quizScreen.transform,
            "QUESTION 1 OF 3",
            26,
            FontStyle.Bold,
            new Color(0.62f, 0.94f, 0.72f, 1f),
            new Vector2(0.08f, 0.82f),
            new Vector2(0.92f, 0.87f),
            TextAnchor.MiddleLeft
        );

        GameObject questionCard = CreateAnchoredPanel(
            "Question Card",
            quizScreen.transform,
            new Color(0.07f, 0.20f, 0.17f, 1f),
            new Vector2(0.08f, 0.62f),
            new Vector2(0.92f, 0.81f)
        );

        quizQuestionLabel = AddLabel(
            questionCard.transform,
            "",
            36,
            FontStyle.Bold,
            Color.white,
            new Vector2(0.06f, 0.08f),
            new Vector2(0.94f, 0.92f),
            TextAnchor.MiddleCenter
        );

        for (int i = 0; i < 4; i++)
        {
            int answerIndex = i;

            float top = 0.58f - (i * 0.09f);
            float bottom = top - 0.07f;

            quizAnswerButtons[i] = CreateButton(
                quizScreen.transform,
                "",
                new Vector2(0.10f, bottom),
                new Vector2(0.90f, top),
                answerButtonColor,
                Color.white,
                () => SelectQuizAnswer(answerIndex)
            );
        }

        quizFeedbackLabel = AddLabel(
            quizScreen.transform,
            "Choose the best answer.",
            28,
            FontStyle.Normal,
            new Color(0.79f, 0.88f, 0.83f, 1f),
            new Vector2(0.10f, 0.14f),
            new Vector2(0.90f, 0.21f),
            TextAnchor.MiddleCenter
        );

        CreateButton(
            quizScreen.transform,
            "STUDY NOTES",
            new Vector2(0.10f, 0.05f),
            new Vector2(0.47f, 0.12f),
            new Color(0.10f, 0.25f, 0.21f, 1f),
            Color.white,
            OpenNotes
        );

        nextQuestionButton = CreateButton(
            quizScreen.transform,
            "NEXT QUESTION",
            new Vector2(0.53f, 0.05f),
            new Vector2(0.90f, 0.12f),
            new Color(0.58f, 0.91f, 0.65f, 1f),
            new Color(0.035f, 0.12f, 0.105f, 1f),
            NextQuestion
        );

        quizScreen.SetActive(false);
    }

    // =====================================================
    // STUDY NOTES SCREEN
    // =====================================================

    private void BuildNotesScreen(Transform canvas)
    {
        notesScreen = CreatePanel(
            "Study Notes Screen",
            canvas,
            new Color(0.035f, 0.12f, 0.105f, 1f)
        );

        AddLabel(
            notesScreen.transform,
            "STUDY NOTES",
            54,
            FontStyle.Bold,
            Color.white,
            new Vector2(0.08f, 0.87f),
            new Vector2(0.92f, 0.94f),
            TextAnchor.MiddleCenter
        );

        AddLabel(
            notesScreen.transform,
            "Quick review before your quiz",
            28,
            FontStyle.Normal,
            new Color(0.62f, 0.94f, 0.72f, 1f),
            new Vector2(0.08f, 0.81f),
            new Vector2(0.92f, 0.86f),
            TextAnchor.MiddleCenter
        );

        CreateStudyCard(
            notesScreen.transform,
            "NUCLEUS",
            "Stores most of the cell's genetic material (DNA) and helps control cell activities.",
            new Vector2(0.08f, 0.59f),
            new Vector2(0.92f, 0.77f)
        );

        CreateStudyCard(
            notesScreen.transform,
            "MITOCHONDRION",
            "Produces usable energy for the cell through cellular respiration.",
            new Vector2(0.08f, 0.36f),
            new Vector2(0.92f, 0.54f)
        );

        CreateStudyCard(
            notesScreen.transform,
            "CELL MEMBRANE",
            "Forms the cell's boundary and helps control which substances enter and leave.",
            new Vector2(0.08f, 0.13f),
            new Vector2(0.92f, 0.31f)
        );

        CreateButton(
            notesScreen.transform,
            "BACK TO QUIZ",
            new Vector2(0.08f, 0.04f),
            new Vector2(0.47f, 0.10f),
            new Color(0.58f, 0.91f, 0.65f, 1f),
            new Color(0.035f, 0.12f, 0.105f, 1f),
            ReturnToQuiz
        );

        CreateButton(
            notesScreen.transform,
            "HOME",
            new Vector2(0.53f, 0.04f),
            new Vector2(0.92f, 0.10f),
            new Color(0.10f, 0.25f, 0.21f, 1f),
            Color.white,
            ReturnHome
        );

        notesScreen.SetActive(false);
    }

    private void CreateStudyCard(
        Transform parent,
        string title,
        string body,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        GameObject card = CreateAnchoredPanel(
            title + " Card",
            parent,
            new Color(0.07f, 0.20f, 0.17f, 1f),
            anchorMin,
            anchorMax
        );

        AddLabel(
            card.transform,
            title,
            32,
            FontStyle.Bold,
            new Color(0.62f, 0.94f, 0.72f, 1f),
            new Vector2(0.06f, 0.58f),
            new Vector2(0.94f, 0.92f),
            TextAnchor.MiddleLeft
        );

        AddLabel(
            card.transform,
            body,
            26,
            FontStyle.Normal,
            Color.white,
            new Vector2(0.06f, 0.10f),
            new Vector2(0.94f, 0.58f),
            TextAnchor.MiddleLeft
        );
    }

    // =====================================================
    // AR SCREEN CONTROLS
    // =====================================================

    private void BuildARControls(Transform canvas)
    {
        arControls = new GameObject(
            "AR Controls",
            typeof(RectTransform)
        );

        arControls.transform.SetParent(canvas, false);

        RectTransform controlsRect =
            arControls.GetComponent<RectTransform>();

        controlsRect.anchorMin = Vector2.zero;
        controlsRect.anchorMax = Vector2.one;
        controlsRect.offsetMin = Vector2.zero;
        controlsRect.offsetMax = Vector2.zero;

        CreateButton(
            arControls.transform,
            "HOME",
            new Vector2(0.06f, 0.91f),
            new Vector2(0.34f, 0.97f),
            new Color(0.035f, 0.12f, 0.105f, 0.88f),
            Color.white,
            ReturnHome
        );

        CreateButton(
            arControls.transform,
            "MODE",
            new Vector2(0.36f, 0.91f),
            new Vector2(0.64f, 0.97f),
            new Color(0.035f, 0.12f, 0.105f, 0.88f),
            Color.white,
            ToggleMode
        );

        arStatusLabel = AddLabel(
            arControls.transform,
            "Starting AR...",
            30,
            FontStyle.Normal,
            Color.white,
            new Vector2(0.10f, 0.04f),
            new Vector2(0.90f, 0.10f),
            TextAnchor.MiddleCenter
        );

        arControls.SetActive(false);
    }

    // =====================================================
    // QUIZ LOGIC
    // =====================================================

    private void OpenQuiz()
    {
        SetARExperienceActive(false);

        if (homeScreen != null)
            homeScreen.SetActive(false);

        if (notesScreen != null)
            notesScreen.SetActive(false);

        if (quizScreen != null)
            quizScreen.SetActive(true);

        currentQuestionIndex = 0;

        LoadQuestion();
    }

    private void LoadQuestion()
    {
        quizAnswered = false;

        quizCounterLabel.text =
            "QUESTION " +
            (currentQuestionIndex + 1) +
            " OF " +
            quizQuestions.Length;

        quizQuestionLabel.text =
            quizQuestions[currentQuestionIndex];

        quizFeedbackLabel.text =
            "Choose the best answer.";

        quizFeedbackLabel.color =
            new Color(0.79f, 0.88f, 0.83f, 1f);

        for (int i = 0; i < quizAnswerButtons.Length; i++)
        {
            Button button = quizAnswerButtons[i];

            button.interactable = true;

            Image image = button.GetComponent<Image>();

            if (image != null)
                image.color = answerButtonColor;

            SetButtonText(
                button,
                quizAnswers[currentQuestionIndex][i]
            );
        }

        if (nextQuestionButton != null)
        {
            nextQuestionButton.interactable = false;

            if (currentQuestionIndex ==
                quizQuestions.Length - 1)
            {
                SetButtonText(
                    nextQuestionButton,
                    "RESTART QUIZ"
                );
            }
            else
            {
                SetButtonText(
                    nextQuestionButton,
                    "NEXT QUESTION"
                );
            }
        }
    }

    private void SelectQuizAnswer(int selectedIndex)
    {
        if (quizAnswered)
            return;

        quizAnswered = true;

        int correctIndex =
            correctAnswers[currentQuestionIndex];

        foreach (Button button in quizAnswerButtons)
        {
            button.interactable = false;
        }

        Image selectedImage =
            quizAnswerButtons[selectedIndex].GetComponent<Image>();

        Image correctImage =
            quizAnswerButtons[correctIndex].GetComponent<Image>();

        if (selectedIndex == correctIndex)
        {
            if (selectedImage != null)
                selectedImage.color = correctColor;

            quizFeedbackLabel.text =
                "Correct! " +
                quizExplanations[currentQuestionIndex];

            quizFeedbackLabel.color =
                new Color(0.62f, 0.94f, 0.72f, 1f);
        }
        else
        {
            if (selectedImage != null)
                selectedImage.color = incorrectColor;

            if (correctImage != null)
                correctImage.color = correctColor;

            quizFeedbackLabel.text =
                "Not quite. The correct answer is " +
                quizAnswers[currentQuestionIndex][correctIndex] +
                ". " +
                quizExplanations[currentQuestionIndex];

            quizFeedbackLabel.color =
                new Color(1f, 0.72f, 0.65f, 1f);
        }

        if (nextQuestionButton != null)
            nextQuestionButton.interactable = true;
    }

    private void NextQuestion()
    {
        if (!quizAnswered)
            return;

        if (currentQuestionIndex < quizQuestions.Length - 1)
            currentQuestionIndex++;
        else
            currentQuestionIndex = 0;

        LoadQuestion();
    }

    private void OpenNotes()
    {
        if (quizScreen != null)
            quizScreen.SetActive(false);

        if (notesScreen != null)
            notesScreen.SetActive(true);
    }

    private void ReturnToQuiz()
    {
        if (notesScreen != null)
            notesScreen.SetActive(false);

        if (quizScreen != null)
            quizScreen.SetActive(true);
    }

    // =====================================================
    // NAVIGATION
    // =====================================================

    private void EnterAR()
    {
        if (homeScreen != null)
            homeScreen.SetActive(false);

        if (quizScreen != null)
            quizScreen.SetActive(false);

        if (notesScreen != null)
            notesScreen.SetActive(false);

        SetARExperienceActive(true);
    }

    private void ReturnHome()
    {
        SetARExperienceActive(false);

        if (homeScreen != null)
            homeScreen.SetActive(true);

        if (quizScreen != null)
            quizScreen.SetActive(false);

        if (notesScreen != null)
            notesScreen.SetActive(false);
    }

    private void ToggleMode()
    {
        mode = mode == ARMode.ImageTracking
            ? ARMode.PlanePlacement
            : ARMode.ImageTracking;

        ApplyMode();

        currentARStatus = null;

        if (arExperienceActive)
            UpdateARStatus();
    }

    // =====================================================
    // AR MODE CONTROL
    // =====================================================

    private void ApplyMode()
    {
        bool images =
            arExperienceActive &&
            mode == ARMode.ImageTracking;

        bool planes =
            arExperienceActive &&
            mode == ARMode.PlanePlacement;

        if (imageManager != null)
        {
            imageManager.enabled = images;

            foreach (var trackedImage in imageManager.trackables)
            {
                if (trackedImage != null)
                    trackedImage.gameObject.SetActive(images);
            }
        }

        if (planeManager != null)
        {
            planeManager.enabled = planes;

            foreach (var plane in planeManager.trackables)
            {
                if (plane != null)
                    plane.gameObject.SetActive(planes);
            }
        }

        if (raycastManager != null)
            raycastManager.enabled = planes;

        if (cellInteraction != null)
        {
            cellInteraction.canPlace = planes;

            if (!planes)
            {
                cellInteraction.ClearSpawned();
            }
            else if (cellInteraction.isTrackedImage)
            {
                cellInteraction.ClearSpawned();
            }

            cellInteraction.enabled = true;
        }

        if (cellControlsPanel != null)
            cellControlsPanel.SetActive(planes);
    }

    private void SetARExperienceActive(bool active)
    {
        arExperienceActive = active;

        if (arSession != null)
            arSession.enabled = active;

        if (arCameraManager != null)
            arCameraManager.enabled = active;

        ApplyMode();

        if (arControls != null)
            arControls.SetActive(active);

        if (!active && cellControlsPanel != null)
            cellControlsPanel.SetActive(false);
    }

    // =====================================================
    // AR STATUS
    // =====================================================

    private void UpdateARStatus()
    {
        string status;

        if (arSession == null)
        {
            status = "AR Session is missing from the scene.";
        }
        else
        {
            switch (ARSession.state)
            {
                case ARSessionState.SessionTracking:

                    status = mode == ARMode.ImageTracking
                        ? "Point the camera at a cell QR code."
                        : "Move phone slowly to scan a surface.";

                    break;

                case ARSessionState.SessionInitializing:

                    status =
                        "Starting camera and tracking...";

                    break;

                case ARSessionState.NeedsInstall:
                case ARSessionState.Installing:

                    status =
                        "Install Google Play Services for AR.";

                    break;

                case ARSessionState.Unsupported:

                    status =
                        "ARCore is not supported on this device.";

                    break;

                default:

                    status =
                        "Checking AR availability...";

                    break;
            }
        }

        if (status != currentARStatus)
        {
            currentARStatus = status;

            if (arStatusLabel != null)
                arStatusLabel.text = status;
        }
    }

    // =====================================================
    // UI HELPERS
    // =====================================================

    private GameObject CreatePanel(
        string name,
        Transform parent,
        Color color)
    {
        GameObject panel = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Image)
        );

        panel.transform.SetParent(parent, false);

        RectTransform rect =
            panel.GetComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        panel.GetComponent<Image>().color = color;

        return panel;
    }

    private GameObject CreateAnchoredPanel(
        string name,
        Transform parent,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        GameObject panel = new GameObject(
            name,
            typeof(RectTransform),
            typeof(Image)
        );

        panel.transform.SetParent(parent, false);

        RectTransform rect =
            panel.GetComponent<RectTransform>();

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        panel.GetComponent<Image>().color = color;

        return panel;
    }

    private Text AddLabel(
        Transform parent,
        string text,
        int fontSize,
        FontStyle style,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        TextAnchor alignment)
    {
        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(Text)
        );

        labelObject.transform.SetParent(parent, false);

        RectTransform rect =
            labelObject.GetComponent<RectTransform>();

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text label =
            labelObject.GetComponent<Text>();

        label.text = text;

        label.font =
            Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );

        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = color;
        label.alignment = alignment;

        label.horizontalOverflow =
            HorizontalWrapMode.Wrap;

        label.verticalOverflow =
            VerticalWrapMode.Truncate;

        label.raycastTarget = false;

        return label;
    }

    private Button CreateButton(
        Transform parent,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color background,
        Color foreground,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(
            label + " Button",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button)
        );

        buttonObject.transform.SetParent(parent, false);

        RectTransform rect =
            buttonObject.GetComponent<RectTransform>();

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image =
            buttonObject.GetComponent<Image>();

        image.color = background;

        Button button =
            buttonObject.GetComponent<Button>();

        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        AddLabel(
            buttonObject.transform,
            label,
            32,
            FontStyle.Bold,
            foreground,
            Vector2.zero,
            Vector2.one,
            TextAnchor.MiddleCenter
        );

        return button;
    }

    private void SetButtonText(
        Button button,
        string text)
    {
        if (button == null)
            return;

        Text label =
            button.GetComponentInChildren<Text>();

        if (label != null)
            label.text = text;
    }
}