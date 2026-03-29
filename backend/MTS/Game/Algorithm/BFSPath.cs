namespace MTS.Game.Algorithm;

public static class BFSPath
{
    public static List<T> FindPath<T>(
        Dictionary<T, List<T>> graph,
        T start,
        T goal)
    {
        var visited = new HashSet<T>();
        var queue = new Queue<T>();
        var parent = new Dictionary<T, T>();
        visited.Add(start);
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var u = queue.Dequeue();
            if (u.Equals(goal))
                break;
            foreach (var v in graph[u])
            {
                if (!visited.Contains(v))
                {
                    visited.Add(v);
                    parent[v] = u;
                    queue.Enqueue(v);
                }
            }
        }
        if (!visited.Contains(goal))
            return null;
        var path = new List<T>();
        var cur = goal;

        while (!cur.Equals(start))
        {
            path.Add(cur);
            cur = parent[cur];
        }
        path.Add(start);
        path.Reverse();
        return path;
    }
}