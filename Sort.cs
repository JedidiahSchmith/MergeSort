using System.Collections;
using System.Diagnostics;

namespace Sort
{
    public static class Sort<T> where T : IComparable<T>
    {
        private static readonly int MergeSortInsertionSortFloorTrigger = 10;

        private static T[]? MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort;
        private static T[]? MultithreadedInsertionFloorMemoryConservativeMergeSortAuxiliarySpace;
        private static Dictionary<Thread, int[]>? workers;
        private static int log2ArraySizeAsNearestInt;
        private readonly static int cpuCorecountSquared = Environment.ProcessorCount * Environment.ProcessorCount;

        public delegate void SorterForArrays(T[] arrayToSort);

        public static TimeSpan SorterTimer(T[] originalArray, SorterForArrays arraySorter)
        {
            return SorterTimer(originalArray, arraySorter, out _);
        }

        public static TimeSpan SorterTimer(T[] originalArray, SorterForArrays arraySorter, out T[] sortedArray)
        {
            T[] arrayToSort = (T[])originalArray.Clone();

            Stopwatch sw = Stopwatch.StartNew();
            arraySorter(arrayToSort);
            sw.Stop();
            sortedArray = arrayToSort;
            return sw.Elapsed;
        }



        public static void DynamicIntCountSort(int[] arrayOfInts)
        {
            int[] countOfInts = new int[1];

            for (int i = 0; i < arrayOfInts.Length; i++)
            {
                int value = arrayOfInts[i];
                if (value >= countOfInts.Length)
                    GrowCountingArrayToFitLimit(ref countOfInts, 2 * value);

                countOfInts[value]++;
            }

            int arrayIndex = 0;
            for (int i = 0; i < countOfInts.Length; i++)
            {
                while (countOfInts[i]-- != 0)
                {
                    arrayOfInts[arrayIndex++] = i;
                }
            }


        }

        private static void GrowCountingArrayToFitLimit(ref int[] countOfInts, int newLimit)
        {
            int[] newCountOfInts = new int[newLimit + 1];

            for (int i = 0; i < countOfInts.Length; i++)
            {
                newCountOfInts[i] = countOfInts[i];
            }

            countOfInts = newCountOfInts;
        }

        public static void Log2ThreadsForSizeInsertionFloorMemoryConservativeMergeSort(T[] arrayToSort)
        {

            if (arrayToSort == null) return;

            log2ArraySizeAsNearestInt = (int)Math.Log2(arrayToSort.Length);
            MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort = arrayToSort;
            MultithreadedInsertionFloorMemoryConservativeMergeSortAuxiliarySpace = new T[arrayToSort.Length];
            workers = new();
            lock (workers)
            {
                workers.Add(Thread.CurrentThread, new int[] { 0, MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort.Length });
            }

            Log2ThreadsForSizeInsertionFloorMemoryConservativeMergeSort();


            MultithreadedInsertionFloorMemoryConservativeMergeSortAuxiliarySpace = null;

            //save work
            for (int i = 0; i < arrayToSort.Length; i++)
            {
                arrayToSort[i] = MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort[i];
            }

            MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort = null;
        }



        private static void Merge(T[] array, int start, int endBeforeHere, int partitionSeparator, T[] auxiliarySpace)
        {
            //copy work from section with two sorted paritions
            for (int i = start; i < endBeforeHere; i++)
            {
                auxiliarySpace[i] = array[i];
            }
            int leftPartitionIndex = start;
            int rightPartitionIndex = partitionSeparator;
            int finalSortedIndex = start;

            //Merge the two arrays together
            while (leftPartitionIndex < partitionSeparator || rightPartitionIndex < endBeforeHere)
            {
                int comparisonValue;
                if (leftPartitionIndex < partitionSeparator && rightPartitionIndex < endBeforeHere)
                    comparisonValue = auxiliarySpace[leftPartitionIndex].CompareTo(array[rightPartitionIndex]);
                else if (leftPartitionIndex < partitionSeparator)
                {
                    //right Array exhausted
                    comparisonValue = -1;
                }
                else
                {
                    //left Array exhausted
                    comparisonValue = 1;
                }

                if (comparisonValue > 0)
                {
                    array[finalSortedIndex++] = auxiliarySpace[rightPartitionIndex++];
                }
                else
                {
                    array[finalSortedIndex++] = auxiliarySpace[leftPartitionIndex++];
                }
            }
        }

