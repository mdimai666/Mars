# JoinNode

## Description

A **Join Node** is a node that combines multiple incoming messages into one. It acts as a data aggregator: it waits for messages from different inputs or a certain number of messages, then forms an array from them and passes them on.

---

## When to Use

Use a Join Node when you need to:
- Aggregate data from multiple sources before processing
- Wait for several parallel processes to complete
- Collect a group of messages over a certain period of time
- Receive a fixed number of messages before performing the next step

---

## Operating Modes

The node supports three modes, which are selected in the **Mode** property:

### 1. InputAggregation (wait for all inputs)

Waits until at least one message arrives at **each** input port. After that, all received data is collected into an array and sent to the 'on join' output port.

If a port has not received a message within the specified timeout, the accumulated data will be sent to the 'on InputAggregation timeout' port, and the task will time out.

**Setting:** `InputAggregationTimeoutSeconds` — timeout in seconds (default 15).

### 2. CountAggregation (Aggregation by Count)

Aggregates the specified number of messages, **regardless of which input** they came from. Once the required number of messages has been collected, they are all sent as an array.

**Setting:** `MessageCount` — number of messages to collect (default 5).

### 3. TimeAggregation (Aggregation by Time)

Aggregates all messages received within the specified time interval and sends them as a single array after the interval ends.

**Setting:** `AggregationTimeSeconds` — collection duration in seconds (default: 5).

---

## Important Notes

- In `InputAggregation` mode, each task is tracked separately; different executions are not mixed.
- If a timeout occurs in `InputAggregation` mode, partially collected data is not lost and is sent to a special output port.
- In `CountAggregation` and `TimeAggregation` modes, messages are processed in the order they are received.
