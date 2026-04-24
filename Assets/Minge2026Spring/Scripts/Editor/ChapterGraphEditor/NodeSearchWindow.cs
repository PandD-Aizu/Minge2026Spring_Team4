using System.Collections.Generic;
using Minge2026Spring.Scripts.Application.DTOs;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Minge2026Spring.Scripts.Editor.ChapterGraphEditor
{
    public class NodeSearchWindow : ScriptableObject, ISearchWindowProvider
    {
        private ChapterGraphView graphView;

        public void Init(ChapterGraphView graphView)
        {
            this.graphView = graphView;
        }

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Create Node"), 0),
                new SearchTreeEntry(new GUIContent("Dialogue Node"))
                {
                    level = 1,
                    userData = ChapterNodeType.Dialogue
                },
                new SearchTreeEntry(new GUIContent("Choice Node"))
                {
                    level = 1,
                    userData = ChapterNodeType.Choice
                },
                new SearchTreeEntry(new GUIContent("LLM Node"))
                {
                    level = 1,
                    userData = ChapterNodeType.LLM
                }
            };

            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            var nodeType = (ChapterNodeType)entry.userData;
            graphView.CreateNewNode($"New{nodeType}", nodeType);
            return true;
        }
    }
}