        public static void FairShareThreadingWithInsertionReconstructionAndInsertionFloorMemoryConservativeMergeSort(T[] arrayToSort)
        {

            if (arrayToSort == null) return;

            MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort = arrayToSort;
            MultithreadedInsertionFloorMemoryConservativeMergeSortAuxiliarySpace = new T[arrayToSort.Length];
            workers = new();
            int partitionSize = MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort.Length / Environment.ProcessorCount;

            for (int i = 0; i < Environment.ProcessorCount; i++)
            {
                Thread newThread = new Thread(FairShareThreadingWithInsertionReconstructionAndInsertionFloorMemoryConservativeMergeSort);
                newThread.Start();
                workers.Add(newThread, new int[] { i * partitionSize, partitionSize * (i + 1), 0 });
            }

            foreach (Thread thread in workers.Keys)
            {
                thread.Join();
            }




            //save work
            for (int i = 0; i < arrayToSort.Length; i++)
            {
                arrayToSort[i] = MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort[i];
            }

            //InsertionSort(arrayToSort);


            int[] MergePartitionIndexes = new int[workers.Count * 2];
            int MergePartitionIndexesPointer = 0;
            foreach (int[] values in workers.Values)
            {
                MergePartitionIndexes[MergePartitionIndexesPointer++] = values[0];
                MergePartitionIndexes[MergePartitionIndexesPointer++] = values[1];
            }

            DynamicIntCountSort(MergePartitionIndexes);

            while (MergePartitionIndexes[1] != int.MaxValue)
            {

                Merge(arrayToSort, 0, MergePartitionIndexes[2], MergePartitionIndexes[1], MultithreadedInsertionFloorMemoryConservativeMergeSortAuxiliarySpace);
                MergePartitionIndexes[1] = int.MaxValue;

                Sort<int>.InsertionSort(MergePartitionIndexes);
            }

            /*            //Recombineds the sorted partitions
                        for (int multipliyer = 1; multipliyer <= arrayToSort.Length / partitionSize; multipliyer *= 2)
                        {

                            for (int i = 0; i < Environment.ProcessorCount; i += multipliyer)
                            {
                                int startHere = i * partitionSize;
                                int endBeforeHere = partitionSize * (i + 1);
                                int totalPartitionSize = partitionSize;
                                int partitionSeparator = startHere + totalPartitionSize / 2;

                                Merge(MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort, startHere, endBeforeHere, partitionSeparator, MultithreadedInsertionFloorMemoryConservativeMergeSortAuxiliarySpace);
                            }

                        }*/

            

            workers = null;
            MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort = null;
            MultithreadedInsertionFloorMemoryConservativeMergeSortAuxiliarySpace = null;
        }
        private static void FairShareThreadingWithInsertionReconstructionAndInsertionFloorMemoryConservativeMergeSort()
        {
            int[] values;
            lock (workers)
            {
                _ = workers.TryGetValue(Thread.CurrentThread, out values);
            }

            int startHere = values[0];
            int endBeforeHere = values[1];
            int totalPartitionSize = endBeforeHere - startHere;

            if (totalPartitionSize < MergeSortInsertionSortFloorTrigger)
            {
                InsertionSort(startHere, endBeforeHere, MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort);
            }
            else
            {
                int partitionSeparator = startHere + totalPartitionSize / 2;

                splitMergeLeftAndRight(MemoryConservativeMergeSort, startHere, endBeforeHere, partitionSeparator, MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort, MultithreadedInsertionFloorMemoryConservativeMergeSortAuxiliarySpace);

                Merge(MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort, startHere, endBeforeHere, partitionSeparator, MultithreadedInsertionFloorMemoryConservativeMergeSortAuxiliarySpace);
            }
        }

