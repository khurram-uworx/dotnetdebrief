package main

// recommended resources
// tour of go; https://go.dev/tour
// introducing go book
// and ofcourse chatgpt

//import "fmt"
import (
	"fmt"
	"math/rand"
	"time"
)

// go version
// go run hello.go
// go build hello.go and you are good to go with that binary; nothing else needed, Sandbox?

// notice type is the last thing
func add(x int, y int) int {
	return x + y
}

func main() {
	// start typing it below and expereince how it auto complete and you dont have to jump to last and punch ;
	// fmt.Println(time.Now())
	fmt.Println(time.Now(), rand.Intn(10))

	const hello string = "Hello World"
	var x int // notice type is the last thing
	// and this continues to the function definition above (add)
	var i, j int = 1, 2
	x = 5
	fmt.Println("add(5, 3): ", add(x, i+j))

	/*
		bool
		string
		int, int8, int16, int32, int64
		uint, uint8, uint16, uint32, uint64, uintptr
		byte is alias for uint8
		rune is alias for int32 represent unicode
		float32, float64
		complex64 complex128
		int, uint, uintptr are 32bit or 64bit depending on systems

		similar to Java/C# they have default values; like false, 0 and "" but it doesnt complain
		unlike c you have to explicitly cast/convert int64(some-int32-variable)
		unlike c there is no pointer arithmetic
	*/

	var sumOfIntegers = 1 + 1 // idiomatic go names; very much like C# PascalCase for exported things and camelCase for others
	var sf1 = 1 + 1.1         // you can have snake_case not liked that much
	sf2 := 1.1 + 1.1          // idiomatic go variable + assignment
	var ascii = "Hello"[1]    // e will be 101
	var secondCharacter = string(ascii)

	fmt.Println(hello, sumOfIntegers, sf1, sf2, secondCharacter) // semicolon is option; succinctness

	var (
		a = 5
		b = 6
		c = 7
	) // these are independent variables; not tuple
	fmt.Println(a, b, c)

	var name string // var is not placeholder of type like C#; its language construct for variable declaration
	fmt.Print("What's your name? ")
	fmt.Scanf("%s", &name) // scanf and printf of C
	fmt.Printf("Hi there %s", name)
}

// godoc:
// go install -v golang.org/x/tools/cmd/godoc@latest
// go doc fmt Println (godoc)
