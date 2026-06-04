# QueueNode

This node is designed for structured and controlled processing of message flows. It accumulates incoming data and passes it on for further processing in a strictly dosed manner, waiting for the completion of previous operations.

## Key Features

* **Flexible retrieval modes**:
* **FIFO** (First In, First Out) – the first element received is processed (standard queue).
* **LIFO** (Last In, First Out) – the last element received is processed (stack).
* **Load Control**: Limits the maximum number of simultaneously running tasks (`MaxTask`). This protects subsequent nodes or external systems from overload.

## Node Settings

| Parameter | Description | Default Value |
| :--- | :--- | :--- |
| **Mode** | The order in which elements are retrieved: FIFO or LIFO. | FIFO |
| **MaxTask** | The maximum number of elements that can be processed simultaneously. | 1 |

## How to use

1. **Adding data**: Simply send a message to the node's input. It will be automatically added to the queue.
2. **Automatic processing**: The node will automatically send the next element to the output as soon as the number of active tasks falls below the value of `MaxTask`.
3. **Completion**: When all elements have been processed and the queue is empty, the node will signal the end of the cycle.
