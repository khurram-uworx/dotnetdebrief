package main

import (
	"errors"
	"fmt"
	"net/http"
	"os"
)

func divide(a, b int) (int, error) {
	if b == 0 {
		return 0, errors.New("cannot divide by zero")
	}
	return a / b, nil
}

func main() {
	result, err := divide(4, 0)

	if err != nil {
		//log.Fatal(err) // os.Exit(-1)
		fmt.Println("Couldnt divide", err)
	} else {
		fmt.Println("Result:", result)
	}

	file, err := os.Open("invalid.file")
	if err != nil {
		fmt.Println("Error opening file:", err)
	} else {
		defer file.Close()
		fmt.Println("File opened successfully")
	}

	response, err := http.Get("https://www.google.com")
	if err != nil {
		fmt.Println("Error making GET request:", err)
	} else {
		defer response.Body.Close()
		fmt.Println("Response status:", response.Status)
	}
}
