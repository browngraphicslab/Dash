using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dash
{
    public static class KMeansCluster
    {
        public static (int[], double[]) Cluster(double[] rawData, int numClusters)
        {
            //double[] data = Normalized(rawData);
            double[] data = rawData;

            bool changed = true, success = true;

            if (data.Length < numClusters)
            {
                return (null, new[] {0.0, 0.0});
            }
            int[] clustering = InitClustering(data.Length, numClusters, 0);
            double[] means = new double[numClusters];

            int maxCount = data.Length * 10;
            int ct = 0;
            while (changed && success && ct < maxCount)
            {
                ct++;
                success = UpdateMeans(data, clustering, means);
                changed = UpdateClustering(data, clustering, means);
            }

            return (clustering, means);
        }

        private static double[] Normalized(double[] rawData)
        {
            double[] result = new double[rawData.Length];
            Array.Copy(rawData, result, rawData.Length);

            double resSum = result.Sum();
            double mean = resSum / result.Length;

            double sum = 0.0;
            for (int i = 0; i < result.Length; i++)
            {
                sum += (result[i] - mean) * (result[i] - mean);
            }

            double sd = sum / result.Length;
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = (result[i] - mean) / sd;
            }

            return result;
        }

        private static int[] InitClustering(int numVals, int numClusters, int randomSeed)
        {
            Random random = new Random(randomSeed);
            int[] clustering = new int[numVals];
            for (int i = 0; i < numClusters; i++)
            {
                clustering[i] = i;
            }

            for (int i = numClusters; i < clustering.Length; i++)
            {
                clustering[i] = random.Next(0, numClusters);
            }

            return clustering;
        }

        private static bool UpdateMeans(double[] data, int[] clustering, double[] means)
        {
            int numClusters = means.Length;
            int[] clusterCounts = new int[numClusters];
            for (int i = 0; i < data.Length; i++)
            {
                int cluster = clustering[i];
                clusterCounts[cluster]++;
            }

            for (int k = 0; k < numClusters; k++)
            {
                if (clusterCounts[k] == 0)
                {
                    return false;
                }
            }

            for (int k = 0; k < means.Length; k++)
            {
                means[k] = 0.0;
            }

            for (int i = 0; i < data.Length; i++)
            {
                int cluster = clustering[i];
                means[cluster] += data[i];
            }

            for (int k = 0; k < means.Length; k++)
            {
                means[k] /= clusterCounts[k];
            }

            return true;
        }

        private static bool UpdateClustering(double[] data, int[] clustering, double[] means)
        {
            int numClusters = means.Length;
            bool changed = false;

            int[] newClustering = new int[clustering.Length];
            Array.Copy(clustering, newClustering, clustering.Length);

            double[] distances = new double[numClusters];

            for (int i = 0; i < data.Length; i++)
            {
                for (int k = 0; k < numClusters; k++)
                {
                    distances[k] = Distance(data[i], means[k]);
                }

                int newClusterID = MinIndex(distances);
                if (newClusterID != newClustering[i])
                {
                    changed = true;
                    newClustering[i] = newClusterID;
                }
            }

            if (!changed)
            {
                return false;
            }

            int[] clusterCounts = new int[numClusters];
            for (int i = 0; i < data.Length; i++)
            {
                int cluster = newClustering[i];
                clusterCounts[cluster]++;
            }

            for (int k = 0; k < numClusters; k++)
            {
                if (clusterCounts[k] == 0)
                {
                    return false;
                }
            }

            Array.Copy(newClustering, clustering, newClustering.Length);
            return true;
        }

        private static double Distance(double val, double mean)
        {
            return Math.Abs(val - mean);
        }

        private static int MinIndex(double[] distances)
        {
            int indexOfMin = 0;
            double smallDist = distances[0];
            for (int i = 0; i < distances.Length; i++)
            {
                if (distances[i] < smallDist)
                {
                    smallDist = distances[i];
                    indexOfMin = i;
                }
            }

            return indexOfMin;
        }
    }
}
