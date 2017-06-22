using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class Graph
    {
        /// <summary>
        /// Dictionary mapping input ReferenceFieldModel to the list of output ReferenceFieldModels
        /// </summary>
        private Dictionary<ReferenceFieldModel, List<ReferenceFieldModel>> _edges = new Dictionary<ReferenceFieldModel, List<ReferenceFieldModel>>(); 

        public void AddEdge(ReferenceFieldModel inputRef, ReferenceFieldModel outputRef)
        {
            if (_edges.ContainsKey(inputRef))
                _edges[inputRef].Add(outputRef);
            else
                _edges[inputRef] = new List<ReferenceFieldModel> { outputRef };
        }

        public void RemoveEdge(ReferenceFieldModel inputRef, ReferenceFieldModel outputRef)
        {
            if (_edges.ContainsKey(inputRef))
            {
                _edges[inputRef].Remove(outputRef); 
            }
        }

        public bool IsCyclic()
        {
            HashSet<ReferenceFieldModel> visited = new HashSet<ReferenceFieldModel>();
            HashSet<ReferenceFieldModel> recStack = new HashSet<ReferenceFieldModel>();


            foreach (var edge in _edges)
            {
                if (IsCyclicUtil(edge.Key, ref visited, ref recStack))
                {
                    return true;
                }
            }

            return false; 
        }


        private bool IsCyclicUtil(ReferenceFieldModel input, ref HashSet<ReferenceFieldModel> visited, ref HashSet<ReferenceFieldModel> recStack)
        {
            if (!visited.Contains(input))
            {
                // Mark the current node as visited and part of recursion stack
                visited.Add(input);
                recStack.Add(input);

                // Recur for all the vertices adjacent to this vertex
                if (_edges.ContainsKey(input))
                {
                    foreach (var edge in _edges[input])
                    {
                        if (!visited.Contains(edge) && IsCyclicUtil(edge, ref visited, ref recStack))
                            return true;
                        if (recStack.Contains(edge))
                            return true;
                    }
                }
            }
            recStack.Remove(input);

            return false; 
        }

    }
}
