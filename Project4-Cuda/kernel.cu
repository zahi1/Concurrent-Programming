#include <cuda_runtime.h>
#include <iostream>
#include <fstream>
#include <cstring>
#include <vector>
#include <string>
#include <cuda.h>
#include "device_launch_parameters.h"
#include <sstream>

// Define Student structure
struct Student {
    char name[256];
    int year;
    float grade;
};

// Define Result structure
struct Result {
    char data[256];
};

__device__ char gradeFromScore(float score) {
    if (score >= 9) return 'A';
    else if (score >= 8) return 'B';
    else if (score >= 7) return 'C';
    else if (score >= 6) return 'D';
    else return 'F';
}

__global__ void processStudents(Student* students, Result* results, int numStudents, int* resultCounter) {
    int idx = blockIdx.x * blockDim.x + threadIdx.x;
    if (idx < numStudents) {
        Student s = students[idx];
        if (s.name[0] > 'P') {
            // Convert name to uppercase
            for (int i = 0; s.name[i] != '\0'; i++) {
                if (s.name[i] >= 'a' && s.name[i] <= 'z') {
                    s.name[i] = s.name[i] - 'a' + 'A';
                }
            }

            // Prepare result string
            char resultString[256];
            int cursor = 0;
            for (int i = 0; s.name[i] != '\0'; i++) {
                resultString[cursor++] = s.name[i];
            }
            resultString[cursor++] = '-';

            // Convert year to string and append
            int year = s.year;

            resultString[cursor++] = '0' + year;

            // Append grade
            char grade = gradeFromScore(s.grade);
            resultString[cursor++] = grade;
            resultString[cursor] = '\0';  // Null-terminate the string

            // Write to result array using atomic operation
            int pos = atomicAdd(resultCounter, 1);
            for (int i = 0; resultString[i] != '\0'; i++) {
                results[pos].data[i] = resultString[i];
            }
            results[pos].data[cursor] = '\0'; // Null-terminate the result
        }
    }
}


std::vector<Student> readStudentsFromFile(const char* filename) {
    std::vector<Student> students;
    std::ifstream file(filename);
    std::string line;

    while (std::getline(file, line)) {
        Student s;
        sscanf(line.c_str(), "%[^,],%d,%f", s.name, &s.year, &s.grade);
        students.push_back(s);
    }

    file.close();
    return students;
}

int main() {
    // Read students from file
    std::vector<Student> studentVector = readStudentsFromFile("data1.txt");
    int numStudents = studentVector.size();

    // Prepare arrays for CUDA
    Student* h_students = new Student[numStudents];
    Result* h_results = new Result[numStudents];
    int h_resultCounter = 0;

    for (int i = 0; i < numStudents; ++i) {
        h_students[i] = studentVector[i];
    }

    // Allocate memory on GPU
    Student* d_students;
    Result* d_results;
    int* d_resultCounter;
    cudaMalloc(&d_students, numStudents * sizeof(Student));
    cudaMalloc(&d_results, numStudents * sizeof(Result));
    cudaMalloc(&d_resultCounter, sizeof(int));

    // Copy data from host to device
    cudaMemcpy(d_students, h_students, numStudents * sizeof(Student), cudaMemcpyHostToDevice);
    cudaMemcpy(d_resultCounter, &h_resultCounter, sizeof(int), cudaMemcpyHostToDevice);

    // Launch kernel
    int blockSize = 64; // or another appropriate value
    int numBlocks = (numStudents + blockSize - 1) / blockSize;
    processStudents << <numBlocks, blockSize >> > (d_students, d_results, numStudents, d_resultCounter);


    // Copy results back to host
    cudaMemcpy(h_results, d_results, numStudents * sizeof(Result), cudaMemcpyDeviceToHost);
    cudaMemcpy(&h_resultCounter, d_resultCounter, sizeof(int), cudaMemcpyDeviceToHost);

    // Write results to a file
    std::ofstream outFile("result.txt");
    for (int i = 0; i < h_resultCounter; i++) {
        outFile << h_results[i].data << std::endl;
    }
    outFile.close();

    // Free memory
    delete[] h_students;
    delete[] h_results;
    cudaFree(d_students);
    cudaFree(d_results);
    cudaFree(d_resultCounter);

    return 0;
}