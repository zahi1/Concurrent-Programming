using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lab1_Conc
{
    class DataMonitor
    {
        private static readonly object LockObject = new object();
        private const int MaxSize = 10;
        private readonly Student[] Students;
        private int Count;
        private int counter = 0;
        const int max = 25;


        public DataMonitor()

        {
            Count = 0;
            Students = new Student[MaxSize];
        }

        public void AddStudent(Student student)
        {
            lock (LockObject)
            {
                while (Count >= MaxSize)
                {
                    Monitor.Wait(LockObject);
                }

                Students[Count++] = student;
                counter++;
                Monitor.Pulse(LockObject);
            }
        }

        public int StudentCount() { return Count; }

        public Student GetStudent(int index) { return Students[index]; }

        public Student RemoveStudent()
        {
            lock (LockObject)
            {
                while (Count == 0 && counter < max)
                {
                    Monitor.Wait(LockObject);
                }
                if (Count == 0 && counter >= max)
                {
                    return null;
                }
              
                Student removedStudent = Students[--Count];
                Monitor.Pulse(LockObject);
                return removedStudent;               
                
            }
        }

    }

}