        public static void CPUCoresSquaredThreadsForSizeInsertionFloorMemoryConservativeMergeSort(T[] arrayToSort)
        {

            if (arrayToSort == null) return;
            MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort = arrayToSort;
            MultithreadedInsertionFloorMemoryConservativeMergeSortAuxiliarySpace = new T[arrayToSort.Length];
            workers = new();
            lock (workers)
            {
                workers.Add(Thread.CurrentThread, new int[] { 0, MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort.Length });
            }

            CPUCoresSquaredThreadsForSizeInsertionFloorMemoryConservativeMergeSort();

            MultithreadedInsertionFloorMemoryConservativeMergeSortAuxiliarySpace = null;

            //save work
            for (int i = 0; i < arrayToSort.Length; i++)
            {
                arrayToSort[i] = MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort[i];
            }

            MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort = null;
        }

        private static void CPUCoresSquaredThreadsForSizeInsertionFloorMemoryConservativeMergeSort()
        {
            int[] values;
            lock (workers)
            {
                _ = workers.TryGetValue(Thread.CurrentThread, out values);
            }

            int startHere = values[0];
            int endBeforeHere = values[1];
            int totalPartitionSize = endBeforeHere - startHere;

            if (totalPartitionSize < MergeSortInsertionSortFloorTrigger)
            {
                InsertionSort(startHere, endBeforeHere, MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort);
            }
            else
            {
                int partitionSeparator = startHere + totalPartitionSize / 2;
                if (workers.Count < cpuCorecountSquared)
                {

                    //Sort left partition
                    Thread leftSortThread = new Thread(CPUCoresSquaredThreadsForSizeInsertionFloorMemoryConservativeMergeSort);
                    //Sort right partition
                    Thread rightSortThread = new Thread(CPUCoresSquaredThreadsForSizeInsertionFloorMemoryConservativeMergeSort);
                    lock (workers)
                    {
                        workers.Add(leftSortThread, new int[] { startHere, partitionSeparator });
                        workers.Add(rightSortThread, new int[] { partitionSeparator, endBeforeHere });
                    }

                    leftSortThread.Start();
                    rightSortThread.Start();
                    leftSortThread.Join();
                    rightSortThread.Join();
                    lock (workers)
                    {
                        workers.Remove(leftSortThread);
                        workers.Remove(rightSortThread);
                    }
                }
                else
                {
                    splitMergeLeftAndRight(MemoryConservativeMergeSort, startHere, endBeforeHere, partitionSeparator, MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort, MultithreadedInsertionFloorMemoryConservativeMergeSortAuxiliarySpace);
                }
                Merge(MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort, startHere, endBeforeHere, partitionSeparator, MultithreadedInsertionFloorMemoryConservativeMergeSortAuxiliarySpace);
            }
        }

        private delegate void mergeSortVarient(T[] arrayToSort, int start, int end, T[] auxiliarySpace);
        private static void splitMergeLeftAndRight(mergeSortVarient mergeSort, int start, int end, int partitionSeparator, T[] arrayToSort, T[] auxiliarySpace)
        {

            //Sort left partition
            mergeSort(arrayToSort, start, partitionSeparator, auxiliarySpace);
            //Sort right partition
            mergeSort(arrayToSort, partitionSeparator, end, auxiliarySpace);

        }

