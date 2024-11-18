package main

import (
	"fmt"

	"golang.org/x/exp/constraints" // go mod init go-tour/data and then go get golang.org/x/exp/constraints
)

type BinaryTree[T constraints.Ordered] struct {
	Value T
	Left  *BinaryTree[T]
	Right *BinaryTree[T]
}

func NewBinaryTree[T constraints.Ordered](value T) *BinaryTree[T] {
	return &BinaryTree[T]{Value: value}
}
func (t *BinaryTree[T]) Insert(value T) { // Insert adds a new value to the binary tree in sorted order
	insert := func(node *BinaryTree[T], v T) *BinaryTree[T] {
		if node == nil {
			return NewBinaryTree(v)
		}
		node.Insert(v)
		return node
	}

	if value < t.Value {
		t.Left = insert(t.Left, value)
	} else {
		t.Right = insert(t.Right, value)
	}
}
func (t *BinaryTree[T]) InOrder() []T { // InOrder returns a slice of the binary tree values in sorted order
	var result []T
	if t.Left != nil {
		result = append(result, t.Left.InOrder()...)
	}
	result = append(result, t.Value)
	if t.Right != nil {
		result = append(result, t.Right.InOrder()...)
	}
	return result
}

func main() {
	var x [5]int
	y := [5]float32{
		98, 93, 77, 82, 83, // unlike javascript/c# who can forget last comma; you really need it; but why?
	}
	x[4] = 100
	y[4] = 1.1
	fmt.Println("x: ", x)
	fmt.Println("y: ", y)

	array := [5]float32{1, 2, 3, 4, 5}
	slice1 := array[1:4]
	slice2 := make([]float32, 5)        // make does allocation + initialization
	slice3 := append(slice1, 10, 11)    // can we append two slices?
	slice4 := append(slice1, slice2...) // looking familiar; this ... is unpacking second slice; javascript spread operator but order is different
	fmt.Println("Slice1: ", slice1)
	fmt.Println("Slice2: ", slice2)
	fmt.Println("Slice3: ", slice3)
	fmt.Println("Slice4: ", slice4)

	// we also have copy for copying data between slices; append and copy dont work with arrays
	arr1 := [3]int{1, 2, 3}
	arr2 := [5]int{}
	copy(arr2[:], append(arr1[:], 4, 5)) // converting array to slice and then feeding it to another slice
	fmt.Println(arr2)                    // and given slice is always connected to underlying thing; we should see 1, 2, 3, 4, 5

	// var m map[string]int // unordered key-value collection; but its uninitialized and will give runtime error
	m := make(map[string]int) // make can be used to allocate + initialize slices, maps, and channels
	m["a"] = 5                // mutating
	m["b"] = 6
	//elem = m[key] // retreiving; elem, ok = m[key] ok will be true if key exists
	//delete(m, key) // deleting

	fmt.Println(m)

	var pow = []int{1, 2, 4, 8, 16, 32, 64, 128}
	for i, v := range pow { // we can just use i and then fetch the value using pow[i]; we can also use discard for _, value := range pow {
		fmt.Printf("2**%d = %d\n", i, v)
	}

	// channels premier
	ch := make(chan int)            // unbuffered channel
	bufferedCh := make(chan int, 5) // buffered channel with capacity 5
	// go-routine
	go func() {
		ch <- 1 // sending 1
		ch <- 2 // will be discarded
		ch <- 3 // will be discarded
	}()
	bufferedCh <- 1 // we dont need goroutine with buffered channel
	bufferedCh <- 2
	message := <-ch
	fmt.Println("Should be 1", message) // should be 1
	message = <-bufferedCh
	fmt.Println("Should be 1", message) // should be 1
	message = <-bufferedCh
	fmt.Println("Should be 2", message) // should be 2

	root := NewBinaryTree(10)
	root.Insert(5)
	root.Insert(15)
	root.Insert(2)
	root.Insert(7)
	fmt.Println("In-order traversal:", root.InOrder()) // Output: [2 5 7 10 15]
}
