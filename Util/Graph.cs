using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DashShared;

namespace Dash
{
    public class Graph<T>
    {
        /// <summary>
        /// Dictionary mapping input ReferenceFieldModel to the list of output ReferenceFieldModels
        /// </summary>
        private Dictionary<T, List<T>> _edges = new Dictionary<T, List<T>>(); 

        public void AddEdge(T startNode, T endNode)
        {
            if (_edges.ContainsKey(startNode))
                _edges[startNode].Add(endNode);
            else
                _edges[startNode] = new List<T> { endNode };
        }

        public void RemoveEdge(T startNode, T endNode)
        {
            if (_edges.ContainsKey(startNode))
            {
                _edges[startNode].Remove(endNode); 
            }
        }

        public bool IsCyclic()
        {
            HashSet<T> visited = new HashSet<T>();
            HashSet<T> recStack = new HashSet<T>();


            foreach (var edge in _edges)
            {
                if (IsCyclicUtil(edge.Key, ref visited, ref recStack))
                    return true;
            }

            return false; 
        }


        private bool IsCyclicUtil(T startNode, ref HashSet<T> visited, ref HashSet<T> recStack)
        {
            if (!visited.Contains(startNode))
            {
                // Mark the current node as visited and part of recursion stack
                visited.Add(startNode);
                recStack.Add(startNode);

                // Recur for all the vertices adjacent to this vertex
                if (_edges.ContainsKey(startNode))
                {
                    foreach (var edge in _edges[startNode])
                    {
                        if (!visited.Contains(edge) && IsCyclicUtil(edge, ref visited, ref recStack))
                            return true;
                        if (recStack.Contains(edge))
                            return true;
                    }
                }
            }
            recStack.Remove(startNode);

            return false; 
        }

    }
}
