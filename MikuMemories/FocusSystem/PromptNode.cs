using System;
using System.Collections.Generic;


namespace MikuMemories {
    public class PromptNode : IDisposable
    {
        public string Value { get; set; }
        public List<PromptNode> Children { get; set; }

        public PromptNode(string value)
        {
            Value = value;
            Children = new List<PromptNode>();
        }

        public void Dispose()
        {
            foreach (PromptNode child in Children)
            {
                child.Dispose();
            }

            Children = null;
        }
    }
}