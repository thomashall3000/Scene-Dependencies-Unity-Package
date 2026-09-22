using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class SceneDependencyWindow : EditorWindow
{
    private SceneDependencyEntry selectedSceneEntry;

    private List<ValidationIssue> validationIssues = new List<ValidationIssue>();

    [MenuItem("Tools/Scene Dependencies")]
    public static void OpenWindow()
    {
        SceneDependencyWindow window = GetWindow<SceneDependencyWindow>();
        window.minSize = new Vector2(600, 500);
    }

    public void CreateGUI()
    {
        SceneDependencyDatabase database = SceneDependencyDatabaseCreation.GetDatabase();

        rootVisualElement.Clear();

        // -------------------------
        // SPLIT SCREEN
        // -------------------------

        TwoPaneSplitView splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
        splitView.style.flexGrow = 1;

        rootVisualElement.Add(splitView);

        VisualElement leftPane = new VisualElement();
        VisualElement rightPane = new VisualElement();

        leftPane.style.paddingLeft = 8;
        leftPane.style.paddingRight = 8;
        leftPane.style.paddingTop = 8;

        rightPane.style.paddingLeft = 12;
        rightPane.style.paddingRight = 12;
        rightPane.style.paddingTop = 8;
        rightPane.style.paddingBottom = 8;
        rightPane.style.minWidth = 350;

        splitView.Add(leftPane);
        splitView.Add(rightPane);

        // -------------------------
        // LEFT PANE - SCENE LIST
        // -------------------------

        Label scenesLabel = new Label("Scenes");
        scenesLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        scenesLabel.style.marginBottom = 6;

        leftPane.Add(scenesLabel);

        List<SceneDependencyEntry> sceneEntries = new List<SceneDependencyEntry>(database.SceneEntries);

        ListView sceneListView = new ListView();

        sceneListView.itemsSource = sceneEntries;
        sceneListView.selectionType = SelectionType.Single;
        sceneListView.style.flexGrow = 1;

        sceneListView.makeItem = () =>
        {
            Label row = new Label();

            Vector2 startPosition = Vector2.zero;
            bool pointerDown = false;
            bool dragging = false;

            row.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0)
                {
                    return;
                }

                startPosition = evt.position;
                pointerDown = true;
                dragging = false;

                evt.StopPropagation();
            });

            row.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!pointerDown || dragging)
                {
                    return;
                }

                if (Vector2.Distance(startPosition, evt.position) < 5f)
                {
                    return;
                }

                SceneDependencyEntry entry = row.userData as SceneDependencyEntry;

                if (entry == null)
                {
                    return;
                }

                if (Event.current == null || Event.current.type != EventType.MouseDrag)
                {
                    return;
                }

                dragging = true;

                DragAndDrop.PrepareStartDrag();
                DragAndDrop.SetGenericData("SceneDependencyGuid", entry.Guid);
                DragAndDrop.StartDrag("Scene Dependency");

                evt.StopPropagation();
            });

            row.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!pointerDown)
                {
                    return;
                }

                pointerDown = false;

                if (!dragging)
                {
                    SceneDependencyEntry entry = row.userData as SceneDependencyEntry;
                    int index = sceneEntries.IndexOf(entry);

                    sceneListView.SetSelection(index);
                }

                dragging = false;

                evt.StopPropagation();
            });

            return row;
        };

        sceneListView.bindItem = (element, index) =>
        {
            SceneDependencyEntry entry = sceneEntries[index];

            string path = AssetDatabase.GUIDToAssetPath(entry.Guid);
            string sceneName = Path.GetFileNameWithoutExtension(path);

            Label row = (Label)element;

            row.text = sceneName;
            row.userData = entry;
        };

        leftPane.Add(sceneListView);

        // -------------------------
        // RIGHT PANE - SCENE TITLE
        // -------------------------

        Label selectedSceneLabel = new Label("Select a Scene");

        selectedSceneLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        selectedSceneLabel.style.fontSize = 16;
        selectedSceneLabel.style.marginBottom = 8;

        rightPane.Add(selectedSceneLabel);

        rightPane.Add(CreateSeparator());

        // -------------------------
        // DEPENDENCY SECTION
        // -------------------------

        Label dependenciesLabel = new Label("Dependencies");

        dependenciesLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        dependenciesLabel.style.marginTop = 10;
        dependenciesLabel.style.marginBottom = 6;

        rightPane.Add(dependenciesLabel);

        // Drop zone
        VisualElement dropZone = new VisualElement();

        dropZone.style.height = 70;
        dropZone.style.marginBottom = 10;

        dropZone.style.borderLeftWidth = 1;
        dropZone.style.borderRightWidth = 1;
        dropZone.style.borderTopWidth = 1;
        dropZone.style.borderBottomWidth = 1;

        dropZone.style.borderLeftColor = Color.gray;
        dropZone.style.borderRightColor = Color.gray;
        dropZone.style.borderTopColor = Color.gray;
        dropZone.style.borderBottomColor = Color.gray;

        dropZone.style.justifyContent = Justify.Center;
        dropZone.style.alignItems = Align.Center;

        Label dropLabel = new Label("Drop dependencies here");
        dropZone.Add(dropLabel);

        rightPane.Add(dropZone);

        // Dependency cards
        ScrollView dependencyCardsContainer = new ScrollView(ScrollViewMode.Vertical);

        dependencyCardsContainer.style.flexGrow = 1;
        dependencyCardsContainer.style.minHeight = 100;

        rightPane.Add(dependencyCardsContainer);

        // -------------------------
        // SCENE INFORMATION
        // -------------------------

        rightPane.Add(CreateSeparator());

        Label pathLabel = new Label("Path:");
        pathLabel.style.marginTop = 8;

        Label guidLabel = new Label("GUID:");

        rightPane.Add(pathLabel);
        rightPane.Add(guidLabel);

        // -------------------------
        // DIAGNOSTICS
        // -------------------------

        rightPane.Add(CreateSeparator());

        VisualElement diagnosticsHeader = new VisualElement();

        diagnosticsHeader.style.flexDirection = FlexDirection.Row;
        diagnosticsHeader.style.alignItems = Align.Center;

        Label diagnosticsLabel = new Label("Diagnostics");

        diagnosticsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        diagnosticsLabel.style.marginTop = 6;
        diagnosticsLabel.style.marginBottom = 6;
        diagnosticsLabel.style.flexGrow = 1;

        Button showAndHideDiagnostics = new Button();
        bool showing = true;
        showAndHideDiagnostics.text = "Hide";

        Button clearButton = new Button();

        clearButton.text = "Clear";

        diagnosticsHeader.Add(diagnosticsLabel);
        diagnosticsHeader.Add(showAndHideDiagnostics);
        diagnosticsHeader.Add(clearButton);

        rightPane.Add(diagnosticsHeader);

        ScrollView diagnosticsContainer = new ScrollView(ScrollViewMode.Vertical);

        diagnosticsContainer.style.height = 180;
        diagnosticsContainer.style.flexShrink = 0;

        rightPane.Add(diagnosticsContainer);

        showAndHideDiagnostics.clicked += () =>
        {
            if (showing)
            {
                diagnosticsContainer.style.display = DisplayStyle.None;
                showAndHideDiagnostics.text = "Show";
                showing = false;
            }
            else
            {
                diagnosticsContainer.style.display = DisplayStyle.Flex;
                showAndHideDiagnostics.text = "Hide";
                showing = true;
            }
        };

        clearButton.clicked += () =>
        {
            validationIssues.Clear();

            if (selectedSceneEntry != null)
            {
                RefreshDiagnostics(diagnosticsContainer, database, selectedSceneEntry);
            }
        };

        // -------------------------
        // SCENE SELECTION
        // -------------------------

        sceneListView.selectionChanged += selectedItems =>
        {
            foreach (SceneDependencyEntry selectedEntry in selectedItems)
            {
                selectedSceneEntry = selectedEntry;

                string path = AssetDatabase.GUIDToAssetPath(selectedEntry.Guid);
                string sceneName = Path.GetFileNameWithoutExtension(path);

                selectedSceneLabel.text = sceneName;
                pathLabel.text = $"Path: {path}";
                guidLabel.text = $"GUID: {selectedEntry.Guid}";

                RefreshDependencyCards(dependencyCardsContainer, diagnosticsContainer, database, selectedEntry);
                RefreshDiagnostics(diagnosticsContainer, database, selectedEntry);
            }
        };
        
        // -------------------------
        // DEPENDENCY DROP TARGET
        // -------------------------

        dropZone.RegisterCallback<DragUpdatedEvent>(evt =>
        {
            string draggedGuid = DragAndDrop.GetGenericData("SceneDependencyGuid") as string;

            if (string.IsNullOrEmpty(draggedGuid) || selectedSceneEntry == null)
            {
                dropLabel.text = "Drop dependencies here";
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                return;
            }

            var result = SceneDependencyValidator.CanAddDependency(database, selectedSceneEntry.Guid, draggedGuid);

            DragAndDrop.visualMode = DragAndDropVisualMode.Link;

            if (result.issue != null)
            {
                dropLabel.text = result.issue.Message;
            }
            else
            {
                dropLabel.text = "Drop dependencies here";
            }
        });

        dropZone.RegisterCallback<DragLeaveEvent>(evt =>
        {
            dropLabel.text = "Drop dependencies here";
        });

        dropZone.RegisterCallback<DragPerformEvent>(evt =>
        {
            string draggedGuid = DragAndDrop.GetGenericData("SceneDependencyGuid") as string;

            if (string.IsNullOrEmpty(draggedGuid) || selectedSceneEntry == null)
            {
                return;
            }

            var result = SceneDependencyValidator.CanAddDependency(database, selectedSceneEntry.Guid, draggedGuid);
            dropLabel.text = "Drop dependencies here";

            if (!result.isValid)
            {
                validationIssues.Add(result.issue);
                DragAndDrop.AcceptDrag();
                RefreshDiagnostics(diagnosticsContainer, database, selectedSceneEntry);
                return;
            }

            bool added = SceneDependencyService.TryAddDependency(database, selectedSceneEntry.Guid, draggedGuid);

            if (!added)
            {
                return;
            }

            DragAndDrop.AcceptDrag();

            selectedSceneEntry = database.EntryFinder(selectedSceneEntry.Guid);

            RefreshDependencyCards(dependencyCardsContainer, diagnosticsContainer, database, selectedSceneEntry);
            RefreshDiagnostics(diagnosticsContainer, database, selectedSceneEntry);
        });
    }

    private void RefreshDependencyCards(VisualElement dependencyCardsContainer, VisualElement diagnosticsContainer, SceneDependencyDatabase database, SceneDependencyEntry entry)
    {
        dependencyCardsContainer.Clear();

        foreach (string dependencyGuid in entry.DependencyGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(dependencyGuid);
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

            string sceneName;

            if (sceneAsset == null)
            {
                sceneName = "Missing Scene";
            }
            else
            {
                sceneName = Path.GetFileNameWithoutExtension(path);
            }

            VisualElement dependencyCard = new VisualElement();

            dependencyCard.style.flexDirection = FlexDirection.Row;
            dependencyCard.style.alignItems = Align.Center;

            dependencyCard.style.paddingLeft = 8;
            dependencyCard.style.paddingRight = 8;
            dependencyCard.style.paddingTop = 5;
            dependencyCard.style.paddingBottom = 5;

            dependencyCard.style.marginBottom = 5;

            dependencyCard.style.borderLeftWidth = 1;
            dependencyCard.style.borderRightWidth = 1;
            dependencyCard.style.borderTopWidth = 1;
            dependencyCard.style.borderBottomWidth = 1;

            dependencyCard.style.borderLeftColor = Color.gray;
            dependencyCard.style.borderRightColor = Color.gray;
            dependencyCard.style.borderTopColor = Color.gray;
            dependencyCard.style.borderBottomColor = Color.gray;

            Label dependencyLabel = new Label(sceneName);

            dependencyLabel.style.flexGrow = 1;
            dependencyLabel.style.flexShrink = 1;

            Button removeButton = new Button();

            removeButton.text = "Remove";
            removeButton.style.width = 70;
            removeButton.style.minWidth = 70;
            removeButton.style.flexShrink = 0;

            removeButton.clicked += () =>
            {
                bool removed = SceneDependencyService.RemoveDependency(database, entry.Guid, dependencyGuid);

                if (!removed)
                {
                    return;
                }

                SceneDependencyEntry refreshedEntry = database.EntryFinder(entry.Guid);

                RefreshDependencyCards(dependencyCardsContainer, diagnosticsContainer, database, refreshedEntry);
                RefreshDiagnostics(diagnosticsContainer, database, refreshedEntry);
            };

            dependencyCard.Add(dependencyLabel);
            dependencyCard.Add(removeButton);

            dependencyCardsContainer.Add(dependencyCard);
        }
    }

    private void RefreshDiagnostics(VisualElement diagnosticsContainer, SceneDependencyDatabase database, SceneDependencyEntry entry)
    {
        diagnosticsContainer.Clear();

        List<ValidationIssue> issues = SceneDependencyValidationService.ValidateScene(database, entry.Guid);

        issues.AddRange(validationIssues);

        if (issues.Count == 0)
        {
            Label noIssuesLabel = new Label("No issues found.");

            diagnosticsContainer.Add(noIssuesLabel);

            return;
        }

        foreach (ValidationIssue issue in issues)
        {
            VisualElement diagnosticBox = new VisualElement();

            diagnosticBox.style.paddingLeft = 8;
            diagnosticBox.style.paddingRight = 8;
            diagnosticBox.style.paddingTop = 6;
            diagnosticBox.style.paddingBottom = 6;

            diagnosticBox.style.marginBottom = 5;

            diagnosticBox.style.borderLeftWidth = 1;
            diagnosticBox.style.borderRightWidth = 1;
            diagnosticBox.style.borderTopWidth = 1;
            diagnosticBox.style.borderBottomWidth = 1;

            diagnosticBox.style.borderLeftColor = Color.gray;
            diagnosticBox.style.borderRightColor = Color.gray;
            diagnosticBox.style.borderTopColor = Color.gray;
            diagnosticBox.style.borderBottomColor = Color.gray;

            Label headerLabel = new Label($"[{issue.Severity}] {issue.Code}");

            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            Label messageLabel = new Label(issue.Message);

            messageLabel.style.whiteSpace = WhiteSpace.Normal;
            messageLabel.style.marginTop = 3;

            diagnosticBox.Add(headerLabel);
            diagnosticBox.Add(messageLabel);

            diagnosticsContainer.Add(diagnosticBox);
        }
    }

    private static VisualElement CreateSeparator()
    {
        VisualElement separator = new VisualElement();

        separator.style.height = 1;
        separator.style.backgroundColor = Color.gray;
        separator.style.marginTop = 6;
        separator.style.marginBottom = 6;

        return separator;
    }
}