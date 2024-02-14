package main

import (
	"bufio"
	"fmt"
	"log"
	"os"
	"sort"
	"strconv"
	"strings"
	"text/tabwriter"
)

type Student struct {
	ID    int
	Grade float64
	Name  string
	Year  int
}

func NewStudent(ID int, Grade float64, Name string, Year int) Student {
	return Student{
		ID:    ID,
		Grade: Grade,
		Name:  Name,
		Year:  Year,
	}
}

func ReadData() []Student {
	filePath := "Students2.txt"
	var students []Student

	file, err := os.Open(filePath)
	if err != nil {
		log.Fatalf("Error opening file: %v", err)
	}
	defer file.Close()

	scanner := bufio.NewScanner(file)
	for scanner.Scan() {
		line := scanner.Text()
		parts := strings.Split(line, ",")

		if len(parts) == 4 {
			id, err := strconv.Atoi(parts[0])
			if err != nil {
				log.Printf("Error parsing ID: %v", err)
				continue
			}

			grade, err := strconv.ParseFloat(parts[1], 64)
			if err != nil {
				log.Printf("Error parsing Grade: %v", err)
				continue
			}

			name := parts[2]
			year, err := strconv.Atoi(parts[3])
			if err != nil {
				log.Printf("Error parsing Year: %v", err)
				continue
			}

			student := NewStudent(id, grade, name, year)
			students = append(students, student)
		}
	}

	if err := scanner.Err(); err != nil {
		log.Printf("Error reading file: %v", err)
	}

	return students
}

func PrintStudentsToFile(students []Student, filePath string) {
	file, err := os.Create(filePath)
	if err != nil {
		log.Fatalf("Error creating file: %v", err)
	}
	defer file.Close()

	w := tabwriter.NewWriter(file, 0, 0, 2, ' ', tabwriter.TabIndent)

	// Check if the students slice is empty after creating the file
	if len(students) == 0 {
		fmt.Fprintln(w, "No students to write. The result file is empty.")
	} else {
		fmt.Fprintln(w, "ID\tGrade\tName\tYear")
		for _, student := range students {
			fmt.Fprintf(w, "%d\t%.2f\t%s\t%d\n", student.ID, student.Grade, student.Name, student.Year)
		}
	}

	w.Flush() // Make sure to flush the writer to write all buffered data to the file

	if len(students) == 0 {
		fmt.Println("The result file is empty.")
	} else {
		fmt.Println("Student details have been written to", filePath)
	}
}

func processItem(student Student) float64 {
	// Assuming the grade is out of 10, and we're mapping this to a GPA out of 4
	var computedGPA float64

	switch {
	case student.Grade >= 9.0:
		computedGPA = 4.0
	case student.Grade >= 8.0:
		computedGPA = 3.5
	case student.Grade >= 7.0:
		computedGPA = 3.0
	default:
		computedGPA = 2.0
	}

	return computedGPA
}

func dataManager(insertCh <-chan Student, removeCh chan<- Student, writeFlag <-chan int) {
	const DATA_SIZE_ARRAY = 10
	var data [DATA_SIZE_ARRAY]Student
	var dataLast = -1
	var noMoreInMain = false //to track whether more Student objects are expected to be received
	var valueAsked = 0       //to store the values received from the writeFlag channel
	var dataFromMain Student //to store the Student object received from the insertCh channel.

	for {
		// if not empty and not full, elements are getting sent from main;
		if dataLast >= 0 && dataLast < DATA_SIZE_ARRAY-1 && !noMoreInMain {
			select {
			case valueAsked = <-writeFlag:
				if valueAsked == 1 {
					//Checks if the buffer is empty and no more data is expected.
					if dataLast == -1 && noMoreInMain {
						removeCh <- completed
					} else {
						removeCh <- data[dataLast]
						fmt.Printf("Data manager: Sent student %s for processing\n", data[dataLast].Name)
						dataLast--
					}
				}
				// in that it is empty; Waits for and receives a Student object from the insertCh channel
			case dataFromMain = <-insertCh:
				if dataFromMain != completed {
					dataLast++
					data[dataLast] = dataFromMain
					fmt.Printf("Data manager: Inserted student %s\n", dataFromMain.Name)
				} else {
					noMoreInMain = true
				}
			}
			// 2nd case, if it's empty
		} else if dataLast == -1 && !noMoreInMain {
			dataFromMain = <-insertCh
			if dataFromMain != completed {
				dataLast++
				data[dataLast] = dataFromMain
				fmt.Printf("Data manager: Inserted student %s\n", dataFromMain.Name)
			} else {
				noMoreInMain = true
				fmt.Println("Data manager: Array is full, cannot insert new items")
			}
			//3rd case when it's full
		} else {
			valueAsked = <-writeFlag
			if valueAsked == 1 {
				if dataLast == -1 && noMoreInMain {
					removeCh <- completed
				} else {
					removeCh <- data[dataLast]
					fmt.Printf("Data manager: Sent student %s for processing\n", data[dataLast].Name)
					dataLast--
				}
			}
		}
	}
}

func workerProcess(removeCh <-chan Student, filteredCh chan<- Student, writeFlag chan<- int) {
	var finished = false
	for !finished {
		writeFlag <- 1
		var received = <-removeCh
		if received == completed { // Use a zero value of Student to signal completion
			finished = true
			fmt.Println("Worker: No more items to process. Shutting down")
		} else {
			computedGPA := processItem(received) // processItem computes GPA
			fmt.Printf("Worker: Processing student %s\n", received.Name)
			if computedGPA > 3.0 { // Filter based on GPA
				filteredCh <- received
			}
		}
	}
	filteredCh <- completed // Send a zero value of Student to signal completion
}

func resultManager(filteredCh <-chan Student, resultToMainChannel chan<- []Student) {
	var results []Student

	var receivedFinishSignals = 0
	for receivedFinishSignals < NUMBER_OF_THREADS {
		var received = <-filteredCh
		if received == completed { // Use a zero value of Student to signal completion
			receivedFinishSignals++
		} else {
			results = append(results, received)
			fmt.Printf("Result manager: Received processed student %s\n", received.Name)
		}
	}

	// Sort the slice of students alphabetically by Name
	sort.Slice(results, func(i, j int) bool {
		return results[i].Name < results[j].Name
	})

	if len(results) > 0 {
		resultToMainChannel <- results
	} else {
		resultToMainChannel <- []Student{} // Use Student slice instead of Student
	}
}

const NUMBER_OF_THREADS = 5

var completed = Student{-1, 0, "END", 0}

func main() {
	// Create channels
	students := ReadData()
	var insertCh = make(chan Student)              //To send Student objects to the dataManager
	var removeCh = make(chan Student)              //For dataManager to send Student objects to workerProcess
	var filteredCh = make(chan Student)            // For workerProcess to send filtered Student objects to resultManager
	var resultToMainChannel = make(chan []Student) //For resultManager to send the final sorted results back to the main function
	var writeFlag = make(chan int)                 //writeFlag: For signaling within the dataManager

	for i := 0; i < NUMBER_OF_THREADS; i++ {
		go workerProcess(removeCh, filteredCh, writeFlag)
	}

	go dataManager(insertCh, removeCh, writeFlag)
	go resultManager(filteredCh, resultToMainChannel)

	// Sends data, Iterates over the students slice and sends each Student object to the dataManager via the insertCh channel
	for _, student := range students {
		insertCh <- student
	}
	// Inform dataManager that there is no more students
	insertCh <- completed

	// Wait for receiving the results
	var result []Student = <-resultToMainChannel

	PrintStudentsToFile(result, "Results.txt")
}
