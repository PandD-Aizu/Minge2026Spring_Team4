using System.Collections.Generic;
using Minge2026Spring.Scripts.Application.DTOs;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Minge2026Spring.Scripts.Editor.ChapterGraphEditor
{
    public class ChapterNode : Node
    {
        public ChapterBlock BlockData;
        private ChapterGraphView graphView;

        public Port InputPort;
        public Port DefaultOutputPort;
        public List<Port> ChoicePorts = new List<Port>();

        public ChapterNode(ChapterBlock block, ChapterGraphView graphView)
        {
            this.BlockData = block;
            this.graphView = graphView;
            this.title = string.IsNullOrEmpty(block.blockId) ? "New Block" : block.blockId;
            UpdateTitle();

            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "Input";
            inputContainer.Add(InputPort);

            RefreshPorts();
        }

        public void UpdateTitle()
        {
            this.title = $"{BlockData.nodeType}: {BlockData.blockId}";
            RefreshNodeVisuals();
        }

        private void RefreshNodeVisuals()
        {
            // ノードの種類に合わせて色を変える
            var titleContainer = this.Q("title");
            if (titleContainer != null)
            {
                switch (BlockData.nodeType)
                {
                    case ChapterNodeType.Dialogue:
                        titleContainer.style.backgroundColor = new Color(0.2f, 0.4f, 0.8f, 0.8f); // Blueish
                        break;
                    case ChapterNodeType.Choice:
                        titleContainer.style.backgroundColor = new Color(0.2f, 0.7f, 0.3f, 0.8f); // Greenish
                        break;
                    case ChapterNodeType.LLM:
                        titleContainer.style.backgroundColor = new Color(0.7f, 0.2f, 0.6f, 0.8f); // Purpleish
                        break;
                }
            }
        }

        public new void RefreshPorts()
        {
            outputContainer.Clear();
            ChoicePorts.Clear();
            DefaultOutputPort = null;

            if (BlockData.nodeType == ChapterNodeType.Choice)
            {
                if (BlockData.choices is not null)
                {
                    for (int i = 0; i < BlockData.choices.Length; i++)
                    {
                        var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                        port.portName = string.IsNullOrEmpty(BlockData.choices[i].choiceText) ? $"Choice {i + 1}" : BlockData.choices[i].choiceText;
                        outputContainer.Add(port);
                        ChoicePorts.Add(port);
                    }
                }
            }
            else
            {
                DefaultOutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                DefaultOutputPort.portName = "Next";
                outputContainer.Add(DefaultOutputPort);
            }
            
            RefreshExpandedState();
            base.RefreshPorts();
        }

        public override void OnSelected()
        {
            base.OnSelected();
            graphView.OnNodeSelected(this);
        }
    }
}