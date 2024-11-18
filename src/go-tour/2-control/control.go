package main

import (
	"fmt"
	"time"
)

func main() {
	for i := 1; i <= 5; i++ { // why without { } will not work and how C/Java/C#'s () make sense
		if i%2 == 0 { // we saved () but we have to live with {}; Python has same problem and its solves by forcing spaced block and :
			// also not liking tight use of operators; i % 2 but may i%2 makes sense in Go mentality
			fmt.Println(i, "even")
		} else { // we can have else if {}
			fmt.Println(i, "odd")
		}
	}

	// we can use for like how variable is initialized ; condition with if as well
	// if x := 5; x < 5 { }

	fmt.Println("When's Saturday?")
	now := time.Now()
	today := now.Weekday()
	switch { // switch cases are evaluated from top to bottom and stop on first succeeded case
	// knowing this is important because we can have function calls like case isThisValid() or case isThatValid()
	// switch with no condition; as a replacement for many if/elses
	case now.Hour() < 12:
		fmt.Println("Good morning!")
	case now.Hour() < 17:
		fmt.Println("Good afternoon.")
	default:
		fmt.Println("Good evening.")
	}
	switch time.Saturday {
	case today + 0:
		fmt.Println("Today.") // unlike c/java/c# we dont need break; its implicit
	case today + 1:
		fmt.Println("Tomorrow.")
	case today + 2:
		fmt.Println("In two days.")
	default:
		fmt.Println("Too far away.")
	}

	// go doesnt has while {} or do {} while loops; its for loop can be shorten and used instead
	j := 1
	for j <= 3 {
		fmt.Println(j)
		j++ // = j + 1
	}

	//infinite loop
	//for {
	//}

	// we have goto, defer, return, break, continue, fallthrough
	defer fmt.Println("Deferred 1: This will run at the end of main function") // last in first out manner if many defer calls
	defer fmt.Println("Deferred 2: This will run at the end of main function")
	goto Skip // Jump to the label 'Skip'
	fmt.Println("This will be skipped")
Skip:
	fmt.Println("Reached the label 'Skip'")

	day := "Tuesday"
	switch day {
	case "Monday":
		fmt.Println("It's Monday")
	case "Tuesday":
		fmt.Println("It's Tuesday") // we should see this
		fallthrough                 // given break is implicit; if we want to fall through we can
	case "Wednesday":
		fmt.Println("It's Wednesday") // and this too due to fallthrough
	default:
		fmt.Println("Another day")
	}
}
