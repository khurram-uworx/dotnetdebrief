package main

import (
	"fmt"
	"math"
)

type Vertex struct { // we dont have classes :)
	X float64
	Y float64
}
type Book struct {
	Title  string
	Author string
	price  int // notice its camelCased; Go will not "export" it in package; and its only available within the package
}
type Magazine struct {
	Title string
	Issue int
}

func (v Vertex) Abs() float64 {
	return math.Sqrt(v.X*v.X + v.Y*v.Y)
}
func (v *Vertex) Scale(f float64) { // notice the use of pointer as we want to mutate the original structure/type
	v.X = v.X * f
	v.Y = v.Y * f
}
func (b *Book) SetPrice(newPrice int) { // GetAge is exported (public) and this is the extent of encapsulation in Go
	// our checks and validations
	if newPrice > 0 {
		b.price = newPrice
	}
}

type Printable interface { // but we do have interfaces; what how?
	Print()
}

func (b Book) Print() {
	fmt.Printf("Book Title: %s, Author: %s\n", b.Title, b.Author)
}
func (m Magazine) Print() {
	fmt.Printf("Magazine Title: %s, Issue: %d\n", m.Title, m.Issue)
}

type Marker interface{} // sure; everything gets marked? good/bad?

func main() {
	// https://go.dev/tour/methods/9; Structural Typing :)
	var book Printable = Book{"The Go Programming Language", "Alan A. A. Donovan", 25}
	var magazine Printable = Magazine{"Tech Weekly", 42} // interfaces are implemented implicitly; no inheritance/implements
	book.Print()                                         // Calls Book's Print
	magazine.Print()                                     // Calls Magazine's Print

	book2, ok2 := magazine.(Book) // validating underlying type
	if ok2 {                      // bracket on next line doesnt work; removing brackets for one liner blocks also doesnt work
		fmt.Println("magazine though Printable but its not a Book")
	} else { // sadly moving else to next line also doesnt work
		fmt.Println("magazine though Printable is infact a Book", book2)
	}

	//pointers; but we dont have pointer arithmetic
	i := 42
	p := &i         // point to i
	fmt.Println(*p) // read i through the pointer
	*p = 21         // set i through the pointer
	fmt.Println(i)  // see the new value of i

	v := Vertex{3, 4}    // methods on types; sort of extension methods in C#
	v.Scale(10)          // as a convenience v is being passed automatically by reference because of function signature/pointer
	fmt.Println(v.Abs()) // we can alias say type MyFloat float64 and then can have methods on alias

	v1 := Vertex{1, 2}
	v2 := Vertex{X: 1}  // Y:0 is implicit
	v3 := Vertex{}      // X:0 and Y:0
	p2 := &Vertex{1, 2} // has type *Vertex
	v1.X = 4
	p3 := &v1 // we can have pointer to structure
	p3.X = 5  // and instead of doing (*p).X we can simply do p.X; idiomatic Go
	fmt.Println("Vertex, should be 5, 2", v1)
	fmt.Println("Vertex, should be 1, 0", v2)
	fmt.Println("Vertex, should be 0, 0", v3)
	fmt.Println("Vertex, should be 1, 2", *p2)
	fmt.Println("Vertex, should be 5, 2", *p3)
}
