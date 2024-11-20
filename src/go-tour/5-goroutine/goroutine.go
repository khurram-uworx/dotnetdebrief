package main

import (
	"fmt"
	"time"
)

func printMessage() {
	fmt.Println("Hello from Goroutine!")
	time.Sleep(time.Millisecond * 500)
	fmt.Println("Bye from Goroutine!")
}

func main() {
	go printMessage()
	time.Sleep(time.Second) // Wait for goroutine to complete
	fmt.Println("Hello from Main!")
}
