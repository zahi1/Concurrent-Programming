using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lab1_Conc
{
    class Program
    {
        const string FilePath1 = "IFU-1_ElHelouZ_L1_dat.txt";
        const string FilePath2 = @"C:\Users\helou\Downloads\Lab1_ConcFixed\Lab1_Conc\Lab1_Conc\IFU-1_ElHelouZ_L1_dat2.txt";
        const string FilePath3 = @"C:\Users\helou\Downloads\Lab1_ConcFixed\Lab1_Conc\Lab1_Conc\IFU-1_ElHelouZ_L1_dat3.txt";
        static void Read(string filePath, DataMonitor students)
        {
            string name, line;
            int year, n;
            double grade;
            using (StreamReader reader = new StreamReader(filePath))
            {
                n = File.ReadAllLines(filePath).Length;
                for (int i = 0; i < n; i++)
                {
                    line = reader.ReadLine();
                    string[] parts = line.Split(',');
                    name = parts[0];
                    year = int.Parse(parts[1]);
                    grade = double.Parse(parts[2]);
                    Student student = new Student(name, year, grade);
                    students.AddStudent(student);
                }
            }
        }

        static void Worker(DataMonitor students, SortedResultMonitor sortedResults)
        {
            try
            {
                while (true)
                {
                    var student = students.RemoveStudent();
                    if (student == null)
                    {
                        break;
                    }
                    double compGrade = ComputeResultBasedOnGPA(student);

                    if(compGrade>3.0)
                    {
                        sortedResults.AddItemSorted(student);
                    }
                   
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e);
            }
        }


        private static double ComputeResultBasedOnGPA(Student student)
        {
            double gpa = student.Grade; 

            double computedResult;

            if (gpa >= 9.0)
            {
                computedResult = 4.0; 
            }
            else if (gpa >= 8.0)
            {
                computedResult = 3.5;
            }
            else if (gpa >= 7.0)
            {
                computedResult = 3.0; 
            }
            else
            {
                computedResult = 2.0; 
            }

            return computedResult;
        }


        static void Main(string[] args)
        {
            DataMonitor studentsData = new DataMonitor();
            SortedResultMonitor sortedResults = new SortedResultMonitor();
            List<Thread> threads = new List<Thread>();
            int workerThread = 6;



            for (int i = 0; i < workerThread; i++)
            {
                var thread = new Thread(() => Worker(studentsData, sortedResults));
                threads.Add(thread);
            }

            foreach (var thread in threads)
            {
                thread.Start();
            }

            Read(FilePath1, studentsData);


            foreach (var thread in threads)
            {
                thread.Join();

            }
            sortedResults.SaveResultsToFile();
            sortedResults.DisplayResultsInConsole();
            Console.ReadKey();
        }

    }
}
