using System.Collections.Generic;
using System.Linq;
using Minge2026Spring.Scripts.Application.DTOs;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Minge2026Spring.Scripts.Editor.ChapterGraphEditor
{
    public class ChapterGraphView : GraphView
    {
        private ChapterGraphEditor editor;
        private NodeSearchWindow searchWindow;

        public ChapterGraphView(ChapterGraphEditor editor)
        {
            this.editor = editor;
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
            
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            
            // 背景のグリッド
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
            
            // スタイルシートを適用
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:StyleSheet ChapterGraphStyle");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                styleSheets.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(path));
            }

            AddMiniMap();
            AddSearchWindow();
        }

        private void AddMiniMap()
        {
            var miniMap = new MiniMap { anchored = true };
            miniMap.SetPosition(new Rect(10, 30, 200, 140));
            Add(miniMap);
        }

        private void AddSearchWindow()
        {
            searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
            searchWindow.Init(this);
            nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindow);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports
                .ToList()
                .Where(endPort => endPort.direction != startPort.direction 
                                  && endPort.node != startPort.node)
                .ToList();
        }

        public void CreateNewNode(string blockId, ChapterNodeType nodeType)
        {
            var block = new ChapterBlock
            {
                blockId = blockId,
                nodeType = nodeType,
                dialogues = new Dialogue[0],
                choices = new Choice[0]
            };
            
            CreateNode(block);
        }

        public ChapterNode CreateNode(ChapterBlock block)
        {
            var node = new ChapterNode(block, this);
            node.SetPosition(new Rect(100, 100, 200, 150));
            AddElement(node);
            return node;
        }

        public void ClearGraph()
        {
            DeleteElements(nodes.ToList());
            DeleteElements(edges.ToList());
        }

        public void OnNodeSelected(ChapterNode node)
        {
            editor.OnNodeSelected(node);
        }
    }
}