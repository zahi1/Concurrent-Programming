using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab1_Conc
{
    class Student : IComparable<Student>, IEquatable<Student>
    {
        public string Name { get; set; }
        public int Year { get; set; }
        public double Grade { get; set; }
        


        public Student()
        {
            Name = "";
            Year = 0;
            Grade = 0.0;
   
        }

        public Student(string name, int year, double grade)
        {
            Name = name;
            Year = year;
            Grade = grade;
       
        }

        public override string ToString()
        {
            return string.Format(" {0, -14} |{1, 8:d} |{2, 8:f2} |", Name, Year, Grade);
        }

        public int CompareTo(Student other)
        {
            int p = String.Compare(this.Name, other.Name, StringComparison.CurrentCulture);
            if (p > 0)
                return 1;
            else if (p < 0)
                return -1;
            else
                return 0;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Student);
        }

        public bool Equals(Student other)
        {
            return Name == other.Name;
        }

    }
}