        public static void CPUCoresThreadsForSizeInsertionFloorMemoryConservativeMergeSort(T[] arrayToSort)
        {

            if (arrayToSort == null) return;

            log2ArraySizeAsNearestInt = (int)Math.Log2(arrayToSort.Length);
            MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort = arrayToSort;
            MultithreadedInsertionFloorMemoryConservativeMergeSortAuxiliarySpace = new T[arrayToSort.Length];
            workers = new();
            lock (workers)
            {
                workers.Add(Thread.CurrentThread, new int[] { 0, MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort.Length });
            }

            CPUCoresThreadsForSizeInsertionFloorMemoryConservativeMergeSort();

            MultithreadedInsertionFloorMemoryConservativeMergeSortAuxiliarySpace = null;

            //save work
            for (int i = 0; i < arrayToSort.Length; i++)
            {
                arrayToSort[i] = MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort[i];
            }

            MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort = null;
        }

        private static void CPUCoresThreadsForSizeInsertionFloorMemoryConservativeMergeSort()
        {
            int[] values;
            lock (workers)
            {
                _ = workers.TryGetValue(Thread.CurrentThread, out values);
            }

            int startHere = values[0];
            int endBeforeHere = values[1];
            int totalPartitionSize = endBeforeHere - startHere;

            if (totalPartitionSize < MergeSortInsertionSortFloorTrigger)
            {
                InsertionSort(startHere, endBeforeHere, MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort);
            }
            else
            {
                int partitionSeparator = startHere + totalPartitionSize / 2;
                if (workers.Count < Environment.ProcessorCount)
                {

                    //Sort left partition
                    Thread leftSortThread = new Thread(Log2ThreadsForSizeInsertionFloorMemoryConservativeMergeSort);
                    //Sort right partition
                    Thread rightSortThread = new Thread(Log2ThreadsForSizeInsertionFloorMemoryConservativeMergeSort);
                    lock (workers)
                    {
                        workers.Add(leftSortThread, new int[] { startHere, partitionSeparator });
                        workers.Add(rightSortThread, new int[] { partitionSeparator, endBeforeHere });
                    }

                    leftSortThread.Start();
                    rightSortThread.Start();
                    leftSortThread.Join();
                    rightSortThread.Join();
                    lock (workers)
                    {
                        workers.Remove(leftSortThread);
                        workers.Remove(rightSortThread);
                    }
                }
                else
                {
                    splitMergeLeftAndRight(MemoryConservativeMergeSort, startHere, endBeforeHere, partitionSeparator, MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort, MultithreadedInsertionFloorMemoryConservativeMergeSortAuxiliarySpace);
                }
                Merge(MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort, startHere, endBeforeHere, partitionSeparator, MultithreadedInsertionFloorMemoryConservativeMergeSortAuxiliarySpace);
            }
        }

        private static void Log2ThreadsForSizeInsertionFloorMemoryConservativeMergeSort()
        {
            int[] values;
            lock (workers)
            {
                _ = workers.TryGetValue(Thread.CurrentThread, out values);
            }

            int startHere = values[0];
            int endBeforeHere = values[1];
            int totalPartitionSize = endBeforeHere - startHere;

            if (totalPartitionSize < MergeSortInsertionSortFloorTrigger)
            {
                InsertionSort(startHere, endBeforeHere, MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort);
            }
            else
            {
                int partitionSeparator = startHere + totalPartitionSize / 2;
                if (workers.Count < log2ArraySizeAsNearestInt)
                {

                    //Sort left partition
                    Thread leftSortThread = new Thread(Log2ThreadsForSizeInsertionFloorMemoryConservativeMergeSort);
                    //Sort right partition
                    Thread rightSortThread = new Thread(Log2ThreadsForSizeInsertionFloorMemoryConservativeMergeSort);
                    lock (workers)
                    {
                        workers.Add(leftSortThread, new int[] { startHere, partitionSeparator });
                        workers.Add(rightSortThread, new int[] { partitionSeparator, endBeforeHere });
                    }

                    leftSortThread.Start();
                    rightSortThread.Start();
                    leftSortThread.Join();
                    rightSortThread.Join();
                    lock (workers)
                    {
                        workers.Remove(leftSortThread);
                        workers.Remove(rightSortThread);
                    }
                }
                else
                {
                    splitMergeLeftAndRight(MemoryConservativeMergeSort, startHere, endBeforeHere, partitionSeparator, MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort, MultithreadedInsertionFloorMemoryConservativeMergeSortAuxiliarySpace);
                }
                Merge(MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort, startHere, endBeforeHere, partitionSeparator, MultithreadedInsertionFloorMemoryConservativeMergeSortAuxiliarySpace);
            }
        }

