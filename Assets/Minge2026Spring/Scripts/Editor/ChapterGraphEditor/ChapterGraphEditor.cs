using System.Collections.Generic;
using System.IO;
using System.Linq;
using Minge2026Spring.Scripts.Application.DTOs;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Minge2026Spring.Scripts.Editor
{
    public class ChapterGraphEditor : EditorWindow
    {
        private ChapterGraphView graphView;
        private IMGUIContainer inspectorContainer;
        private ChapterNode selectedNode;

        [MenuItem("Tools/Chapter Graph Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<ChapterGraphEditor>("ChapterGraph");
            window.minSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
            ConstructGraphView();
            ConstructSplitView();
            GenerateToolbar();
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
            var rightPane = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            inspectorContainer = new IMGUIContainer(DrawInspector);
            rightPane.Add(inspectorContainer);
            splitView.Add(rightPane);
        }

        private void GenerateToolbar()
        {
            var toolbar = new Toolbar();

            var btnAddNode = new Button(() => graphView.CreateNewNode("NewBlock"))
            {
                text = "Add Node"
            };

            var btnSave = new Button(SaveData)
            {
                text = "Save JSON"
            };

            var btnLoad = new Button(LoadData)
            {
                text = "Load JSON"
            };
            
            toolbar.Add(btnAddNode);
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
                GUILayout.Label("ノードを選択してください", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            var block = selectedNode.BlockData;
            
            GUILayout.Label("Block Settings", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            block.blockId = EditorGUILayout.TextField("Block ID", block.blockId);
            selectedNode.title = block.blockId;
            block.waitingTime = EditorGUILayout.FloatField("Waiting Time(s)", block.waitingTime);
            
            GUILayout.Space(10);
            
            // Dialoguesの編集
            GUILayout.Label("Dialogues", EditorStyles.boldLabel);
            if (block.dialogues is null)
                block.dialogues = new Dialogue[0];

            var dialogueList = block.dialogues.ToList();
            for (int i = 0; i < dialogueList.Count; i++)
            {
                GUILayout.BeginVertical("box");
                var dialogue = dialogueList[i];
                dialogue.speaker = EditorGUILayout.TextField("Speaker", dialogue.speaker);
                dialogue.iconId = EditorGUILayout.TextField("Icon ID", dialogue.iconId);
                
                GUILayout.Label("Message:");
                dialogue.message = EditorGUILayout.TextArea(dialogue.message, GUILayout.Height(40));
                dialogue.waitingTime = EditorGUILayout.FloatField("Waiting Time(s)", dialogue.waitingTime);
                
                dialogueList[i] = dialogue;

                if (GUILayout.Button("Remove Dialogue", GUILayout.Width(120)))
                {
                    dialogueList.RemoveAt(i);
                    i--;
                }
                
                GUILayout.EndVertical();
            }
            
            if (GUILayout.Button("Add Dialogue"))
                dialogueList.Add(new Dialogue());
            
            block.dialogues = dialogueList.ToArray();
            
            GUILayout.Space(10);
            
            // Choicesの編集
            GUILayout.Label("Choices", EditorStyles.boldLabel);
            if (block.choices is null)
                block.choices = new Choice[0];

            var choiceList = block.choices.ToList();
            bool choicesChanged = false;
            for (int i = 0; i < choiceList.Count; i++)
            {
                GUILayout.BeginHorizontal();
                var choice = choiceList[i];
                choice.choiceText = EditorGUILayout.TextField(choice.choiceText);
                choiceList[i] = choice;

                if (GUILayout.Button("X", GUILayout.Width(30)))
                {
                    choiceList.RemoveAt(i);
                    choicesChanged = true;
                    i--;
                }
                
                GUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add Choice"))
            {
                choiceList.Add(new Choice
                {
                    choiceText = "New Choice"
                });
                choicesChanged = true;
            }

            block.choices = choiceList.ToArray();

            if (choicesChanged)
            {
                selectedNode.RefreshPorts();
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(this);
            }
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
            nodes = nodes.OrderBy(node => node.InputPort.connections.Any() ? 1 : 0).ToList();
            
            var blocks = new List<ChapterBlock>();

            foreach (var node in nodes)
            {
                var block = node.BlockData;
                
                // 接続されているエッジから遷移先のIDを自動設定する
                if (block.choices is null || block.choices.Length == 0)
                {
                    var edge = node.DefaultOutputPort.connections.FirstOrDefault();
                    block.nextBlockId = 
                        edge is not null ? ((ChapterNode)edge.input.node).BlockData.blockId : "";
                }
                else
                {
                    block.nextBlockId = "";
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
                nodeDict[block.blockId] = node;
            }

            foreach (var block in chapter.blocks)
            {
                if (!nodeDict.TryGetValue(block.blockId, out var parentNode))
                    continue;

                if (block.choices is null || block.choices.Length == 0)
                {
                    if (!string.IsNullOrEmpty(block.nextBlockId) &&
                        nodeDict.TryGetValue(block.nextBlockId, out var childNode))
                    {
                        var edge = parentNode.DefaultOutputPort.ConnectTo(childNode.InputPort);
                        graphView.AddElement(edge);
                    }
                }
                else
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

            int index = 0;
            foreach (var node in nodeDict.Values)
            {
                node.SetPosition(new Rect(index * 250, index * 100, 200, 150));
                index++;
            }
        }
    }
}