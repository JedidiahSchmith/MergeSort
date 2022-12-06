using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sort
{
    internal class DynamicWorkloadMergeSort<ItemToSort> where ItemToSort : IComparable
    {

        static int WorkID = 0;
        static int LayerCount = 1;
        static WorkingState ws = WorkingState.SortMode;
        private enum WorkingState { SortMode, MergeMode }

        Thread[] threadPool = new Thread[System.Environment.ProcessorCount];
        ItemToSort[] thingsToSort;

        public DynamicWorkloadMergeSort(ItemToSort[] thingsToSort)
        {

            this.thingsToSort = thingsToSort;

            for (int i = 0; i < threadPool.Length; i++)
            {
                threadPool[i] = new Thread(DynamicWorker);
                
            }

            foreach (Thread t in threadPool)
            {
                t.Start();
            }


            throw new NotImplementedException();
        }

        public void DynamicWorker()
        {
            bool running = true;
            while (running)
            {

                switch (ws)
                {
                    case WorkingState.SortMode:
                        getSu
                        Sort(this.thingsToSort,)



                        break;
                    case WorkingState.MergeMode:



                        break;
                }
            }
        }
    }


    public static void MergeSort(ItemToSort[] thingsToSort, int threadPoolCount)
    {
        throw new NotImplementedException();

        Barrier barrier = new(threadPoolCount);
        ((int, int, int, int), (int, int, int, int)) value = GetSubArrays();
        Sort(thingsToSort, value.Item1);
        Sort(thingsToSort, value.Item2);
        Merge(thingsToSort, value.Item1, value.Item2);


    }

    private static void Sort(ItemToSort[] thingsToSort, (int, int, int, int) Values)
    {
        throw new NotImplementedException();
        int WorkID = Values.Item1;
    }

    public static void Merge(ItemToSort[] thingsToSort, (int, int, int, int) item1, (int, int, int, int) item2)
    {

        throw new NotImplementedException();


    }

    public static ((int, int, int, int), (int, int, int, int)) GetSubArrays()
    {
        int workID2 = Interlocked.Add(ref WorkID, 2);
        int workID1 = workID2 - 1;



        ((int, int, int, int), (int, int, int, int)) value = default;
        return value;
        throw new NotImplementedException();
    }

}
}