        public static void MaxMultithreadedInsertionFloorMemoryConservativeMergeSort(T[] arrayToSort)
        {
            if (arrayToSort == null) return;
            MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort = arrayToSort;
            MultithreadedInsertionFloorMemoryConservativeMergeSortAuxiliarySpace = new T[arrayToSort.Length];
            workers = new();
            lock (workers)
            {
                workers.Add(Thread.CurrentThread, new int[] { 0, MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort.Length });
            }

            MaxMultithreadedInsertionFloorMemoryConservativeMergeSort();

            MultithreadedInsertionFloorMemoryConservativeMergeSortAuxiliarySpace = null;

            //save work
            for (int i = 0; i < arrayToSort.Length; i++)
            {
                arrayToSort[i] = MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort[i];
            }

            MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort = null;
        }

        private static void MaxMultithreadedInsertionFloorMemoryConservativeMergeSort()
        {
            int[] values;
            lock (workers)
            {
                _ = workers.TryGetValue(Thread.CurrentThread, out values);
            }

            int startHere = values[0];
            int endBeforeHere = values[1];
            int totalPartitionSize = endBeforeHere - startHere;

            if (totalPartitionSize < MergeSortInsertionSortFloorTrigger)
            {
                InsertionSort(startHere, endBeforeHere, MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort);
            }
            else //Below is a near copy of MemoryConservativeMergeSort
            {
                int partitionSeparator = startHere + totalPartitionSize / 2;
                //Sort left partition
                Thread leftSortThread = new Thread(MaxMultithreadedInsertionFloorMemoryConservativeMergeSort);
                //Sort right partition
                Thread rightSortThread = new Thread(MaxMultithreadedInsertionFloorMemoryConservativeMergeSort);
                lock (workers)
                {
                    workers.Add(leftSortThread, new int[] { startHere, partitionSeparator });
                    workers.Add(rightSortThread, new int[] { partitionSeparator, endBeforeHere });
                }
                leftSortThread.Start();
                rightSortThread.Start();
                leftSortThread.Join();
                rightSortThread.Join();

                Merge(MultithreadedInsertionFloorMemoryConservativeMergeSortArrayToSort, startHere, endBeforeHere, partitionSeparator, MultithreadedInsertionFloorMemoryConservativeMergeSortAuxiliarySpace);
            }
        }


        public static void InsertionFloorMemoryConservativeMergeSort(T[] arrayToSort)
        {
            if (arrayToSort == null) return;

            InsertionFloorMemoryConservativeMergeSort(arrayToSort, 0, arrayToSort.Length, new T[arrayToSort.Length]);

        }


        private static void InsertionFloorMemoryConservativeMergeSort(T[] arrayToSort, int startHere, int endBeforeHere, T[] auxiliarySpace)
        {
            int totalPartitionSize = endBeforeHere - startHere;

            if (totalPartitionSize < MergeSortInsertionSortFloorTrigger)
            {
                InsertionSort(startHere, endBeforeHere, arrayToSort);
            }
            else
            {

                int partitionSeparator = startHere + totalPartitionSize / 2;

                splitMergeLeftAndRight(MemoryConservativeMergeSort, startHere, endBeforeHere, partitionSeparator, arrayToSort, auxiliarySpace);

                Merge(arrayToSort, startHere, endBeforeHere, partitionSeparator, auxiliarySpace);

            }
        }

