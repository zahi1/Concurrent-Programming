using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab1_Conc
{
    class SortedResultMonitor
    {
        const int ArraySize = 25;
        private readonly Student[] Students;
        private int Count;
        const string ResultsFilePath = "result.txt";

        public SortedResultMonitor()
        {
            Count = 0;
            Students = new Student[ArraySize];
        }

        public Student Retrieve(int index) { return Students[index]; }

        public void AddItemSorted(Student newItem)
        {
            lock(this)
            {
                Students[Count++] = newItem;
                PerformSort();
            }                     
        }

        public int GetCount()
        {
            return Count;
        }

        public Student GetItems()
        {
            return Students[Count];
        }

        public void PerformSort()
        {
            var isSorted = true;
            while (isSorted)
            {
                isSorted = false;
                for (int i = 0; i < GetCount() - 1; i++)
                {
                    var currentStudent = Students[i];
                    var nextStudent = Students[i + 1];
                    if (currentStudent.CompareTo(nextStudent) > 0)
                    {
                        Students[i] = nextStudent;
                        Students[i + 1] = currentStudent;
                        isSorted = true;
                    }
                }
            }
        }

        public void DisplayResultsInConsole()
        {
            Console.WriteLine("Results Sorted In Console");
            Console.WriteLine(new string('-', 37));
            Console.WriteLine(" Name    \t|    Year |   Grade |");
            Console.WriteLine(new string('-', 37));
            if (GetCount() == 0)
            {
                Console.WriteLine("No students found.");
            }
            else
            {
                for (int i = 0; i < GetCount(); i++)
                {
                    var student = Retrieve(i);
                    if (student != null)
                        Console.WriteLine("{0} ", student.ToString());
                }
            }
            Console.WriteLine(new string('-', 37));
            Console.WriteLine();
        }

        public void SaveResultsToFile()
        {
            using (StreamWriter fileWriter = new StreamWriter(ResultsFilePath))
            {
                fileWriter.WriteLine("Results Sorted In Console");
                fileWriter.WriteLine(new string('-', 37));
                fileWriter.WriteLine(" Name    \t|    Year |   Grade |");
                fileWriter.WriteLine(new string('-', 37));
                if (GetCount() == 0)
                {
                    fileWriter.WriteLine("No students found.");
                }
                else
                {
                    for (int i = 0; i < GetCount(); i++)
                    {
                        var student = Retrieve(i);
                        if (student != null)
                            fileWriter.WriteLine("{0} ", student.ToString());
                    }
                }
                fileWriter.WriteLine(new string('-', 37));
                fileWriter.WriteLine();
            }
        }
    }

}
