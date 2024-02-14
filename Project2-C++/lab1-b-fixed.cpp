// lab1-b-fixed.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <fstream>
#include <vector>
#include <algorithm>
#include <string>
#include <omp.h>
#include <iomanip>
using namespace std;

class Student {
public:
    string Name;
    int Year;
    double Grade;

    Student(string name, int year, double grade) : Name(name), Year(year), Grade(grade) {}

    int CompareTo(const Student& other) const {
        return Name.compare(other.Name);
    }
};

class ResultMonitor {
private:
    static const int maxSize = 300;
    vector<Student> result_students;

public:
    ResultMonitor() {
        result_students.reserve(maxSize);
    }

    void AddResultData(const Student& stud) {
#pragma omp critical
        {
            int insertIndex = -1;
            for (int i = 0; i < result_students.size(); i++) {
                if (stud.CompareTo(result_students[i]) < 0) {
                    insertIndex = i;
                    break;
                }
            }
            if (insertIndex == -1) {
                result_students.push_back(stud);
            }
            else {
                result_students.insert(result_students.begin() + insertIndex, stud);
            }
        }
    }

    int GetCount() const {
        return result_students.size();
    }

    const vector<Student>& GetData() const
    {
        return result_students;
    }
};

double ComputeResultBasedOnGPA(const Student& student) {
    double gpa = student.Grade;
    double computedResult;

    if (gpa >= 9.0) {
        computedResult = 4.0;
    }
    else if (gpa >= 8.0) {
        computedResult = 3.5;
    }
    else if (gpa >= 7.0) {
        computedResult = 3.0;
    }
    else {
        computedResult = 2.0;
    }

    return computedResult;
}

vector<Student> ReadStudentsFromFile(const string& filename) {
    vector<Student> students;

    try {
        ifstream reader(filename);
        string line;

        while (getline(reader, line)) {
            size_t delimiterPos = line.find(';');

            if (delimiterPos != string::npos) {
                string student_name = line.substr(0, delimiterPos);
                int year = stoi(line.substr(delimiterPos + 1, line.find(';', delimiterPos + 1) - delimiterPos - 1));
                double grade = stod(line.substr(line.find(';', delimiterPos + 1) + 1));

                Student student(student_name, year, grade);
                students.push_back(student);
            }
        }
    }
    catch (const exception& e) {
        cerr << "Exception: " << e.what() << endl;
    }

    return students;
}

void WriteResultsToFile(const string& resultfile, const ResultMonitor& rm, int intSum, double floatSum) {
    ofstream writer(resultfile);
    if (rm.GetCount() != 0)
    {
        writer << "----------------------------------------\n";
        writer << "|    Name       |   Year    |   Grade  | \n";
        writer << "----------------------------------------\n";

        for (const auto& s : rm.GetData()) {
            writer << "| " << setw(15) << left << s.Name
                << "| " << setw(9) << s.Year
                << "| " << setw(8) << s.Grade << " |\n";
        }

        writer << "----------------------------------------\n";
        writer << "The Sum of Years that we have: " << intSum << "\n";
        writer << "The sum of Grades that we have: " << floatSum << "\n";
    }
    else
    {
        writer << "No data is filtered!";
    }

}


int main() {
    string resultfile = "result.txt";
    string datafile1 = "zahidat1.txt";
    string datafile2 = "zahidat2.txt";
    string datafile3 = "zahidat3.txt";

    vector<Student> student_list = ReadStudentsFromFile(datafile2); 

    int numThreads = 4; 
    int intSum = 0;
    double floatSum = 0.0;
    ResultMonitor rm;

    omp_set_num_threads(numThreads);

    int dataSize = student_list.size();
    int chunkSize = dataSize / numThreads;
    int remainder = dataSize % numThreads;


#pragma omp parallel reduction(+:intSum,floatSum) default(none) shared(student_list, rm, dataSize, numThreads, chunkSize, remainder)
    {
        int threadId = omp_get_thread_num();
        int startIdx = threadId * chunkSize + min(threadId, remainder);
        int endIdx = (threadId + 1) * chunkSize + min(threadId + 1, remainder);

        for (int i = startIdx; i < endIdx; ++i) {
            const Student& s = student_list[i];
            double computed = ComputeResultBasedOnGPA(s);

            if (computed > 3) {
                intSum += s.Year;
                floatSum += s.Grade;
                rm.AddResultData(s);
            }
        }
    }
//#pragma omp parallel
//    {
//        int threadId = omp_get_thread_num();
//        int startIdx = threadId * chunkSize + std::min(threadId, remainder);
//        int endIdx = (threadId + 1) * chunkSize + std::min(threadId + 1, remainder);
//        int threadIntSum = 0;
//        double threadFloatSum = 0.0;
//        for (int i = startIdx; i < endIdx; i++) {
//            if (i < dataSize) {
//                const Student& s = student_list[i];
//                // Calculate results based on student attributes, you can modify this part as needed
//                // For this example, we are using a dummy function ComputeResultBasedOnGPA
//                double computed = ComputeResultBasedOnGPA(s);
//
//                // Check a condition and add the student to the result monitor
//                if (computed > 3) { // Modify this condition as needed
//                    threadIntSum += s.Year;
//                    threadFloatSum += s.Grade;
//                    rm.AddResultData(s);
//                }
//            }
//        }
//
//#pragma omp critical
//        {
//            intSum += threadIntSum;
//            floatSum += threadFloatSum;
//        }
//    }
    WriteResultsToFile(resultfile, rm, intSum, floatSum);

    return 0;
}
