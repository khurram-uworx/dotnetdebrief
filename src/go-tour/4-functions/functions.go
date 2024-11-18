package main

import (
	"fmt"
	"math"
)

func swap(x, y string) (string, string) { //how we can "shorten" variable declaration; we can do the same with function parameters
	return y, x
}

func split(sum int) (x, y int) {
	x = sum * 4 / 9
	y = sum - x
	return
}

func compute(fn func(float64, float64) float64) float64 { // function as parameter; not C# Func<T> like cool but they are here
	return fn(3, 4)
}

func main() {
	x, y := split(17)
	fmt.Println(x, y)

	hypotenuse := func(x, y float64) float64 { // local function
		return math.Sqrt(x*x + y*y)
	}
	fmt.Println(hypotenuse(5, 12))
	fmt.Println(compute(hypotenuse))

	toAdd := 5
	add5 := func(x int) int { // closure
		return x + toAdd
	}
	fmt.Println("add5 with 2: ", add5(2))
	toAdd = 6
	fmt.Println("add5 with 2: ", add5(2)) // Go Closure captures the "outside variables" by reference
}
