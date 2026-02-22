using System.Collections.Generic;
using Minge2026Spring.Scripts.Application.DTOs;
using UnityEditor.Experimental.GraphView;

namespace Minge2026Spring.Scripts.Editor
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

            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "Input";
            inputContainer.Add(InputPort);

            RefreshPorts();
        }

        public new void RefreshPorts()
        {
            outputContainer.Clear();
            ChoicePorts.Clear();
            DefaultOutputPort = null;

            if (BlockData.choices is null || BlockData.choices.Length == 0)
            {
                DefaultOutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                DefaultOutputPort.portName = "Next";
                outputContainer.Add(DefaultOutputPort);
            }
            else
            {
                for (int i = 0; i < BlockData.choices.Length; i++)
                {
                    var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                    port.portName = string.IsNullOrEmpty(BlockData.choices[i].choiceText) ? $"Choice {i + 1}" : BlockData.choices[i].choiceText;
                    outputContainer.Add(port);
                    ChoicePorts.Add(port);
                }
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