        public static void MemoryConservativeMergeSort(T[] arrayToSort)
        {
            if (arrayToSort == null) return;

            MemoryConservativeMergeSort(arrayToSort, 0, arrayToSort.Length, new T[arrayToSort.Length]);
        }

        public static void MemoryConservativeMergeSort(T[] arrayToSort, int startHere, int endBeforeHere, T[] auxiliarySpace)
        {
            int totalPartitionSize = endBeforeHere - startHere;

            if (totalPartitionSize < 2)
                return;

            int partitionSeparator = startHere + totalPartitionSize / 2;

            splitMergeLeftAndRight(MemoryConservativeMergeSort, startHere, endBeforeHere, partitionSeparator, arrayToSort, auxiliarySpace);

            Merge(arrayToSort, startHere, endBeforeHere, partitionSeparator, auxiliarySpace);
        }

        public static void MemoryLitteringMergeSort(T[] arrayToSort)
        {
            if (arrayToSort == null) return;

            if (arrayToSort.Length < 2)
                return;

            Array[] splitArrays = arraySplitter(arrayToSort);
            T[] leftArray = (T[])splitArrays[0];
            T[] rightArray = (T[])splitArrays[1];

            //Sort first half
            MemoryLitteringMergeSort(leftArray);
            //Sort second half
            MemoryLitteringMergeSort(rightArray);

            int currentIndexLeft = 0;

            int currentIndexRight = 0;

            int finalSortedIndex = 0;

            //Merge the two arrays together
            while (currentIndexLeft < leftArray.Length || currentIndexRight < rightArray.Length)
            {
                int comparisonValue;
                if (currentIndexLeft < leftArray.Length && currentIndexRight < rightArray.Length)
                {
                    comparisonValue = leftArray[currentIndexLeft].CompareTo(rightArray[currentIndexRight]);
                }
                else if (currentIndexLeft < leftArray.Length)
                {
                    //right Array exhausted
                    comparisonValue = -1;
                }
                else
                {
                    //left Array exhausted
                    comparisonValue = 1;
                }


                if (comparisonValue > 0)
                {
                    arrayToSort[finalSortedIndex++] = rightArray[currentIndexRight++];
                }
                else
                {
                    arrayToSort[finalSortedIndex++] = leftArray[currentIndexLeft++];
                }
            }
        }

        public static void InsertionSort(T[] arrayOfStuff)
        {
            InsertionSort(0, arrayOfStuff.Length, arrayOfStuff);
        }
        public static void InsertionSort(int startIndex, int SortBeforeThisIndex, T[] arrayOfStuff)
        {
            int currentIndex = startIndex;
            while (currentIndex < SortBeforeThisIndex - 1)
            {
                if (currentIndex >= startIndex && arrayOfStuff[currentIndex].CompareTo(arrayOfStuff[currentIndex + 1]) > 0)
                {
                    Swap(arrayOfStuff, currentIndex, currentIndex + 1);
                    currentIndex--;
                }
                else
                    currentIndex++;
            }
        }

        private static void Swap(T[] arrayOfStuff, int firstIndex, int secondIndex)
        {
            T object1 = arrayOfStuff[firstIndex];
            arrayOfStuff[firstIndex] = arrayOfStuff[secondIndex];
            arrayOfStuff[secondIndex] = object1;
        }


        private static Array[] arraySplitter(T[] arrayToSplit)
        {
            bool isEvenLength = arrayToSplit.Length % 2 == 0;

            T[] leftArray = new T[arrayToSplit.Length / 2];
            T[] rightArray = new T[isEvenLength ? arrayToSplit.Length / 2 : arrayToSplit.Length / 2 + 1];

            for (int i = 0; i < leftArray.Length; i++)
            {
                leftArray[i] = arrayToSplit[i];
            }
            for (int i = 0; i < rightArray.Length; i++)
            {
                rightArray[i] = arrayToSplit[i + leftArray.Length];
            }

            return new Array[] { leftArray, rightArray };
        }
    }
}