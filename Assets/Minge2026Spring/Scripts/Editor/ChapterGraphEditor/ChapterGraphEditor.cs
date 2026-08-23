using System.Collections.Generic;
using System.IO;
using System.Linq;
using FMODUnity;
using Minge2026Spring.Scripts.Application.DTOs;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Minge2026Spring.Scripts.Editor.ChapterGraphEditor
{
    public class ChapterGraphEditor : EditorWindow
    {
        private ChapterGraphView graphView;
        private IMGUIContainer inspectorContainer;
        private ChapterNode selectedNode;
        private EventReferencePicker voiceEventPicker;

        private sealed class EventReferencePicker : ScriptableObject
        {
            public EventReference EventReference;
        }

        [MenuItem("Tools/Chapter Graph Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<ChapterGraphEditor>("ChapterGraph");
            window.minSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
            voiceEventPicker = CreateInstance<EventReferencePicker>();
            voiceEventPicker.hideFlags = HideFlags.HideAndDontSave;
            ConstructGraphView();
            ConstructSplitView();
            GenerateToolbar();

            // スタイルシートを適用
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:StyleSheet ChapterGraphStyle");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                rootVisualElement.styleSheets.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(path));
            }
        }

        private void OnDisable()
        {
            if (voiceEventPicker is not null)
                DestroyImmediate(voiceEventPicker);
        }

        private void ConstructGraphView()
        {
            graphView = new ChapterGraphView(this)
            {
                name = "Chapter Graph"
            };
            
            graphView.StretchToParentSize();
        }

        private void ConstructSplitView()
        {
            // 画面を左右に分割
            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(splitView);
            
            // 左側のグラフビュー
            var leftPane = new VisualElement();
            leftPane.Add(graphView);
            splitView.Add(leftPane);
            
            // 右側のインスペクタ
            var rightPane = new ScrollView(ScrollViewMode.VerticalAndHorizontal)
            {
                name = "inspector-pane"
            };
            
            var inspectorTitle = new Label("Node Inspector")
            {
                name = "inspector-title"
            };
            rightPane.Add(inspectorTitle);
            
            inspectorContainer = new IMGUIContainer(DrawInspector);
            rightPane.Add(inspectorContainer);
            splitView.Add(rightPane);
        }

        private void GenerateToolbar()
        {
            var toolbar = new Toolbar();

            var titleLabel = new Label("Chapter Graph Editor")
            {
                name = "toolbar-title"
            };
            toolbar.Add(titleLabel);

            var btnAddDialogue = new Button(() => graphView.CreateNewNode("NewDialogue", ChapterNodeType.Dialogue))
            {
                text = "Add Dialogue"
            };

            var btnAddChoice = new Button(() => graphView.CreateNewNode("NewChoice", ChapterNodeType.Choice))
            {
                text = "Add Choice"
            };

            var btnAddLLM = new Button(() => graphView.CreateNewNode("NewLLM", ChapterNodeType.LLM))
            {
                text = "Add LLM"
            };

            var btnSave = new Button(SaveData)
            {
                text = "Save JSON"
            };

            var btnLoad = new Button(LoadData)
            {
                text = "Load JSON"
            };
            
            toolbar.Add(btnAddDialogue);
            toolbar.Add(btnAddChoice);
            toolbar.Add(btnAddLLM);
            toolbar.Add(new ToolbarSpacer()
            {
                flex = true
            });
            toolbar.Add(btnLoad);
            toolbar.Add(btnSave);
            
            rootVisualElement.Add(toolbar);
            toolbar.PlaceInFront(rootVisualElement.ElementAt(0));
        }

        public void OnNodeSelected(ChapterNode node)
        {
            selectedNode = node;
            inspectorContainer.MarkDirtyRepaint();
        }

        private void DrawInspector()
        {
            if (selectedNode is null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("ノードを選択してください", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                return;
            }

            var block = selectedNode.BlockData;
            
            // Header Section
            GUILayout.BeginVertical("helpBox");
            GUILayout.Label("Basic Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            block.blockId = EditorGUILayout.TextField("Block ID", block.blockId);
            selectedNode.UpdateTitle();
            block.nodeType = (ChapterNodeType)EditorGUILayout.EnumPopup("Node Type", block.nodeType);
            block.waitingTime = EditorGUILayout.FloatField("Waiting Time(s)", block.waitingTime);
            GUILayout.EndVertical();
            
            GUILayout.Space(15);
            
            if (block.nodeType == ChapterNodeType.Dialogue || block.nodeType == ChapterNodeType.LLM)
            {
                // Dialoguesの編集
                GUILayout.Label("Dialogue Sequence", EditorStyles.boldLabel);
                if (block.dialogues is null)
                    block.dialogues = new Dialogue[0];

                var dialogueList = block.dialogues.ToList();
                for (int i = 0; i < dialogueList.Count; i++)
                {
                    GUILayout.BeginVertical("helpBox");
                    var dialogue = dialogueList[i];
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Entry #{i + 1}", EditorStyles.miniBoldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("✕", GUILayout.Width(20)))
                    {
                        dialogueList.RemoveAt(i);
                        i--;
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                        continue;
                    }
                    GUILayout.EndHorizontal();

                    dialogue.speakerKey = EditorGUILayout.TextField("Speaker Key", dialogue.speakerKey);
                    dialogue.iconId = EditorGUILayout.TextField("Icon ID", dialogue.iconId);

                    voiceEventPicker.EventReference = string.IsNullOrWhiteSpace(dialogue.voiceEventPath)
                        ? default
                        : RuntimeManager.PathToEventReference(dialogue.voiceEventPath);
                    var pickerObject = new SerializedObject(voiceEventPicker);
                    pickerObject.Update();
                    var pickerProperty = pickerObject.FindProperty(nameof(EventReferencePicker.EventReference));
                    EditorGUILayout.PropertyField(pickerProperty, new GUIContent("Voice Event"));
                    pickerObject.ApplyModifiedProperties();
                    dialogue.voiceEventPath = voiceEventPicker.EventReference.IsNull
                        ? string.Empty
                        : voiceEventPicker.EventReference.Path;
                    
                    dialogue.messageKey = EditorGUILayout.TextField("Message Key", dialogue.messageKey);
                    dialogue.waitingTime = EditorGUILayout.FloatField("Wait After(s)", dialogue.waitingTime);
                    
                    dialogueList[i] = dialogue;
                    GUILayout.EndVertical();
                    GUILayout.Space(5);
                }
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+ Add Dialogue Entry", GUILayout.Height(25)))
                    dialogueList.Add(new Dialogue());
                
                if (dialogueList.Count > 0 && GUILayout.Button("Clear All", GUILayout.Width(70), GUILayout.Height(25)))
                    dialogueList.Clear();
                GUILayout.EndHorizontal();
                
                block.dialogues = dialogueList.ToArray();
            }

            if (block.nodeType == ChapterNodeType.Choice)
            {
                // Choicesの編集
                GUILayout.Label("Branching Choices", EditorStyles.boldLabel);
                if (block.choices is null)
                    block.choices = new Choice[0];

                var choiceList = block.choices.ToList();
                bool choicesChanged = false;
                for (int i = 0; i < choiceList.Count; i++)
                {
                    GUILayout.BeginVertical("helpBox");
                    var choice = choiceList[i];

                    GUILayout.BeginHorizontal();
                    choice.choiceTextKey = EditorGUILayout.TextField($"Choice {i+1} Key", choice.choiceTextKey);

                    if (GUILayout.Button("✕", GUILayout.Width(25)))
                    {
                        choiceList.RemoveAt(i);
                        choicesChanged = true;
                        i--;
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                        continue;
                    }
                    GUILayout.EndHorizontal();

                    choice = DrawChoiceMoraleFields(choice);
                    choiceList[i] = choice;
                    GUILayout.EndVertical();
                }

                if (GUILayout.Button("+ Add Choice Option", GUILayout.Height(25)))
                {
                    choiceList.Add(new Choice());
                    choicesChanged = true;
                }

                block.choices = choiceList.ToArray();

                if (choicesChanged)
                {
                    selectedNode.RefreshPorts();
                }
            }

            if (block.nodeType == ChapterNodeType.LLM)
            {
                GUILayout.Space(10);
                // FreeChatの編集
                GUILayout.Label("AI Conversation (FreeChat)", EditorStyles.boldLabel);
                if (block.freeChats is null)
                    block.freeChats = new FreeChat[0];

                var freeChatList = block.freeChats.ToList();
                for (int i = 0; i < freeChatList.Count; i++)
                {
                    GUILayout.BeginVertical("helpBox");
                    var freeChat = freeChatList[i];
                    
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Config #{i + 1}", EditorStyles.miniBoldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("✕", GUILayout.Width(20)))
                    {
                        freeChatList.RemoveAt(i);
                        i--;
                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                        continue;
                    }
                    GUILayout.EndHorizontal();

                    freeChat.waitingTime = EditorGUILayout.FloatField("Prep Time(s)", freeChat.waitingTime);
                    freeChat.recordDuration = EditorGUILayout.FloatField("Record Limit(s)", freeChat.recordDuration);
                    freeChatList[i] = freeChat;
                    GUILayout.EndVertical();
                }

                if (GUILayout.Button("+ Add FreeChat Config", GUILayout.Height(25)))
                    freeChatList.Add(new FreeChat { waitingTime = 1.0f, recordDuration = 5.0f });

                block.freeChats = freeChatList.ToArray();
            }

            if (EditorGUI.EndChangeCheck())
            {
                selectedNode.RefreshPorts();
                EditorUtility.SetDirty(this);
            }
        }

        private static Choice DrawChoiceMoraleFields(Choice choice)
        {
            GUILayout.Space(4);
            GUILayout.Label("Morale Delta", EditorStyles.miniBoldLabel);

            choice.characterAMoraleDelta = EditorGUILayout.IntField("Got", choice.characterAMoraleDelta);
            choice.characterBMoraleDelta = EditorGUILayout.IntField("Ryuta", choice.characterBMoraleDelta);
            choice.characterCMoraleDelta = EditorGUILayout.IntField("Milu", choice.characterCMoraleDelta);
            choice.characterDMoraleDelta = EditorGUILayout.IntField("Kashiwa", choice.characterDMoraleDelta);

            return choice;
        }

        /// <summary>
        /// Jsonの保存と読み込み
        /// </summary>
        private void SaveData()
        {
            var path = EditorUtility.SaveFilePanel("Save Chapter Json", "Assets", "ChapterData", "json");
            if (string.IsNullOrEmpty(path))
                return;

            Chapter chapter = new Chapter();
            
            var nodes = 
                graphView.nodes
                    .ToList()
                    .Cast<ChapterNode>()
                    .ToList();
            
            var blocks = new List<ChapterBlock>();

            foreach (var node in nodes)
            {
                var block = node.BlockData;
                
                // ポジションの保存
                block.nodePosX = node.GetPosition().x;
                block.nodePosY = node.GetPosition().y;
                
                // 接続されているエッジから遷移先のIDを自動設定する
                if (block.nodeType != ChapterNodeType.Choice)
                {
                    block.choices = new Choice[0];
                    if (node.DefaultOutputPort is not null)
                    {
                        var edge = node.DefaultOutputPort.connections.FirstOrDefault();
                        block.nextBlockId = 
                            edge is not null ? ((ChapterNode)edge.input.node).BlockData.blockId : "";
                    }
                }
                else
                {
                    block.nextBlockId = "";
                    if (block.choices is not null)
                    {
                        for (int i = 0; i < block.choices.Length; i++)
                        {
                            if (i < node.ChoicePorts.Count)
                            {
                                var edge = node.ChoicePorts[i].connections.FirstOrDefault();
                                block.choices[i].nextBlockId =
                                    edge is not null ? ((ChapterNode)edge.input.node).BlockData.blockId : "";
                            }
                        }
                    }
                }
                
                blocks.Add(block);
            }

            chapter.blocks = blocks.ToArray();
            string json = JsonUtility.ToJson(chapter, true);
            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            Debug.Log("Json Saved: " + path);
        }

        private void LoadData()
        {
            var path = EditorUtility.OpenFilePanel("Load Chapter Json", "Assets", "json");
            if (string.IsNullOrEmpty(path))
                return;

            string json = File.ReadAllText(path);
            Chapter chapter = JsonUtility.FromJson<Chapter>(json);
            
            graphView.ClearGraph();
            if (chapter.blocks is null)
                return;

            // ノードを生成
            var nodeDict = new Dictionary<string, ChapterNode>();
            foreach (var block in chapter.blocks)
            {
                var node = graphView.CreateNode(block);
                node.SetPosition(new Rect(block.nodePosX, block.nodePosY, 200, 150));
                nodeDict[block.blockId] = node;
            }

            foreach (var block in chapter.blocks)
            {
                if (!nodeDict.TryGetValue(block.blockId, out var parentNode))
                    continue;

                if (block.nodeType != ChapterNodeType.Choice)
                {
                    if (!string.IsNullOrEmpty(block.nextBlockId) &&
                        nodeDict.TryGetValue(block.nextBlockId, out var childNode))
                    {
                        if (parentNode.DefaultOutputPort is not null)
                        {
                            var edge = parentNode.DefaultOutputPort.ConnectTo(childNode.InputPort);
                            graphView.AddElement(edge);
                        }
                    }
                }
                else
                {
                    if (block.choices is not null)
                    {
                        for (int i = 0; i < block.choices.Length; i++)
                        {
                            var targetId = block.choices[i].nextBlockId;
                            if (!string.IsNullOrEmpty(targetId) && nodeDict.TryGetValue(targetId, out var childNode))
                            {
                                var edge = parentNode.ChoicePorts[i].ConnectTo(childNode.InputPort);
                                graphView.AddElement(edge);
                            }
                        }
                    }
                }
            }
        }
    }
}
